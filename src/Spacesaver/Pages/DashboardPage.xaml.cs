using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Controls;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Pages;

public sealed partial class DashboardPage : Page
{
    private double _displayedProgress;
    private bool _headerEntrancePlayed;

    public DashboardPage()
    {
        InitializeComponent();
        AppServices.Progress.ProgressChanged += RefreshProgress;
        Unloaded += (_, _) => AppServices.Progress.ProgressChanged -= RefreshProgress;

        SafetyInfoBar.IsOpen = !AppServices.Settings.SafetyInfoBarDismissed;
        CtaCard.AccentBrush = SemanticColorHelper.GetAccentBrush(SemanticTone.Informative);
        ProgressCard.AccentBrush = SemanticColorHelper.GetAccentBrush(SemanticTone.Good);

        QuickWinsTile.Category = TaskCategory.QuickWins;
        SystemCleanupTile.Category = TaskCategory.SystemCleanup;
        LargeWinsTile.Category = TaskCategory.LargeSpaceWins;
        ManualReviewTile.Category = TaskCategory.ManualReview;

        Loaded += (_, _) => PlayHeaderEntranceIfNeeded();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateDriveSummary();
        RefreshProgress();
    }

    private void PlayHeaderEntranceIfNeeded()
    {
        if (_headerEntrancePlayed)
            return;

        _headerEntrancePlayed = true;
        _ = AnimationHelper.PlayOpacityEntranceAsync(PageHeaderControl);
    }

    private void UpdateDriveSummary()
    {
        try
        {
            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var driveInfo = new DriveInfo(systemDrive.TrimEnd('\\'));
            if (!driveInfo.IsReady)
            {
                PageHeaderControl.Subtitle = "Drive information unavailable.";
                return;
            }

            var usedPercent = driveInfo.TotalSize > 0
                ? (driveInfo.TotalSize - driveInfo.AvailableFreeSpace) * 100.0 / driveInfo.TotalSize
                : 0;

            PageHeaderControl.Subtitle =
                $"{driveInfo.Name} — {usedPercent:0}% used · " +
                $"{FileSystemHelper.FormatBytes(driveInfo.AvailableFreeSpace)} free of " +
                $"{FileSystemHelper.FormatBytes(driveInfo.TotalSize)}";

            var tone = SemanticColorHelper.ForDiskUsage(usedPercent);
            PageHeaderControl.UsageProgressBar.Foreground = SemanticColorHelper.GetAccentBrush(tone);
            PageHeaderControl.SubtitleTextBlock.Foreground = SemanticColorHelper.GetForegroundBrush(tone);
            PageHeaderControl.ProgressValue = usedPercent;
        }
        catch
        {
            PageHeaderControl.Subtitle = "Drive information unavailable.";
        }
    }

    private void RefreshProgress()
    {
        var completed = AppServices.Progress.CompletedCount;
        var total = AppServices.Progress.TotalTasks;
        ProgressText.Text = $"{completed} of {total} complete";

        var target = total == 0 ? 0 : completed * 100.0 / total;
        ProgressRing.IsIndeterminate = false;
        ProgressRing.Foreground = SemanticColorHelper.GetAccentBrush(
            SemanticColorHelper.ForProgress(completed, total));

        _ = AnimationHelper.AnimateDoubleAsync(
            v =>
            {
                _displayedProgress = v;
                ProgressRing.Value = v;
            },
            _displayedProgress,
            target,
            durationMs: 450);
    }

    private void SafetyInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args) =>
        AppServices.Settings.SafetyInfoBarDismissed = true;

    private void AnalyzeAndClean_Click(object sender, RoutedEventArgs e)
    {
        AnimationHelper.Pulse(AnalyzeButton);
        NavigationHelper.NavigateToCleanup(this);
    }

    private void ViewTasks_Click(object sender, RoutedEventArgs e) =>
        NavigationHelper.NavigateToTaskGuide(this);

    private void CategoryTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CategoryTile tile)
            return;

        AnimationHelper.Pulse(tile.TileButtonElement);
        NavigationHelper.NavigateToTaskGuide(this, tile.Category);
    }
}
