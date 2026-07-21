using System.Text.Json;
using System.Text.Json.Serialization;
using Spacesaver.Helpers;
using Spacesaver.Models;

namespace Spacesaver.Services;

public sealed record AddFolderResult(bool Added, OrganiserFolder? Folder, string? Warning, string? Error);

/// <summary>
/// Persists project-to-folder associations in %LocalAppData%\PCSpacesaver\projects.json.
/// Metadata only — this service never touches the associated folders themselves.
/// </summary>
public sealed class ProjectOrganiserService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _storagePath;
    private readonly List<OrganiserProject> _projects = [];

    public event Action? ProjectsChanged;

    public ProjectOrganiserService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PCSpacesaver",
            "projects.json"))
    {
    }

    public ProjectOrganiserService(string storagePath)
    {
        _storagePath = storagePath;
        Load();
    }

    public IReadOnlyList<OrganiserProject> Projects => _projects;

    public OrganiserProject CreateProject(string name)
    {
        var project = new OrganiserProject { Name = name.Trim() };
        _projects.Add(project);
        Persist();
        return project;
    }

    public bool RenameProject(Guid projectId, string newName)
    {
        var project = FindProject(projectId);
        if (project is null)
            return false;

        project.Name = newName.Trim();
        Persist();
        return true;
    }

    public bool RemoveProject(Guid projectId)
    {
        var project = FindProject(projectId);
        if (project is null)
            return false;

        _projects.Remove(project);
        Persist();
        return true;
    }

    public AddFolderResult AddFolder(
        Guid projectId,
        string path,
        ProjectFolderType folderType = ProjectFolderType.Untagged)
    {
        var project = FindProject(projectId);
        if (project is null)
            return new AddFolderResult(false, null, null, "Project not found.");

        string normalized;
        try
        {
            normalized = OrganiserPathHelper.NormalizePath(path);
        }
        catch (Exception)
        {
            return new AddFolderResult(false, null, null, "That path is not valid.");
        }

        if (project.Folders.Any(f => string.Equals(f.Path, normalized, StringComparison.OrdinalIgnoreCase)))
            return new AddFolderResult(false, null, null, "That folder is already part of this project.");

        var overlapping = project.Folders
            .FirstOrDefault(f => OrganiserPathHelper.Overlaps(f.Path, normalized));

        var folder = new OrganiserFolder
        {
            Path = normalized,
            FolderType = folderType
        };
        OrganiserFolderFsMetadataHelper.TryApply(folder);
        project.Folders.Add(folder);
        Persist();

        var warning = overlapping is null
            ? null
            : $"Overlaps \"{overlapping.Path}\" — the project total will double-count this data.";

        return new AddFolderResult(true, folder, warning, null);
    }

    public bool RemoveFolder(Guid projectId, Guid folderId)
    {
        var project = FindProject(projectId);
        var folder = project?.Folders.FirstOrDefault(f => f.Id == folderId);
        if (project is null || folder is null)
            return false;

        project.Folders.Remove(folder);
        Persist();
        return true;
    }

    public int RemoveFolders(Guid projectId, IReadOnlyCollection<Guid> folderIds)
    {
        var project = FindProject(projectId);
        if (project is null || folderIds.Count == 0)
            return 0;

        var ids = folderIds as HashSet<Guid> ?? folderIds.ToHashSet();
        var removedCount = project.Folders.RemoveAll(folder => ids.Contains(folder.Id));
        if (removedCount > 0)
            Persist();

        return removedCount;
    }

    public bool SetFolderType(Guid projectId, Guid folderId, ProjectFolderType folderType)
    {
        var folder = FindProject(projectId)?.Folders.FirstOrDefault(f => f.Id == folderId);
        if (folder is null)
            return false;

        folder.FolderType = folderType;
        Persist();
        return true;
    }

    /// <summary>
    /// Records a scan result. Offline/error results keep the last good cached size
    /// so a disconnected USB drive doesn't wipe what we know about it.
    /// </summary>
    public bool UpdateFolderScan(Guid projectId, Guid folderId, FolderMeasureResult result)
    {
        var folder = FindProject(projectId)?.Folders.FirstOrDefault(f => f.Id == folderId);
        if (folder is null)
            return false;

        var preserveCache = result.State is FolderScanState.Offline or FolderScanState.Error
            && folder.LastMeasuredBytes is not null;

        if (!preserveCache)
        {
            folder.LastMeasuredBytes = result.Bytes;
            folder.LastMeasuredUtc = DateTimeOffset.UtcNow;
            OrganiserFolderFsMetadataHelper.TryApply(folder);
        }

        folder.LastScanState = result.State;
        folder.LastError = result.Error;

        Persist();
        return true;
    }

    /// <summary>
    /// Lightweight pass: refresh filesystem created/modified times for folders that are currently readable.
    /// Skips offline/missing paths and leaves last-known dates in place.
    /// </summary>
    public int RefreshFolderTimestamps()
    {
        var changed = 0;
        foreach (var project in _projects)
        {
            foreach (var folder in project.Folders)
            {
                if (OrganiserFolderFsMetadataHelper.TryApply(folder))
                    changed++;
            }
        }

        if (changed > 0)
            Persist();

        return changed;
    }

    private OrganiserProject? FindProject(Guid projectId) =>
        _projects.FirstOrDefault(p => p.Id == projectId);

    private void Load()
    {
        if (!File.Exists(_storagePath))
            return;

        try
        {
            var json = File.ReadAllText(_storagePath);
            var projects = JsonSerializer.Deserialize<List<OrganiserProject>>(json, JsonOptions);
            if (projects is not null)
                _projects.AddRange(projects);
        }
        catch
        {
            _projects.Clear();
        }
    }

    private void Persist()
    {
        Save();
        ProjectsChanged?.Invoke();
    }

    private void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_projects, JsonOptions);
            var tempPath = _storagePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _storagePath, overwrite: true);
        }
        catch
        {
            // Ignore persistence failures; associations stay in memory for this session.
        }
    }
}
