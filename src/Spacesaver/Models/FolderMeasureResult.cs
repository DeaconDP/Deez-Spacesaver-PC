namespace Spacesaver.Models;

public sealed record FolderMeasureResult(long Bytes, FolderScanState State, string? Error);
