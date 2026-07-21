using Spacesaver.Helpers;
using Spacesaver.Models;

namespace Spacesaver.Services;

/// <summary>Aggregates cached folder sizes into project totals and per-drive breakdowns.</summary>
public static class ProjectSizeCalculator
{
    public static long TotalBytes(OrganiserProject project) =>
        project.Folders.Sum(f => f.LastMeasuredBytes ?? 0);

    public static IReadOnlyList<DriveUsage> ByDrive(OrganiserProject project)
    {
        var totals = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in project.Folders)
        {
            if (folder.LastMeasuredBytes is not { } bytes)
                continue;

            var root = OrganiserPathHelper.GetDriveRoot(folder.Path) ?? "?";
            totals[root] = totals.GetValueOrDefault(root) + bytes;
        }

        return totals
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new DriveUsage(pair.Key, pair.Value))
            .ToList();
    }
}
