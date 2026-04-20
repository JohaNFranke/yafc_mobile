# YafcMobile

Port do Yafc-CE (https://github.com/Yafc-CE/yafc-ce, GPL-3.0) para Android.

## Status

### Fase 1 — Modelo e I/O de .yafc — EM ANDAMENTO
- [x] Schema mapeado do arquivo real (v2.18.0.0, 40 páginas)
- [x] Modelos C# para Project, Settings, Preferences, Page, ProductionTable, RecipeRow, ProductionLink, Modules
- [x] Loader/saver via System.Text.Json
- [x] Validador Python confirmou schema completo contra arquivo real
- [ ] Build e teste com .NET 8 (pendente: sem dotnet no container)
- [ ] Ajuste de line endings CRLF e indent de 2 espaços para igualar byte-a-byte ao desktop

### Fase 2 — Parser de mods Factorio — NÃO INICIADA
- [ ] Port de Yafc.Parser (Lua data stage)
- [ ] NLua ou MoonSharp no Android
- [ ] Storage Access Framework para pasta de mods

### Fase 3 — Solver — NÃO INICIADA
- [ ] Google OrTools build ARM64 Android ou fallback puro C#

### Fase 4 — UI Avalonia — NÃO INICIADA
- [ ] Avalonia Mobile
- [ ] Adaptação de layouts densos do Yafc

### Fase 5 — Análises — NÃO INICIADA
- [ ] Milestone, Automation, Cost, Flow

## Estrutura

```
src/
  Yafc.Model.Mobile/      # Modelos e I/O do .yafc
  Yafc.App.Android/       # App Avalonia Android (pendente)
tests/
  Yafc.Model.Mobile.Tests/# xUnit contra project.yafc real
  validate_schema.py      # Validação rápida do schema JSON
```

## Licença

GPL-3.0 (herdado do Yafc-CE).
