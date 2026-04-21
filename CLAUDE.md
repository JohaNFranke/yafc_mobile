# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a port of **Yafc-CE** (Yet Another Factorio Calculator - Community Edition) to Android/iOS/web using .NET 8 and Avalonia UI. It is a production planning calculator for the game Factorio that reads game data via an embedded Lua 5.2 VM and parses mod files.

Development is in progress (see README.md for phase status). Currently active: Model & I/O layer and Lua-based Factorio data parsing.

## Build Commands

```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~RoundTripTests"

# Publish Android APK
dotnet publish -c Release -f net8.0-android Yafc.App/Yafc.App.Android/Yafc.App.Android.csproj
```

## Architecture

### Layer Separation

**`src/Yafc.Model.Mobile/`** — Pure model/IO layer, no UI dependencies.
- `YafcProject`, `ProjectPage`, `ProductionTable`, `RecipeRow` — domain models mapping the `.yafc` JSON schema
- `ProjectFile` — static load/save via `System.Text.Json`
- `TypedRef` — strongly-typed references to game objects (e.g. `"Recipe.stone-brick@Quality.normal"`)

**`Yafc.App/Yafc.App/`** — Shared Avalonia UI (ViewModels + Views).
- `MainViewModel` — top-level controller: file loading, page management, Lua integration
- `FactorioDataSource` (partial class split across Services/) — mod discovery, Lua execution pipeline
- `LuaContext` — P/Invoke wrapper around the native Lua 5.2 VM; executes `data.lua` and mod scripts
- `YafcDataExtractor` — extracts embedded Lua files (Sandbox.lua, Defines*.lua) to disk before parsing

**Platform heads** (`Yafc.App.Android`, `Yafc.App.iOS`, `Yafc.App.Desktop`, `Yafc.App.Browser`) — thin entry points only. Android's `MainActivity.cs` must call `LuaNativeLoader.Initialize()` before Avalonia starts to load `liblua52.so`.

### Lua / Native Library

The Factorio data pipeline works by:
1. Copying game files to app storage (`FactorioDataCopier`)
2. Scanning mods (`ModDiscovery`, `FactorioDataSource`)
3. Running Lua scripts through the embedded VM (`LuaContext` P/Invoke → `liblua52.so` on Android, `lua52.dll` on Windows)
4. Extracting prototype statistics (`DataRawInspector`)

The native Lua lib is pre-built: `lib/arm64-v8a/liblua52.so` (Android) and `lua52.dll` (Windows).

### Testing

Tests live in `tests/Yafc.Model.Mobile.Tests/`. `RoundTripTests` loads real `.yafc` project files from `test-data/`, serializes and deserializes them, and asserts byte-exact JSON equality. Always run tests after changing model or serialization code.

### Key Conventions

- UI strings and comments are in **Portuguese**.
- Models use `System.Text.Json` with custom converters; avoid `Newtonsoft.Json` in the model layer (it is used only for `ModInfo` deserialization in the service layer).
- ViewModels use `CommunityToolkit.MVVM` (`[ObservableProperty]`, `[RelayCommand]`).
