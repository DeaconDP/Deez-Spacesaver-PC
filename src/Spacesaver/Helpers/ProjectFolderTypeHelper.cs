using Spacesaver.Models;

namespace Spacesaver.Helpers;

/// <summary>UI-free labels for folder types (shared by display and sort).</summary>
public static class ProjectFolderTypeHelper
{
    public static string GetLabel(ProjectFolderType type) => type switch
    {
        ProjectFolderType.Untagged => "Untagged",
        ProjectFolderType.Working => "Working",
        ProjectFolderType.Parent => "Parent",
        ProjectFolderType.Asset => "Assets",
        ProjectFolderType.Backup => "Backup",
        ProjectFolderType.Junk => "Junk",
        ProjectFolderType.Template => "Template",
        ProjectFolderType.Experiment => "Experiment",
        ProjectFolderType.Other => "Other",
        _ => "Other"
    };
}
