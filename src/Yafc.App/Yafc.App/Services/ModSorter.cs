using System;
using System.Collections.Generic;
using System.Linq;

namespace Yafc.App.Services;

public enum DependencyKind
{
    Required,
    Optional,
    HiddenOptional,
    NoLoadOrder,
    Incompatible,
}

public record ModDependency(DependencyKind Kind, string TargetMod);

public static class ModSorter
{
    /// <summary>
    /// Parses a single dependency string like "? flib >= 0.5.0" into a structured entry.
    /// Returns null if the string is malformed.
    /// </summary>
    public static ModDependency? ParseDependency(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();

        DependencyKind kind = DependencyKind.Required;
        if (s.StartsWith("(?)")) { kind = DependencyKind.HiddenOptional; s = s.Substring(3).Trim(); }
        else if (s.StartsWith("?")) { kind = DependencyKind.Optional; s = s.Substring(1).Trim(); }
        else if (s.StartsWith("~")) { kind = DependencyKind.NoLoadOrder; s = s.Substring(1).Trim(); }
        else if (s.StartsWith("!")) { kind = DependencyKind.Incompatible; s = s.Substring(1).Trim(); }

        // Strip version part like ">= 1.1.0" or "= 2.0"
        var idx = s.IndexOfAny(new[] { '>', '<', '=' });
        if (idx > 0) s = s.Substring(0, idx).Trim();

        if (string.IsNullOrEmpty(s)) return null;
        return new ModDependency(kind, s);
    }

    /// <summary>
    /// Returns enabled mods in load order.
    /// Throws if a required dependency is missing or a cycle is detected.
    /// </summary>
    public static List<FactorioMod> Sort(IEnumerable<FactorioMod> allMods)
    {
        var byName = allMods.Where(m => m.Enabled)
                            .ToDictionary(m => m.Info.Name, m => m);

        // Validate required dependencies exist
        foreach (var mod in byName.Values)
        {
            var deps = ParseDeps(mod);
            foreach (var dep in deps.Where(d => d.Kind == DependencyKind.Required))
            {
                if (!byName.ContainsKey(dep.TargetMod))
                {
                    throw new InvalidOperationException(
                        $"Mod '{mod.Info.Name}' requer '{dep.TargetMod}' que nao foi encontrado");
                }
            }
        }

        // Kahn's algorithm (topological sort) with priority for core > base > alphabetical
        var result = new List<FactorioMod>();
        var inDegree = byName.ToDictionary(kv => kv.Key, kv => 0);
        var edges = new Dictionary<string, List<string>>();
        foreach (var name in byName.Keys) edges[name] = new();

        foreach (var mod in byName.Values)
        {
            var deps = ParseDeps(mod);
            foreach (var dep in deps)
            {
                // Only Required and Optional create load-order edges
                if (dep.Kind is not (DependencyKind.Required or DependencyKind.Optional or DependencyKind.HiddenOptional))
                    continue;
                if (!byName.ContainsKey(dep.TargetMod)) continue;
                // dep.TargetMod -> mod.Info.Name (dep must load before mod)
                edges[dep.TargetMod].Add(mod.Info.Name);
                inDegree[mod.Info.Name]++;
            }
        }

        // Queue sorted: core > base > alphabetical
        var ready = new SortedSet<string>(Comparer<string>.Create(CompareLoadPriority));
        foreach (var kv in inDegree)
            if (kv.Value == 0) ready.Add(kv.Key);

        while (ready.Count > 0)
        {
            var name = ready.Min!;
            ready.Remove(name);
            result.Add(byName[name]);

            foreach (var next in edges[name])
            {
                if (--inDegree[next] == 0) ready.Add(next);
            }
        }

        if (result.Count != byName.Count)
        {
            var missing = byName.Keys.Except(result.Select(m => m.Info.Name));
            throw new InvalidOperationException(
                $"Ciclo de dependencias detectado entre: {string.Join(", ", missing)}");
        }

        return result;
    }

    private static List<ModDependency> ParseDeps(FactorioMod mod)
    {
        var list = new List<ModDependency>();
        if (mod.Info.Dependencies is null)
        {
            // If no dependencies declared, default is to require "base"
            // except for core/base themselves
            if (mod.Info.Name != "core" && mod.Info.Name != "base")
                list.Add(new ModDependency(DependencyKind.Required, "base"));
            return list;
        }
        foreach (var raw in mod.Info.Dependencies)
        {
            var dep = ParseDependency(raw);
            if (dep is not null) list.Add(dep);
        }
        return list;
    }

    private static int CompareLoadPriority(string a, string b)
    {
        int PrioOf(string name) => name switch
        {
            "core" => 0,
            "base" => 1,
            _ => 2
        };
        var cmp = PrioOf(a).CompareTo(PrioOf(b));
        if (cmp != 0) return cmp;
        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }
}