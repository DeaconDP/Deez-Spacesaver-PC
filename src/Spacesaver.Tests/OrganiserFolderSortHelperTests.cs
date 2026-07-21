using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Tests;

public sealed class OrganiserFolderSortHelperTests
{
    [Fact]
    public void OrderByMode_Tag_SortsByTypeLabelThenPath()
    {
        var folders = new[]
        {
            Folder(@"C:\Zulu", ProjectFolderType.Working),
            Folder(@"C:\Alpha", ProjectFolderType.Backup),
            Folder(@"C:\Beta", ProjectFolderType.Asset),
            Folder(@"C:\Gamma", ProjectFolderType.Asset)
        };

        var ordered = OrganiserFolderSortHelper
            .OrderByMode(folders, f => f, OrganiserFolderSortMode.Tag)
            .Select(f => f.Path)
            .ToArray();

        // Assets, Backup, Working — then path within Assets
        Assert.Equal(
            [@"C:\Beta", @"C:\Gamma", @"C:\Alpha", @"C:\Zulu"],
            ordered);
    }

    [Fact]
    public void OrderByMode_Location_SortsByPathIgnoreCase()
    {
        var folders = new[]
        {
            Folder(@"D:\Zebra"),
            Folder(@"c:\alpha"),
            Folder(@"C:\Beta")
        };

        var ordered = OrganiserFolderSortHelper
            .OrderByMode(folders, f => f, OrganiserFolderSortMode.Location)
            .Select(f => f.Path)
            .ToArray();

        Assert.Equal([@"c:\alpha", @"C:\Beta", @"D:\Zebra"], ordered);
    }

    [Fact]
    public void OrderByMode_Size_SortsDescending_NullsLast()
    {
        var folders = new[]
        {
            Folder(@"C:\Small", bytes: 10),
            Folder(@"C:\Unscanned", bytes: null),
            Folder(@"C:\Large", bytes: 1000),
            Folder(@"C:\AlsoUnscanned", bytes: null),
            Folder(@"C:\Medium", bytes: 100)
        };

        var ordered = OrganiserFolderSortHelper
            .OrderByMode(folders, f => f, OrganiserFolderSortMode.Size)
            .Select(f => f.Path)
            .ToArray();

        Assert.Equal(
            [@"C:\Large", @"C:\Medium", @"C:\Small", @"C:\AlsoUnscanned", @"C:\Unscanned"],
            ordered);
    }

    [Fact]
    public void OrderByMode_DateCreated_SortsNewestFirst_NullsLast()
    {
        var older = DateTimeOffset.Parse("2020-01-01T00:00:00Z");
        var newer = DateTimeOffset.Parse("2024-06-01T00:00:00Z");
        var newest = DateTimeOffset.Parse("2025-01-01T00:00:00Z");

        var folders = new[]
        {
            Folder(@"C:\Old", created: older),
            Folder(@"C:\Unknown"),
            Folder(@"C:\New", created: newest),
            Folder(@"C:\AlsoUnknown"),
            Folder(@"C:\Mid", created: newer)
        };

        var ordered = OrganiserFolderSortHelper
            .OrderByMode(folders, f => f, OrganiserFolderSortMode.DateCreated)
            .Select(f => f.Path)
            .ToArray();

        Assert.Equal(
            [@"C:\New", @"C:\Mid", @"C:\Old", @"C:\AlsoUnknown", @"C:\Unknown"],
            ordered);
    }

    [Fact]
    public void OrderByMode_DateModified_SortsNewestFirst_NullsLast()
    {
        var older = DateTimeOffset.Parse("2021-03-01T00:00:00Z");
        var newer = DateTimeOffset.Parse("2023-08-15T00:00:00Z");

        var folders = new[]
        {
            Folder(@"C:\Stale", modified: older),
            Folder(@"C:\NoDate"),
            Folder(@"C:\Fresh", modified: newer),
            Folder(@"C:\AlsoNoDate")
        };

        var ordered = OrganiserFolderSortHelper
            .OrderByMode(folders, f => f, OrganiserFolderSortMode.DateModified)
            .Select(f => f.Path)
            .ToArray();

        Assert.Equal(
            [@"C:\Fresh", @"C:\Stale", @"C:\AlsoNoDate", @"C:\NoDate"],
            ordered);
    }

    [Fact]
    public void AppSettingsService_PersistsOrganiserFolderSort()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pcs-settings-{Guid.NewGuid():N}.json");
        try
        {
            var service = new AppSettingsService(path);
            Assert.Equal(OrganiserFolderSortMode.Location, service.OrganiserFolderSort);

            service.SetOrganiserFolderSort(OrganiserFolderSortMode.DateModified);

            var reloaded = new AppSettingsService(path);
            Assert.Equal(OrganiserFolderSortMode.DateModified, reloaded.OrganiserFolderSort);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
            var tmp = path + ".tmp";
            if (File.Exists(tmp))
                File.Delete(tmp);
        }
    }

    private static OrganiserFolder Folder(
        string path,
        ProjectFolderType type = ProjectFolderType.Untagged,
        long? bytes = null,
        DateTimeOffset? created = null,
        DateTimeOffset? modified = null) =>
        new()
        {
            Path = path,
            FolderType = type,
            LastMeasuredBytes = bytes,
            CreatedUtc = created,
            ModifiedUtc = modified
        };
}
