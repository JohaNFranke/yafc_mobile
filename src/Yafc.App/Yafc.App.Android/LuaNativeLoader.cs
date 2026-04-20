using System;
using System.IO;
using Android.Content;

namespace Yafc.App.Android;

public static class LuaNativeLoader
{
    public static void Initialize(Context context)
    {
        try
        {
            var nativeLibDir = context.ApplicationInfo?.NativeLibraryDir;
            if (string.IsNullOrEmpty(nativeLibDir))
            {
                Yafc.App.Services.AppLog.Write("LuaNativeLoader: NativeLibraryDir is null");
                return;
            }

            var libPath = Path.Combine(nativeLibDir, "liblua52.so");
            Yafc.App.Services.AppLog.Write($"LuaNativeLoader: trying {libPath}");

            if (!File.Exists(libPath))
            {
                Yafc.App.Services.AppLog.Write($"LuaNativeLoader: file not found at {libPath}");

                // List what's actually there
                try
                {
                    var files = Directory.GetFiles(nativeLibDir);
                    foreach (var f in files)
                        Yafc.App.Services.AppLog.Write($"  existing: {f}");
                }
                catch (Exception e)
                {
                    Yafc.App.Services.AppLog.Write($"  dir list error: {e.Message}");
                }
                return;
            }

            // Load lib via java first (more reliable on Android)
            try
            {
                Java.Lang.JavaSystem.Load(libPath);
                Yafc.App.Services.AppLog.Write($"LuaNativeLoader: JavaSystem.Load OK");
            }
            catch (Exception e)
            {
                Yafc.App.Services.AppLog.Write($"LuaNativeLoader: JavaSystem.Load failed: {e.Message}");
            }

            // Also load via .NET NativeLibrary so P/Invoke can find it
            try
            {
                System.Runtime.InteropServices.NativeLibrary.Load(libPath);
                Yafc.App.Services.AppLog.Write($"LuaNativeLoader: NativeLibrary.Load OK");
            }
            catch (Exception e)
            {
                Yafc.App.Services.AppLog.Write($"LuaNativeLoader: NativeLibrary.Load failed: {e.Message}");
            }
        }
        catch (Exception ex)
        {
            Yafc.App.Services.AppLog.Write($"LuaNativeLoader FAILED: {ex.GetType().Name}: {ex.Message}");
        }
    }
}