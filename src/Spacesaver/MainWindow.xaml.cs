using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Pages;
using Spacesaver.Services;

namespace Spacesaver;

public sealed partial class MainWindow : Window
{
    private bool _initialNavigationDone;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");

        NavigationService.Instance.Initialize(NavView, NavFrame);
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialNavigationDone)
            return;

        if (!AppServices.Settings.HasCompletedOnboarding)
        {
            _initialNavigationDone = true;
            NavFrame.Navigate(typeof(WelcomePage));
            return;
        }

        _initialNavigationDone = true;
        NavigationService.Instance.Navigate(typeof(DashboardPage), null, "dashboard");
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

        if (e.SourcePageType is Type pageType)
        {
            var tag = NavigationService.GetTagForPageType(pageType);
            if (tag is not null)
                NavigationService.Instance.SyncNavSelection(tag);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (!_initialNavigationDone)
            return;

        if (!NavigationService.Instance.ShouldHandleSelectionChanged())
            return;

        if (args.IsSettingsSelected)
        {
            NavigationService.Instance.Navigate(typeof(SettingsPage), null, "settings");
            return;
        }

        if (args.SelectedItem is not NavigationViewItem item)
            return;

        var tag = item.Tag as string;
        var pageType = NavigationService.GetPageTypeForTag(tag ?? string.Empty);
        if (pageType is null)
            return;

        NavigationService.Instance.Navigate(pageType, null, tag);
    }
}
