using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.ViewModels;

public sealed class RecommendationRowViewModel : INotifyPropertyChanged
{
    public RecommendationRowViewModel(CleanupRecommendation recommendation)
    {
        Recommendation = recommendation;
        RefreshBrushes();
    }

    public CleanupRecommendation Recommendation { get; }

    public CleanupTask Task => Recommendation.Task;

    public bool IsSelected
    {
        get => Recommendation.IsSelected;
        set
        {
            if (Recommendation.IsSelected == value)
                return;

            Recommendation.IsSelected = value;
            OnPropertyChanged();
        }
    }

    public string Title => Task.Title;
    public string TaskId => Recommendation.TaskId;
    public string ScanDetail => Recommendation.ScanDetail;
    public string RiskSummary => Recommendation.RiskSummary;
    public string FormattedSize => Recommendation.ReclaimableBytes > 0
        ? DiskScanService.FormatBytes(Recommendation.ReclaimableBytes)
        : Task.EstimatedSavings;
    public string ImpactLabel => ImpactLevelHelper.GetLabel(Recommendation.Impact);
    public string RiskLabel => Task.RiskLabel;
    public string ExecutionLabel => Recommendation.ExecutionMode switch
    {
        ExecutionMode.AutoClean => "Runs in this app",
        ExecutionMode.LaunchTool => "Opens Windows tool",
        ExecutionMode.ManualOnly => "Manual review required",
        _ => string.Empty
    };

    public Brush ImpactBrush { get; private set; } = ImpactLevelHelper.GetAccentBrush(ImpactLevel.Low);
    public Brush ImpactTintBrush { get; private set; } = ImpactLevelHelper.GetTintBrush(ImpactLevel.Low);
    public Brush RiskBrush { get; private set; } = RiskLevelHelper.GetBadgeBrush(RiskLevel.Safe);
    public Brush RiskTintBrush { get; private set; } = RiskLevelHelper.GetBadgeTintBrush(RiskLevel.Safe);

    public Brush ExecutionForeground { get; private set; } =
        SemanticColorHelper.GetForegroundBrush(SemanticTone.Informative);

    public Visibility SizeVisibility => Recommendation.ReclaimableBytes > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshBrushes()
    {
        ImpactBrush = ImpactLevelHelper.GetAccentBrush(Recommendation.Impact);
        ImpactTintBrush = ImpactLevelHelper.GetTintBrush(Recommendation.Impact);
        RiskBrush = RiskLevelHelper.GetBadgeBrush(Task.RiskLevel);
        RiskTintBrush = RiskLevelHelper.GetBadgeTintBrush(Task.RiskLevel);
        ExecutionForeground = SemanticColorHelper.GetForegroundBrush(
            SemanticColorHelper.ForExecutionMode(Recommendation.ExecutionMode));
        OnPropertyChanged(nameof(ImpactBrush));
        OnPropertyChanged(nameof(ImpactTintBrush));
        OnPropertyChanged(nameof(RiskBrush));
        OnPropertyChanged(nameof(RiskTintBrush));
        OnPropertyChanged(nameof(ExecutionForeground));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
