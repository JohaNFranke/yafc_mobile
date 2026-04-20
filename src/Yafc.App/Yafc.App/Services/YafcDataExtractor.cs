using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Yafc.App.Services;

/// <summary>
/// Extracts the Lua auxiliary files (Sandbox.lua, Defines*.lua, Postprocess*.lua,
/// Serpent.lua, Mod-fixes/*.lua) from embedded resources into a directory on disk.
/// The Yafc-CE parser reads these via File.ReadAllBytes("Data/..."), so we mirror
/// that layout in the app's cache folder and set working directory to it before parsing.
/// </summary>
public static class YafcDataExtractor
{
    /// <summary>
    /// Extracts embedded Lua resources into <paramref name="targetRoot"/>/Data/...
    /// and returns the targetRoot path. Idempotent (overwrites existing files).
    /// </summary>
    public static string ExtractTo(string targetRoot)
    {
        var dataDir = Path.Combine(targetRoot, "Data");
        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(Path.Combine(dataDir, "Mod-fixes"));

        var asm = typeof(YafcDataExtractor).Assembly;
        // Resource names look like: Yafc.App.YafcLuaData.Sandbox.lua
        //                           Yafc.App.YafcLuaData.Mod_fixes.Krastorio2.data_updates.lua
        // (dots in paths/filenames get mangled by the compiler; we restore them)
        const string prefix = "Yafc.App.YafcLuaData.";

        int count = 0;
        foreach (var resName in asm.GetManifestResourceNames())
        {
            if (!resName.StartsWith(prefix, StringComparison.Ordinal)) continue;

            // Example: "Yafc.App.YafcLuaData.Mod_fixes.Krastorio2.data_updates.lua"
            // Sub: "Mod_fixes.Krastorio2.data_updates.lua"
            string sub = resName.Substring(prefix.Length);

            // Split by the LAST two dots: "<path-with-dots>.<ext>"
            // sub ends with ".lua" so strip and split the remainder as path components.
            if (!sub.EndsWith(".lua", StringComparison.Ordinal)) continue;
            string noExt = sub[..^4];

            // Path components are separated by dots in the resource name, but filenames
            // can also contain dots. We know the embedded structure is exactly:
            //   <filename>.lua             (top-level: 1 component)
            //   Mod-fixes/<filename>.lua   (2 components: the directory + filename)
            // The directory "Mod-fixes" becomes "Mod_fixes" in the resource name because
            // the C# compiler converts invalid identifier chars ('-') to '_'.
            string relativePath;
            if (noExt.StartsWith("Mod_fixes.", StringComparison.Ordinal))
            {
                // Filename is everything after "Mod_fixes." but that filename may itself
                // contain dots (e.g. "Krastorio2.data-updates" -> "Krastorio2.data_updates").
                // Just keep the filename as-is (dots and all), then replace _ with - to
                // match the disk layout used in mod-fix names.
                string filename = noExt.Substring("Mod_fixes.".Length) + ".lua";
                // Restore any dashes that were mangled to underscores
                filename = RestoreDashes(filename);
                relativePath = Path.Combine("Mod-fixes", filename);
            }
            else
            {
                // Top-level file: name may already be valid (Sandbox, Defines2, Serpent, etc).
                // Defines1.1.lua comes as "Defines1.1.lua" (valid), so noExt = "Defines1.1"
                relativePath = noExt + ".lua";
            }

            string outputPath = Path.Combine(dataDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using (var src = asm.GetManifestResourceStream(resName)!)
            using (var dst = File.Create(outputPath))
            {
                src.CopyTo(dst);
            }
            count++;
        }

        AppLog.Write($"YafcDataExtractor: extracted {count} files to {dataDir}");
        return targetRoot;
    }

    // Heuristic: for mod-fix filenames, the original disk names used '-' which the
    // resource naming converts to '_'. Known files from Yafc-CE master:
    //   EvenMoreTextPlates.globals.lua
    //   Krastorio2.data-updates.lua     -> resource: "Krastorio2.data_updates.lua"
    //   pyhardmode.prototypes.mining.lua
    //   textplates.textplates.lua
    //   Ultracube.data-updates.lua      -> resource: "Ultracube.data_updates.lua"
    // We only need to flip "_updates" back to "-updates" as a minimal fix.
    // If more mod-fixes arrive with different separators, extend this.
    private static string RestoreDashes(string filename)
    {
        return filename.Replace("data_updates.lua", "data-updates.lua", StringComparison.Ordinal);
    }
}