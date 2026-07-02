namespace Spacesaver.Models;

public sealed class FolderEntry
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public long SizeBytes { get; set; }
    public double PercentOfParent { get; set; }
    public int ChildFolderCount { get; set; }
    public FolderImportance Importance { get; set; }
    public string ClassificationReason { get; set; } = string.Empty;
    public FolderScanState ScanState { get; set; } = FolderScanState.Pending;
    public string? RelatedTaskId { get; set; }
}
