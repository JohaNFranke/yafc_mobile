using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Yafc.App.Services;

/// <summary>
/// Mirror of Factorio mod info.json.
/// See: https://wiki.factorio.com/Tutorial:Mod_structure#info.json
/// </summary>
public sealed class ModInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("factorio_version")]
    public string? FactorioVersion { get; set; }

    /// <summary>
    /// Strings like "base", "? optional-mod", "! incompatible", "(?) hidden",
    /// "~ no-load-order", or "base >= 1.1.0".
    /// </summary>
    [JsonPropertyName("dependencies")]
    public List<string>? Dependencies { get; set; }
}

public sealed class FactorioMod
{
    public ModInfo Info { get; set; } = new();

    /// <summary>Path to the mod's root folder (contains info.json, data.lua, etc).</summary>
    public string FolderPath { get; set; } = "";

    /// <summary>True for built-in mods from data/ (core, base, ...), false for user mods.</summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>Whether the user has this mod enabled (from mod-list.json).</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>Mirror of mod-list.json from the user's mods folder.</summary>
public sealed class ModList
{
    [JsonPropertyName("mods")]
    public List<ModListEntry> Mods { get; set; } = new();
}

public sealed class ModListEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}