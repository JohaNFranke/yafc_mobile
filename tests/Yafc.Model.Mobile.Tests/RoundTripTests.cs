using Xunit;

using Yafc.Model.Mobile;

namespace Yafc.Model.Mobile.Tests;

public class RoundTripTests
{
    /// <summary>
    /// Finds project.yafc by walking up from the test binary until it sees
    /// a test-data folder, or falls back to the uploads path.
    /// Put your .yafc file at: &lt;solution&gt;/test-data/project.yafc
    /// </summary>
    private static string FindSamplePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "test-data", "project.yafc");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        if (File.Exists("/mnt/user-data/uploads/project.yafc"))
            return "/mnt/user-data/uploads/project.yafc";

        throw new FileNotFoundException(
            "project.yafc not found. Create a 'test-data' folder at the solution root " +
            "and place your project.yafc inside it.");
    }

    [Fact]
    public void Load_RealProject_Succeeds()
    {
        var p = ProjectFile.LoadFromFile(FindSamplePath());
        Assert.Equal("2.18.0.0", p.YafcVersion);
        Assert.Equal(40, p.Pages.Count);
        Assert.Equal(19, p.DisplayPages.Count);
        Assert.Equal(11, p.Settings.Milestones.Count);
    }

    [Fact]
    public void Load_FirstPage_IsStoneTable()
    {
        var p = ProjectFile.LoadFromFile(FindSamplePath());
        var first = p.Pages[0];
        Assert.Equal("Yafc.Model.ProductionTable", first.ContentType);
        Assert.Equal("Stone", first.Name);

        var content = (ProductionTable)first.GetContent(ProjectFile.Options)!;
        Assert.Equal(10, content.Recipes.Count);
        Assert.Equal(10, content.Links.Count);
    }

    [Fact]
    public void Recipe_References_DecomposeCorrectly()
    {
        var p = ProjectFile.LoadFromFile(FindSamplePath());
        var table = (ProductionTable)p.Pages[0].GetContent(ProjectFile.Options)!;
        var r = table.Recipes[0];
        Assert.Equal("Recipe", r.Recipe.Type);
        Assert.Equal("stone-brick", r.Recipe.Name);
        Assert.Equal("Quality.normal", r.Recipe.Quality);
    }

    [Fact]
    public void RoundTrip_ProducesEquivalentJson()
    {
        var p = ProjectFile.LoadFromFile(FindSamplePath());
        using var ms = new MemoryStream();
        ProjectFile.Save(p, ms);
        ms.Position = 0;
        var reloaded = ProjectFile.Load(ms);

        Assert.Equal(p.YafcVersion, reloaded.YafcVersion);
        Assert.Equal(p.Pages.Count, reloaded.Pages.Count);
        Assert.Equal(p.Settings.Milestones, reloaded.Settings.Milestones);
    }

    [Fact]
    public void RoundTrip_IsByteExact()
    {
        var path = FindSamplePath();
        var original = File.ReadAllBytes(path);
        var p = ProjectFile.LoadFromFile(path);

        using var ms = new MemoryStream();
        ProjectFile.Save(p, ms);
        var roundtripped = ms.ToArray();

        Assert.Equal(original.Length, roundtripped.Length);

        int firstDiff = -1;
        for (int i = 0; i < original.Length; i++)
        {
            if (original[i] != roundtripped[i])
            {
                firstDiff = i;
                break;
            }
        }

        if (firstDiff >= 0)
        {
            int start = Math.Max(0, firstDiff - 50);
            int len = Math.Min(150, original.Length - start);
            var origCtx = System.Text.Encoding.UTF8.GetString(original, start, len);
            var newCtx = System.Text.Encoding.UTF8.GetString(roundtripped, start, len);
            throw new Xunit.Sdk.XunitException(
                $"First byte diff at offset {firstDiff}\n\n" +
                $"Original context:\n{origCtx}\n\n" +
                $"Roundtrip context:\n{newCtx}");
        }
    }

[Fact]
    public void RoundTrip_AfterTouchingAllPages_IsStillByteExact()
    {
        var path = FindSamplePath();
        var original = File.ReadAllBytes(path);
        var p = ProjectFile.LoadFromFile(path);

        // Simulate what the ViewModel does: touches every page
        foreach (var page in p.Pages)
            page.GetContent(ProjectFile.Options);

        using var ms = new MemoryStream();
        ProjectFile.Save(p, ms);
        var roundtripped = ms.ToArray();

        int firstDiff = -1;
        int max = System.Math.Min(original.Length, roundtripped.Length);
        for (int i = 0; i < max; i++)
        {
            if (original[i] != roundtripped[i]) { firstDiff = i; break; }
        }

        if (original.Length != roundtripped.Length || firstDiff >= 0)
        {
            int offset = firstDiff >= 0 ? firstDiff : max;
            int start = System.Math.Max(0, offset - 80);
            int len = System.Math.Min(200, original.Length - start);
            int len2 = System.Math.Min(200, roundtripped.Length - start);
            var origCtx = System.Text.Encoding.UTF8.GetString(original, start, len);
            var newCtx = System.Text.Encoding.UTF8.GetString(roundtripped, start, len2);
            throw new Xunit.Sdk.XunitException(
                $"Sizes: orig={original.Length} new={roundtripped.Length}\n" +
                $"First diff offset: {offset}\n\n" +
                $"Original context:\n{origCtx}\n\n" +
                $"New context:\n{newCtx}");
        }
    }


}
