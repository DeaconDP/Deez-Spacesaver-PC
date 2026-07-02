using Microsoft.UI.Xaml.Controls;
using Spacesaver.Models;
using Spacesaver.Pages;
using Spacesaver.Services;

namespace Spacesaver.Helpers;

public static class NavigationHelper
{
    public static void NavigateToTaskGuide(Page page, TaskCategory? category = null)
    {
        if (page.Frame is not Frame frame)
            return;

        NavigationService.Instance.Navigate(
            typeof(TaskGuidePage),
            category is null ? null : new TaskGuideNavigationArgs { InitialCategory = category },
            "tasks");
    }

    public static void NavigateToTaskDetail(Page page, string taskId)
    {
        if (page.Frame is Frame frame)
            frame.Navigate(typeof(TaskDetailPage), taskId);
    }

    public static void NavigateToCleanup(Page page)
    {
        NavigationService.Instance.Navigate(typeof(CleanupWizardPage), null, "cleanup");
    }

    public static void NavigateToStorageExplorer(Page page, string? path = null)
    {
        NavigationService.Instance.Navigate(typeof(StorageExplorerPage), path, "storage");
    }

    public static void NavigateToDashboard(Page page)
    {
        NavigationService.Instance.Navigate(typeof(DashboardPage), null, "dashboard");
    }

    [Obsolete("Use NavigateToTaskGuide")]
    public static void NavigateToTaskList(Page page, TaskCategory? category, string title)
    {
        NavigateToTaskGuide(page, category);
    }

    [Obsolete("Use NavigateToCleanup")]
    public static void NavigateToRecommendations(Page page) => NavigateToCleanup(page);

    [Obsolete("Use NavigateToStorageExplorer")]
    public static void NavigateToFolderOrganizer(Page page) => NavigateToStorageExplorer(page);
}
