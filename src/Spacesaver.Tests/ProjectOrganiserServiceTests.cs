using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Tests;

public class ProjectOrganiserServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _storagePath;

    public ProjectOrganiserServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SpacesaverTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _storagePath = Path.Combine(_tempDir, "projects.json");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Temp cleanup best-effort.
        }
    }

    [Fact]
    public void CreateRenameRemove_RoundTripsThroughStorage()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("  Alpha  ");
        Assert.Equal("Alpha", project.Name);

        service.RenameProject(project.Id, "Alpha Prime");
        service.AddFolder(project.Id, @"C:\Projects\Alpha", ProjectFolderType.Working);

        var reloaded = new ProjectOrganiserService(_storagePath);
        var loaded = Assert.Single(reloaded.Projects);
        Assert.Equal("Alpha Prime", loaded.Name);
        var folder = Assert.Single(loaded.Folders);
        Assert.Equal(@"C:\Projects\Alpha", folder.Path);
        Assert.Equal(ProjectFolderType.Working, folder.FolderType);

        Assert.True(reloaded.RemoveProject(loaded.Id));
        Assert.Empty(new ProjectOrganiserService(_storagePath).Projects);
    }

    [Fact]
    public void AddFolder_BatchLoop_AddsDistinct_RejectsDuplicates_Persists()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");

        var paths = new[]
        {
            @"C:\Projects\Alpha",
            @"D:\Assets\Alpha",
            @"E:\Backups\Alpha",
            @"C:\Projects\Alpha", // duplicate of first
            @"c:\projects\alpha\" // case/slash variant of first
        };

        var added = 0;
        var duplicates = 0;
        foreach (var path in paths)
        {
            var result = service.AddFolder(project.Id, path);
            if (result.Added)
                added++;
            else
                duplicates++;
        }

        Assert.Equal(3, added);
        Assert.Equal(2, duplicates);
        Assert.Equal(3, project.Folders.Count);

        var reloaded = new ProjectOrganiserService(_storagePath);
        var loaded = Assert.Single(reloaded.Projects);
        Assert.Equal(3, loaded.Folders.Count);
        Assert.Contains(loaded.Folders, f => f.Path.Equals(@"C:\Projects\Alpha", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(loaded.Folders, f => f.Path.Equals(@"D:\Assets\Alpha", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(loaded.Folders, f => f.Path.Equals(@"E:\Backups\Alpha", StringComparison.OrdinalIgnoreCase));
        Assert.All(loaded.Folders, f => Assert.Equal(ProjectFolderType.Untagged, f.FolderType));
    }

    [Fact]
    public void AddFolder_RejectsDuplicatePath_CaseInsensitive()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");

        var first = service.AddFolder(project.Id, @"C:\Projects\Alpha", ProjectFolderType.Working);
        Assert.True(first.Added);

        var duplicate = service.AddFolder(project.Id, @"c:\projects\alpha\", ProjectFolderType.Backup);
        Assert.False(duplicate.Added);
        Assert.NotNull(duplicate.Error);
        Assert.Single(project.Folders);
    }

    [Fact]
    public void AddFolder_WarnsOnParentChildOverlap_ButStillAdds()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");

        service.AddFolder(project.Id, @"C:\Projects\Alpha", ProjectFolderType.Working);
        var nested = service.AddFolder(project.Id, @"C:\Projects\Alpha\Renders", ProjectFolderType.Asset);

        Assert.True(nested.Added);
        Assert.NotNull(nested.Warning);
        Assert.Equal(2, project.Folders.Count);
    }

    [Fact]
    public void AddFolder_AllowsSamePathInDifferentProjects()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var alpha = service.CreateProject("Alpha");
        var beta = service.CreateProject("Beta");

        Assert.True(service.AddFolder(alpha.Id, @"C:\Shared\Assets", ProjectFolderType.Asset).Added);
        Assert.True(service.AddFolder(beta.Id, @"C:\Shared\Assets", ProjectFolderType.Asset).Added);
    }

    [Fact]
    public void UpdateFolderScan_OfflineResult_PreservesCachedSize()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");
        var folder = service.AddFolder(project.Id, @"E:\Backups\Alpha", ProjectFolderType.Backup).Folder!;

        service.UpdateFolderScan(project.Id, folder.Id, new FolderMeasureResult(12345, FolderScanState.Scanned, null));
        var scannedAt = folder.LastMeasuredUtc;
        Assert.Equal(12345, folder.LastMeasuredBytes);

        service.UpdateFolderScan(project.Id, folder.Id, new FolderMeasureResult(0, FolderScanState.Offline, "Drive is not connected."));

        Assert.Equal(12345, folder.LastMeasuredBytes);
        Assert.Equal(scannedAt, folder.LastMeasuredUtc);
        Assert.Equal(FolderScanState.Offline, folder.LastScanState);

        var reloaded = new ProjectOrganiserService(_storagePath);
        var loadedFolder = Assert.Single(Assert.Single(reloaded.Projects).Folders);
        Assert.Equal(12345, loadedFolder.LastMeasuredBytes);
        Assert.Equal(FolderScanState.Offline, loadedFolder.LastScanState);
    }

    [Fact]
    public void UpdateFolderScan_ErrorResult_PreservesCachedSize()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");
        var folder = service.AddFolder(project.Id, @"C:\Projects\Alpha", ProjectFolderType.Working).Folder!;

        service.UpdateFolderScan(project.Id, folder.Id, new FolderMeasureResult(999, FolderScanState.Scanned, null));
        service.UpdateFolderScan(project.Id, folder.Id, new FolderMeasureResult(0, FolderScanState.Error, "boom"));

        Assert.Equal(999, folder.LastMeasuredBytes);
        Assert.Equal(FolderScanState.Error, folder.LastScanState);
        Assert.Equal("boom", folder.LastError);
    }

    [Fact]
    public void UpdateFolderScan_MissingResult_UpdatesToZero()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");
        var folder = service.AddFolder(project.Id, @"C:\Projects\Alpha", ProjectFolderType.Working).Folder!;

        service.UpdateFolderScan(project.Id, folder.Id, new FolderMeasureResult(999, FolderScanState.Scanned, null));
        service.UpdateFolderScan(project.Id, folder.Id, new FolderMeasureResult(0, FolderScanState.Missing, "gone"));

        Assert.Equal(0, folder.LastMeasuredBytes);
        Assert.Equal(FolderScanState.Missing, folder.LastScanState);
    }

    [Fact]
    public void AddFolder_DefaultsToUntagged_WhenTypeOmitted()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");

        var result = service.AddFolder(project.Id, @"C:\Projects\Alpha");

        Assert.True(result.Added);
        Assert.Equal(ProjectFolderType.Untagged, result.Folder!.FolderType);

        var reloaded = new ProjectOrganiserService(_storagePath);
        Assert.Equal(ProjectFolderType.Untagged, Assert.Single(Assert.Single(reloaded.Projects).Folders).FolderType);
    }

    [Fact]
    public void SetFolderType_PersistsChange()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");
        var folder = service.AddFolder(project.Id, @"C:\Projects\Alpha").Folder!;

        Assert.Equal(ProjectFolderType.Untagged, folder.FolderType);
        Assert.True(service.SetFolderType(project.Id, folder.Id, ProjectFolderType.Backup));

        var reloaded = new ProjectOrganiserService(_storagePath);
        Assert.Equal(ProjectFolderType.Backup, Assert.Single(Assert.Single(reloaded.Projects).Folders).FolderType);
    }

    [Fact]
    public void SetFolderType_CanClearBackToUntagged()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");
        var folder = service.AddFolder(project.Id, @"C:\Projects\Alpha", ProjectFolderType.Working).Folder!;

        Assert.True(service.SetFolderType(project.Id, folder.Id, ProjectFolderType.Untagged));
        Assert.Equal(ProjectFolderType.Untagged, folder.FolderType);
    }

    [Fact]
    public void RemoveFolder_RemovesOnlyThatFolder()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");
        var keep = service.AddFolder(project.Id, @"C:\Projects\Alpha", ProjectFolderType.Working).Folder!;
        var remove = service.AddFolder(project.Id, @"E:\Backups\Alpha", ProjectFolderType.Backup).Folder!;

        Assert.True(service.RemoveFolder(project.Id, remove.Id));
        Assert.Equal(keep.Id, Assert.Single(project.Folders).Id);
    }

    [Fact]
    public void RemoveFolders_RemovesMatchingIdsOnce_AndPersists()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var alpha = service.CreateProject("Alpha");
        var beta = service.CreateProject("Beta");
        var keep = service.AddFolder(alpha.Id, @"C:\Projects\Alpha", ProjectFolderType.Working).Folder!;
        var removeOne = service.AddFolder(alpha.Id, @"D:\Assets\Alpha", ProjectFolderType.Asset).Folder!;
        var removeTwo = service.AddFolder(alpha.Id, @"E:\Backups\Alpha", ProjectFolderType.Backup).Folder!;
        var otherProjectFolder = service.AddFolder(beta.Id, @"C:\Projects\Beta", ProjectFolderType.Working).Folder!;

        var removed = service.RemoveFolders(
            alpha.Id,
            [removeOne.Id, removeTwo.Id, Guid.NewGuid()]);

        Assert.Equal(2, removed);
        Assert.Equal(keep.Id, Assert.Single(alpha.Folders).Id);
        Assert.Equal(otherProjectFolder.Id, Assert.Single(beta.Folders).Id);

        var reloaded = new ProjectOrganiserService(_storagePath);
        var reloadedAlpha = Assert.Single(reloaded.Projects, project => project.Id == alpha.Id);
        var reloadedBeta = Assert.Single(reloaded.Projects, project => project.Id == beta.Id);
        Assert.Equal(keep.Id, Assert.Single(reloadedAlpha.Folders).Id);
        Assert.Equal(otherProjectFolder.Id, Assert.Single(reloadedBeta.Folders).Id);
    }

    [Fact]
    public void RemoveFolders_UnknownProjectOrIds_ReturnsZero()
    {
        var service = new ProjectOrganiserService(_storagePath);
        var project = service.CreateProject("Alpha");
        var folder = service.AddFolder(project.Id, @"C:\Projects\Alpha").Folder!;

        Assert.Equal(0, service.RemoveFolders(Guid.NewGuid(), [folder.Id]));
        Assert.Equal(0, service.RemoveFolders(project.Id, [Guid.NewGuid()]));
        Assert.Equal(folder.Id, Assert.Single(project.Folders).Id);
    }

    [Fact]
    public void Load_CorruptFile_StartsEmptyInsteadOfThrowing()
    {
        File.WriteAllText(_storagePath, "{not valid json");
        var service = new ProjectOrganiserService(_storagePath);
        Assert.Empty(service.Projects);
    }
}
