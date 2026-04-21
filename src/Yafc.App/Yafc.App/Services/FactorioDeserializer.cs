using System;
using System.Collections.Generic;
using Yafc.Model.Mobile;

namespace Yafc.Parser;

// Converts data.raw LuaTables into a GameDatabase.
// Must be called before LuaContext.Dispose() — LuaTable refs become invalid after that.
internal static class FactorioDeserializer
{
    private static readonly HashSet<string> _itemTypes = new(StringComparer.Ordinal)
    {
        "item", "ammo", "armor", "blueprint", "blueprint-book", "capsule",
        "gun", "item-with-entity-data", "item-with-label", "item-with-inventory",
        "item-with-tags", "module", "rail-planner", "repair-tool",
        "selection-tool", "tool", "upgrade-item", "deconstruction-item",
        "spidertron-remote", "space-platform-starter-pack",
    };

    private static readonly HashSet<string> _entityTypes = new(StringComparer.Ordinal)
    {
        "assembling-machine", "furnace", "rocket-silo",
    };

    public static GameDatabase Build(LuaTable data, Yafc.App.Services.StringPool pool)
    {
        var db = new GameDatabase();
        if (data["raw"] is not LuaTable raw) return db;

        foreach (var (key, value) in raw.ObjectElements)
        {
            if (key is not string typeName || value is not LuaTable bucket) continue;

            if (_itemTypes.Contains(typeName))
                ReadItems(bucket, db, pool, GameGoodsType.Item);
            else if (typeName == "fluid")
                ReadItems(bucket, db, pool, GameGoodsType.Fluid);
            else if (typeName == "recipe")
                ReadRecipes(bucket, db, pool);
            else if (_entityTypes.Contains(typeName))
                ReadEntities(bucket, db, pool);
            else if (typeName == "technology")
                ReadTechnologies(bucket, db, pool);
        }

        return db;
    }

    private static void ReadItems(LuaTable bucket, GameDatabase db, Yafc.App.Services.StringPool pool, GameGoodsType type)
    {
        foreach (var (_, proto) in bucket.ObjectElements)
        {
            if (proto is not LuaTable t || t["name"] is not string name) continue;
            name = pool.Intern(name);
            db.Items[name] = new GameItem { Name = name, Type = type };
        }
    }

    private static void ReadRecipes(LuaTable bucket, GameDatabase db, Yafc.App.Services.StringPool pool)
    {
        foreach (var (_, proto) in bucket.ObjectElements)
        {
            if (proto is not LuaTable t || t["name"] is not string name) continue;
            name = pool.Intern(name);

            // Factorio 1.1 can nest normal/expensive variants; prefer "normal".
            LuaTable root = t["normal"] is LuaTable n ? n : t;

            string category = pool.Intern(root["category"] is string c ? c : "crafting");
            float energy = root["energy_required"] is double e ? (float)e : 0.5f;
            bool enabled = root["enabled"] is bool b ? b : true;

            var ingredients = ReadIngredients(root["ingredients"] as LuaTable, pool);
            var products = ReadProducts(root, pool);

            db.Recipes[name] = new GameRecipe
            {
                Name = name,
                Category = category,
                EnergyRequired = energy,
                Enabled = enabled,
                Ingredients = ingredients,
                Products = products,
            };
        }
    }

    private static GameIngredient[] ReadIngredients(LuaTable? table, Yafc.App.Services.StringPool pool)
    {
        if (table is null) return [];
        var elements = table.ArrayElements;
        var tmp = new List<GameIngredient>(elements.Count);
        foreach (var item in elements)
        {
            if (item is LuaTable t && ParseIngredient(t, pool) is { } ingr)
                tmp.Add(ingr);
        }
        return tmp.Count == 0 ? [] : tmp.ToArray();
    }

    private static GameIngredient? ParseIngredient(LuaTable t, Yafc.App.Services.StringPool pool)
    {
        // Long form: {type="item", name="iron-ore", amount=1}
        // Short form: {"iron-ore", 1}
        string? name = t["name"] is string n ? n : t[1] as string;
        if (name is null) return null;
        name = pool.Intern(name);
        var goodsType = t["type"] is string tp && tp == "fluid" ? GameGoodsType.Fluid : GameGoodsType.Item;
        float amount = t["amount"] is double a ? (float)a : t[2] is double a2 ? (float)a2 : 1f;
        return new GameIngredient(name, goodsType, amount);
    }

    private static GameProduct[] ReadProducts(LuaTable root, Yafc.App.Services.StringPool pool)
    {
        if (root["results"] is LuaTable results)
        {
            var elements = results.ArrayElements;
            var tmp = new List<GameProduct>(elements.Count);
            foreach (var item in elements)
            {
                if (item is LuaTable t && ParseProduct(t, pool) is { } prod)
                    tmp.Add(prod);
            }
            return tmp.Count == 0 ? [] : tmp.ToArray();
        }

        // Old format: result + optional result_count
        if (root["result"] is string resultName)
        {
            float count = root["result_count"] is double rc ? (float)rc : 1f;
            return [new GameProduct(pool.Intern(resultName), GameGoodsType.Item, count, 1f)];
        }

        return [];
    }

    private static GameProduct? ParseProduct(LuaTable t, Yafc.App.Services.StringPool pool)
    {
        string? name = t["name"] is string n ? n : t[1] as string;
        if (name is null) return null;
        name = pool.Intern(name);
        var goodsType = t["type"] is string tp && tp == "fluid" ? GameGoodsType.Fluid : GameGoodsType.Item;
        float amount = t["amount"] is double a ? (float)a : t[2] is double a2 ? (float)a2 : 1f;
        float prob = t["probability"] is double p ? (float)p : 1f;
        return new GameProduct(name, goodsType, amount, prob);
    }

    private static void ReadEntities(LuaTable bucket, GameDatabase db, Yafc.App.Services.StringPool pool)
    {
        foreach (var (_, proto) in bucket.ObjectElements)
        {
            if (proto is not LuaTable t || t["name"] is not string name) continue;
            name = pool.Intern(name);
            string[] categories = ReadStringArray(t["crafting_categories"] as LuaTable, pool);
            float speed = t["crafting_speed"] is double s ? (float)s : 1f;
            db.Entities[name] = new GameEntity
            {
                Name = name,
                CraftingCategories = categories,
                CraftingSpeed = speed,
            };
        }
    }

    private static void ReadTechnologies(LuaTable bucket, GameDatabase db, Yafc.App.Services.StringPool pool)
    {
        foreach (var (_, proto) in bucket.ObjectElements)
        {
            if (proto is not LuaTable t || t["name"] is not string name) continue;
            name = pool.Intern(name);
            string[] prereqs = ReadStringArray(t["prerequisites"] as LuaTable, pool);
            string[] unlocked = ReadUnlockedRecipes(t["effects"] as LuaTable, pool);
            db.Technologies[name] = new GameTechnology
            {
                Name = name,
                Prerequisites = prereqs,
                UnlockedRecipes = unlocked,
            };
        }
    }

    private static string[] ReadStringArray(LuaTable? table, Yafc.App.Services.StringPool pool)
    {
        if (table is null) return [];
        var elements = table.ArrayElements;
        var arr = new string[elements.Count];
        int i = 0;
        foreach (var item in elements)
        {
            if (item is string s) arr[i++] = pool.Intern(s);
        }
        return i == arr.Length ? arr : arr[..i];
    }

    private static string[] ReadUnlockedRecipes(LuaTable? effects, Yafc.App.Services.StringPool pool)
    {
        if (effects is null) return [];
        var elements = effects.ArrayElements;
        var result = new List<string>(elements.Count);
        foreach (var item in elements)
        {
            if (item is LuaTable t
                && t["type"] is string tp && tp == "unlock-recipe"
                && t["recipe"] is string recipe)
            {
                result.Add(pool.Intern(recipe));
            }
        }
        return result.Count == 0 ? [] : result.ToArray();
    }
}
