namespace Spacesaver.Models;

public sealed record DriveSnapshot
{
    public required string Root { get; init; }
    public string? VolumeLabel { get; init; }
    public DriveType DriveType { get; init; }
    public bool IsReady { get; init; }
    public long TotalBytes { get; init; }
    public long FreeBytes { get; init; }
}
