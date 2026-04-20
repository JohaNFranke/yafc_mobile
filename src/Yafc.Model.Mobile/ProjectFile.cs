using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yafc.Model.Mobile;

public static class ProjectFile
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public static YafcProject Load(Stream stream)
    {
        var project = JsonSerializer.Deserialize<YafcProject>(stream, Options)
            ?? throw new InvalidDataException("Empty .yafc file");
        return project;
    }

    public static YafcProject LoadFromFile(string path)
    {
        using var fs = File.OpenRead(path);
        return Load(fs);
    }

    public static void Save(YafcProject project, Stream stream)
    {
        foreach (var page in project.Pages)
            page.FlushContent(Options);

        var opts = new JsonSerializerOptions(Options) { WriteIndented = true };
        JsonSerializer.Serialize(stream, project, opts);
    }

    public static void SaveToFile(YafcProject project, string path)
    {
        using var fs = File.Create(path);
        Save(project, fs);
    }
}
