using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Spacesaver.Controls;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;
using Spacesaver.ViewModels;

namespace Spacesaver.Pages;

public sealed partial class TaskDetailPage : Page
{
    private CleanupTask? _task;

    public TaskDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not string taskId)
            return;

        _task = TaskCatalog.GetById(taskId);
        if (_task is null)
            return;

        BindTask(_task);
    }

    private async void BindTask(CleanupTask task)
    {
        TaskNumberText.Text = $"Task #{task.Number}";
        PageHeaderControl.Title = task.Title;
        PageHeaderControl.Subtitle = task.Description;

        var categoryTone = SemanticColorHelper.ForTaskCategory(task.Category);
        var riskTone = SemanticColorHelper.ForRiskLevel(task.RiskLevel);

        ApplyChip(SavingsChip, task.EstimatedSavings, categoryTone);
        ApplyChip(RiskChip, task.RiskLabel, riskTone);
        ApplyChip(CategoryChip, task.CategoryLabel, categoryTone);

        AdminInfoBar.IsOpen = task.RequiresAdmin;
        if (AdminInfoBar.IsOpen)
            AnimationHelper.FadeSlideIn(AdminInfoBar);

        if (!string.IsNullOrWhiteSpace(task.Warning))
        {
            WarningInfoBar.Message = task.Warning;
            WarningInfoBar.IsOpen = true;
            AnimationHelper.FadeSlideIn(WarningInfoBar);
        }
        else
        {
            WarningInfoBar.IsOpen = false;
        }

        StepsList.ItemsSource = task.Steps
            .Select((step, index) => new StepRowViewModel(index + 1, step, categoryTone))
            .ToList();

        if (task.PrimaryAction is not null)
        {
            OpenToolButton.Content = task.PrimaryAction.ButtonLabel;
            OpenToolButton.Visibility = Visibility.Visible;
        }
        else
        {
            OpenToolButton.Visibility = Visibility.Collapsed;
        }

        CopyCommandButton.Visibility = string.IsNullOrWhiteSpace(task.CopyCommand)
            ? Visibility.Collapsed
            : Visibility.Visible;

        DoneToggle.IsOn = AppServices.Progress.IsCompleted(task.Id);

        await AnimationHelper.PlayStaggerEntranceAsync([
            PageHeaderControl,
            StepsList,
            OpenToolButton,
            DoneToggle
        ]);

        AnimationHelper.AttachHoverLift(OpenToolButton, hoverScale: 1.02f);
    }

    private static void ApplyChip(SemanticChip chip, string label, SemanticTone tone)
    {
        chip.Label = label;
        chip.AccentBrush = SemanticColorHelper.GetAccentBrush(tone);
        chip.TintBrush = SemanticColorHelper.GetTintBrush(tone);
    }

    private async void OpenToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (_task?.PrimaryAction is null)
            return;

        AnimationHelper.Pulse(OpenToolButton);
        var launched = await AppServices.Launcher.LaunchAsync(_task.PrimaryAction);
        if (!launched)
        {
            var dialog = new ContentDialog
            {
                Title = "Could not open tool",
                Content = "Try opening the tool manually from Start or Settings.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    private async void CopyCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (_task?.CopyCommand is null)
            return;

        AnimationHelper.Pulse(CopyCommandButton);
        await WindowsLauncherService.CopyToClipboardAsync(_task.CopyCommand);

        var dialog = new ContentDialog
        {
            Title = "Command copied",
            Content = "Paste into an Administrator PowerShell or Terminal window.",
            CloseButtonText = "OK",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private void DoneToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_task is null)
            return;

        AppServices.Progress.SetCompleted(_task.Id, DoneToggle.IsOn);

        if (DoneToggle.IsOn)
            AnimationHelper.Pulse(DoneToggle, peakScale: 1.03f);
    }
}
