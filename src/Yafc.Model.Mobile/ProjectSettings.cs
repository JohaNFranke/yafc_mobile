using System.Text.Json.Serialization;

namespace Yafc.Model.Mobile;

public sealed class ProjectSettings
{
    [JsonPropertyName("milestones")]
    public List<string> Milestones { get; set; } = new();

    [JsonPropertyName("itemFlags")]
    public Dictionary<string, int> ItemFlags { get; set; } = new();

    [JsonPropertyName("miningProductivity")]
    public float MiningProductivity { get; set; }

    [JsonPropertyName("researchSpeedBonus")]
    public float ResearchSpeedBonus { get; set; }

    [JsonPropertyName("researchProductivity")]
    public float ResearchProductivity { get; set; }

    [JsonPropertyName("productivityTechnologyLevels")]
    public Dictionary<string, int> ProductivityTechnologyLevels { get; set; } = new();

    [JsonPropertyName("reactorSizeX")]
    public int ReactorSizeX { get; set; } = 2;

    [JsonPropertyName("reactorSizeY")]
    public int ReactorSizeY { get; set; } = 2;

    [JsonPropertyName("PollutionCostModifier")]
    public float PollutionCostModifier { get; set; }

    [JsonPropertyName("spoilingRate")]
    public float SpoilingRate { get; set; } = 1f;
}

public sealed class ProjectPreferences
{
    [JsonPropertyName("time")]
    public int Time { get; set; } = 1;

    [JsonPropertyName("itemUnit")]
    public float ItemUnit { get; set; }

    [JsonPropertyName("fluidUnit")]
    public float FluidUnit { get; set; }

    [JsonPropertyName("defaultBelt")]
    public string? DefaultBelt { get; set; }

    [JsonPropertyName("defaultInserter")]
    public string? DefaultInserter { get; set; }

    [JsonPropertyName("inserterCapacity")]
    public int InserterCapacity { get; set; } = 1;

    [JsonPropertyName("sourceResources")]
    public List<string> SourceResources { get; set; } = new();

    [JsonPropertyName("favorites")]
    public List<string> Favorites { get; set; } = new();

    [JsonPropertyName("targetTechnology")]
    public string? TargetTechnology { get; set; }

    [JsonPropertyName("iconScale")]
    public float IconScale { get; set; } = 1f;

    [JsonPropertyName("maxMilestonesPerTooltipLine")]
    public int MaxMilestonesPerTooltipLine { get; set; } = 28;

    [JsonPropertyName("showMilestoneOnInaccessible")]
    public bool ShowMilestoneOnInaccessible { get; set; } = true;
}
