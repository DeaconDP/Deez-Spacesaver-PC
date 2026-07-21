using Spacesaver.Models;

namespace Spacesaver.Helpers;

/// <summary>Reads filesystem created/modified times for organiser folders without measuring size.</summary>
public static class OrganiserFolderFsMetadataHelper
{
    public readonly record struct FolderTimestamps(DateTimeOffset CreatedUtc, DateTimeOffset ModifiedUtc);

    public static bool TryRead(string path, out FolderTimestamps timestamps)
    {
        timestamps = default;

        try
        {
            var root = OrganiserPathHelper.GetDriveRoot(path);
            if (root is null || !IsDriveReady(root) || !Directory.Exists(path))
                return false;

            var info = new DirectoryInfo(path);
            timestamps = new FolderTimestamps(
                new DateTimeOffset(DateTime.SpecifyKind(info.CreationTimeUtc, DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(info.LastWriteTimeUtc, DateTimeKind.Utc)));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Updates <see cref="OrganiserFolder.CreatedUtc"/> / <see cref="OrganiserFolder.ModifiedUtc"/>
    /// when the path is readable. Returns true when values changed. Does not clear cached dates on failure.
    /// </summary>
    public static bool TryApply(OrganiserFolder folder)
    {
        if (!TryRead(folder.Path, out var timestamps))
            return false;

        if (folder.CreatedUtc == timestamps.CreatedUtc && folder.ModifiedUtc == timestamps.ModifiedUtc)
            return false;

        folder.CreatedUtc = timestamps.CreatedUtc;
        folder.ModifiedUtc = timestamps.ModifiedUtc;
        return true;
    }

    private static bool IsDriveReady(string root)
    {
        try
        {
            if (root.StartsWith(@"\\", StringComparison.Ordinal))
                return Directory.Exists(root);

            return new DriveInfo(root).IsReady;
        }
        catch
        {
            return false;
        }
    }
}
