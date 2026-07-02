using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _themeUiReady;

    public SettingsPage()
    {
        InitializeComponent();
        AppServices.Progress.ProgressChanged += RefreshSummary;
        Unloaded += (_, _) => AppServices.Progress.ProgressChanged -= RefreshSummary;

        AnimationHelper.WirePageLoaded(
            this,
            [PageHeaderControl, ProgressCard, SafetyCard],
            [ProgressCard, SafetyCard]);
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshSummary();
        LoadThemeSelection();
        SafetyInfoToggle.IsOn = !AppServices.Settings.SafetyInfoBarDismissed;
    }

    private void LoadThemeSelection()
    {
        _themeUiReady = false;
        var preference = AppServices.Settings.ThemePreference;
        foreach (var child in ThemeRadioButtons.Items)
        {
            if (child is RadioButton radio &&
                string.Equals(radio.Tag as string, preference.ToString(), StringComparison.Ordinal))
            {
                radio.IsChecked = true;
                break;
            }
        }

        _themeUiReady = true;
    }

    private void RefreshSummary() =>
        ProgressSummaryText.Text =
            $"{AppServices.Progress.CompletedCount} of {AppServices.Progress.TotalTasks} tasks marked complete";

    private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_themeUiReady)
            return;

        if (ThemeRadioButtons.SelectedItem is not RadioButton radio)
            return;

        if (!Enum.TryParse<ThemePreference>(radio.Tag as string, out var preference))
            return;

        AppServices.Settings.ThemePreference = preference;
        ThemeHelper.ApplyTheme(preference);
    }

    private void SafetyInfoToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        AppServices.Settings.SafetyInfoBarDismissed = !SafetyInfoToggle.IsOn;
    }

    private void ReplayOnboarding_Click(object sender, RoutedEventArgs e)
    {
        AppServices.Settings.HasCompletedOnboarding = false;
        if (Frame is Frame frame)
            frame.Navigate(typeof(WelcomePage));
    }

    private async void ResetProgress_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Reset progress?",
            Content = "This clears all completed task checkmarks. Your files are not affected.",
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            AppServices.Progress.Reset();
            AnimationHelper.Pulse(ProgressCard);
        }
    }
}
