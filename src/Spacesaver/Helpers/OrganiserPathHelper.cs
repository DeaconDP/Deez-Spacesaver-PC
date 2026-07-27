namespace Spacesaver.Helpers;

public static class OrganiserPathHelper
{
    /// <summary>Full path with trailing separators removed (drive roots keep theirs, e.g. "C:\").</summary>
    public static string NormalizePath(string path)
    {
        var full = Path.GetFullPath(path.Trim());
        var root = Path.GetPathRoot(full);

        if (!string.IsNullOrEmpty(root) && string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
            return root;

        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static bool PathsEqual(string a, string b) =>
        string.Equals(NormalizePath(a), NormalizePath(b), StringComparison.OrdinalIgnoreCase);

    public static string? GetDriveRoot(string path)
    {
        try
        {
            return Path.GetPathRoot(Path.GetFullPath(path));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Short display label for a drive root: "C:\" → "C:", UNC roots stay as-is.</summary>
    public static string GetDriveLabel(string? root) =>
        string.IsNullOrEmpty(root)
            ? "?"
            : root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>Leaf folder name for list display; drive roots keep their drive label.</summary>
    public static string GetFolderDisplayName(string path)
    {
        try
        {
            var normalized = NormalizePath(path);
            var root = GetDriveRoot(normalized);
            if (!string.IsNullOrEmpty(root) && PathsEqual(normalized, root))
                return GetDriveLabel(root);

            var name = Path.GetFileName(normalized);
            return string.IsNullOrEmpty(name) ? GetDriveLabel(root) : name;
        }
        catch
        {
            return path;
        }
    }

    /// <summary>True when <paramref name="candidate"/> equals or lives inside <paramref name="parent"/>.</summary>
    public static bool IsSameOrDescendant(string parent, string candidate)
    {
        var p = NormalizePath(parent);
        var c = NormalizePath(candidate);

        if (string.Equals(p, c, StringComparison.OrdinalIgnoreCase))
            return true;

        var prefix = p.EndsWith(Path.DirectorySeparatorChar) ? p : p + Path.DirectorySeparatorChar;
        return c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when either path contains the other — sizes would double-count.</summary>
    public static bool Overlaps(string a, string b) =>
        IsSameOrDescendant(a, b) || IsSameOrDescendant(b, a);
}
