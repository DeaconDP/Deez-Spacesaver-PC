namespace Spacesaver.ViewModels;

/// <summary>UI-only section header between folder groups when sorting by tag.</summary>
public sealed class OrganiserFolderSectionHeader(string title)
{
    public string Title { get; } = title;
}
