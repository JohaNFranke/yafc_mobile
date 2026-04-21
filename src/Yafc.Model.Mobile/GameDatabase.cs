using System.Collections.Generic;

namespace Yafc.Model.Mobile;

public enum GameGoodsType { Item, Fluid }

// Value types: many instances per parse, benefit from stack allocation.
public readonly record struct GameIngredient(string Name, GameGoodsType Type, float Amount);
public readonly record struct GameProduct(string Name, GameGoodsType Type, float Amount, float Probability);

public sealed class GameItem
{
    public required string Name { get; init; }
    public required GameGoodsType Type { get; init; }
}

public sealed class GameRecipe
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required float EnergyRequired { get; init; }
    public required bool Enabled { get; init; }
    public required GameIngredient[] Ingredients { get; init; }
    public required GameProduct[] Products { get; init; }
}

public sealed class GameEntity
{
    public required string Name { get; init; }
    public required string[] CraftingCategories { get; init; }
    public required float CraftingSpeed { get; init; }
}

public sealed class GameTechnology
{
    public required string Name { get; init; }
    public required string[] Prerequisites { get; init; }
    public required string[] UnlockedRecipes { get; init; }
}

public sealed class GameDatabase
{
    public Dictionary<string, GameItem> Items { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, GameRecipe> Recipes { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, GameEntity> Entities { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, GameTechnology> Technologies { get; } = new(StringComparer.Ordinal);
}
