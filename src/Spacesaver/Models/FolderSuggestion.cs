namespace Spacesaver.Models;

public sealed record FolderSuggestion(
    Guid ProjectId,
    string ProjectName,
    string Path,
    string Reason,
    bool IsSibling);
