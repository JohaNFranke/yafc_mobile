using System.Text.Json.Serialization;

namespace Yafc.Model.Mobile;

/// <summary>
/// Typed reference to a game object. Format: "Type.name" (e.g. "Item.stone-brick").
/// Yafc .yafc files serialize all prototype references this way.
/// </summary>
public sealed class TypedRef
{
    [JsonPropertyName("target")]
    public string Target { get; set; } = "";

    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    [JsonIgnore]
    public string? Type => Target?.Split('.', 2) is { Length: 2 } p ? p[0] : null;

    [JsonIgnore]
    public string? Name => Target?.Split('.', 2) is { Length: 2 } p ? p[1] : null;

    public override string ToString() => Quality is null ? Target : $"{Target}@{Quality}";
}