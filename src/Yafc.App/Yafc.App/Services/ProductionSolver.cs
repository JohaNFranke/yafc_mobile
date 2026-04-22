using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.LinearSolver;
using Yafc.Model.Mobile;

namespace Yafc.App.Services;

public static class ProductionSolver
{
    // Solve: given a GameDatabase and a list of desired outputs, find the minimum recipe rates
    // that satisfy all flows. Returns Infeasible if no valid production chain exists.
    public static ProductionPlan Solve(
        GameDatabase db,
        IReadOnlyList<(string ItemName, float Rate)> goals)
    {
        if (goals.Count == 0) return ProductionPlan.Infeasible;

        Solver? solver;
        try
        {
            solver = Solver.CreateSolver("GLOP");
        }
        catch (Exception ex)
        {
            // Native lib not loaded yet (happens on Android before OrToolsNativeLoader runs)
            return ProductionPlan.Error($"OrTools não disponível: {ex.Message}");
        }

        if (solver is null)
            return ProductionPlan.Error("GLOP solver não pôde ser criado.");

        // ── 1. Index all recipes ──────────────────────────────────────────────
        // Yafc assume que toda a árvore de tecnologia está disponível; recipes
        // com Enabled=false são apenas "requerem research" e ainda são válidos
        // no plano. Filtrar aqui tornava a maioria dos itens INFEASIBLE.
        var recipes = db.Recipes.Values.ToArray();
        if (recipes.Length == 0) return ProductionPlan.Infeasible;

        var vars = new Variable[recipes.Length];
        for (int i = 0; i < recipes.Length; i++)
            vars[i] = solver.MakeNumVar(0.0, double.PositiveInfinity, recipes[i].Name);

        // ── 2. Build net-production coefficients per item ────────────────────
        // key: itemName  value: list of (variable, coefficient)
        var netCoeffs = new Dictionary<string, List<(Variable V, double Coeff)>>(StringComparer.Ordinal);

        for (int i = 0; i < recipes.Length; i++)
        {
            var r = recipes[i];
            var v = vars[i];

            foreach (var prod in r.Products)
                Accumulate(netCoeffs, prod.Name, v, prod.Amount * prod.Probability);
            foreach (var ingr in r.Ingredients)
                Accumulate(netCoeffs, ingr.Name, v, -ingr.Amount);
        }

        // ── 3. Add goal constraints (hard lower bounds on desired outputs) ────
        var goalSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (itemName, rate) in goals)
        {
            goalSet.Add(itemName);
            var ct = solver.MakeConstraint(rate, double.PositiveInfinity, $"goal_{itemName}");
            if (netCoeffs.TryGetValue(itemName, out var list))
                foreach (var (v, c) in list)
                    ct.SetCoefficient(v, c);
        }

        // ── 4. Add non-negative flow for intermediate items ───────────────────
        // Only constrain items that have at least one producer AND one consumer
        // among the enabled recipes. Raw resources (only consumers) are left free.
        foreach (var (itemName, list) in netCoeffs)
        {
            if (goalSet.Contains(itemName)) continue;
            bool hasProducer = list.Any(x => x.Coeff > 0);
            bool hasConsumer = list.Any(x => x.Coeff < 0);
            if (!hasProducer || !hasConsumer) continue;

            var ct = solver.MakeConstraint(0.0, double.PositiveInfinity, $"flow_{itemName}");
            foreach (var (v, c) in list)
                ct.SetCoefficient(v, c);
        }

        // ── 5. Minimize total recipe usage (proxy for factory count) ─────────
        var obj = solver.Objective();
        foreach (var v in vars)
            obj.SetCoefficient(v, 1.0);
        obj.SetMinimization();

        // ── 6. Solve ──────────────────────────────────────────────────────────
        var status = solver.Solve();
        if (status != Solver.ResultStatus.OPTIMAL && status != Solver.ResultStatus.FEASIBLE)
            return ProductionPlan.Infeasible;

        // ── 7. Extract solution ───────────────────────────────────────────────
        var activeRecipes = new List<RecipeSolution>();
        var produced = new Dictionary<string, float>(StringComparer.Ordinal);
        var consumed = new Dictionary<string, float>(StringComparer.Ordinal);

        for (int i = 0; i < recipes.Length; i++)
        {
            float rate = (float)vars[i].SolutionValue();
            if (rate < 1e-7f) continue;

            var r = recipes[i];
            activeRecipes.Add(new RecipeSolution(r.Name, r.Category, rate, r.EnergyRequired));

            foreach (var prod in r.Products)
                produced[prod.Name] = produced.GetValueOrDefault(prod.Name) + rate * prod.Amount * prod.Probability;
            foreach (var ingr in r.Ingredients)
                consumed[ingr.Name] = consumed.GetValueOrDefault(ingr.Name) + rate * ingr.Amount;
        }

        // Net inputs = items consumed more than produced (raw resources)
        var inputs = new List<ResourceFlow>();
        foreach (var (item, cons) in consumed)
        {
            float net = cons - produced.GetValueOrDefault(item);
            if (net > 1e-7f)
                inputs.Add(new ResourceFlow(item, GetType(db, item), net));
        }

        // Net outputs = items produced more than consumed (desired + byproducts)
        var outputs = new List<ResourceFlow>();
        foreach (var (item, prod) in produced)
        {
            float net = prod - consumed.GetValueOrDefault(item);
            if (net > 1e-7f)
                outputs.Add(new ResourceFlow(item, GetType(db, item), net));
        }

        inputs.Sort((a, b) => string.CompareOrdinal(a.ItemName, b.ItemName));
        outputs.Sort((a, b) => string.CompareOrdinal(a.ItemName, b.ItemName));
        activeRecipes.Sort((a, b) => string.CompareOrdinal(a.Category, b.Category));

        var goalFlows = new List<ResourceFlow>(goals.Count);
        foreach (var (itemName, rate) in goals)
            goalFlows.Add(new ResourceFlow(itemName, GetType(db, itemName), rate));

        return new ProductionPlan
        {
            Feasible = true,
            ActiveRecipes = activeRecipes,
            Inputs = inputs,
            Outputs = outputs,
            Goals = goalFlows,
        };
    }

    private static void Accumulate(
        Dictionary<string, List<(Variable V, double Coeff)>> dict,
        string key, Variable v, double coeff)
    {
        if (!dict.TryGetValue(key, out var list))
            dict[key] = list = new List<(Variable, double)>();
        list.Add((v, coeff));
    }

    private static GameGoodsType GetType(GameDatabase db, string itemName) =>
        db.Items.TryGetValue(itemName, out var item) ? item.Type : GameGoodsType.Item;
}
