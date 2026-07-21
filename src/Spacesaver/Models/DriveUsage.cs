namespace Spacesaver.Models;

/// <summary>Cached bytes a project occupies on one drive root (e.g. "C:\").</summary>
public sealed record DriveUsage(string Root, long Bytes);
