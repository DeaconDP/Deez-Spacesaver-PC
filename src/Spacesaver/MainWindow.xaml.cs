using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Pages;
using Windows.Graphics;

namespace Spacesaver;

public sealed partial class MainWindow : Window
{
    private const int DefaultWidth = 1100;
    private const int DefaultHeight = 720;
    private const int MinWidth = 720;
    private const int MinHeight = 520;

    private bool _initialNavigationDone;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon(
            Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));

        ApplyWindowSizePolicy();
    }

    private void ApplyWindowSizePolicy()
    {
        AppWindow.Resize(new SizeInt32(DefaultWidth, DefaultHeight));
        WindowMinSizeHelper.SetMinSize(this, MinWidth, MinHeight);
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialNavigationDone)
            return;

        _initialNavigationDone = true;
        NavView.SelectedItem = NavView.MenuItems[0];
        NavFrame.Navigate(typeof(HomePage));
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (NavFrame.CanGoBack)
            NavFrame.GoBack();
    }

    private void NavFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        AppTitleBar.IsBackButtonVisible = NavFrame.CanGoBack;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (!_initialNavigationDone)
            return;

        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is not NavigationViewItem item)
            return;

        switch (item.Tag)
        {
            case "home":
                NavFrame.Navigate(typeof(HomePage));
                break;
            case "project-organiser":
                NavFrame.Navigate(typeof(ProjectOrganiserPage));
                break;
            case "all-tasks":
                NavFrame.Navigate(typeof(TaskListPage), new TaskListNavigationArgs { Title = "All Tasks" });
                break;
            case "quick-wins":
                NavFrame.Navigate(typeof(TaskListPage), new TaskListNavigationArgs
                {
                    Category = TaskCategory.QuickWins,
                    Title = "Quick Wins"
                });
                break;
            case "system-cleanup":
                NavFrame.Navigate(typeof(TaskListPage), new TaskListNavigationArgs
                {
                    Category = TaskCategory.SystemCleanup,
                    Title = "System Cleanup"
                });
                break;
            case "large-wins":
                NavFrame.Navigate(typeof(TaskListPage), new TaskListNavigationArgs
                {
                    Category = TaskCategory.LargeSpaceWins,
                    Title = "Large Space Wins"
                });
                break;
            case "manual-review":
                NavFrame.Navigate(typeof(TaskListPage), new TaskListNavigationArgs
                {
                    Category = TaskCategory.ManualReview,
                    Title = "Manual Review"
                });
                break;
        }
    }
}
