using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Dialogs;

public static class CleanupResultsDialog
{
    public static async Task ShowAsync(XamlRoot xamlRoot, IReadOnlyList<ApplyActionResult> results)
    {
        var panel = new StackPanel { Spacing = 8, MaxWidth = 440 };

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
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }
}
