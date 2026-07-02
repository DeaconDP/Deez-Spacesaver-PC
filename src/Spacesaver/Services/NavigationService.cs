using Microsoft.UI.Xaml.Controls;
using Spacesaver.Pages;

namespace Spacesaver.Services;

public sealed class NavigationService
{
    private NavigationView? _navView;
    private Frame? _frame;
    private bool _suppressSelectionSync;

    public static NavigationService Instance { get; } = new();

    public void Initialize(NavigationView navView, Frame frame)
    {
        _navView = navView;
        _frame = frame;
    }

    public bool Navigate(Type pageType, object? parameter = null, string? navTag = null)
    {
        if (_frame is null)
            return false;

        if (!_frame.Navigate(pageType, parameter))
            return false;

        if (navTag is not null)
            SyncNavSelection(navTag);

        return true;
    }

    public void SyncNavSelection(string tag)
    {
        if (_navView is null)
            return;

        if (tag == "settings")
        {
            _suppressSelectionSync = true;
            _navView.SelectedItem = null;
            _suppressSelectionSync = false;
            return;
        }

        foreach (var item in _navView.MenuItems)
        {
            if (item is not NavigationViewItem navItem)
                continue;

            if (!string.Equals(navItem.Tag as string, tag, StringComparison.Ordinal))
                continue;

            _suppressSelectionSync = true;
            _navView.SelectedItem = navItem;
            _suppressSelectionSync = false;
            return;
        }
    }

    public bool ShouldHandleSelectionChanged() => !_suppressSelectionSync;

    public static Type? GetPageTypeForTag(string tag) => tag switch
    {
        "dashboard" => typeof(DashboardPage),
        "cleanup" => typeof(CleanupWizardPage),
        "storage" => typeof(StorageExplorerPage),
        "tasks" => typeof(TaskGuidePage),
        _ => null
    };

    public static string? GetTagForPageType(Type pageType)
    {
        if (pageType == typeof(DashboardPage))
            return "dashboard";
        if (pageType == typeof(CleanupWizardPage))
            return "cleanup";
        if (pageType == typeof(StorageExplorerPage))
            return "storage";
        if (pageType == typeof(TaskGuidePage))
            return "tasks";
        if (pageType == typeof(SettingsPage))
            return "settings";

        return null;
    }
}
