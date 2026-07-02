using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Dialogs;

public static class ConfirmCleanupDialog
{
    public static async Task<bool> ShowAsync(XamlRoot xamlRoot, IReadOnlyList<CleanupRecommendation> selected)
    {
        var panel = new StackPanel { Spacing = 12, MaxWidth = 480 };

        panel.Children.Add(new TextBlock
        {
            Text = "You are about to apply the following actions. Review risks carefully.",
            TextWrapping = TextWrapping.WrapWholeWords
        });

        var autoItems = selected.Where(r => r.ExecutionMode == ExecutionMode.AutoClean).ToList();
        var launchItems = selected.Where(r => r.ExecutionMode == ExecutionMode.LaunchTool).ToList();
        var manualItems = selected.Where(r => r.ExecutionMode == ExecutionMode.ManualOnly).ToList();

        if (autoItems.Count > 0)
            panel.Children.Add(BuildGroup("Runs in this app (automatic)", autoItems, SemanticTone.Good));

        if (launchItems.Count > 0)
            panel.Children.Add(BuildGroup("Opens Windows tools (you confirm in each tool)", launchItems, SemanticTone.Informative));

        if (manualItems.Count > 0)
            panel.Children.Add(BuildGroup("Manual review required", manualItems, SemanticTone.Attention));

        if (selected.Any(r => r.Task.RiskLevel == RiskLevel.Advanced))
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
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(riskAck, "Risk acknowledgment");
        panel.Children.Add(riskAck);

        var dialog = new ContentDialog
        {
            Title = "Confirm cleanup actions",
            Content = new ScrollViewer
            {
                Content = panel,
                MaxHeight = 420
            },
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot
        };

        dialog.PrimaryButtonClick += (_, args) =>
        {
            if (riskAck.IsChecked != true)
                args.Cancel = true;
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private static StackPanel BuildGroup(
        string header,
        IReadOnlyList<CleanupRecommendation> items,
        SemanticTone headerTone)
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

            var riskTone = SemanticColorHelper.ForRiskLevel(item.Task.RiskLevel);
            group.Children.Add(new TextBlock
            {
                Text = $"  Risk: {item.RiskSummary}",
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                Foreground = SemanticColorHelper.GetForegroundBrush(riskTone),
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(8, 0, 0, 4)
            });
        }

        return group;
    }
}
