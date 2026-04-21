# build-ortools-android.ps1
# Compila libGoogle.OrTools.so para Android ARM64 e copia para o projeto.
#
# Pré-requisitos:
#   - Android NDK 25+ instalado
#   - CMake 3.22+ no PATH
#   - Ninja no PATH  (winget install Ninja-build.Ninja)
#   - Git no PATH
#   - ~4 GB de espaço em disco
#
# Uso:
#   .\scripts\build-ortools-android.ps1
#   .\scripts\build-ortools-android.ps1 -NdkRoot "C:\Android\ndk\26.1.10909125"

param(
    [string]$NdkRoot = "",
    [string]$OrToolsVersion = "v9.15",
    [string]$AndroidApi = "23",
    [string]$BuildType = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $ScriptDir
$BuildDir  = Join-Path $RepoRoot "build\ortools-android"
$DestDir   = Join-Path $RepoRoot "src\Yafc.App\Yafc.App.Android\lib\arm64-v8a"

# ── Detect NDK ───────────────────────────────────────────────────────────────
if (-not $NdkRoot) {
    $candidates = @(
        "$env:ANDROID_NDK_HOME",
        "$env:ANDROID_NDK_ROOT",
        "$env:ANDROID_NDK",
        "$env:LOCALAPPDATA\Android\Sdk\ndk",
        "C:\Android\ndk"
    )
    foreach ($c in $candidates) {
        if ($c -and (Test-Path $c)) {
            # If it points to a versioned folder, use it; otherwise find the latest inside
            if (Test-Path "$c\build\cmake\android.toolchain.cmake") {
                $NdkRoot = $c
                break
            }
            $sub = Get-ChildItem $c -Directory | Sort-Object Name -Descending | Select-Object -First 1
            if ($sub -and (Test-Path "$($sub.FullName)\build\cmake\android.toolchain.cmake")) {
                $NdkRoot = $sub.FullName
                break
            }
        }
    }
}

if (-not $NdkRoot -or -not (Test-Path "$NdkRoot\build\cmake\android.toolchain.cmake")) {
    Write-Error @"
Android NDK nao encontrado. Instale via Android Studio (SDK Manager > NDK) e passe o caminho:
  .\scripts\build-ortools-android.ps1 -NdkRoot 'C:\caminho\para\ndk\25.x.xxxxxx'
"@
    exit 1
}
$Toolchain = "$NdkRoot\build\cmake\android.toolchain.cmake"
Write-Host "[NDK] $NdkRoot"

# ── Clone OR-Tools ────────────────────────────────────────────────────────────
$SrcDir = Join-Path $BuildDir "src"
if (-not (Test-Path $SrcDir)) {
    Write-Host "[git] Clonando or-tools $OrToolsVersion..."
    git clone https://github.com/google/or-tools.git --depth=1 --branch $OrToolsVersion $SrcDir
} else {
    Write-Host "[git] Fonte ja existe em $SrcDir (skip clone)"
}

# ── CMake configure ───────────────────────────────────────────────────────────
$OutDir = Join-Path $BuildDir "out"
New-Item -ItemType Directory -Force $OutDir | Out-Null

Write-Host "[cmake] Configurando para android-arm64..."
cmake -B $OutDir -G Ninja `
    -DCMAKE_TOOLCHAIN_FILE="$Toolchain" `
    -DANDROID_ABI=arm64-v8a `
    -DANDROID_PLATFORM="android-$AndroidApi" `
    -DCMAKE_BUILD_TYPE=$BuildType `
    -DBUILD_SHARED_LIBS=ON `
    -DBUILD_SAMPLES=OFF `
    -DBUILD_EXAMPLES=OFF `
    -DBUILD_TESTING=OFF `
    -DUSE_COINOR=OFF `
    -DUSE_GLPK=OFF `
    -DUSE_SCIP=OFF `
    -DUSE_HIGHS=ON `
    $SrcDir

if ($LASTEXITCODE -ne 0) { Write-Error "cmake configure falhou"; exit 1 }

# ── Build ─────────────────────────────────────────────────────────────────────
$Jobs = [System.Environment]::ProcessorCount
Write-Host "[cmake] Compilando com $Jobs threads (pode demorar 15-40 min)..."
cmake --build $OutDir --target ortools -- -j$Jobs

if ($LASTEXITCODE -ne 0) { Write-Error "cmake build falhou"; exit 1 }

# ── Copy result ───────────────────────────────────────────────────────────────
$LibPath = Get-ChildItem $OutDir -Recurse -Filter "libGoogle.OrTools.so" |
           Select-Object -First 1

if (-not $LibPath) {
    Write-Error "libGoogle.OrTools.so nao encontrada em $OutDir apos build."
    exit 1
}

New-Item -ItemType Directory -Force $DestDir | Out-Null
Copy-Item $LibPath.FullName (Join-Path $DestDir "libGoogle.OrTools.so") -Force
Write-Host "[ok] libGoogle.OrTools.so copiada para $DestDir"
Write-Host ""
Write-Host "Proximo passo: rebuild do APK"
Write-Host "  dotnet publish -c Release -f net8.0-android src\Yafc.App\Yafc.App.Android\Yafc.App.Android.csproj"
