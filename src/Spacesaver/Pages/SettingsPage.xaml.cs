using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Helpers;
using Spacesaver.Services;

namespace Spacesaver.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _resetBusy;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        AppServices.Progress.ProgressChanged += RefreshSummary;
        Unloaded += (_, _) => AppServices.Progress.ProgressChanged -= RefreshSummary;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshSummary();
        await AnimationHelper.PlayStaggerEntranceAsync([HeaderPanel, ProgressSection, SafetySection]);
        AnimationHelper.AttachHoverLift(ResetButton);
    }

    private void RefreshSummary()
    {
        ProgressSummaryText.Text =
            $"{AppServices.Progress.CompletedCount} of {AppServices.Progress.TotalTasks} tasks marked complete";
    }

    private void SetResetStatus(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            ResetStatusText.Visibility = Visibility.Collapsed;
            ResetStatusText.Text = string.Empty;
            return;
        }

        ResetStatusText.Text = message;
        ResetStatusText.Visibility = Visibility.Visible;
    }

    private async void ResetProgress_Click(object sender, RoutedEventArgs e)
    {
        if (_resetBusy)
            return;

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
        if (result != ContentDialogResult.Primary)
            return;

        _resetBusy = true;
        ResetButton.IsEnabled = false;
        SetResetStatus("Resetting…");

        try
        {
            AppServices.Progress.Reset();
            AnimationHelper.Pulse(ProgressSection);
            SetResetStatus("Progress cleared.");
            RefreshSummary();
        }
        catch (Exception ex)
        {
            SetResetStatus($"Reset failed: {ex.Message}");
        }
        finally
        {
            _resetBusy = false;
            ResetButton.IsEnabled = true;
        }
    }
}
