using System.Collections.Generic;
using System.Linq;
using Yafc.Parser;

namespace Yafc.App.Services;

/// <summary>
/// Immutable snapshot of data.raw contents, safe to read after the LuaContext is disposed.
/// All relevant numbers and keys are copied out of the Lua tables eagerly in BuildSnapshot.
/// </summary>
public sealed record DataRawSnapshot(
    int TotalTypes,
    int TotalPrototypes,
    IReadOnlyList<(string Type, int Count)> CountsByType);

internal static class DataRawInspector
{
    /// <summary>
    /// Walks data.raw once and extracts prototype counts per type.
    /// The LuaTable references passed in stop being valid once the LuaContext is disposed,
    /// so this method must run before that.
    /// </summary>
    public static DataRawSnapshot BuildSnapshot(LuaTable data)
    {
        // data.raw is the dictionary type -> { protoName -> protoTable }
        var raw = data["raw"] as LuaTable;
        if (raw is null)
        {
            return new DataRawSnapshot(0, 0, []);
        }

        var counts = new List<(string Type, int Count)>();
        int total = 0;
        foreach (var (key, value) in raw.ObjectElements)
        {
            if (key is not string typeName) continue;
            if (value is not LuaTable bucket) continue;

            int n = 0;
            foreach (var _ in bucket.ObjectElements) n++;
            counts.Add((typeName, n));
            total += n;
        }

        // Alphabetical by type name, stable and easy to diff
        counts.Sort((a, b) => string.CompareOrdinal(a.Type, b.Type));

        return new DataRawSnapshot(counts.Count, total, counts);
    }

    /// <summary>
    /// Formats a snapshot for display: header with totals, top 10 by count,
    /// then the full alphabetical list.
    /// </summary>
    public static string FormatSnapshot(DataRawSnapshot snapshot)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"data.raw: {snapshot.TotalTypes} types, {snapshot.TotalPrototypes} prototypes");
        sb.AppendLine();

        if (snapshot.TotalTypes == 0)
        {
            sb.AppendLine("(empty)");
            return sb.ToString();
        }

        sb.AppendLine("Top 10 types by count:");
        foreach (var (type, count) in snapshot.CountsByType
                     .OrderByDescending(x => x.Count)
                     .Take(10))
        {
            sb.AppendLine($"  {count,6}  {type}");
        }

        sb.AppendLine();
        sb.AppendLine($"All {snapshot.TotalTypes} types (alphabetical):");
        foreach (var (type, count) in snapshot.CountsByType)
        {
            sb.AppendLine($"  {count,6}  {type}");
        }

        return sb.ToString();
    }
}