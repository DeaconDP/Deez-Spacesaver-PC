using Microsoft.UI.Xaml.Media;
using Spacesaver.Models;

namespace Spacesaver.Helpers;

public enum SemanticTone
{
    Good,
    Important,
    Attention,
    Informative
}

public static class SemanticColorHelper
{
    // Cyberpunk-aligned accents (teal / orange / pink / blue)
    public static Windows.UI.Color GoodAccent { get; } = Windows.UI.Color.FromArgb(255, 0, 229, 192);
    public static Windows.UI.Color ImportantAccent { get; } = Windows.UI.Color.FromArgb(255, 255, 138, 76);
    public static Windows.UI.Color AttentionAccent { get; } = Windows.UI.Color.FromArgb(255, 255, 61, 129);
    public static Windows.UI.Color InformativeAccent { get; } = Windows.UI.Color.FromArgb(255, 107, 158, 255);

    public static Brush GetAccentBrush(SemanticTone tone) =>
        new SolidColorBrush(GetAccentColor(tone));

    public static Brush GetTintBrush(SemanticTone tone) =>
        new SolidColorBrush(GetTintColor(tone));

    public static Brush GetForegroundBrush(SemanticTone tone) =>
        new SolidColorBrush(GetAccentColor(tone));

    public static Windows.UI.Color GetAccentColor(SemanticTone tone) =>
        tone switch
        {
            SemanticTone.Good => GoodAccent,
            SemanticTone.Important => ImportantAccent,
            SemanticTone.Attention => AttentionAccent,
            SemanticTone.Informative => InformativeAccent,
            _ => InformativeAccent
        };

    public static Windows.UI.Color GetTintColor(SemanticTone tone)
    {
        var accent = GetAccentColor(tone);
        return Windows.UI.Color.FromArgb(28, accent.R, accent.G, accent.B);
    }

    public static SemanticTone ForRiskLevel(RiskLevel level) =>
        level switch
        {
            RiskLevel.Safe => SemanticTone.Good,
            RiskLevel.Caution => SemanticTone.Attention,
            RiskLevel.Advanced => SemanticTone.Important,
            _ => SemanticTone.Informative
        };

    public static SemanticTone ForImpactLevel(ImpactLevel level) =>
        level switch
        {
            ImpactLevel.High => SemanticTone.Good,
            ImpactLevel.Medium => SemanticTone.Important,
            ImpactLevel.Low => SemanticTone.Informative,
            _ => SemanticTone.Informative
        };

    public static SemanticTone ForTaskCategory(TaskCategory category) =>
        category switch
        {
            TaskCategory.QuickWins => SemanticTone.Good,
            TaskCategory.SystemCleanup => SemanticTone.Important,
            TaskCategory.LargeSpaceWins => SemanticTone.Attention,
            TaskCategory.ManualReview => SemanticTone.Informative,
            _ => SemanticTone.Informative
        };

    public static SemanticTone ForExecutionMode(ExecutionMode mode) =>
        mode switch
        {
            ExecutionMode.AutoClean => SemanticTone.Good,
            ExecutionMode.LaunchTool => SemanticTone.Informative,
            ExecutionMode.ManualOnly => SemanticTone.Attention,
            _ => SemanticTone.Informative
        };

    public static SemanticTone ForApplyOutcome(ApplyOutcome outcome) =>
        outcome switch
        {
            ApplyOutcome.Success => SemanticTone.Good,
            ApplyOutcome.Skipped => SemanticTone.Attention,
            ApplyOutcome.Failed => SemanticTone.Important,
            _ => SemanticTone.Informative
        };

    public static SemanticTone ForDiskUsage(double usedPercent) =>
        usedPercent >= 85 ? SemanticTone.Attention
        : usedPercent >= 70 ? SemanticTone.Important
        : SemanticTone.Good;

    public static SemanticTone ForProgress(int completed, int total)
    {
        if (total <= 0)
            return SemanticTone.Informative;

        var ratio = (double)completed / total;
        return ratio >= 1 ? SemanticTone.Good
            : ratio >= 0.5 ? SemanticTone.Informative
            : ratio > 0 ? SemanticTone.Attention
            : SemanticTone.Informative;
    }

    public static void ApplyCategoryAccent(Microsoft.UI.Xaml.Controls.Button button, TaskCategory category)
    {
        var tone = ForTaskCategory(category);
        button.BorderBrush = GetAccentBrush(tone);
        button.BorderThickness = new Microsoft.UI.Xaml.Thickness(3, 1, 1, 1);
        button.Background = GetTintBrush(tone);
    }

    public static void ApplyChip(
        Microsoft.UI.Xaml.Controls.Border border,
        Microsoft.UI.Xaml.Controls.TextBlock text,
        SemanticTone tone)
    {
        border.Background = GetTintBrush(tone);
        border.BorderBrush = GetAccentBrush(tone);
        border.BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
        text.Foreground = GetForegroundBrush(tone);
    }
}
