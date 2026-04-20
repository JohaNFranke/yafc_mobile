using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Yafc.App.Services;

public record CopyProgress(string CurrentFile, int FilesDone, int TotalKnown);

public static class FactorioDataCopier
{
    /// <summary>
    /// Copies (and extracts zips) from a user-picked SAF folder into internal cache.
    /// Returns the cache folder path where files were materialized.
    /// </summary>
    public static async Task<string> CopyToCacheAsync(
        IStorageFolder source,
        string cacheSubfolder,
        IProgress<CopyProgress>? progress = null)
    {
        var cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YafcApp", "factorio-cache", cacheSubfolder);

        if (Directory.Exists(cacheRoot))
            Directory.Delete(cacheRoot, recursive: true);
        Directory.CreateDirectory(cacheRoot);

        // Validacao: a pasta raiz precisa ter data/ e/ou mods/
        bool hasData = false, hasMods = false;
        await foreach (var item in source.GetItemsAsync())
        {
            if (item is IStorageFolder sub)
            {
                var name = sub.Name.ToLowerInvariant();
                if (name == "data") hasData = true;
                else if (name == "mods") hasMods = true;
            }
        }

        if (!hasData && !hasMods)
        {
            throw new InvalidOperationException(
                "Pasta invalida. A pasta raiz deve conter 'data' e/ou 'mods'.");
        }

        AppLog.Write($"Estrutura detectada: data={hasData}, mods={hasMods}");

        int done = 0;
        await CopyFolderRecursive(source, cacheRoot, progress, () => ++done);

        AppLog.Write($"Factorio cache copied to: {cacheRoot}");
        return cacheRoot;
    }

    private static async Task CopyFolderRecursive(
        IStorageFolder source,
        string destPath,
        IProgress<CopyProgress>? progress,
        Func<int> incrementDone)
    {
        // Pular pastas irrelevantes pro parser do Yafc
        string folderName = source.Name.ToLowerInvariant();
        
        if (folderName is "saves" or "temp" or "archive" or "scenarios" or "blueprints"
            or "script-output" or "bin" or "doc-html" or "config"
            or "achievements" or "profile-stats")
        {
            AppLog.Write($"Pulando pasta irrelevante: {source.Name}");
            return;
        }

        Directory.CreateDirectory(destPath);

        await foreach (var item in source.GetItemsAsync())
        {
            if (item is IStorageFolder subFolder)
            {
                var subDest = Path.Combine(destPath, subFolder.Name);
                AppLog.Write($"Entrando pasta: {subFolder.Name}");
                await CopyFolderRecursive(subFolder, subDest, progress, incrementDone);
            }
            else if (item is IStorageFile file)
            {
                var destFile = Path.Combine(destPath, file.Name);

                // Skip arquivos irrelevantes
                string fn = file.Name.ToLowerInvariant();

                // mod-settings.dat eh essencial para o parser, preservar mesmo sendo .dat
                bool isModSettings = fn == "mod-settings.dat";

                if (!isModSettings && (fn.EndsWith(".log") || fn.EndsWith(".dat") || fn.EndsWith(".vdf") ||
                    fn is "player-data.json" or "server-adminlist.json" or "config.ini"))
                {
                    AppLog.Write($"Pulando irrelevante: {file.Name}");
                    continue;
                }

                AppLog.Write($"Copiando: {destFile} (len path: {destFile.Length})");

                try
                {
                    await CopyFile(file, destFile);
                }
                catch (System.Exception ex)
                {
                    AppLog.Write($"ERRO copiando {file.Name}: {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                if (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var extractDir = Path.Combine(destPath,
                            Path.GetFileNameWithoutExtension(file.Name));
                        ZipFile.ExtractToDirectory(destFile, extractDir, overwriteFiles: true);
                        File.Delete(destFile);

                        // Achata aninhamento duplo: zips de Factorio costumam extrair
                        // como <name>/<name>/... mas o parser espera <name>/...
                        FlattenRedundantNesting(extractDir);
                    }
                    catch (Exception ex)
                    {
                        AppLog.Write($"Falha extraindo {file.Name}: {ex.Message}");
                    }
                }

                int n = incrementDone();
                progress?.Report(new CopyProgress(file.Name, n, -1));
            }
        }
    }

    private static async Task CopyFile(IStorageFile source, string destPath)
    {
        using var src = await source.OpenReadAsync();
        using var dst = File.Create(destPath);
        await src.CopyToAsync(dst);
    }

    private static void FlattenRedundantNesting(string dir)
    {
        // Se dir contem exatamente uma subpasta e nenhum arquivo no nivel raiz,
        // move o conteudo da subpasta para dir e deleta a subpasta.
        try
        {
            var files = Directory.GetFiles(dir);
            var subdirs = Directory.GetDirectories(dir);

            if (files.Length == 0 && subdirs.Length == 1)
            {
                var inner = subdirs[0];
                // So achata se o inner nao for um diretorio regular do mod (graphics, locale, etc).
                // Heuristica: se o inner TEM info.json, entao ele deveria estar no nivel pai.
                if (File.Exists(Path.Combine(inner, "info.json")))
                {
                    AppLog.Write($"Flatten: {inner} -> {dir}");

                    // Move tudo do inner para dir
                    foreach (var f in Directory.GetFiles(inner))
                    {
                        var dest = Path.Combine(dir, Path.GetFileName(f));
                        File.Move(f, dest);
                    }
                    foreach (var d in Directory.GetDirectories(inner))
                    {
                        var dest = Path.Combine(dir, Path.GetFileName(d));
                        Directory.Move(d, dest);
                    }
                    Directory.Delete(inner, recursive: false);
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"FlattenRedundantNesting falhou em {dir}: {ex.Message}");
        }
    }

}