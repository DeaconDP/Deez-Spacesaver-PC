using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;
using Spacesaver.ViewModels;

namespace Spacesaver.Pages;

public sealed partial class StorageExplorerPage : Page
{
    private readonly ObservableCollection<FolderRowViewModel> _rows = [];
    private readonly Dictionary<string, FolderRowViewModel> _rowByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _breadcrumbPaths = [];
    private CancellationTokenSource? _scanCts;
    private string _currentPath = FileSystemHelper.GetSystemDriveRoot();

    public StorageExplorerPage()
    {
        InitializeComponent();
        FolderList.ItemsSource = _rows;
        FolderList.ContainerContentChanging += FolderList_ContainerContentChanging;

        ScanStatus.RescanButtonControl.Click += RescanButton_Click;
        ScanStatus.CancelButton.Click += CancelScanButton_Click;

        AnimationHelper.WirePageLoaded(this, [PageHeaderControl]);
        Unloaded += OnUnloaded;
    }

    private void FolderList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || args.Phase > 0)
            return;

        if (args.ItemContainer is ListViewItem { Content: FrameworkElement content })
            AnimationHelper.AttachHoverLift(content, hoverLift: -2f, hoverScale: 1.008f);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => _scanCts?.Cancel();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string path && !string.IsNullOrWhiteSpace(path))
            _currentPath = path;
        else
            _currentPath = FileSystemHelper.GetSystemDriveRoot();

        _ = RunScanAsync(_currentPath);
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

    private void RebuildBreadcrumb(string path)
    {
        _breadcrumbPaths.Clear();
        var labels = new List<string>();

        var segments = new List<(string Label, string FullPath)>();
        var normalized = path.TrimEnd('\\');

        if (normalized.Length <= 3 && normalized.EndsWith(':'))
        {
            segments.Add((normalized + "\\", normalized + "\\"));
        }
        else
        {
            var root = Path.GetPathRoot(normalized) ?? FileSystemHelper.GetSystemDriveRoot();
            segments.Add((root, root));

            var relative = normalized[root.Length..].TrimStart('\\');
            if (!string.IsNullOrEmpty(relative))
            {
                var built = root.TrimEnd('\\');
                foreach (var part in relative.Split('\\', StringSplitOptions.RemoveEmptyEntries))
                {
                    built = Path.Combine(built, part);
                    segments.Add((part, built));
                }
            }
        }

        foreach (var (label, fullPath) in segments)
        {
            labels.Add(label);
            _breadcrumbPaths.Add(fullPath);
        }

        FolderBreadcrumb.ItemsSource = labels;
    }

    private void FolderBreadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < 0 || args.Index >= _breadcrumbPaths.Count)
            return;

        NavigateToPath(_breadcrumbPaths[args.Index]);
    }

    private void NavigateToPath(string path)
    {
        _currentPath = path.EndsWith('\\') ? path : path + (path.EndsWith(':') ? "\\" : "");
        if (_currentPath.Length == 2 && _currentPath[1] == ':')
            _currentPath += "\\";

        _ = RunScanAsync(_currentPath);
    }

    private async Task RunScanAsync(string path)
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;
        var dispatcher = DispatcherQueue;

        var scanRing = ScanStatus.ScanRing;
        var scanText = ScanStatus.StatusText;

        scanRing.Visibility = Visibility.Visible;
        scanRing.IsActive = true;
        ScanStatus.CancelButton.Visibility = Visibility.Visible;
        ScanStatus.RescanButtonControl.Visibility = Visibility.Collapsed;
        _rows.Clear();
        _rowByPath.Clear();
        RebuildBreadcrumb(path);
        UpdateDriveSummary();

        var progress = new Progress<string>(status =>
        {
            dispatcher.TryEnqueue(() => scanText.Text = status);
        });

        var entryProgress = new Progress<FolderEntry>(entry =>
        {
            dispatcher.TryEnqueue(() =>
            {
                if (_rowByPath.TryGetValue(entry.Path, out var existing))
                {
                    existing.RefreshFromEntry();
                }
                else
                {
                    var row = new FolderRowViewModel(entry);
                    _rowByPath[entry.Path] = row;
                    _rows.Add(row);
                }

                ResortRows();
            });
        });

        try
        {
            var entries = await AppServices.FolderScan.ScanChildrenAsync(
                path,
                progress,
                entryProgress,
                token);

            token.ThrowIfCancellationRequested();

            dispatcher.TryEnqueue(() =>
            {
                scanText.Text = entries.Count == 0
                    ? "No subfolders in this location."
                    : $"Showing {entries.Count} folder(s) in {path}, largest first.";
                scanText.Foreground = SemanticColorHelper.GetForegroundBrush(SemanticTone.Informative);
            });
        }
        catch (OperationCanceledException)
        {
            dispatcher.TryEnqueue(() =>
            {
                scanText.Text = "Scan cancelled.";
                scanText.Foreground = SemanticColorHelper.GetForegroundBrush(SemanticTone.Attention);
            });
        }
        catch (Exception ex)
        {
            dispatcher.TryEnqueue(() =>
            {
                scanText.Text = $"Scan failed: {ex.Message}";
                scanText.Foreground = SemanticColorHelper.GetForegroundBrush(SemanticTone.Important);
            });
        }
        finally
        {
            dispatcher.TryEnqueue(() =>
            {
                scanRing.IsActive = false;
                scanRing.Visibility = Visibility.Collapsed;
                ScanStatus.CancelButton.Visibility = Visibility.Collapsed;
                ScanStatus.RescanButtonControl.Visibility = Visibility.Visible;
            });
        }
    }

    private void ResortRows()
    {
        var sorted = _rows.OrderByDescending(r => r.Entry.SizeBytes).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var currentIndex = _rows.IndexOf(sorted[i]);
            if (currentIndex >= 0 && currentIndex != i)
                _rows.Move(currentIndex, i);
        }
    }

    private void RescanButton_Click(object sender, RoutedEventArgs e)
    {
        AnimationHelper.Pulse(ScanStatus.RescanButtonControl);
        _ = RunScanAsync(_currentPath);
    }

    private void CancelScanButton_Click(object sender, RoutedEventArgs e)
    {
        AnimationHelper.Pulse(ScanStatus.CancelButton);
        _scanCts?.Cancel();
    }

    private void DrillDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string path)
            NavigateToPath(path);
    }

    private async void OpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string path)
            return;

        await AppServices.Launcher.LaunchAsync(LaunchAction.Explorer(path, "Open Folder"));
    }

    private void RelatedTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton button && button.Tag is string taskId && !string.IsNullOrEmpty(taskId))
            NavigationHelper.NavigateToTaskDetail(this, taskId);
    }
}
