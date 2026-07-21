using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Tests;

public sealed class FolderSuggestionServiceTests : IDisposable
{
    private readonly string _root;
    private readonly FolderSuggestionService _service = new();

    public FolderSuggestionServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"pcs-suggest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void FindSuggestions_FindsSibling_ExcludesAlreadyAttached()
    {
        var parent = Path.Combine(_root, "Parent");
        var attached = Path.Combine(parent, "Working");
        var sibling = Path.Combine(parent, "Backup");
        Directory.CreateDirectory(attached);
        Directory.CreateDirectory(sibling);

        var project = new OrganiserProject
        {
            Name = "Alpha",
            Folders =
            [
                new OrganiserFolder { Path = attached }
            ]
        };

        var results = _service.FindSuggestions(
            [project],
            readyDriveRoots: [],
            allProjects: [project],
            excludeIfAttachedAnywhere: false);

        Assert.Contains(results, s => PathsEqual(s.Path, sibling) && s.IsSibling);
        Assert.DoesNotContain(results, s => PathsEqual(s.Path, attached));
    }

    [Fact]
    public void FindSuggestions_NameMatch_RespectsDepthAndTokens()
    {
        // depth: root(0) / Games(1) / Alpha(2) — within max depth 3
        var games = Path.Combine(_root, "Games");
        var match = Path.Combine(games, "Alpha");
        var variant = Path.Combine(games, "Alpha_Backup");
        // depth 4 folder should not be searched as a match target under walk rules
        var tooDeep = Path.Combine(_root, "a", "b", "c", "AlphaDeep");
        Directory.CreateDirectory(match);
        Directory.CreateDirectory(variant);
        Directory.CreateDirectory(tooDeep);

        var project = new OrganiserProject { Name = "Alpha" };

        var results = _service.FindSuggestions(
            [project],
            readyDriveRoots: [_root],
            allProjects: [project],
            excludeIfAttachedAnywhere: true);

        Assert.Contains(results, s => PathsEqual(s.Path, match) && !s.IsSibling);
        Assert.Contains(results, s => PathsEqual(s.Path, variant));
        Assert.DoesNotContain(results, s => PathsEqual(s.Path, tooDeep));
    }

    [Fact]
    public void FindSuggestions_DedupsSiblingAndNameMatch()
    {
        var parent = Path.Combine(_root, "Hub");
        var attached = Path.Combine(parent, "Alpha");
        var sibling = Path.Combine(parent, "Alpha_Assets");
        Directory.CreateDirectory(attached);
        Directory.CreateDirectory(sibling);

        var project = new OrganiserProject
        {
            Name = "Alpha",
            Folders = [new OrganiserFolder { Path = attached }]
        };

        var results = _service.FindSuggestions(
            [project],
            readyDriveRoots: [_root],
            allProjects: [project],
            excludeIfAttachedAnywhere: false);

        var matches = results.Where(s => PathsEqual(s.Path, sibling)).ToList();
        Assert.Single(matches);
        Assert.True(matches[0].IsSibling);
    }

    [Fact]
    public void MatchesName_ShortNamesRequireExactEquality()
    {
        var tokens = FolderSuggestionService.BuildMatchTokens("AB");
        Assert.True(FolderSuggestionService.MatchesName("AB", tokens, "AB"));
        Assert.False(FolderSuggestionService.MatchesName("ABC", tokens, "AB"));
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(
            Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
}
