using System;
using System.Runtime.InteropServices;
using System.Linq;

namespace Yafc.App.Services;

/// <summary>
/// Minimal test: loads lua52 natively (P/Invoke) and runs hello world.
/// Adapted from Yafc-CE (GPL-3.0) LuaContext.cs - trimmed to essentials.
/// </summary>
public static partial class NativeLuaTest
{

    private static bool _initialized = false;

    public static void InitializeLibraryPath(string? customPath = null)
    {
        if (_initialized) return;

        System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(
            typeof(NativeLuaTest).Assembly,
            (name, assembly, searchPath) =>
            {
                if (name == "lua52")
                {
                    // Try custom path first
                    if (!string.IsNullOrEmpty(customPath) &&
                        System.IO.File.Exists(customPath))
                    {
                        return System.Runtime.InteropServices.NativeLibrary.Load(customPath);
                    }

                    // Try the library name variations
                    foreach (var candidate in new[] { "lua52", "liblua52", "liblua52.so", "liblua.so" })
                    {
                        try
                        {
                            return System.Runtime.InteropServices.NativeLibrary.Load(
                                candidate, assembly, searchPath);
                        }
                        catch { }
                    }
                }
                return IntPtr.Zero; // default
            });

        _initialized = true;
    }

    private const string LUA = "lua52";

    [LibraryImport(LUA)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial IntPtr luaL_newstate();

    [LibraryImport(LUA)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial IntPtr luaL_openlibs(IntPtr state);

    [LibraryImport(LUA)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void lua_close(IntPtr state);

    [LibraryImport(LUA, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int luaL_loadstring(IntPtr state, string s);

    [LibraryImport(LUA)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int lua_pcallk(IntPtr state, int nargs, int nresults, int msgh, IntPtr ctx, IntPtr k);

    [LibraryImport(LUA, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int lua_getglobal(IntPtr state, string var);

    [LibraryImport(LUA)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial IntPtr lua_tolstring(IntPtr state, int idx, out IntPtr len);

    [LibraryImport(LUA)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void lua_settop(IntPtr state, int idx);

    public static string RunHelloWorld()
    {
        IntPtr L = IntPtr.Zero;
        try
        {
            InitializeLibraryPath();
            L = luaL_newstate();
            if (L == IntPtr.Zero)
                return "FALHOU: luaL_newstate retornou NULL";

            luaL_openlibs(L);

            int loadResult = luaL_loadstring(L, "greeting = 'Ola do Lua nativo! 2+2 = ' .. (2+2)");
            if (loadResult != 0)
                return $"FALHOU: luaL_loadstring erro {loadResult}";

            int callResult = lua_pcallk(L, 0, 0, 0, IntPtr.Zero, IntPtr.Zero);
            if (callResult != 0)
                return $"FALHOU: lua_pcallk erro {callResult}";

            lua_getglobal(L, "greeting");
            IntPtr strPtr = lua_tolstring(L, -1, out IntPtr len);
            string? result = strPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(strPtr, (int)len) : "(null)";
            lua_settop(L, -2);

            return $"Lua52 nativo OK: {result}";
        }
        catch (DllNotFoundException ex)
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"DllNotFoundException: {ex.Message}");
            info.AppendLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            info.AppendLine($"Arch: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            info.AppendLine($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");

            // Procura .so em caminhos conhecidos do Android
            var searchPaths = new[]
            {
                "/data/data/com.CompanyName.Yafc.App/lib",
                "/data/app",
                AppContext.BaseDirectory,
                "/proc/self/maps",
            };

            foreach (var path in searchPaths)
            {
                info.AppendLine($"--- {path} ---");
                try
                {
                    if (path == "/proc/self/maps")
                    {
                        // Lista o que o processo ja tem carregado que menciona "lua"
                        var lines = System.IO.File.ReadAllLines(path);
                        foreach (var line in lines)
                            if (line.Contains("lua", StringComparison.OrdinalIgnoreCase))
                                info.AppendLine($"  {line}");
                    }
                    else if (System.IO.Directory.Exists(path))
                    {
                        var files = System.IO.Directory.GetFiles(path, "*lua*",
                            System.IO.SearchOption.AllDirectories);
                        foreach (var f in files.Take(20)) info.AppendLine($"  {f}");
                        if (files.Length == 0) info.AppendLine("  (nenhum lua*)");
                    }
                    else info.AppendLine("  (nao existe)");
                }
                catch (Exception e) { info.AppendLine($"  ERRO: {e.Message}"); }
            }

            // Tenta carregar usando NativeLibrary direto no caminho do APK
            info.AppendLine("--- Tentativa manual ---");
            try
            {
                var libPath = "/data/data/com.CompanyName.Yafc.App/lib/arm64/liblua52.so";
                if (System.IO.File.Exists(libPath))
                {
                    info.AppendLine($"Tentando carregar: {libPath}");
                    var h = System.Runtime.InteropServices.NativeLibrary.Load(libPath);
                    info.AppendLine($"Carregou! handle={h}");
                }
                else
                {
                    info.AppendLine($"Nao existe: {libPath}");
                }
            }
            catch (Exception e) { info.AppendLine($"  ERRO: {e.Message}"); }

            return info.ToString();
        }
        catch (Exception ex)
        {
            return $"FALHOU: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            if (L != IntPtr.Zero) lua_close(L);
        }
    }
}