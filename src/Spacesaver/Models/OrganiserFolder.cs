namespace Spacesaver.Models;

/// <summary>A folder associated with an organiser project. Metadata only — never a file operation target.</summary>
public sealed class OrganiserFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Path { get; set; } = string.Empty;
    public ProjectFolderType FolderType { get; set; } = ProjectFolderType.Untagged;
    public long? LastMeasuredBytes { get; set; }
    public DateTimeOffset? LastMeasuredUtc { get; set; }
    /// <summary>Filesystem creation time when last read successfully; null if never captured.</summary>
    public DateTimeOffset? CreatedUtc { get; set; }
    /// <summary>Filesystem last-write time when last read successfully; null if never captured.</summary>
    public DateTimeOffset? ModifiedUtc { get; set; }
    public FolderScanState LastScanState { get; set; } = FolderScanState.NotScanned;
    public string? LastError { get; set; }
}
