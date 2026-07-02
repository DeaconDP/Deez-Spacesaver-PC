using Spacesaver.Helpers;
using Spacesaver.Models;

namespace Spacesaver.Services;

public static class FolderClassifier
{
    private static readonly HashSet<string> CriticalRootNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows",
        "Program Files",
        "Program Files (x86)",
        "ProgramData",
        "$Recycle.Bin",
        "System Volume Information"
    };

    private static readonly HashSet<string> ImportantRootNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Users",
        "Recovery"
    };

    private static readonly HashSet<string> ImportantProfileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Documents",
        "Pictures",
        "Desktop",
        "Music",
        "Videos",
        "OneDrive"
    };

    public static (FolderImportance Importance, string Reason, string? RelatedTaskId) Classify(string folderPath)
    {
        var normalized = NormalizePath(folderPath);
        var name = Path.GetFileName(normalized.TrimEnd('\\'));
        var parent = Path.GetDirectoryName(normalized.TrimEnd('\\'));

        var relatedTaskId = TryGetRelatedTaskId(normalized);

        if (IsReclaimableName(name) || name.Equals("Windows.old", StringComparison.OrdinalIgnoreCase))
        {
            return (FolderImportance.Reclaimable,
                "Known cleanup or temporary location — review before removing.",
                relatedTaskId);
        }

        if (parent is not null)
        {
            var parentName = Path.GetFileName(parent.TrimEnd('\\'));

            if (parentName.Equals("Users", StringComparison.OrdinalIgnoreCase))
            {
                return (FolderImportance.Important,
                    "User profile folder — contains personal files and app data.",
                    relatedTaskId);
            }

            if (IsUserProfilePath(parent))
            {
                if (name.Equals("Downloads", StringComparison.OrdinalIgnoreCase))
                {
                    return (FolderImportance.Review,
                        "Downloads often holds installers and old files worth reviewing.",
                        relatedTaskId ?? "downloads");
                }

                if (name.Equals("AppData", StringComparison.OrdinalIgnoreCase))
                {
                    return (FolderImportance.Critical,
                        "Application data — required by programs; some caches inside may be reclaimable via cleanup tasks.",
                        relatedTaskId);
                }

                if (ImportantProfileNames.Contains(name))
                {
                    return (FolderImportance.Important,
                        "Personal files — keep unless you have backups elsewhere.",
                        relatedTaskId);
                }

                return (FolderImportance.Review,
                    "User folder — inspect contents in Explorer before removing anything.",
                    relatedTaskId);
            }
        }

        if (CriticalRootNames.Contains(name))
        {
            return (FolderImportance.Critical,
                "Operating system or installed applications — do not delete.",
                relatedTaskId);
        }

        if (ImportantRootNames.Contains(name))
        {
            return (FolderImportance.Important,
                name.Equals("Users", StringComparison.OrdinalIgnoreCase)
                    ? "All user profiles and personal data live here."
                    : "System recovery data — keep unless you know you do not need it.",
                relatedTaskId);
        }

        return (FolderImportance.Review,
            "Unknown folder — open in Explorer to decide if it is still needed.",
            relatedTaskId);
    }

    public static string? TryGetRelatedTaskId(string folderPath)
    {
        var normalized = NormalizePath(folderPath);
        var name = Path.GetFileName(normalized.TrimEnd('\\'));

        if (name.Equals("Windows.old", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(@"\Windows.old", StringComparison.OrdinalIgnoreCase))
            return "windows-old";

        if (name.Equals("$Recycle.Bin", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(@"\$Recycle.Bin", StringComparison.OrdinalIgnoreCase))
            return "recycle-bin";

        if (name.Equals("Downloads", StringComparison.OrdinalIgnoreCase)
            && IsUserProfilePath(Path.GetDirectoryName(normalized.TrimEnd('\\')) ?? string.Empty))
            return "downloads";

        var tempPath = NormalizePath(Environment.GetEnvironmentVariable("TEMP") ?? string.Empty);
        if (!string.IsNullOrEmpty(tempPath)
            && (normalized.Equals(tempPath, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(tempPath + "\\", StringComparison.OrdinalIgnoreCase)))
            return "user-temp";

        if (normalized.Contains("DeliveryOptimization", StringComparison.OrdinalIgnoreCase)
            && normalized.Contains("Cache", StringComparison.OrdinalIgnoreCase))
            return "delivery-optimization";

        if (IsBrowserCachePath(normalized))
            return "browser-cache";

        return null;
    }

    private static bool IsReclaimableName(string name)
    {
        if (name.Contains("cache", StringComparison.OrdinalIgnoreCase)
            || name.Contains("temp", StringComparison.OrdinalIgnoreCase)
            || name.Contains("tmp", StringComparison.OrdinalIgnoreCase))
            return true;

        return name.Contains("DeliveryOptimization", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBrowserCachePath(string path)
    {
        return path.Contains(@"\Cache", StringComparison.OrdinalIgnoreCase)
            && (path.Contains(@"Microsoft\Edge", StringComparison.OrdinalIgnoreCase)
                || path.Contains(@"Google\Chrome", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsUserProfilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var normalized = NormalizePath(path);
        var usersRoot = NormalizePath(Path.Combine(FileSystemHelper.GetSystemDriveRoot(), "Users"));

        if (!normalized.StartsWith(usersRoot, StringComparison.OrdinalIgnoreCase))
            return false;

        var relative = normalized[usersRoot.Length..].TrimStart('\\');
        if (string.IsNullOrEmpty(relative))
            return false;

        var segments = relative.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 1;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        try
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path))
                .TrimEnd('\\');
        }
        catch
        {
            return path.TrimEnd('\\');
        }
    }
}
