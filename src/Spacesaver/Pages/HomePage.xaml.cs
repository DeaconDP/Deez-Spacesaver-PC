using Microsoft.UI.Xaml.Controls;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Pages;

public sealed partial class HomePage : Page
{
    private double _displayedProgress;

    public HomePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        AppServices.Progress.ProgressChanged += RefreshProgress;
        Unloaded += (_, _) => AppServices.Progress.ProgressChanged -= RefreshProgress;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ApplyCategoryColors();
        RefreshProgress();

        await AnimationHelper.PlayStaggerEntranceAsync([
            HeaderPanel,
            ProgressCard,
            CategoriesPanel,
            SafetyInfoBar
        ]);

        AnimationHelper.AttachHoverLift(ProgressCard);
        AnimationHelper.AttachHoverLift(ScanButton);
        AnimationHelper.AttachHoverLift(QuickWinsButton);
        AnimationHelper.AttachHoverLift(SystemCleanupButton);
        AnimationHelper.AttachHoverLift(LargeWinsButton);
        AnimationHelper.AttachHoverLift(ManualReviewButton);
        AnimationHelper.AttachHoverLift(QuickWinsButtonNarrow);
        AnimationHelper.AttachHoverLift(SystemCleanupButtonNarrow);
        AnimationHelper.AttachHoverLift(LargeWinsButtonNarrow);
        AnimationHelper.AttachHoverLift(ManualReviewButtonNarrow);
    }

    private void PageRoot_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        // Content-width fallback when AdaptiveTrigger window width includes the nav pane.
        var useNarrow = e.NewSize.Width < 560;
        if (useNarrow)
        {
            WideCategories.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            NarrowCategories.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
        else if (e.NewSize.Width >= 640)
        {
            WideCategories.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            NarrowCategories.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    private void ApplyCategoryColors()
    {
        SemanticColorHelper.ApplyCategoryAccent(QuickWinsButton, TaskCategory.QuickWins);
        SemanticColorHelper.ApplyCategoryAccent(SystemCleanupButton, TaskCategory.SystemCleanup);
        SemanticColorHelper.ApplyCategoryAccent(LargeWinsButton, TaskCategory.LargeSpaceWins);
        SemanticColorHelper.ApplyCategoryAccent(ManualReviewButton, TaskCategory.ManualReview);
        SemanticColorHelper.ApplyCategoryAccent(QuickWinsButtonNarrow, TaskCategory.QuickWins);
        SemanticColorHelper.ApplyCategoryAccent(SystemCleanupButtonNarrow, TaskCategory.SystemCleanup);
        SemanticColorHelper.ApplyCategoryAccent(LargeWinsButtonNarrow, TaskCategory.LargeSpaceWins);
        SemanticColorHelper.ApplyCategoryAccent(ManualReviewButtonNarrow, TaskCategory.ManualReview);
        ProgressCard.BorderBrush = SemanticColorHelper.GetAccentBrush(SemanticTone.Informative);
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

    private void ScanRecommendations_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        AnimationHelper.Pulse(ScanButton);
        NavigationHelper.NavigateToRecommendations(this);
    }

    private void StartQuickWins_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        NavigationHelper.NavigateToTaskList(this, TaskCategory.QuickWins, "Quick Wins");

    private void QuickWins_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        NavigationHelper.NavigateToTaskList(this, TaskCategory.QuickWins, "Quick Wins");

    private void SystemCleanup_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        NavigationHelper.NavigateToTaskList(this, TaskCategory.SystemCleanup, "System Cleanup");

    private void LargeWins_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        NavigationHelper.NavigateToTaskList(this, TaskCategory.LargeSpaceWins, "Large Space Wins");

    private void ManualReview_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        NavigationHelper.NavigateToTaskList(this, TaskCategory.ManualReview, "Manual Review");
}
