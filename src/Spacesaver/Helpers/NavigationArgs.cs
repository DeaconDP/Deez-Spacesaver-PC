using Microsoft.UI.Xaml.Controls;
using Spacesaver.Models;

namespace Spacesaver.Helpers;

public sealed class TaskGuideNavigationArgs
{
    public TaskCategory? InitialCategory { get; init; }
}

public sealed class TaskListNavigationArgs
{
    public TaskCategory? Category { get; init; }
    public string Title { get; init; } = "All Tasks";
}
