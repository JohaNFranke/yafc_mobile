# YafcMobile

Native port of Yafc-CE (https://github.com/Yafc-CE/yafc-ce, GPL-3.0) to Android, rewritten in .NET 8.0.

## Architecture Overview
This project ports the calculation engine and interface of Yafc-CE to mobile devices. To ensure maximum performance when extracting game recipes (`data.raw`), the project abandons C# interpreters (like NLua) and uses a custom `P/Invoke` implementation of a native `Lua52.dll`, compiled specifically for Android ARM/x86 and Windows.

## Project Status

### Phase 1 — Lua Engine and Mod Loading — COMPLETED ✅
- [x] Custom Lua build chain (`Lua52.dll`) patched for Windows + Android
- [x] Lua P/Invoke validated and working on both platforms
- [x] Full port of the original Yafc-ce `LuaContext` and `FactorioDataSource`
- [x] Mod discovery, sorting, and settings preservation system (`mod-settings.dat`)
- [x] Successful execution of `Sandbox.lua`, `Defines2.0.lua`, and `Postprocess.lua`
- [x] Yafc-ce mod-fixes included and tested
- [x] Stress Test: Absolute success loading 34 simultaneous mods (including the complete Pyanodons ecosystem) with no memory errors.

### Phase 2 — Deserialization and Data Model (.NET 8) — IN PROGRESS 🚧
- [ ] Port of `FactorioDataDeserializer` (conversion of `data.raw` → typed Items/Recipes)
- [ ] Refactoring `Yafc.Model` for Mobile Garbage Collector optimization (.NET 8 `record structs` and String Pooling)
- [ ] Complete decoupling of the visual rendering pipeline (`Yafc.UI` and `Yafc.I18n`) from the data engine.

### Phase 3 — Project I/O and Parser (.yafc) — IN PROGRESS 🚧
- [x] JSON Schema mapped from the real desktop file (v2.18.0.0, 40 pages)
- [x] C# models established for Project, Settings, Preferences, Page, ProductionTable, etc.
- [x] Optimized loader/saver using `System.Text.Json`
- [x] Python validator operational to confirm the complete schema against real files
- [ ] Full build and test integration with .NET 8 in the CI/CD container
- [ ] Byte-for-byte synchronization with desktop (CRLF and indentation adjustments)

### Phase 4 — Mathematical Solver — NOT STARTED ⏳
- [ ] Linear resolution engine implementation
- [ ] Primary target: Google OrTools via native ARM64/Android build.
- [ ] Secondary target (Safe Fallback): Purely managed C# Linear Programming Solver.

### Phase 5 — Mobile UI/UX (Avalonia) — NOT STARTED ⏳
- [ ] Avalonia Mobile framework implementation.
- [ ] Complete MVVM refactoring: Discarding dense desktop panel layouts in favor of mobile navigation paradigms (Bottom Nav, infinite virtual lists).
- [ ] Storage Access Framework for the user to point to the mods folder in the Android system.

### Phase 6 — Advanced Analytics — NOT STARTED ⏳
- [ ] Integration of Milestone, Automation, Cost, and Flow algorithms.

## Repository Structure

```text
src/
  Yafc.Core/              # Lua Engine, Deserializer, and Domain Logic (Platform Agnostic)
  Yafc.Model.Mobile/      # Data models and .yafc format I/O (.NET 8)
  Yafc.App.Android/       # UI Layer and Views (Avalonia Android)
tests/
  Yafc.Model.Mobile.Tests/# xUnit against real project.yafc
  validate_schema.py      # Quick JSON schema validation
