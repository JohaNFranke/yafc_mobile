using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Yafc.App.ViewModels;

namespace Yafc.App.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PickFileStreamAsync = PickFileStreamAsync;
            vm.PickSaveStreamAsync = PickSaveStreamAsync;
            vm.PickFolderAsync = PickFolderAsync;
        }
    }

    private async Task<(Stream? stream, string? displayName)> PickFileStreamAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return (null, null);

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Abrir project.yafc",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Yafc project") { Patterns = new[] { "*.yafc" } },
                new("Todos") { Patterns = new[] { "*" } }
            }
        });

        var file = files.FirstOrDefault();
        if (file is null) return (null, null);

        var stream = await file.OpenReadAsync();
        return (stream, file.Name);
    }

    private async Task<(Stream? stream, string? displayName)> PickSaveStreamAsync(string suggestedName)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return (null, null);

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Salvar project.yafc",
            SuggestedFileName = suggestedName,
            DefaultExtension = "yafc"
        });

        if (file is null) return (null, null);

        var stream = await file.OpenWriteAsync();
        // Garante que comece do zero (SAF nao trunca automaticamente)
        if (stream.CanSeek)
        {
            stream.SetLength(0);
            stream.Position = 0;
        }
        return (stream, file.Name);
    }

    private async Task<IStorageFolder?> PickFolderAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return null;

        var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Selecionar pasta Factorio",
            AllowMultiple = false
        });

        return folders.FirstOrDefault();
    }

}