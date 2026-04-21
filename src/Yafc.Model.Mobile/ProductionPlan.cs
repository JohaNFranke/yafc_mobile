using System.Collections.Generic;

namespace Yafc.Model.Mobile;

public readonly record struct RecipeSolution(string RecipeName, string Category, float Rate);
public readonly record struct ResourceFlow(string ItemName, GameGoodsType Type, float Rate);

public sealed class ProductionPlan
{
    public required bool Feasible { get; init; }
    public required IReadOnlyList<RecipeSolution> ActiveRecipes { get; init; }
    public required IReadOnlyList<ResourceFlow> Inputs { get; init; }
    public required IReadOnlyList<ResourceFlow> Outputs { get; init; }
    public IReadOnlyList<ResourceFlow> Goals { get; init; } = [];
    public string? ErrorMessage { get; init; }

    public static readonly ProductionPlan Infeasible = new()
    {
        Feasible = false,
        ActiveRecipes = [],
        Inputs = [],
        Outputs = [],
    };

    public static ProductionPlan Error(string message) => new()
    {
        Feasible = false,
        ActiveRecipes = [],
        Inputs = [],
        Outputs = [],
        ErrorMessage = message,
    };
}
