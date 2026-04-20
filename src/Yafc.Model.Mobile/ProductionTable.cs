using System.Text.Json.Serialization;

namespace Yafc.Model.Mobile;

public sealed class ProductionLink
{
    [JsonPropertyName("goods")]
    public TypedRef Goods { get; set; } = new();

    [JsonPropertyName("amount")]
    public float Amount { get; set; }

    [JsonPropertyName("algorithm")]
    public int Algorithm { get; set; }
}

public sealed class ModuleEntry
{
    [JsonPropertyName("module")]
    public TypedRef? Module { get; set; }

    [JsonPropertyName("fixedCount")]
    public int FixedCount { get; set; }
}

public sealed class RecipeModules
{
    [JsonPropertyName("beacon")]
    public TypedRef? Beacon { get; set; }

    [JsonPropertyName("list")]
    public List<ModuleEntry> List { get; set; } = new();

    [JsonPropertyName("beaconList")]
    public List<ModuleEntry> BeaconList { get; set; } = new();
}

public sealed class RecipeRow
{
    [JsonPropertyName("recipe")]
    public TypedRef Recipe { get; set; } = new();

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("entity")]
    public TypedRef? Entity { get; set; }

    [JsonPropertyName("fuel")]
    public TypedRef? Fuel { get; set; }

    [JsonPropertyName("fixedBuildings")]
    public float FixedBuildings { get; set; }

    [JsonPropertyName("fixedFuel")]
    public bool FixedFuel { get; set; }

    [JsonPropertyName("fixedIngredient")]
    public TypedRef? FixedIngredient { get; set; }

    [JsonPropertyName("fixedProduct")]
    public TypedRef? FixedProduct { get; set; }

    [JsonPropertyName("builtBuildings")]
    public int? BuiltBuildings { get; set; }

    [JsonPropertyName("showTotalIO")]
    public bool ShowTotalIO { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("tag")]
    public int Tag { get; set; }

    [JsonPropertyName("modules")]
    public RecipeModules? Modules { get; set; }

    [JsonPropertyName("subgroup")]
    public ProductionTable? Subgroup { get; set; }

    [JsonPropertyName("variants")]
    public List<System.Text.Json.JsonElement> Variants { get; set; } = new();
}

public sealed class PageModuleDefaults
{
    [JsonPropertyName("fillMiners")]
    public bool FillMiners { get; set; }

    [JsonPropertyName("autoFillPayback")]
    public float AutoFillPayback { get; set; }

    [JsonPropertyName("fillerModule")]
    public TypedRef? FillerModule { get; set; }

    [JsonPropertyName("beacon")]
    public TypedRef? Beacon { get; set; }

    [JsonPropertyName("beaconModule")]
    public TypedRef? BeaconModule { get; set; }

    [JsonPropertyName("beaconsPerBuilding")]
    public int BeaconsPerBuilding { get; set; } = 8;

    [JsonPropertyName("overrideCrafterBeacons")]
    public Dictionary<string, object> OverrideCrafterBeacons { get; set; } = new();
}

public sealed class ProductionTable : PageContent
{
    [JsonPropertyName("expanded")]
    public bool Expanded { get; set; } = true;

    [JsonPropertyName("links")]
    public List<ProductionLink> Links { get; set; } = new();

    [JsonPropertyName("recipes")]
    public List<RecipeRow> Recipes { get; set; } = new();

    [JsonPropertyName("modules")]
    public PageModuleDefaults? Modules { get; set; }
}
