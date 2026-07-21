using Spacesaver.Helpers;
using Spacesaver.Models;

namespace Spacesaver.Services;

/// <summary>
/// Discovers folders that may belong on organiser projects (siblings, then shallow name match).
/// Read-only on disk — never attaches folders itself.
/// </summary>
public sealed class FolderSuggestionService
{
    public const int DefaultMaxResults = 50;
    public const int NameMatchMaxDepth = 3;

    private static readonly HashSet<string> SkippedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows",
        "Program Files",
        "Program Files (x86)",
        "$Recycle.Bin",
        "System Volume Information",
        "Recovery",
        "PerfLogs"
    };

    public IReadOnlyList<FolderSuggestion> FindSuggestions(
        IReadOnlyList<OrganiserProject> scopeProjects,
        IReadOnlyList<string> readyDriveRoots,
        IReadOnlyList<OrganiserProject> allProjects,
        bool excludeIfAttachedAnywhere,
        CancellationToken cancellationToken = default,
        int maxResults = DefaultMaxResults)
    {
        if (scopeProjects.Count == 0 || maxResults <= 0)
            return [];

        var results = new List<FolderSuggestion>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pass A — siblings (preferred order)
        foreach (var project in scopeProjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CollectSiblings(project, results, seen, maxResults, cancellationToken);
            if (results.Count >= maxResults)
                return results;
        }

        // Pass B — name match
        var exclusionProjects = excludeIfAttachedAnywhere ? allProjects : scopeProjects;
        var excludedPaths = BuildExcludedPaths(exclusionProjects);

        foreach (var project in scopeProjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (results.Count >= maxResults)
                break;

            CollectNameMatches(
                project,
                readyDriveRoots,
                excludedPaths,
                results,
                seen,
                maxResults,
                cancellationToken);
        }

        return results;
    }

    private static void CollectSiblings(
        OrganiserProject project,
        List<FolderSuggestion> results,
        HashSet<string> seen,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var attached = project.Folders
            .Select(f => SafeNormalize(f.Path))
            .Where(p => p is not null)
            .Cast<string>()
            .ToList();

        if (attached.Count == 0)
            return;

        foreach (var attachedPath in attached)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (results.Count >= maxResults)
                return;

            string parent;
            try
            {
                parent = Path.GetDirectoryName(attachedPath) ?? string.Empty;
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                continue;

            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateDirectories(parent);
            }
            catch
            {
                continue;
            }

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (results.Count >= maxResults)
                    return;

                var normalized = SafeNormalize(child);
                if (normalized is null || !seen.Add(normalized))
                    continue;

                if (attached.Any(a => OrganiserPathHelper.Overlaps(a, normalized)))
                    continue;

                results.Add(new FolderSuggestion(
                    project.Id,
                    project.Name,
                    normalized,
                    $"Sibling of {attachedPath}",
                    IsSibling: true));
            }
        }
    }

    private static void CollectNameMatches(
        OrganiserProject project,
        IReadOnlyList<string> readyDriveRoots,
        HashSet<string> excludedPaths,
        List<FolderSuggestion> results,
        HashSet<string> seen,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var name = NormalizeProjectName(project.Name);
        if (string.IsNullOrEmpty(name))
            return;

        var tokens = BuildMatchTokens(name);

        foreach (var root in readyDriveRoots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (results.Count >= maxResults)
                return;

            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                continue;

            WalkForNameMatch(
                root,
                depth: 0,
                project,
                tokens,
                name,
                excludedPaths,
                results,
                seen,
                maxResults,
                cancellationToken);
        }
    }

    private static void WalkForNameMatch(
        string directory,
        int depth,
        OrganiserProject project,
        IReadOnlyList<string> tokens,
        string projectName,
        HashSet<string> excludedPaths,
        List<FolderSuggestion> results,
        HashSet<string> seen,
        int maxResults,
        CancellationToken cancellationToken)
    {
        if (depth >= NameMatchMaxDepth || results.Count >= maxResults)
            return;

        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateDirectories(directory);
        }
        catch
        {
            return;
        }

        foreach (var child in children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (results.Count >= maxResults)
                return;

            string folderName;
            try
            {
                folderName = Path.GetFileName(child.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrEmpty(folderName) || SkippedDirectoryNames.Contains(folderName))
                continue;

            var normalized = SafeNormalize(child);
            if (normalized is null)
                continue;

            var alreadySeen = !seen.Add(normalized);
            if (!alreadySeen && MatchesName(folderName, tokens, projectName) && !IsExcluded(normalized, excludedPaths))
            {
                results.Add(new FolderSuggestion(
                    project.Id,
                    project.Name,
                    normalized,
                    $"Name matches \"{project.Name}\"",
                    IsSibling: false));
            }

            WalkForNameMatch(
                child,
                depth + 1,
                project,
                tokens,
                projectName,
                excludedPaths,
                results,
                seen,
                maxResults,
                cancellationToken);
        }
    }

    private static HashSet<string> BuildExcludedPaths(IReadOnlyList<OrganiserProject> projects)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in projects)
        {
            foreach (var folder in project.Folders)
            {
                var normalized = SafeNormalize(folder.Path);
                if (normalized is not null)
                    set.Add(normalized);
            }
        }

        return set;
    }

    private static bool IsExcluded(string candidate, HashSet<string> excludedPaths)
    {
        if (excludedPaths.Contains(candidate))
            return true;

        foreach (var excluded in excludedPaths)
        {
            if (OrganiserPathHelper.Overlaps(excluded, candidate))
                return true;
        }

        return false;
    }

    internal static string NormalizeProjectName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }

    internal static IReadOnlyList<string> BuildMatchTokens(string normalizedName)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            normalizedName,
            normalizedName.Replace(' ', '_'),
            normalizedName.Replace(' ', '-'),
            $"{normalizedName}_Backup",
            $"{normalizedName} Backup",
            $"{normalizedName}-Backup",
            $"{normalizedName}_Assets",
            $"{normalizedName} Assets",
            $"{normalizedName}-Assets",
            $"Backup of {normalizedName}",
            $"{normalizedName}_Working",
            $"{normalizedName} Working"
        };

        return tokens.ToList();
    }

    internal static bool MatchesName(string folderName, IReadOnlyList<string> tokens, string projectName)
    {
        foreach (var token in tokens)
        {
            if (folderName.Equals(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Short names: equals-only to avoid noise
        if (projectName.Length < 3)
            return false;

        return folderName.Contains(projectName, StringComparison.OrdinalIgnoreCase);
    }

    private static string? SafeNormalize(string path)
    {
        try
        {
            return OrganiserPathHelper.NormalizePath(path);
        }
        catch
        {
            return null;
        }
    }
}
