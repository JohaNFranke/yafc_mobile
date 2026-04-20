using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Yafc.App.Services;

public static class ModDiscovery
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Scans the cache folder for mods. Looks in data/ (built-in mods like core, base)
    /// and mods/ (user mods). Returns a list with enabled status applied.
    /// </summary>
    public static List<FactorioMod> Discover(string cacheRoot)
    {
        var result = new List<FactorioMod>();
        var dataPath = Path.Combine(cacheRoot, "data");
        var modsPath = Path.Combine(cacheRoot, "mods");

        if (Directory.Exists(dataPath))
            ScanFolder(dataPath, isBuiltIn: true, result);

        if (Directory.Exists(modsPath))
        {
            ScanFolder(modsPath, isBuiltIn: false, result);
            ApplyModList(modsPath, result);
        }

        AppLog.Write($"ModDiscovery: found {result.Count} mods total " +
                     $"({result.Count(m => m.IsBuiltIn)} built-in, " +
                     $"{result.Count(m => !m.IsBuiltIn)} user, " +
                     $"{result.Count(m => m.Enabled)} enabled)");

        return result;
    }

    private static void ScanFolder(string folder, bool isBuiltIn, List<FactorioMod> result)
    {
        foreach (var dir in Directory.EnumerateDirectories(folder))
        {
            // O info.json pode estar direto na pasta, ou numa subpasta
            // quando o zip foi extraido e tinha pasta raiz interna
            var actualDir = ResolveModDir(dir);
            if (actualDir is null)
            {
                AppLog.Write($"Skipping {dir}: no info.json found");
                continue;
            }

            var infoPath = Path.Combine(actualDir, "info.json");

            try
            {
                var json = File.ReadAllText(infoPath);
                var info = JsonSerializer.Deserialize<ModInfo>(json, JsonOptions);
                if (info is null || string.IsNullOrWhiteSpace(info.Name))
                {
                    AppLog.Write($"Skipping {actualDir}: invalid info.json");
                    continue;
                }

                result.Add(new FactorioMod
                {
                    Info = info,
                    FolderPath = actualDir,
                    IsBuiltIn = isBuiltIn,
                    Enabled = true,
                });
            }
            catch (Exception ex)
            {
                AppLog.Write($"Error reading {infoPath}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Returns the folder that actually contains info.json.
    /// Handles the case of zip-extracted mods that have a redundant inner folder.
    /// </summary>
    private static string? ResolveModDir(string dir)
    {
        if (File.Exists(Path.Combine(dir, "info.json")))
            return dir;

        // Some zips extract with an inner folder of the same name
        foreach (var inner in Directory.EnumerateDirectories(dir))
        {
            if (File.Exists(Path.Combine(inner, "info.json")))
                return inner;
        }

        return null;
    }

    private static void ApplyModList(string modsPath, List<FactorioMod> mods)
    {
        var modListPath = Path.Combine(modsPath, "mod-list.json");
        if (!File.Exists(modListPath))
        {
            AppLog.Write("No mod-list.json found, all mods enabled by default");
            return;
        }

        try
        {
            var json = File.ReadAllText(modListPath);
            var list = JsonSerializer.Deserialize<ModList>(json, JsonOptions);
            if (list is null) return;

            foreach (var entry in list.Mods)
            {
                var mod = mods.FirstOrDefault(m => m.Info.Name == entry.Name);
                if (mod is not null)
                    mod.Enabled = entry.Enabled;
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"Error reading mod-list.json: {ex.Message}");
        }
    }
}