using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Spacesaver.Coordinators;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;
using Spacesaver.ViewModels;

namespace Spacesaver.Pages;

public sealed partial class CleanupWizardPage : Page
{
    private readonly CleanupWizardCoordinator _coordinator = new();
    private int _currentStep;
    private bool _scanStarted;
    private IReadOnlyList<ApplyActionResult>? _lastResults;

    public CleanupWizardPage()
    {
        InitializeComponent();
        RecommendationsList.ItemsSource = _coordinator.Rows;
        RecommendationsList.ContainerContentChanging += RecommendationsList_ContainerContentChanging;

        ScanStatus.RescanButtonControl.Click += RescanButton_Click;

        RiskAckCheckbox.Checked += (_, _) => UpdateFooter();
        RiskAckCheckbox.Unchecked += (_, _) => UpdateFooter();

        _coordinator.SelectionChanged += UpdateFooter;
        _coordinator.ScanCompleted += OnScanCompleted;
        _coordinator.ScanStatusChanged += status =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ScanStatus.StatusText.Text = status;
                FooterSummaryText.Text = status;
            });
        };

        AnimationHelper.WirePageLoaded(this, [PageHeaderControl, StepIndicatorControl]);
        Unloaded += (_, _) => _coordinator.CancelScan();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (!_scanStarted)
        {
            _scanStarted = true;
            GoToStep(0);
            _ = StartScanAsync();
        }
    }

    private async Task StartScanAsync()
    {
        var scanRing = ScanStatus.ScanRing;
        var scanText = ScanStatus.StatusText;
        var rescanButton = ScanStatus.RescanButtonControl;

        scanRing.Visibility = Visibility.Visible;
        scanRing.IsActive = true;
        rescanButton.Visibility = Visibility.Collapsed;
        NextButton.IsEnabled = false;
        FooterStatusText.Visibility = Visibility.Collapsed;

        var progress = new Progress<string>(status => scanText.Text = status);

        await _coordinator.RunScanAsync(progress);

        scanRing.IsActive = false;
        scanRing.Visibility = Visibility.Collapsed;
        rescanButton.Visibility = Visibility.Visible;
        UpdateFooter();
    }

    private void OnScanCompleted(DiskScanResult result)
    {
        PageHeaderControl.Subtitle =
            $"{result.DriveName} — {result.UsedPercent:0}% used · " +
            $"{DiskScanService.FormatBytes(result.FreeBytes)} free of " +
            $"{DiskScanService.FormatBytes(result.TotalBytes)}";

        var tone = SemanticColorHelper.ForDiskUsage(result.UsedPercent);
        PageHeaderControl.UsageProgressBar.Foreground = SemanticColorHelper.GetAccentBrush(tone);
        PageHeaderControl.SubtitleTextBlock.Foreground = SemanticColorHelper.GetForegroundBrush(tone);
        _ = AnimationHelper.AnimateDoubleAsync(
            v => PageHeaderControl.ProgressValue = v,
            PageHeaderControl.ProgressValue,
            result.UsedPercent);

        NextButton.IsEnabled = _coordinator.Rows.Count > 0;
    }

    private void GoToStep(int step)
    {
        _currentStep = step;
        StepIndicatorControl.CurrentStep = step;

        ScanStepPanel.Visibility = step == 0 ? Visibility.Visible : Visibility.Collapsed;
        ReviewStepPanel.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        ConfirmStepPanel.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        ResultsStepPanel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.Visibility = step is 1 or 2 ? Visibility.Visible : Visibility.Collapsed;

        NextButton.Content = step switch
        {
            0 => "Review selections",
            1 => "Confirm",
            2 => "Apply",
            _ => "Scan again"
        };

        if (step == 2)
            BuildConfirmSummary();

        if (step == 3)
            NextButton.IsEnabled = true;

        UpdateFooter();
    }

    private void UpdateFooter()
    {
        if (_currentStep == 0)
        {
            NextButton.IsEnabled = _coordinator.Rows.Count > 0 && !_coordinator.IsApplying;
            return;
        }

        if (_currentStep == 1)
        {
            var (count, bytes) = _coordinator.GetSelectionSummary();
            FooterSummaryText.Text = count == 0
                ? "0 selected"
                : bytes > 0
                    ? $"{count} selected · ~{DiskScanService.FormatBytes(bytes)} recoverable"
                    : $"{count} selected";
            NextButton.IsEnabled = count > 0 && !_coordinator.IsApplying;
            return;
        }

        if (_currentStep == 2)
        {
            FooterSummaryText.Text = $"{_coordinator.GetSelectedRecommendations().Count} action(s) ready to apply";
            NextButton.IsEnabled = RiskAckCheckbox.IsChecked == true && !_coordinator.IsApplying;
        }
    }

    private void BuildConfirmSummary()
    {
        ConfirmSummaryPanel.Children.Clear();
        var selected = _coordinator.GetSelectedRecommendations();

        foreach (var item in selected)
        {
            var text = $"• {item.Task.Title}";
            if (item.ReclaimableBytes > 0)
                text += $" ({DiskScanService.FormatBytes(item.ReclaimableBytes)})";

            ConfirmSummaryPanel.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.WrapWholeWords
            });

            var riskTone = SemanticColorHelper.ForRiskLevel(item.Task.RiskLevel);
            ConfirmSummaryPanel.Children.Add(new TextBlock
            {
                Text = $"  Risk: {item.RiskSummary}",
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                Foreground = SemanticColorHelper.GetForegroundBrush(riskTone),
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(8, 0, 0, 4)
            });
        }

        AdvancedWarningBar.IsOpen = selected.Any(r => r.Task.RiskLevel == RiskLevel.Advanced);
        RiskAckCheckbox.IsChecked = false;
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

    private void RescanButton_Click(object sender, RoutedEventArgs e)
    {
        AnimationHelper.Pulse(ScanStatus.RescanButtonControl);
        GoToStep(0);
        _ = StartScanAsync();
    }

    private void LearnMore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton button && button.Tag is string taskId)
            NavigationHelper.NavigateToTaskDetail(this, taskId);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep == 2)
            GoToStep(1);
        else if (_currentStep == 1)
            GoToStep(0);
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        AnimationHelper.Pulse(NextButton);

        switch (_currentStep)
        {
            case 0:
                GoToStep(1);
                break;
            case 1:
                GoToStep(2);
                break;
            case 2:
                await ApplySelectedAsync();
                break;
            case 3:
                GoToStep(0);
                _ = StartScanAsync();
                break;
        }
    }

    private async Task ApplySelectedAsync()
    {
        var selected = _coordinator.GetSelectedRecommendations();
        if (selected.Count == 0)
            return;

        NextButton.IsEnabled = false;
        BackButton.IsEnabled = false;
        FooterStatusText.Visibility = Visibility.Visible;

        var progress = new Progress<(string TaskId, string Status)>(update =>
        {
            FooterStatusText.Text = update.Status;
        });

        try
        {
            _lastResults = await _coordinator.ApplySelectedAsync(progress);
            ShowResults(_lastResults);
            GoToStep(3);
        }
        catch (Exception ex)
        {
            FooterStatusText.Text = $"Apply failed: {ex.Message}";
            FooterStatusText.Foreground = SemanticColorHelper.GetForegroundBrush(SemanticTone.Important);
        }
        finally
        {
            BackButton.IsEnabled = true;
            UpdateFooter();
        }
    }

    private void ShowResults(IReadOnlyList<ApplyActionResult> results)
    {
        ResultsDetailPanel.Children.Clear();

        var succeeded = results.Count(r => r.Outcome == ApplyOutcome.Success);
        var failed = results.Count(r => r.Outcome == ApplyOutcome.Failed);
        var skipped = results.Count(r => r.Outcome == ApplyOutcome.Skipped);

        ResultsSummaryText.Text = $"Done — {succeeded} succeeded, {skipped} skipped, {failed} failed.";
        ResultsSummaryText.Foreground = failed > 0
            ? SemanticColorHelper.GetForegroundBrush(SemanticTone.Important)
            : skipped > 0
                ? SemanticColorHelper.GetForegroundBrush(SemanticTone.Attention)
                : SemanticColorHelper.GetForegroundBrush(SemanticTone.Good);

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

            ResultsDetailPanel.Children.Add(new TextBlock
            {
                Text = line,
                TextWrapping = TextWrapping.WrapWholeWords,
                Foreground = SemanticColorHelper.GetForegroundBrush(
                    SemanticColorHelper.ForApplyOutcome(result.Outcome))
            });
        }

        FooterSummaryText.Text = ResultsSummaryText.Text;
    }
}
