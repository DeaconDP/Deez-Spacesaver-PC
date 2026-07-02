namespace Spacesaver.Helpers;

public readonly struct DirectorySizeResult
{
    public long Bytes { get; init; }
    public bool IsPartial { get; init; }
    public bool IsInaccessible { get; init; }
}

public static class FileSystemHelper
{
    public static long MeasureDirectoryBytes(string? path, CancellationToken cancellationToken = default) =>
        MeasureDirectory(path, cancellationToken).Bytes;

    public static DirectorySizeResult MeasureDirectory(string? path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return new DirectorySizeResult { Bytes = 0, IsInaccessible = true };

        long total = 0;
        var hadErrors = false;

        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                    hadErrors = true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new DirectorySizeResult { Bytes = total, IsPartial = true, IsInaccessible = total == 0 };
        }

        return new DirectorySizeResult
        {
            Bytes = total,
            IsPartial = hadErrors,
            IsInaccessible = false
        };
    }

    public static int CountChildFolders(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return 0;

        try
        {
            return Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly).Count();
        }
        catch
        {
            return 0;
        }
    }

    public static IReadOnlyList<string> EnumerateChildFolderPaths(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return [];

        try
        {
            return Directory
                .EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";

        // First division moves bytes into KB; the loop continues through MB, GB, and TB.
        double value = bytes / 1024.0;
        string[] units = ["KB", "MB", "GB", "TB"];
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex <= 1
            ? $"{value:0} {units[unitIndex]}"
            : $"{value:0.##} {units[unitIndex]}";
    }

    public static string GetSystemDriveRoot()
    {
        var root = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
        return root.EndsWith('\\') ? root : root + "\\";
    }
}
