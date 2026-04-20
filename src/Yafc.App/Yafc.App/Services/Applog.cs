using System;
using System.IO;

namespace Yafc.App.Services;

public static class AppLog
{
    public static string LogDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YafcApp");

    public static string LogFilePath => Path.Combine(LogDirectory, "yafc-app.log");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(LogFilePath, line);
        }
        catch
        {
            // ignore: never crash the app because of logging
        }
    }

    public static void WriteException(string context, Exception ex)
    {
        Write($"--- {context} ---");
        Write(ex.ToString());
    }

    public static string ReadAll()
    {
        try
        {
            return File.Exists(LogFilePath) ? File.ReadAllText(LogFilePath) : "(vazio)";
        }
        catch (Exception ex)
        {
            return $"Erro lendo log: {ex.Message}";
        }
    }

    public static void Clear()
    {
        try { if (File.Exists(LogFilePath)) File.Delete(LogFilePath); } catch { }
    }
}