using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Pages;

public sealed partial class TaskDetailPage : Page
{
    private CleanupTask? _task;
    private bool _actionBusy;

    public TaskDetailPage()
    {
        InitializeComponent();
    }

    private void PageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < 520;
        ActionPanel.Orientation = narrow ? Orientation.Vertical : Orientation.Horizontal;
        ChipPanel.Orientation = narrow ? Orientation.Vertical : Orientation.Horizontal;
        OpenToolButton.HorizontalAlignment = narrow ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
        CopyCommandButton.HorizontalAlignment = narrow ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
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
        TitleText.Text = task.Title;
        DescriptionText.Text = task.Description;
        SavingsText.Text = task.EstimatedSavings;
        RiskText.Text = task.RiskLabel;
        CategoryText.Text = task.CategoryLabel;

        var categoryTone = SemanticColorHelper.ForTaskCategory(task.Category);
        SemanticColorHelper.ApplyChip(SavingsBorder, SavingsText, categoryTone);
        SemanticColorHelper.ApplyChip(
            RiskBorder,
            RiskText,
            SemanticColorHelper.ForRiskLevel(task.RiskLevel));
        SemanticColorHelper.ApplyChip(CategoryBorder, CategoryText, categoryTone);

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

        StepsList.Items.Clear();
        var stepPanels = new List<UIElement>();
        var stepTone = categoryTone;
        var stepAccent = SemanticColorHelper.GetAccentColor(stepTone);
        var stepTint = SemanticColorHelper.GetTintColor(stepTone);

        for (var i = 0; i < task.Steps.Count; i++)
        {
            // Grid (Auto | *) so step text measures against available width — not infinite StackPanel width.
            var row = new Grid
            {
                Margin = new Thickness(0, 0, 0, 10),
                ColumnSpacing = 12
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var number = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(stepTint),
                BorderBrush = new SolidColorBrush(stepAccent),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(stepAccent)
                }
            };

            var text = new TextBlock
            {
                Text = task.Steps[i],
                TextWrapping = TextWrapping.WrapWholeWords,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(number, 0);
            Grid.SetColumn(text, 1);
            row.Children.Add(number);
            row.Children.Add(text);

            StepsList.Items.Add(row);
            stepPanels.Add(row);
        }

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
        SetActionStatus(null);

        await AnimationHelper.PlayStaggerEntranceAsync([
            TaskHeaderPanel,
            StepsPanel,
            ActionPanel,
            DoneToggle
        ]);

        await AnimationHelper.PlayStaggerEntranceAsync(stepPanels, staggerMs: 45);
        AnimationHelper.AttachHoverLift(OpenToolButton);
    }

    private void SetActionBusy(bool busy)
    {
        _actionBusy = busy;
        var canOpen = !busy && _task?.PrimaryAction is not null;
        OpenToolButton.IsEnabled = canOpen;
        CopyCommandButton.IsEnabled = !busy && !string.IsNullOrWhiteSpace(_task?.CopyCommand);
    }

    private void SetActionStatus(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            ActionStatusText.Visibility = Visibility.Collapsed;
            ActionStatusText.Text = string.Empty;
            return;
        }

        ActionStatusText.Text = message;
        ActionStatusText.Visibility = Visibility.Visible;
    }

    private async void OpenToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (_task?.PrimaryAction is null || _actionBusy)
            return;

        SetActionBusy(true);
        SetActionStatus("Opening tool…");
        OpenToolButton.IsHitTestVisible = false;

        try
        {
            AnimationHelper.Pulse(OpenToolButton);
            var launched = await AppServices.Launcher.LaunchAsync(_task.PrimaryAction);
            if (!launched)
            {
                SetActionStatus("Could not open tool. Try Start or Settings manually.");
                var dialog = new ContentDialog
                {
                    Title = "Could not open tool",
                    Content = "Try opening the tool manually from Start or Settings.",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };
                await dialog.ShowAsync();
            }
            else
            {
                SetActionStatus("Tool launched.");
            }
        }
        finally
        {
            OpenToolButton.IsHitTestVisible = true;
            SetActionBusy(false);
        }
    }

    private async void CopyCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (_task?.CopyCommand is null || _actionBusy)
            return;

        SetActionBusy(true);
        SetActionStatus("Copying command…");

        try
        {
            AnimationHelper.Pulse(CopyCommandButton);
            await WindowsLauncherService.CopyToClipboardAsync(_task.CopyCommand);
            SetActionStatus("Command copied to clipboard.");

            var dialog = new ContentDialog
            {
                Title = "Command copied",
                Content = "Paste into an Administrator PowerShell or Terminal window.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }
        finally
        {
            SetActionBusy(false);
        }
    }

    private void DoneToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_task is null)
            return;

        AppServices.Progress.SetCompleted(_task.Id, DoneToggle.IsOn);

        if (DoneToggle.IsOn)
            AnimationHelper.Pulse(DoneToggle, peakScale: 1.02f);
    }
}
