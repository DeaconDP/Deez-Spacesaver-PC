using Microsoft.UI.Xaml.Media;
using Spacesaver.Models;

namespace Spacesaver.Helpers;

public static class OrganiserDisplayHelper
{
    public static string GetTypeLabel(ProjectFolderType type) =>
        ProjectFolderTypeHelper.GetLabel(type);

    public static Windows.UI.Color GetTypeColor(ProjectFolderType type) => type switch
    {
        ProjectFolderType.Untagged => Windows.UI.Color.FromArgb(255, 139, 144, 160),
        ProjectFolderType.Working => SemanticColorHelper.GoodAccent,
        ProjectFolderType.Parent => Windows.UI.Color.FromArgb(255, 120, 190, 160),
        ProjectFolderType.Asset => SemanticColorHelper.InformativeAccent,
        ProjectFolderType.Backup => SemanticColorHelper.ImportantAccent,
        ProjectFolderType.Junk => SemanticColorHelper.AttentionAccent,
        ProjectFolderType.Template => Windows.UI.Color.FromArgb(255, 64, 196, 255),
        ProjectFolderType.Experiment => Windows.UI.Color.FromArgb(255, 200, 220, 80),
        ProjectFolderType.Other => Windows.UI.Color.FromArgb(255, 180, 160, 120),
        _ => Windows.UI.Color.FromArgb(255, 139, 144, 160)
    };

    public static Brush GetTypeBrush(ProjectFolderType type) =>
        new SolidColorBrush(GetTypeColor(type));

    public static Brush GetTypeTintBrush(ProjectFolderType type)
    {
        var color = GetTypeColor(type);
        return new SolidColorBrush(Windows.UI.Color.FromArgb(28, color.R, color.G, color.B));
    }

    public static string? GetStateLabel(FolderScanState state) => state switch
    {
        FolderScanState.NotScanned => "Not scanned",
        FolderScanState.Partial => "Partial",
        FolderScanState.Offline => "Offline — cached size",
        FolderScanState.Missing => "Missing",
        FolderScanState.Error => "Error",
        _ => null
    };

    public static Brush GetStateBrush(FolderScanState state) => state switch
    {
        FolderScanState.Partial => SemanticColorHelper.GetForegroundBrush(SemanticTone.Important),
        FolderScanState.Offline => SemanticColorHelper.GetForegroundBrush(SemanticTone.Attention),
        FolderScanState.Missing => SemanticColorHelper.GetForegroundBrush(SemanticTone.Important),
        FolderScanState.Error => SemanticColorHelper.GetForegroundBrush(SemanticTone.Attention),
        _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 139, 144, 160))
    };

    public static string FormatRelativeTime(DateTimeOffset? utc)
    {
        if (utc is not { } time)
            return "never scanned";

        return $"scanned {FormatRelativeAgeCore(time)}";
    }

    /// <summary>Relative age without a verb prefix (e.g. "2 days ago", "12 Jan 2024").</summary>
    public static string FormatRelativeAge(DateTimeOffset? utc)
    {
        if (utc is not { } time)
            return "unknown";

        return FormatRelativeAgeCore(time);
    }

    private static string FormatRelativeAgeCore(DateTimeOffset time)
    {
        var elapsed = DateTimeOffset.UtcNow - time;

        if (elapsed < TimeSpan.FromMinutes(1))
            return "just now";
        if (elapsed < TimeSpan.FromHours(1))
            return $"{(int)elapsed.TotalMinutes} min ago";
        if (elapsed < TimeSpan.FromHours(24))
            return $"{(int)elapsed.TotalHours} h ago";
        if (elapsed < TimeSpan.FromDays(2))
            return "yesterday";
        if (elapsed < TimeSpan.FromDays(30))
            return $"{(int)elapsed.TotalDays} days ago";

        return time.ToLocalTime().ToString("d MMM yyyy");
    }
}
