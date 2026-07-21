using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;
using Spacesaver.ViewModels;

namespace Spacesaver.Pages;

public sealed partial class RecommendationsPage : Page
{
    private readonly ObservableCollection<RecommendationRowViewModel> _rows = [];
    private DiskScanResult? _scanResult;
    private CancellationTokenSource? _scanCts;
    private bool _isApplying;

    public RecommendationsPage()
    {
        InitializeComponent();
        RecommendationsList.ItemsSource = _rows;
        RecommendationsList.ContainerContentChanging += RecommendationsList_ContainerContentChanging;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void RecommendationsList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || args.Phase > 0)
            return;

        if (args.Item is not RecommendationRowViewModel row)
            return;

        void handler(object? _, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(RecommendationRowViewModel.IsSelected))
                return;

            if (sender.ContainerFromItem(row) is ListViewItem container)
                AnimationHelper.Pulse(container, peakScale: 1.015f);
        }

        row.PropertyChanged -= handler;
        row.PropertyChanged += handler;

        if (args.ItemContainer is ListViewItem { Content: FrameworkElement content })
            AnimationHelper.AttachHoverLift(content, hoverLift: -2f, hoverScale: 1.008f);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await AnimationHelper.PlayEntranceAsync(HeaderPanel);
        AnimationHelper.FadeSlideIn(ApplyFooter);
        _ = RunScanAsync();
    }

    private void PageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < 560;
        if (narrow)
        {
            Grid.SetColumn(ApplyButton, 0);
            Grid.SetRow(ApplyButton, 1);
            ApplyButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            Grid.SetColumn(ApplyButton, 1);
            Grid.SetRow(ApplyButton, 0);
            ApplyButton.HorizontalAlignment = HorizontalAlignment.Right;
        }

        LegendInfoBar.Visibility = e.NewSize.Width < 480
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => _scanCts?.Cancel();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (_scanResult is null && !_isApplying)
            _ = RunScanAsync();
    }

    private async Task RunScanAsync()
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        ScanProgressRing.Visibility = Visibility.Visible;
        ScanProgressRing.IsActive = true;
        RescanButton.Visibility = Visibility.Collapsed;
        ApplyButton.IsEnabled = false;
        ApplyStatusText.Visibility = Visibility.Collapsed;
        _rows.Clear();

        var progress = new Progress<string>(status =>
        {
            ScanStatusText.Text = status;
        });

        try
        {
            _scanResult = await AppServices.Scan.ScanAsync(progress, token);
            token.ThrowIfCancellationRequested();

            foreach (var recommendation in _scanResult.Recommendations)
            {
                var row = new RecommendationRowViewModel(recommendation);
                row.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(RecommendationRowViewModel.IsSelected))
                        UpdateSelectionSummary();
                };
                _rows.Add(row);
            }

            DriveSummaryText.Text =
                $"{_scanResult.DriveName} — {_scanResult.UsedPercent:0}% used · " +
                $"{DiskScanService.FormatBytes(_scanResult.FreeBytes)} free of " +
                $"{DiskScanService.FormatBytes(_scanResult.TotalBytes)}";
            ApplyDriveUsageColors(_scanResult.UsedPercent);
            await AnimationHelper.AnimateDoubleAsync(
                v => DriveUsageBar.Value = v,
                DriveUsageBar.Value,
                _scanResult.UsedPercent);
            ScanStatusText.Text = $"Found {_rows.Count} recommendation(s). Select what you want to apply.";
            ScanStatusText.Foreground = SemanticColorHelper.GetForegroundBrush(SemanticTone.Informative);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            ScanStatusText.Text = $"Scan failed: {ex.Message}";
            ScanStatusText.Foreground = SemanticColorHelper.GetForegroundBrush(SemanticTone.Important);
        }
        finally
        {
            ScanProgressRing.IsActive = false;
            ScanProgressRing.Visibility = Visibility.Collapsed;
            RescanButton.Visibility = Visibility.Visible;
            UpdateSelectionSummary();
        }
    }

    private void UpdateSelectionSummary()
    {
        var selected = _rows.Where(r => r.IsSelected).ToList();
        var count = selected.Count;
        var bytes = selected.Sum(r => r.Recommendation.ReclaimableBytes);

        SelectionSummaryText.Text = count == 0
            ? "0 selected"
            : bytes > 0
                ? $"{count} selected · ~{DiskScanService.FormatBytes(bytes)} recoverable"
                : $"{count} selected";

        ApplyButton.IsEnabled = count > 0 && !_isApplying;

        if (count > 0)
            AnimationHelper.Pulse(ApplyButton, peakScale: 1.03f);
    }

    private void RescanButton_Click(object sender, RoutedEventArgs e)
    {
        AnimationHelper.Pulse(RescanButton);
        _ = RunScanAsync();
    }

    private void LearnMore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton button && button.Tag is string taskId)
            NavigationHelper.NavigateToTaskDetail(this, taskId);
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _rows.Where(r => r.IsSelected).Select(r => r.Recommendation).ToList();
        if (selected.Count == 0)
            return;

        if (!await ConfirmApplyAsync(selected))
            return;

        _isApplying = true;
        ApplyButton.IsEnabled = false;
        RescanButton.IsEnabled = false;
        ApplyStatusText.Visibility = Visibility.Visible;

        var progress = new Progress<(string TaskId, string Status)>(update =>
        {
            ApplyStatusText.Text = update.Status;
        });

        try
        {
            var results = await AppServices.Cleanup.ApplyAsync(selected, progress);

            var succeeded = results.Count(r => r.Outcome == ApplyOutcome.Success);
            var failed = results.Count(r => r.Outcome == ApplyOutcome.Failed);
            var skipped = results.Count(r => r.Outcome == ApplyOutcome.Skipped);

            ApplyStatusText.Text =
                $"Done — {succeeded} succeeded, {skipped} skipped, {failed} failed.";

            ApplyStatusText.Foreground = failed > 0
                ? SemanticColorHelper.GetForegroundBrush(SemanticTone.Important)
                : skipped > 0
                    ? SemanticColorHelper.GetForegroundBrush(SemanticTone.Attention)
                    : SemanticColorHelper.GetForegroundBrush(SemanticTone.Good);

            await ShowResultsDialogAsync(results);
            await RunScanAsync();
        }
        catch (Exception ex)
        {
            ApplyStatusText.Text = $"Apply failed: {ex.Message}";
            ApplyStatusText.Foreground = SemanticColorHelper.GetForegroundBrush(SemanticTone.Important);
        }
        finally
        {
            _isApplying = false;
            RescanButton.IsEnabled = true;
            UpdateSelectionSummary();
        }
    }

    private async Task<bool> ConfirmApplyAsync(IReadOnlyList<CleanupRecommendation> selected)
    {
        var panel = new StackPanel { Spacing = 12 };

        panel.Children.Add(new TextBlock
        {
            Text = "You are about to apply the following actions. Review risks carefully.",
            TextWrapping = TextWrapping.WrapWholeWords
        });

        var autoItems = selected.Where(r => r.ExecutionMode == ExecutionMode.AutoClean).ToList();
        var launchItems = selected.Where(r => r.ExecutionMode == ExecutionMode.LaunchTool).ToList();
        var manualItems = selected.Where(r => r.ExecutionMode == ExecutionMode.ManualOnly).ToList();

        if (autoItems.Count > 0)
            panel.Children.Add(BuildGroup("Runs in this app (automatic)", autoItems, SemanticTone.Good, includeRisk: true));

        if (launchItems.Count > 0)
            panel.Children.Add(BuildGroup("Opens Windows tools (you confirm in each tool)", launchItems, SemanticTone.Informative, includeRisk: true));

        if (manualItems.Count > 0)
            panel.Children.Add(BuildGroup("Manual review required", manualItems, SemanticTone.Attention, includeRisk: true));

        var hasAdvanced = selected.Any(r => r.Task.RiskLevel == RiskLevel.Advanced);
        if (hasAdvanced)
        {
            panel.Children.Add(new InfoBar
            {
                IsOpen = true,
                IsClosable = false,
                Severity = InfoBarSeverity.Error,
                Title = "Advanced actions selected",
                Message = "These cannot be undone automatically. You will complete them in Windows and must confirm each step yourself."
            });
        }

        var riskAck = new CheckBox
        {
            Content = "I have read the risks and want to continue"
        };
        panel.Children.Add(riskAck);

        var dialog = new ContentDialog
        {
            Title = "Confirm cleanup actions",
            Content = new ScrollViewer
            {
                Content = panel,
                MaxHeight = 420,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            },
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        dialog.PrimaryButtonClick += (_, args) =>
        {
            if (!riskAck.IsChecked == true)
                args.Cancel = true;
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private static StackPanel BuildGroup(
        string header,
        IReadOnlyList<CleanupRecommendation> items,
        SemanticTone headerTone,
        bool includeRisk)
    {
        var group = new StackPanel { Spacing = 6 };
        group.Children.Add(new TextBlock
        {
            Text = header,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = SemanticColorHelper.GetForegroundBrush(headerTone)
        });

        foreach (var item in items)
        {
            var text = $"• {item.Task.Title}";
            if (item.ReclaimableBytes > 0)
                text += $" ({DiskScanService.FormatBytes(item.ReclaimableBytes)})";

            group.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.WrapWholeWords
            });

            if (includeRisk)
            {
                var riskTone = SemanticColorHelper.ForRiskLevel(item.Task.RiskLevel);
                group.Children.Add(new TextBlock
                {
                    Text = $"  Risk: {item.RiskSummary}",
                    FontSize = 12,
                    Foreground = SemanticColorHelper.GetForegroundBrush(riskTone),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(8, 0, 0, 4)
                });
            }
        }

        return group;
    }

    private async Task ShowResultsDialogAsync(IReadOnlyList<ApplyActionResult> results)
    {
        var panel = new StackPanel { Spacing = 8 };

        foreach (var result in results)
        {
            var icon = result.Outcome switch
            {
                ApplyOutcome.Success => "✓",
                ApplyOutcome.Skipped => "○",
                _ => "✗"
            };

            var line = $"{icon} {result.Title}: {result.Message}";
            if (result.BytesFreed > 0)
                line += $" ({DiskScanService.FormatBytes(result.BytesFreed)} freed)";

            panel.Children.Add(new TextBlock
            {
                Text = line,
                TextWrapping = TextWrapping.WrapWholeWords,
                Foreground = SemanticColorHelper.GetForegroundBrush(
                    SemanticColorHelper.ForApplyOutcome(result.Outcome))
            });
        }

        var dialog = new ContentDialog
        {
            Title = "Cleanup results",
            Content = panel,
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private void ApplyDriveUsageColors(double usedPercent)
    {
        var tone = SemanticColorHelper.ForDiskUsage(usedPercent);
        DriveUsageBar.Foreground = SemanticColorHelper.GetAccentBrush(tone);
        DriveSummaryText.Foreground = SemanticColorHelper.GetForegroundBrush(tone);
    }
}
