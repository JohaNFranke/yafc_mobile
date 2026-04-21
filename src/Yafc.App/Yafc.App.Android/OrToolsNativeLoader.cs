using System;
using System.IO;
using Android.Content;

namespace Yafc.App.Android;

public static class OrToolsNativeLoader
{
    public static void Initialize(Context context)
    {
        var nativeLibDir = context.ApplicationInfo?.NativeLibraryDir;
        if (string.IsNullOrEmpty(nativeLibDir))
        {
            Yafc.App.Services.AppLog.Write("OrToolsNativeLoader: NativeLibraryDir is null");
            return;
        }

        var libPath = Path.Combine(nativeLibDir, "libGoogle.OrTools.so");
        Yafc.App.Services.AppLog.Write($"OrToolsNativeLoader: trying {libPath}");

        if (!File.Exists(libPath))
        {
            Yafc.App.Services.AppLog.Write("OrToolsNativeLoader: libGoogle.OrTools.so not found — solver disabled on Android");
            return;
        }

        try
        {
            Java.Lang.JavaSystem.Load(libPath);
            Yafc.App.Services.AppLog.Write("OrToolsNativeLoader: JavaSystem.Load OK");
        }
        catch (Exception e)
        {
            Yafc.App.Services.AppLog.Write($"OrToolsNativeLoader: JavaSystem.Load failed: {e.Message}");
        }

        try
        {
            System.Runtime.InteropServices.NativeLibrary.Load(libPath);
            Yafc.App.Services.AppLog.Write("OrToolsNativeLoader: NativeLibrary.Load OK");
        }
        catch (Exception e)
        {
            Yafc.App.Services.AppLog.Write($"OrToolsNativeLoader: NativeLibrary.Load failed: {e.Message}");
        }
    }
}
