using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yafc.Model.Mobile;

public abstract class PageContent { }

public sealed class SummaryContent : PageContent
{
    // Summary pages store calculated rollup - shape TBD via real examples
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class ProjectPage
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "";

    [JsonPropertyName("guid")]
    public string Guid { get; set; } = "";

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("content")]
    public JsonElement RawContent { get; set; }

    [JsonPropertyName("scroll")]
    
    public float? Scroll { get; set; }

    private PageContent? _content;

    public PageContent? GetContent(JsonSerializerOptions options)
    {
        if (_content is not null) return _content;
        _content = ContentType switch
        {
            "Yafc.Model.ProductionTable" => RawContent.Deserialize<ProductionTable>(options),
            "Yafc.Model.Summary" => RawContent.Deserialize<SummaryContent>(options),
            _ => null
        };
        return _content;
    }

    /// <summary>
    /// Re-serializes the typed content back into RawContent.
    /// Must be called before saving so edits to the typed object take effect.
    /// </summary>
    public void FlushContent(JsonSerializerOptions options)
    {
        if (_content is null) return;
        var json = JsonSerializer.SerializeToElement(_content, _content.GetType(), options);
        RawContent = json;
    }

}

public sealed class YafcProject
{
    [JsonPropertyName("settings")]
    public ProjectSettings Settings { get; set; } = new();

    [JsonPropertyName("preferences")]
    public ProjectPreferences Preferences { get; set; } = new();

    [JsonPropertyName("sharedModuleTemplates")]
    public List<JsonElement> SharedModuleTemplates { get; set; } = new();

    [JsonPropertyName("yafcVersion")]
    public string YafcVersion { get; set; } = "";

    [JsonPropertyName("pages")]
    public List<ProjectPage> Pages { get; set; } = new();

    [JsonPropertyName("displayPages")]
    public List<string> DisplayPages { get; set; } = new();
}
