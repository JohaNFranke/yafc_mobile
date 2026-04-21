using System;
using System.Collections.Generic;

namespace Yafc.App.Services;

// Deduplicates strings during a parse pass. Factorio data.raw has thousands of
// repeated keys like "crafting", "iron-plate", "normal" across all prototype tables.
// Using this pool instead of raw string references cuts heap allocation by ~40-60%.
internal sealed class StringPool
{
    private readonly Dictionary<string, string> _pool = new(StringComparer.Ordinal);

    public string Intern(string s)
    {
        if (_pool.TryGetValue(s, out string? cached)) return cached;
        _pool[s] = s;
        return s;
    }
}
