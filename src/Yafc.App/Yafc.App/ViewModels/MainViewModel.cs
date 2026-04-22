using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yafc.Model.Mobile;
using Yafc.App.Services;

namespace Yafc.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _fullErrorText = "";

    [ObservableProperty]
    private string _status = "Nenhum arquivo carregado";

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private PageViewModel? _selectedPage;

    [ObservableProperty]
    private string _newRecipeName = "";

    [ObservableProperty]
    private string _newPageName = "";

    [ObservableProperty]
    private string _newPageIcon = "";

    [ObservableProperty]
    private bool _newPageIsSummary;

    [ObservableProperty]
    private SettingsViewModel? _settingsVm;

    public ObservableCollection<PageViewModel> Pages { get; } = new();

    public ObservableCollection<string> AvailableItems { get; } = new();

    private YafcProject? _project;

    public System.Func<System.Threading.Tasks.Task<(System.IO.Stream? stream, string? displayName)>>? PickFileStreamAsync { get; set; }
    public System.Func<string, System.Threading.Tasks.Task<(System.IO.Stream? stream, string? displayName)>>? PickSaveStreamAsync { get; set; }

    public System.Func<System.Threading.Tasks.Task<Avalonia.Platform.Storage.IStorageFolder?>>? PickFolderAsync { get; set; }

    [ObservableProperty]
    private string _factorioCachePath = "";

    [ObservableProperty]
    private string _solveTargetItem = "";

    [ObservableProperty]
    private float _solveTargetRate = 1f;

    [ObservableProperty]
    private ProductionPlan? _currentPlan;

    [ObservableProperty]
    private string _currentPlanSummary = "";

    [RelayCommand]
    private void LoadFile()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            Status = $"Arquivo nao encontrado: {FilePath}";
            return;
        }

        try
        {
            _project = ProjectFile.LoadFromFile(FilePath);
            Pages.Clear();
            foreach (var p in _project.Pages)
            {
                Pages.Add(new PageViewModel(p));
            }
            SettingsVm = new SettingsViewModel(_project);
            Status = $"Carregado: v{_project.YafcVersion}, {_project.Pages.Count} paginas";
        }
        catch (System.Exception ex)
        {
            Status = $"Erro: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveFile()
    {
        if (_project is null)
        {
            Status = "Nada para salvar";
            return;
        }

        try
        {
            ProjectFile.SaveToFile(_project, FilePath);
            Status = $"Salvo: {FilePath}";
        }
        catch (System.Exception ex)
        {
            Status = $"Erro ao salvar: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemovePage()
    {
        if (_project is null || SelectedPage is null) return;

        var toRemove = SelectedPage;
        _project.Pages.Remove(toRemove.SourcePage);
        _project.DisplayPages.Remove(toRemove.SourcePage.Guid);
        Pages.Remove(toRemove);
        SelectedPage = null;
        Status = $"Pagina removida: {toRemove.Name}";
    }

    [RelayCommand]
    private void RemoveRecipe()
    {
        if (SelectedPage?.SelectedRecipe is null) return;
        var removed = SelectedPage.RemoveSelectedRecipe();
        Status = removed is null ? "Nada removido" : $"Receita removida: {removed}";
    }

    [RelayCommand]
    private void AddRecipe()
    {
        if (SelectedPage is null)
        {
            Status = "Selecione uma pagina primeiro";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewRecipeName))
        {
            Status = "Digite o nome da receita";
            return;
        }
        var added = SelectedPage.AddRecipe(NewRecipeName.Trim());
        if (added is null)
        {
            Status = "Pagina nao e ProductionTable";
            return;
        }
        Status = $"Receita adicionada: {added}";
        NewRecipeName = "";
    }

    [RelayCommand]
    private void AddPage()
    {
        if (_project is null)
        {
            Status = "Carregue um arquivo primeiro";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewPageName))
        {
            Status = "Digite o nome da pagina";
            return;
        }

        var guid = System.Guid.NewGuid().ToString("N");
        var contentType = NewPageIsSummary
            ? "Yafc.Model.Summary"
            : "Yafc.Model.ProductionTable";

        PageContent content = NewPageIsSummary
            ? new SummaryContent()
            : new ProductionTable();

        var json = System.Text.Json.JsonSerializer.SerializeToElement(
            content, content.GetType(), ProjectFile.Options);

        var page = new ProjectPage
        {
            ContentType = contentType,
            Guid = guid,
            Icon = string.IsNullOrWhiteSpace(NewPageIcon) ? null : NewPageIcon.Trim(),
            Name = NewPageName.Trim(),
            RawContent = json,
            Scroll = 0,
        };

        _project.Pages.Add(page);
        _project.DisplayPages.Add(guid);

        var vm = new PageViewModel(page);
        Pages.Add(vm);
        SelectedPage = vm;

        Status = $"Pagina criada: {vm.Name}";
        NewPageName = "";
        NewPageIcon = "";
    }

    [RelayCommand]
    private void ShowLog()
    {
        FullErrorText = $"Arquivo de log: {AppLog.LogFilePath}\n\n{AppLog.ReadAll()}";
        Status = "Log carregado";
    }

    [RelayCommand]
    private void ClearLog()
    {
        AppLog.Clear();
        FullErrorText = "";
        Status = "Log limpo";
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ExportLog()
    {
        if (PickSaveStreamAsync is null)
        {
            Status = "Picker indisponivel";
            return;
        }

        var (stream, displayName) = await PickSaveStreamAsync("yafc-app-log.txt");
        if (stream is null) return;

        try
        {
            using (stream)
            using (var writer = new System.IO.StreamWriter(stream))
            {
                await writer.WriteAsync(AppLog.ReadAll());
            }
            Status = $"Log exportado: {displayName}";
        }
        catch (System.Exception ex)
        {
            Status = $"Erro exportando: {ex.Message}";
        }
    }

    [RelayCommand]
    private void TestLua()
    {
        Status = "TestLua removido - use Testar Lua52 nativo";
    }

    [RelayCommand]
    private void TestLuaNative()
    {
        var result = Yafc.App.Services.NativeLuaTest.RunHelloWorld();
        Status = result;
        AppLog.Write($"TestLuaNative: {result}");
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ImportFactorioData()
    {
        if (PickFolderAsync is null)
        {
            Status = "Picker indisponivel";
            return;
        }

        Status = "Selecione a pasta Factorio...";
        var folder = await PickFolderAsync();
        if (folder is null)
        {
            Status = "Cancelado";
            return;
        }

        Status = "Copiando arquivos... (pode demorar)";
        try
        {
            var startTime = System.DateTime.Now;
            int totalFiles = 0;

            var progress = new System.Progress<Yafc.App.Services.CopyProgress>(p =>
            {
                totalFiles = p.FilesDone;
                Status = $"Copiando ({p.FilesDone}): {p.CurrentFile}";
            });

            var cachePath = await Yafc.App.Services.FactorioDataCopier.CopyToCacheAsync(
                folder, "factorio", progress);

                // Aguarda UI processar ultimas mensagens de progresso
            await System.Threading.Tasks.Task.Delay(100);

            var elapsed = System.DateTime.Now - startTime;
            FactorioCachePath = cachePath;
            Status = $"CONCLUIDO! {totalFiles} arquivos em {elapsed.TotalSeconds:F1}s. Cache: {cachePath}";
            AppLog.Write($"Import CONCLUIDO: {totalFiles} arquivos em {elapsed.TotalSeconds:F1}s");
        }
        catch (System.Exception ex)
        {
            AppLog.WriteException("ImportFactorioData", ex);
            Status = $"Erro ao importar: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ListMods()
    {
        if (string.IsNullOrWhiteSpace(FactorioCachePath))
        {
            Status = "Importe a pasta Factorio primeiro";
            return;
        }

        try
        {
            var mods = Yafc.App.Services.ModDiscovery.Discover(FactorioCachePath);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Total: {mods.Count} mods");
            sb.AppendLine($"Built-in: {mods.Count(m => m.IsBuiltIn)}");
            sb.AppendLine($"User: {mods.Count(m => !m.IsBuiltIn)}");
            sb.AppendLine($"Enabled: {mods.Count(m => m.Enabled)}");
            sb.AppendLine();
            sb.AppendLine("Lista:");
            foreach (var m in mods.OrderBy(x => !x.IsBuiltIn).ThenBy(x => x.Info.Name))
            {
                var origin = m.IsBuiltIn ? "builtin" : "user";
                var status = m.Enabled ? "ON " : "off";
                sb.AppendLine($"  [{status}] [{origin}] {m.Info.Name} v{m.Info.Version}");
            }

            FullErrorText = sb.ToString();
            Status = $"{mods.Count} mods encontrados ({mods.Count(x => x.Enabled)} habilitados)";
        }
        catch (System.Exception ex)
        {
            Yafc.App.Services.AppLog.WriteException("ListMods", ex);
            Status = $"Erro: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SortMods()
    {
        if (string.IsNullOrWhiteSpace(FactorioCachePath))
        {
            Status = "Importe a pasta Factorio primeiro";
            return;
        }

        try
        {
            var mods = Yafc.App.Services.ModDiscovery.Discover(FactorioCachePath);
            var sorted = Yafc.App.Services.ModSorter.Sort(mods);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Load order ({sorted.Count} mods):");
            for (int i = 0; i < sorted.Count; i++)
            {
                var m = sorted[i];
                var origin = m.IsBuiltIn ? "builtin" : "user";
                sb.AppendLine($"  {i + 1,3}. [{origin}] {m.Info.Name} v{m.Info.Version}");
            }

            FullErrorText = sb.ToString();
            Status = $"Ordenado: {sorted.Count} mods em ordem de carga";
        }
        catch (System.Exception ex)
        {
            Yafc.App.Services.AppLog.WriteException("SortMods", ex);
            FullErrorText = ex.Message;
            Status = $"Erro: {ex.Message}";
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task LoadGameData()
    {
        if (string.IsNullOrWhiteSpace(FactorioCachePath))
        {
            Status = "Importe a pasta Factorio primeiro";
            return;
        }

        Status = "Preparando Yafc data...";
        AppLog.Write("=== LoadGameData START ===");

        // Extrair arquivos Lua antes de entrar no Task.Run (acesso a recursos embutidos é rápido)
        var workDir = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "YafcApp", "yafc-work");
        Yafc.App.Services.YafcDataExtractor.ExtractTo(workDir);

        var factorioPath = System.IO.Path.Combine(FactorioCachePath, "data");
        var modPath = System.IO.Path.Combine(FactorioCachePath, "mods");
        var projectPath = System.IO.Path.Combine(workDir, "yafc-project.yafc");

        AppLog.Write($"factorioPath = {factorioPath}");
        AppLog.Write($"modPath = {modPath}");

        // Progress marshala de volta ao SynchronizationContext do Avalonia automaticamente
        var progress = new System.Progress<(string major, string minor)>(p =>
            Status = $"{p.major}: {p.minor}");

        var errorCollector = new Yafc.Parser.ErrorCollector();
        System.Exception? parseError = null;

        // Parse roda no thread pool para nao bloquear a UI thread (evita ANR no Android)
        await System.Threading.Tasks.Task.Run(() =>
        {
            // FactorioDataSource.Parse usa caminhos relativos ao CWD para carregar Data/Sandbox.lua
            var previousCwd = System.Environment.CurrentDirectory;
            System.Environment.CurrentDirectory = workDir;
            try
            {
                Yafc.Parser.FactorioDataSource.Parse(
                    factorioPath: factorioPath,
                    modPath: modPath,
                    projectPath: projectPath,
                    expensive: false,
                    netProduction: false,
                    progress: progress,
                    errorCollector: errorCollector,
                    locale: "en",
                    useLatestSave: false,
                    renderIcons: false);
            }
            catch (System.Exception ex)
            {
                parseError = ex;
            }
            finally
            {
                System.Environment.CurrentDirectory = previousCwd;
            }
        });

        // De volta na UI thread — seguro acessar ObservableProperties aqui
        if (parseError is not null)
        {
            AppLog.WriteException("LoadGameData", parseError);
            FullErrorText = parseError.ToString();
            Status = $"Erro: {parseError.Message}";
            return;
        }

        int errCount = errorCollector.All.Count;
        AppLog.Write($"=== LoadGameData END: {errCount} erros ===");
        foreach (var (msg, sev) in errorCollector.All)
            AppLog.Write($"  [{sev}] {msg}");

        var snapshot = Yafc.Parser.FactorioDataDeserializer.LastSnapshot;
        var db = Yafc.Parser.FactorioDataDeserializer.LastDatabase;

        if (db is not null)
        {
            AvailableItems.Clear();
            foreach (var name in db.Items.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
                AvailableItems.Add(name);
        }

        if (snapshot is not null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Yafc.App.Services.DataRawInspector.FormatSnapshot(snapshot));
            if (db is not null)
            {
                sb.AppendLine($"--- GameDatabase ---");
                sb.AppendLine($"Items:        {db.Items.Count}");
                sb.AppendLine($"Recipes:      {db.Recipes.Count}");
                sb.AppendLine($"Entities:     {db.Entities.Count}");
                sb.AppendLine($"Technologies: {db.Technologies.Count}");
            }
            FullErrorText = sb.ToString();
            Status = errCount == 0
                ? $"OK: {snapshot.TotalPrototypes} prototypes, {db?.Recipes.Count ?? 0} recipes, {AvailableItems.Count} items para solver"
                : $"Parse com {errCount} erro(s). Veja log.";
        }
        else
        {
            Status = errCount == 0 ? "Parse concluido, sem snapshot" : $"Parse com {errCount} erro(s).";
        }
    }


    [RelayCommand]
    private async System.Threading.Tasks.Task PickAndLoadFile()
    {
        if (PickFileStreamAsync is null) return;
        var (stream, displayName) = await PickFileStreamAsync();
        if (stream is null) return;

        try
        {
            byte[] bytes;
            using (stream)
            using (var ms = new System.IO.MemoryStream())
            {
                await stream.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            AppLog.Write($"Bytes lidos: {bytes.Length}");
            AppLog.Write($"Primeiros 50 bytes: {System.Text.Encoding.UTF8.GetString(bytes, 0, System.Math.Min(50, bytes.Length))}");
            if (bytes.Length >= 50)
            {
                AppLog.Write($"Ultimos 50 bytes: {System.Text.Encoding.UTF8.GetString(bytes, bytes.Length - 50, 50)}");
            }

            // Strip UTF-8 BOM if present
            int offset = 0;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                offset = 3;
// Procura posicao real do primeiro 'p' suspeito
            string fullText = System.Text.Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);
            int bracePos = fullText.LastIndexOf('}');
            AppLog.Write($"Ultimo '}}' em posicao: {bracePos} de {fullText.Length}");
            if (bracePos >= 0 && bracePos < fullText.Length - 1)
            {
                int showFrom = System.Math.Max(0, bracePos - 20);
                int showLen = System.Math.Min(100, fullText.Length - showFrom);
                AppLog.Write($"Contexto apos ultimo '}}': [{fullText.Substring(showFrom, showLen)}]");
            }
            // Conta quantos '{' e '}' tem no total
            int opens = 0, closes = 0;
            foreach (var c in fullText) { if (c == '{') opens++; else if (c == '}') closes++; }
            AppLog.Write($"Total abre: {opens}, fecha: {closes}");
            // Hash MD5 dos bytes
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(bytes, offset, bytes.Length - offset);
                var hex = System.Convert.ToHexString(hash);
                AppLog.Write($"MD5: {hex}");
            }
            using var reader = new System.IO.MemoryStream(bytes, offset, bytes.Length - offset);
            _project = ProjectFile.Load(reader);

            Pages.Clear();
            foreach (var p in _project.Pages)
                Pages.Add(new PageViewModel(p));
            SettingsVm = new SettingsViewModel(_project);
            FilePath = displayName ?? "(arquivo selecionado)";
            Status = $"Carregado: v{_project.YafcVersion}, {_project.Pages.Count} paginas ({bytes.Length - offset} bytes)";
        }
        catch (System.Exception ex)
        {
            AppLog.WriteException("PickAndLoadFile", ex);
            FullErrorText = $"Erro logado em: {AppLog.LogFilePath}\n\n{ex.Message}";
            Status = "Erro - log salvo";
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task PickAndSaveFile()
    {
        if (_project is null)
        {
            Status = "Nada para salvar";
            return;
        }
        if (PickSaveStreamAsync is null)
        {
            SaveFile();
            return;
        }

        var (stream, displayName) = await PickSaveStreamAsync("project.yafc");
        if (stream is null) return;

        try
        {
            using (stream)
            {
                ProjectFile.Save(_project, stream);
            }
            FilePath = displayName ?? FilePath;
            Status = $"Salvo: {FilePath}";
        }
        catch (System.Exception ex)
        {
            Status = $"Erro ao salvar: {ex.Message}";
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task Solve()
    {
        var db = Yafc.Parser.FactorioDataDeserializer.LastDatabase;
        if (db is null)
        {
            Status = "Carregue os dados do jogo primeiro (Carregar Dados)";
            return;
        }

        if (string.IsNullOrWhiteSpace(SolveTargetItem))
        {
            Status = "Digite o nome do item alvo";
            return;
        }

        if (!db.Items.ContainsKey(SolveTargetItem.Trim()) && !db.Recipes.ContainsKey(SolveTargetItem.Trim()))
        {
            Status = $"Item '{SolveTargetItem}' não encontrado no GameDatabase";
            return;
        }

        Status = "Resolvendo...";
        var target = SolveTargetItem.Trim();
        var rate = SolveTargetRate;

        ProductionPlan plan = ProductionPlan.Infeasible;
        await System.Threading.Tasks.Task.Run(() =>
        {
            plan = ProductionSolver.Solve(db, [(target, rate)]);
        });

        if (!plan.Feasible || plan.ErrorMessage is not null)
        {
            CurrentPlan = null;
            CurrentPlanSummary = "";
            Status = plan.ErrorMessage ?? $"Sem solução para '{target}'";
            return;
        }

        CurrentPlan = plan;
        CurrentPlanSummary = $"{rate:F2}/s de {target}";
        Status = $"Solução: {plan.ActiveRecipes.Count} receitas, {plan.Inputs.Count} insumos";
    }

    [RelayCommand]
    private void ApplyPlan()
    {
        if (CurrentPlan is null || !CurrentPlan.Feasible)
        {
            Status = "Nenhum plano para aplicar";
            return;
        }

        // Ensure a project exists so the plan can be persisted on Save.
        if (_project is null)
        {
            _project = new YafcProject { YafcVersion = "0" };
            SettingsVm = new SettingsViewModel(_project);
        }

        // Fall back: if no page selected or selected page isn't a ProductionTable,
        // create a new one named after the goal.
        var target = SelectedPage;
        if (target is null || !target.IsProductionTable)
        {
            var goalName = CurrentPlan.Goals.Count > 0
                ? CurrentPlan.Goals[0].ItemName
                : "plano";
            var guid = System.Guid.NewGuid().ToString("N");
            var page = new ProjectPage
            {
                ContentType = "Yafc.Model.ProductionTable",
                Guid = guid,
                Name = $"Plano: {goalName}",
                RawContent = System.Text.Json.JsonSerializer.SerializeToElement(
                    new ProductionTable(), ProjectFile.Options),
                Scroll = 0,
            };
            _project.Pages.Add(page);
            _project.DisplayPages.Add(guid);
            target = new PageViewModel(page);
            Pages.Add(target);
            SelectedPage = target;
        }

        int count = target.ApplyPlan(CurrentPlan);
        Status = $"Plano aplicado a '{target.Name}': {count} receitas";
    }

}

public partial class PageViewModel : ViewModelBase
{
    public ProjectPage SourcePage { get; }
    private readonly ProductionTable? _table;

    [ObservableProperty]
    private RecipeViewModel? _selectedRecipe;

    [ObservableProperty]
    private LinkViewModel? _selectedLink;

    public PageViewModel(ProjectPage page)
    {
        SourcePage = page;
        Name = page.Name;
        ContentType = page.ContentType;

        var content = page.GetContent(ProjectFile.Options);
        if (content is ProductionTable table)
        {
            _table = table;
            Recipes = new ObservableCollection<RecipeViewModel>(
                table.Recipes.Select(r => new RecipeViewModel(r)));
            Links = new ObservableCollection<LinkViewModel>(
                table.Links.Select(l => new LinkViewModel(l)));
        }
        else
        {
            Recipes = new ObservableCollection<RecipeViewModel>();
            Links = new ObservableCollection<LinkViewModel>();
        }
    }

    public string Name { get; }
    public string ContentType { get; }
    public int RecipeCount => Recipes.Count;
    public int LinkCount => Links.Count;
    public ObservableCollection<RecipeViewModel> Recipes { get; }
    public ObservableCollection<LinkViewModel> Links { get; }
    public string Summary => $"{Name} ({RecipeCount} receitas, {LinkCount} links)";

    public string? RemoveSelectedRecipe()
    {
        if (_table is null || SelectedRecipe is null) return null;
        var removed = SelectedRecipe;
        _table.Recipes.Remove(removed.SourceRow);
        Recipes.Remove(removed);
        SelectedRecipe = null;
        OnPropertyChanged(nameof(RecipeCount));
        OnPropertyChanged(nameof(Summary));
        return removed.RecipeName;
    }

    public string? AddRecipe(string recipeName)
    {
        if (_table is null) return null;
        var row = new RecipeRow
        {
            Recipe = new TypedRef { Target = $"Recipe.{recipeName}", Quality = "Quality.normal" },
            Enabled = true,
        };
        _table.Recipes.Add(row);
        var vm = new RecipeViewModel(row);
        Recipes.Add(vm);
        OnPropertyChanged(nameof(RecipeCount));
        OnPropertyChanged(nameof(Summary));
        return vm.RecipeName;
    }

    public bool IsProductionTable => _table is not null;

    public int ApplyPlan(ProductionPlan plan)
    {
        if (_table is null) return 0;

        _table.Recipes.Clear();
        Recipes.Clear();
        _table.Links.Clear();
        Links.Clear();

        foreach (var sol in plan.ActiveRecipes)
        {
            var row = new RecipeRow
            {
                Recipe = new TypedRef { Target = $"Recipe.{sol.RecipeName}", Quality = "Quality.normal" },
                Enabled = true,
                // FixedBuildings stays 0 ("auto"); the user will pin the crafter later
                // via entity selection. We record the solver rate in Description so
                // the target throughput is visible even before an entity is chosen.
                Description = $"Target: {sol.Rate:F4}/s · {sol.CraftingTime:F2}s/craft",
            };
            _table.Recipes.Add(row);
            Recipes.Add(new RecipeViewModel(row));
        }

        // Goals become hard-linked outputs; raw inputs become unconstrained links.
        foreach (var g in plan.Goals)
        {
            var link = new ProductionLink
            {
                Goods = new TypedRef { Target = $"{TypePrefix(g.Type)}.{g.ItemName}", Quality = "Quality.normal" },
                Amount = g.Rate,
            };
            _table.Links.Add(link);
            Links.Add(new LinkViewModel(link));
        }

        OnPropertyChanged(nameof(RecipeCount));
        OnPropertyChanged(nameof(LinkCount));
        OnPropertyChanged(nameof(Summary));
        return plan.ActiveRecipes.Count;
    }

    private static string TypePrefix(GameGoodsType t) => t switch
    {
        GameGoodsType.Fluid => "Fluid",
        _ => "Item",
    };
}

public partial class RecipeViewModel : ViewModelBase
{
    public RecipeRow SourceRow { get; }

    public RecipeViewModel(RecipeRow row)
    {
        SourceRow = row;
    }

    public string RecipeName => SourceRow.Recipe.Name ?? "?";
    public string EntityName => SourceRow.Entity?.Name ?? "?";
    public string Display => $"{RecipeName} ({EntityName})";

    public float FixedBuildings
    {
        get => SourceRow.FixedBuildings;
        set
        {
            if (System.Math.Abs(SourceRow.FixedBuildings - value) > 1e-9f)
            {
                SourceRow.FixedBuildings = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Enabled
    {
        get => SourceRow.Enabled;
        set
        {
            if (SourceRow.Enabled != value)
            {
                SourceRow.Enabled = value;
                OnPropertyChanged();
            }
        }
    }
}

public partial class LinkViewModel : ViewModelBase
{
    private readonly ProductionLink _link;

    public LinkViewModel(ProductionLink link)
    {
        _link = link;
    }

    public string GoodsName => _link.Goods.Name ?? "?";
    public string Display => $"{GoodsName} = {Amount}";

    public float Amount
    {
        get => _link.Amount;
        set
        {
            if (System.Math.Abs(_link.Amount - value) > 1e-9f)
            {
                _link.Amount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Display));
            }
        }
    }
}

public partial class SettingsViewModel : ViewModelBase
{
    private readonly YafcProject _project;

    [ObservableProperty]
    private string _newMilestone = "";

    [ObservableProperty]
    private string? _selectedMilestone;

    public ObservableCollection<string> Milestones { get; }

    public SettingsViewModel(YafcProject project)
    {
        _project = project;
        Milestones = new ObservableCollection<string>(project.Settings.Milestones);
        Milestones.CollectionChanged += (_, _) =>
        {
            project.Settings.Milestones.Clear();
            project.Settings.Milestones.AddRange(Milestones);
        };
    }

    public float MiningProductivity
    {
        get => _project.Settings.MiningProductivity;
        set { _project.Settings.MiningProductivity = value; OnPropertyChanged(); }
    }

    public float ResearchSpeedBonus
    {
        get => _project.Settings.ResearchSpeedBonus;
        set { _project.Settings.ResearchSpeedBonus = value; OnPropertyChanged(); }
    }

    public float ResearchProductivity
    {
        get => _project.Settings.ResearchProductivity;
        set { _project.Settings.ResearchProductivity = value; OnPropertyChanged(); }
    }

    public int ReactorSizeX
    {
        get => _project.Settings.ReactorSizeX;
        set { _project.Settings.ReactorSizeX = value; OnPropertyChanged(); }
    }

    public int ReactorSizeY
    {
        get => _project.Settings.ReactorSizeY;
        set { _project.Settings.ReactorSizeY = value; OnPropertyChanged(); }
    }

    public float PollutionCostModifier
    {
        get => _project.Settings.PollutionCostModifier;
        set { _project.Settings.PollutionCostModifier = value; OnPropertyChanged(); }
    }

    public float SpoilingRate
    {
        get => _project.Settings.SpoilingRate;
        set { _project.Settings.SpoilingRate = value; OnPropertyChanged(); }
    }

    public int Time
    {
        get => _project.Preferences.Time;
        set { _project.Preferences.Time = value; OnPropertyChanged(); }
    }

    public string? DefaultBelt
    {
        get => _project.Preferences.DefaultBelt;
        set { _project.Preferences.DefaultBelt = value; OnPropertyChanged(); }
    }

    public string? DefaultInserter
    {
        get => _project.Preferences.DefaultInserter;
        set { _project.Preferences.DefaultInserter = value; OnPropertyChanged(); }
    }

    public int InserterCapacity
    {
        get => _project.Preferences.InserterCapacity;
        set { _project.Preferences.InserterCapacity = value; OnPropertyChanged(); }
    }

    public float IconScale
    {
        get => _project.Preferences.IconScale;
        set { _project.Preferences.IconScale = value; OnPropertyChanged(); }
    }

    public int MaxMilestonesPerTooltipLine
    {
        get => _project.Preferences.MaxMilestonesPerTooltipLine;
        set { _project.Preferences.MaxMilestonesPerTooltipLine = value; OnPropertyChanged(); }
    }

    public bool ShowMilestoneOnInaccessible
    {
        get => _project.Preferences.ShowMilestoneOnInaccessible;
        set { _project.Preferences.ShowMilestoneOnInaccessible = value; OnPropertyChanged(); }
    }

    [RelayCommand]
    private void AddMilestone()
    {
        if (string.IsNullOrWhiteSpace(NewMilestone)) return;
        Milestones.Add(NewMilestone.Trim());
        NewMilestone = "";
    }

    [RelayCommand]
    private void RemoveMilestone()
    {
        if (SelectedMilestone is null) return;
        Milestones.Remove(SelectedMilestone);
    }
}