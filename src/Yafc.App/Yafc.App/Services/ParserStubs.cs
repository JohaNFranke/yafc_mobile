// Minimal stubs that satisfy the Yafc-CE parser's external dependencies.
// Real implementations of these types would come from Yafc.Model / Yafc.UI / Yafc.I18n
// but we only need enough to get LuaContext + FactorioDataSource compiling.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Yafc.UI
{
    // Used by LuaTable to implement text interpolation.
    public interface ILocalizable
    {
        bool Get([NotNullWhen(true)] out string? key, out object[] parameters);
    }
}

namespace Yafc.Parser
{

    // Yafc.Model.Project: real Project contains the parsed game data. Stub for now;
    // will be filled in as we port the deserializer. Holds currentYafcVersion as in original.
    public sealed class Project
    {
        public static readonly Version currentYafcVersion = new Version(2, 15, 0, 0);
    }

    // Stand-in for Yafc.Model.ErrorCollector.
    public sealed class ErrorCollector
    {
        public enum Severity { None, MinorDataLoss, MajorDataLoss, Critical }

        private readonly System.Collections.Generic.List<(string message, Severity severity)> _errors = new();

        public void Error(string message, Severity severity)
        {
            _errors.Add((message, severity));
            Yafc.App.Services.AppLog.Write($"[ErrorCollector:{severity}] {message}");
        }

        public System.Collections.Generic.IReadOnlyList<(string message, Severity severity)> All => _errors;
        public bool HasErrors(Severity minimum) => _errors.Exists(e => e.severity >= minimum);
    }

    // FactorioLocalization stub: parses locale .cfg files. No-op for now.
    internal static class FactorioLocalization
    {
        public static void Parse(System.IO.Stream stream) { /* noop for now */ }
    }

    // Yafc.Model.DataUtils stub: parser writes config paths to these statics.
    // Deserializer (not ported) reads them later. Stored but unused for now.
    internal static class DataUtils
    {
        public static string dataPath = "";
        public static string modsPath = "";
        public static bool expensiveRecipes;
        public static bool netProduction;
    }

    // Placeholder for the real deserializer (Yafc.Parser/Data/FactorioDataDeserializer.cs).
    // The parser checks a version constant and constructs/invokes the deserializer to build a Project.
    // Stubbed to satisfy the compiler; full port comes after the Lua side is proven working.
    internal sealed class FactorioDataDeserializer
    {
        public static readonly Version v2_0 = new Version(2, 0);

        public FactorioDataDeserializer(Version gameVersion) { }

        public Project? LoadData(
            string projectPath,
            LuaTable data,
            LuaTable prototypes,
            bool netProduction,
            System.IProgress<(string, string)> progress,
            ErrorCollector errorCollector,
            bool renderIcons,
            bool useLatestSave)
        {
            Yafc.App.Services.AppLog.Write("FactorioDataDeserializer stub: LoadData called but deserializer not ported yet");
            return null;
        }
    }

}

namespace Serilog
{
    public interface ILogger
    {
        void Information(string template, params object?[] args);
        void Information(Exception exception, string template, params object?[] args);
        void Error(string template, params object?[] args);
        void Error(Exception exception, string template, params object?[] args);
        void Warning(string template, params object?[] args);
        void Debug(string template, params object?[] args);
    }

    internal sealed class AppLogLogger : ILogger
    {
        private readonly string _name;
        public AppLogLogger(string name) { _name = name; }

        public void Information(string template, params object?[] args)
            => Yafc.App.Services.AppLog.Write($"[INF {_name}] {Format(template, args)}");
        public void Information(Exception exception, string template, params object?[] args)
            => Yafc.App.Services.AppLog.Write($"[INF {_name}] {Format(template, args)} :: {exception}");
        public void Error(string template, params object?[] args)
            => Yafc.App.Services.AppLog.Write($"[ERR {_name}] {Format(template, args)}");
        public void Error(Exception exception, string template, params object?[] args)
            => Yafc.App.Services.AppLog.Write($"[ERR {_name}] {Format(template, args)} :: {exception}");
        public void Warning(string template, params object?[] args)
            => Yafc.App.Services.AppLog.Write($"[WRN {_name}] {Format(template, args)}");
        public void Debug(string template, params object?[] args)
            => Yafc.App.Services.AppLog.Write($"[DBG {_name}] {Format(template, args)}");

        private static string Format(string template, object?[] args)
        {
            if (args is null || args.Length == 0) return template;
            var sb = new System.Text.StringBuilder();
            int argIdx = 0;
            int i = 0;
            while (i < template.Length)
            {
                if (template[i] == '{')
                {
                    int close = template.IndexOf('}', i);
                    if (close > 0)
                    {
                        if (argIdx < args.Length)
                            sb.Append(args[argIdx++]?.ToString() ?? "null");
                        i = close + 1;
                        continue;
                    }
                }
                sb.Append(template[i++]);
            }
            for (; argIdx < args.Length; argIdx++)
                sb.Append(' ').Append(args[argIdx]?.ToString() ?? "null");
            return sb.ToString();
        }
    }

    public static class Logging
    {
        public static ILogger GetLogger<T>() => new AppLogLogger(typeof(T).Name);
        public static ILogger GetLogger(Type type) => new AppLogLogger(type.Name);
    }
}

namespace Yafc.I18n
{
    public static class LSs
    {
        // LuaContext strings
        public const string ProgressExecutingModAtDataStage = "Executing {0} at data stage";

        // FactorioDataSource strings
        public const string ProgressInitializing = "Initializing";
        public const string ProgressLoadingModList = "Loading mod list";
        public const string ProgressCreatingLuaContext = "Creating Lua context";
        public const string ProgressCompleted = "Completed";
        public const string CouldNotReadModList = "Could not read mod-list.json at {0}";
        public const string CouldNotReadFactorioInfoJson = "Could not read Factorio info.json";
        public const string UnsupportedFactorioVersion = "Unsupported Factorio version {0}";
        public const string ModNotFoundTryInFactorio = "Mod not found: {0}. Try reinstalling it via Factorio.";
        public const string CircularModDependencies = "Circular mod dependencies among: {0}";
        public const string ListSeparator = ", ";
    }

    public static class LSsExtensions
    {
        public static string L(this string template, params object?[] args)
        {
            if (args is null || args.Length == 0) return template;
            try { return string.Format(template, args); }
            catch { return template; }
        }
    }
}

namespace Yafc.Parser
{
    // Extension helpers from Yafc.Model/DataUtils.cs - only the ones LuaContext needs.
    internal static class LuaTableExtensions
    {
        // Get with 'out' (returns bool success) - used by LuaTable ILocalizable impl
        public static bool Get<T>(this LuaTable table, int index, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? value)
        {
            object? raw = table[index];
            if (raw is T typed) { value = typed; return true; }
            value = default;
            return false;
        }

        public static bool Get<T>(this LuaTable table, string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? value)
        {
            object? raw = table[key];
            if (raw is T typed) { value = typed; return true; }
            value = default;
            return false;
        }

        // Get with default - used by MathExpression for noise variable lookup
        public static T Get<T>(this LuaTable? table, string key, T defaultValue)
        {
            if (table is null) return defaultValue;
            object? raw = table[key];
            if (raw is T typed) return typed;
            // numeric coercion: Lua numbers come as double, but caller may want float
            if (raw is IConvertible conv)
            {
                try { return (T)Convert.ChangeType(conv, typeof(T), System.Globalization.CultureInfo.InvariantCulture); }
                catch { }
            }
            return defaultValue;
        }
    }
}