using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;
using Spacesaver.ViewModels;
using WinRT.Interop;

namespace Spacesaver.Pages;

public sealed partial class ProjectOrganiserPage : Page
{
    private readonly ObservableCollection<OrganiserProjectViewModel> _projects = [];
    private readonly List<(OrganiserProjectViewModel Project, OrganiserFolderViewModel Folder)> _pendingScanItems = [];
    private readonly HashSet<Guid> _queuedOrScanningFolderIds = [];
    private IReadOnlyList<DriveSnapshot> _drives = [];
    private CancellationTokenSource? _scanCts;
    private bool _isScanning;
    private bool _isFolderSizeScanning;
    private int _scanCompleted;
    private int _scanTotal;
    private bool _suppressSortComboChange;

    public ProjectOrganiserPage()
    {
        InitializeComponent();
        ProjectsList.ItemsSource = _projects;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        SyncFolderSortCombo(AppServices.Settings.OrganiserFolderSort);
        LoadProjects();
        await AnimationHelper.PlayStaggerEntranceAsync([HeaderPanel, ToolbarPanel]);
        AnimationHelper.AttachHoverLift(NewProjectButton);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => _scanCts?.Cancel();

    private void LoadProjects()
    {
        AppServices.Organiser.RefreshFolderTimestamps();

        _projects.Clear();
        var sortMode = AppServices.Settings.OrganiserFolderSort;
        foreach (var project in AppServices.Organiser.Projects)
        {
            var viewModel = new OrganiserProjectViewModel(project);
            viewModel.ApplyFolderSort(sortMode);
            _projects.Add(viewModel);
        }

        RefreshDrivesPanel();
        UpdateEmptyState();

        SetPageStatus(
            _projects.Count == 0
                ? string.Empty
                : "Showing cached sizes. Refresh to rescan folders.",
            SemanticTone.Informative);
    }

    private OrganiserFolderSortMode CurrentFolderSortMode =>
        AppServices.Settings.OrganiserFolderSort;

    private void SyncFolderSortCombo(OrganiserFolderSortMode mode)
    {
        _suppressSortComboChange = true;
        try
        {
            foreach (var item in FolderSortComboBox.Items.OfType<ComboBoxItem>())
            {
                if (item.Tag is string tag &&
                    Enum.TryParse<OrganiserFolderSortMode>(tag, out var itemMode) &&
                    itemMode == mode)
                {
                    FolderSortComboBox.SelectedItem = item;
                    return;
                }
            }

            FolderSortComboBox.SelectedIndex = 1; // Location default
        }
        finally
        {
            _suppressSortComboChange = false;
        }
    }

    private void FolderSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSortComboChange)
            return;

        if (FolderSortComboBox.SelectedItem is not ComboBoxItem { Tag: string tag } ||
            !Enum.TryParse<OrganiserFolderSortMode>(tag, out var mode))
            return;

        AppServices.Settings.SetOrganiserFolderSort(mode);
        ApplyFolderSortToAll();
    }

    private void ApplyFolderSortToAll()
    {
        var mode = CurrentFolderSortMode;
        foreach (var project in _projects)
            project.ApplyFolderSort(mode);
    }

    private void ApplyFolderSort(OrganiserProjectViewModel project) =>
        project.ApplyFolderSort(CurrentFolderSortMode);

    private void UpdateEmptyState()
    {
        var isEmpty = _projects.Count == 0;
        EmptyStatePanel.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        ProjectsList.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        RefreshAllButton.IsEnabled = !isEmpty && !_isScanning;
        SuggestionsAllButton.IsEnabled = !isEmpty && !_isScanning;
    }

    private void RefreshDrivesPanel()
    {
        _drives = AppServices.Drives.GetDrives();
        DrivesTable.Children.Clear();
        DrivesTable.RowDefinitions.Clear();
        DrivesTable.ColumnDefinitions.Clear();

        DrivesTable.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        DrivesTable.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        DrivesTable.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        DrivesTable.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        DrivesTable.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        DrivesTable.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

        var metaStyle = (Style)Application.Current.Resources["MetaTextStyle"];
        var mutedBrush = (Brush)Application.Current.Resources["AppMutedBrush"];
        var borderBrush = (Brush)Application.Current.Resources["AppBorderBrush"];
        var surfaceBrush = (Brush)Application.Current.Resources["AppSurfaceBrush"];

        DrivesTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddDriveTableHeaderCell("DRIVE", 1, 0, metaStyle, mutedBrush);
        AddDriveTableHeaderCell("TYPE", 2, 0, metaStyle, mutedBrush);
        AddDriveTableHeaderCell("USED", 3, 0, metaStyle, mutedBrush);
        AddDriveTableHeaderCell("CAPACITY", 4, 0, metaStyle, mutedBrush);

        for (var i = 0; i < _drives.Count; i++)
        {
            var drive = _drives[i];
            var rowIndex = i + 1;
            DrivesTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = OrganiserPathHelper.GetDriveLabel(drive.Root);
            var volumeName = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? null : drive.VolumeLabel;
            var driveName = volumeName is null ? label : $"{label} {volumeName}";
            var typeLabel = drive.DriveType switch
            {
                DriveType.Fixed => "internal",
                DriveType.Removable => "USB",
                DriveType.Network => "network",
                DriveType.CDRom => "disc",
                _ => "drive"
            };

            var usedPercent = drive.IsReady && drive.TotalBytes > 0
                ? (drive.TotalBytes - drive.FreeBytes) * 100.0 / drive.TotalBytes
                : 0;
            var usedText = drive.IsReady ? $"{usedPercent:0}%" : "—";
            var capacityText = drive.IsReady
                ? $"{ByteFormatHelper.FormatBytes(drive.FreeBytes)} free / {ByteFormatHelper.FormatBytes(drive.TotalBytes)}"
                : "offline";
            var accessibleName = drive.IsReady
                ? $"{driveName}, {typeLabel}, {usedText} used, {capacityText}"
                : $"{driveName}, {typeLabel}, offline";

            var swatch = new Rectangle
            {
                Width = 10,
                Height = 10,
                RadiusX = 1,
                RadiusY = 1,
                VerticalAlignment = VerticalAlignment.Center,
                Fill = DriveColorHelper.GetBrush(drive.Root),
                Opacity = drive.IsReady ? 1.0 : 0.4
            };
            Grid.SetColumn(swatch, 0);
            Grid.SetRow(swatch, rowIndex);
            DrivesTable.Children.Add(swatch);

            var nameBlock = new TextBlock
            {
                Text = driveName,
                VerticalAlignment = VerticalAlignment.Center,
                Style = metaStyle,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            if (!drive.IsReady)
                nameBlock.Foreground = SemanticColorHelper.GetForegroundBrush(SemanticTone.Attention);
            Grid.SetColumn(nameBlock, 1);
            Grid.SetRow(nameBlock, rowIndex);
            DrivesTable.Children.Add(nameBlock);

            var typeBlock = new TextBlock
            {
                Text = typeLabel,
                VerticalAlignment = VerticalAlignment.Center,
                Style = metaStyle,
                Foreground = mutedBrush
            };
            Grid.SetColumn(typeBlock, 2);
            Grid.SetRow(typeBlock, rowIndex);
            DrivesTable.Children.Add(typeBlock);

            var usedBlock = new TextBlock
            {
                Text = usedText,
                VerticalAlignment = VerticalAlignment.Center,
                Style = metaStyle,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                MinWidth = 40
            };
            usedBlock.Foreground = drive.IsReady
                ? SemanticColorHelper.GetForegroundBrush(SemanticColorHelper.ForDiskUsage(usedPercent))
                : SemanticColorHelper.GetForegroundBrush(SemanticTone.Attention);
            Grid.SetColumn(usedBlock, 3);
            Grid.SetRow(usedBlock, rowIndex);
            DrivesTable.Children.Add(usedBlock);

            var capacityBlock = new TextBlock
            {
                Text = capacityText,
                VerticalAlignment = VerticalAlignment.Center,
                Style = metaStyle,
                Foreground = drive.IsReady ? mutedBrush : SemanticColorHelper.GetForegroundBrush(SemanticTone.Attention)
            };
            Grid.SetColumn(capacityBlock, 4);
            Grid.SetRow(capacityBlock, rowIndex);
            DrivesTable.Children.Add(capacityBlock);

            var bar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = drive.IsReady ? usedPercent : 0,
                Height = 8,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 80,
                Foreground = DriveColorHelper.GetBrush(drive.Root),
                Background = surfaceBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                Opacity = drive.IsReady ? 1.0 : 0.35,
                IsHitTestVisible = false
            };
            AutomationProperties.SetName(bar, accessibleName);
            Grid.SetColumn(bar, 5);
            Grid.SetRow(bar, rowIndex);
            DrivesTable.Children.Add(bar);
        }

        foreach (var project in _projects)
            project.RefreshTotals(_drives);
    }

    private void AddDriveTableHeaderCell(string text, int column, int row, Style metaStyle, Brush mutedBrush)
    {
        var header = new TextBlock
        {
            Text = text,
            Style = metaStyle,
            Foreground = mutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(header, column);
        Grid.SetRow(header, row);
        DrivesTable.Children.Add(header);
    }

    private void SetPageStatus(string text, SemanticTone tone)
    {
        PageStatusText.Text = text;
        PageStatusText.Foreground = tone == SemanticTone.Informative
            ? (Brush)Application.Current.Resources["AppMutedBrush"]
            : SemanticColorHelper.GetForegroundBrush(tone);
    }

    // ---- Project actions -------------------------------------------------

    private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var name = await PromptForNameAsync("New project", "Create");
        if (string.IsNullOrWhiteSpace(name))
            return;

        var project = AppServices.Organiser.CreateProject(name);
        var viewModel = new OrganiserProjectViewModel(project) { IsExpanded = true };
        _projects.Add(viewModel);
        viewModel.RefreshTotals(_drives);
        UpdateEmptyState();
        SetPageStatus($"Project \"{name}\" created. Add its folders to see sizes.", SemanticTone.Good);
    }

    private async void RenameProject_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not OrganiserProjectViewModel project)
            return;

        var name = await PromptForNameAsync("Rename project", "Rename", project.Name);
        if (string.IsNullOrWhiteSpace(name) || name == project.Name)
            return;

        AppServices.Organiser.RenameProject(project.Model.Id, name);
        project.RefreshName();
    }

    private async void RemoveProject_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not OrganiserProjectViewModel project)
            return;

        var confirmed = await ConfirmAsync(
            "Remove project",
            $"Remove \"{project.Name}\" from the organiser? " +
            "Only the organiser entry is removed — no folders or files on disk are touched.");
        if (!confirmed)
            return;

        AppServices.Organiser.RemoveProject(project.Model.Id);
        _projects.Remove(project);
        UpdateEmptyState();
        SetPageStatus($"Project \"{project.Name}\" removed from the organiser.", SemanticTone.Informative);
    }

    // ---- Folder actions --------------------------------------------------

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not OrganiserProjectViewModel project)
            return;

        if (!project.CanAddFolders)
            return;

        project.IsAddingFolders = true;
        try
        {
            IReadOnlyList<string> paths;
            try
            {
                paths = await PickFoldersWithFallbackAsync(project);
                if (paths.Count == 0)
                    return;
            }
            catch (Exception ex)
            {
                project.SetStatus($"Could not add folders — {ex.Message}", SemanticTone.Attention);
                return;
            }

            var added = new List<OrganiserFolderViewModel>();
            var duplicateCount = 0;
            var errorCount = 0;
            var overlapCount = 0;
            string? firstError = null;

            foreach (var path in paths)
            {
                var result = AppServices.Organiser.AddFolder(project.Model.Id, path);
                if (!result.Added || result.Folder is null)
                {
                    if (result.Error is not null &&
                        result.Error.Contains("already part of this project", StringComparison.OrdinalIgnoreCase))
                    {
                        duplicateCount++;
                    }
                    else
                    {
                        errorCount++;
                        firstError ??= result.Error;
                    }

                    continue;
                }

                if (result.Warning is not null)
                    overlapCount++;

                var folderViewModel = new OrganiserFolderViewModel(project.Model.Id, result.Folder);
                project.Folders.Add(folderViewModel);
                added.Add(folderViewModel);
            }

            project.RefreshTotals(_drives);
            ApplyBatchAddStatus(project, added.Count, duplicateCount, overlapCount, errorCount, firstError);
            if (added.Count > 0)
                ApplyFolderSort(project);

            if (added.Count > 0)
                _ = ScanFoldersAsync(added.Select(folder => (project, folder)).ToList());
        }
        finally
        {
            project.IsAddingFolders = false;
        }
    }

    private static nint GetOwnerHwnd() =>
        App.MainAppWindow is { } window
            ? WindowNative.GetWindowHandle(window)
            : nint.Zero;

    private static async Task<IReadOnlyList<string>> PickFoldersWithFallbackAsync(OrganiserProjectViewModel project)
    {
        var pickResult = await FolderMultiPicker.PickFoldersAsync(GetOwnerHwnd());

        if (pickResult.Cancelled)
            return [];

        if (pickResult.Error is null && pickResult.Paths.Count > 0)
            return pickResult.Paths;

        var fallbackPath = await PickSingleFolderAsync();
        if (fallbackPath is not null)
            return [fallbackPath];

        if (pickResult.Error is not null)
            project.SetStatus($"Could not open folder picker — {pickResult.Error}", SemanticTone.Attention);

        return [];
    }

    private static async Task<string?> PickSingleFolderAsync()
    {
        var picker = new Windows.Storage.Pickers.FolderPicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add("*");

        if (App.MainAppWindow is { } window)
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private static void ApplyBatchAddStatus(
        OrganiserProjectViewModel project,
        int addedCount,
        int duplicateCount,
        int overlapCount,
        int errorCount,
        string? firstError)
    {
        if (addedCount == 0)
        {
            var failParts = new List<string>();
            if (duplicateCount > 0)
                failParts.Add($"{duplicateCount} already in project");
            if (errorCount > 0)
                failParts.Add(firstError ?? $"{errorCount} could not be added");

            project.SetStatus(
                failParts.Count > 0
                    ? $"No folders added — {string.Join("; ", failParts)}."
                    : "No folders added.",
                SemanticTone.Attention);
            return;
        }

        var parts = new List<string> { $"Added {addedCount} folder(s)" };
        if (duplicateCount > 0)
            parts.Add($"{duplicateCount} already in project");
        if (overlapCount > 0)
            parts.Add($"{overlapCount} overlap(s) an existing folder");
        if (errorCount > 0)
            parts.Add($"{errorCount} failed");

        var tone = duplicateCount > 0 || overlapCount > 0 || errorCount > 0
            ? SemanticTone.Important
            : SemanticTone.Good;

        project.SetStatus(string.Join(". ", parts) + ".", tone);
    }

    private void FolderType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: OrganiserFolderViewModel folder, Tag: string tag })
            return;

        var folderType = Enum.Parse<ProjectFolderType>(tag);
        AppServices.Organiser.SetFolderType(folder.ProjectId, folder.Model.Id, folderType);
        folder.Refresh();

        var project = _projects.FirstOrDefault(p => p.Model.Id == folder.ProjectId);
        if (project is not null && CurrentFolderSortMode == OrganiserFolderSortMode.Tag)
            ApplyFolderSort(project);
    }

    private async void RescanFolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not OrganiserFolderViewModel folder)
            return;

        var project = _projects.FirstOrDefault(p => p.Model.Id == folder.ProjectId);
        if (project is null)
            return;

        await ScanFoldersAsync([(project, folder)]);
    }

    private void ViewFolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not OrganiserFolderViewModel folder)
            return;

        var project = _projects.FirstOrDefault(p => p.Model.Id == folder.ProjectId);
        if (project is null)
            return;

        if (!Directory.Exists(folder.Path))
        {
            project.SetStatus(
                $"Cannot open folder — path is missing or offline: {folder.Path}",
                SemanticTone.Attention);
            return;
        }

        var (ok, error) = AppServices.Launcher.OpenFolder(folder.Path);
        if (!ok)
        {
            project.SetStatus(
                $"Could not open folder — {error ?? "unknown error"}",
                SemanticTone.Attention);
        }
    }

    private void SelectFolders_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not OrganiserProjectViewModel project)
            return;

        foreach (var otherProject in _projects.Where(item => !ReferenceEquals(item, project)))
        {
            if (otherProject.IsSelectingFolders)
                otherProject.CancelFolderSelection();
        }

        project.BeginFolderSelection();
    }

    private void SelectAllFolders_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is OrganiserProjectViewModel project)
            project.SelectAllFolders();
    }

    private void CancelFolderSelection_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is OrganiserProjectViewModel project)
            project.CancelFolderSelection();
    }

    private async void RemoveSelectedFolders_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not OrganiserProjectViewModel project)
            return;

        var selected = project.GetSelectedFolders();
        if (selected.Count == 0 || project.IsBusy)
            return;

        var label = selected.Count == 1 ? "1 folder" : $"{selected.Count} folders";
        var confirmed = await ConfirmAsync(
            "Remove selected folders",
            $"Remove {label} from \"{project.Name}\"? " +
            "The folders and their files stay exactly where they are on disk.");
        if (!confirmed)
            return;

        project.IsBusy = true;
        try
        {
            var selectedIds = selected.Select(folder => folder.Model.Id).ToHashSet();
            var removedCount = AppServices.Organiser.RemoveFolders(project.Model.Id, selectedIds);
            var remainingIds = project.Model.Folders.Select(folder => folder.Id).ToHashSet();

            foreach (var folder in selected.Where(folder => !remainingIds.Contains(folder.Model.Id)).ToList())
                project.Folders.Remove(folder);

            project.CancelFolderSelection();
            project.RefreshTotals(_drives);

            if (removedCount == selected.Count)
            {
                project.SetStatus(
                    $"Removed {label} from the organiser. No files were touched.",
                    SemanticTone.Good);
            }
            else
            {
                project.SetStatus(
                    $"Removed {removedCount} of {selected.Count} selected folders. No files were touched.",
                    SemanticTone.Attention);
            }
        }
        finally
        {
            project.IsBusy = false;
        }
    }

    private async void RemoveFolder_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not OrganiserFolderViewModel folder)
            return;

        var project = _projects.FirstOrDefault(p => p.Model.Id == folder.ProjectId);
        if (project is null)
            return;

        var confirmed = await ConfirmAsync(
            "Remove folder",
            $"Remove \"{folder.Path}\" from \"{project.Name}\"? " +
            "The folder and its files stay exactly where they are on disk.");
        if (!confirmed)
            return;

        AppServices.Organiser.RemoveFolder(project.Model.Id, folder.Model.Id);
        project.Folders.Remove(folder);
        project.RefreshTotals(_drives);
    }

    // ---- Scanning --------------------------------------------------------

    private async void RefreshProject_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not OrganiserProjectViewModel project)
            return;

        var items = project.Folders
            .Select(folder => (project, folder))
            .ToList();

        if (items.Count == 0)
        {
            project.SetStatus("Nothing to scan — add a folder first.", SemanticTone.Informative);
            return;
        }

        await ScanFoldersAsync(items);
    }

    private async void RefreshAllButton_Click(object sender, RoutedEventArgs e)
    {
        var items = _projects
            .SelectMany(project => project.Folders.Select(folder => (project, folder)))
            .ToList();

        if (items.Count == 0)
        {
            SetPageStatus("No folders to scan yet. Add folders to your projects first.", SemanticTone.Informative);
            return;
        }

        await ScanFoldersAsync(items);
    }

    private async void SuggestionsAll_Click(object sender, RoutedEventArgs e)
    {
        if (_isScanning || _projects.Count == 0)
            return;

        var models = _projects.Select(p => p.Model).ToList();
        await RunSuggestionsAsync(models, excludeIfAttachedAnywhere: true, statusProject: null);
    }

    private async void SuggestionsProject_Click(object sender, RoutedEventArgs e)
    {
        if (_isScanning)
            return;

        if ((sender as FrameworkElement)?.DataContext is not OrganiserProjectViewModel project)
            return;

        await RunSuggestionsAsync([project.Model], excludeIfAttachedAnywhere: false, statusProject: project);
    }

    private async Task RunSuggestionsAsync(
        IReadOnlyList<OrganiserProject> scopeProjects,
        bool excludeIfAttachedAnywhere,
        OrganiserProjectViewModel? statusProject)
    {
        if (_isScanning || scopeProjects.Count == 0)
            return;

        _isScanning = true;
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        SetScanUi(scanning: true);
        if (statusProject is not null)
            statusProject.IsBusy = true;
        else
            foreach (var project in _projects)
                project.IsBusy = true;

        SetPageStatus("Finding suggestions…", SemanticTone.Informative);

        IReadOnlyList<FolderSuggestion> suggestions;
        var cancelled = false;
        try
        {
            var readyRoots = _drives
                .Where(d => d.IsReady)
                .Select(d => d.Root)
                .ToList();

            suggestions = await Task.Run(
                () => AppServices.Suggestions.FindSuggestions(
                    scopeProjects,
                    readyRoots,
                    AppServices.Organiser.Projects,
                    excludeIfAttachedAnywhere,
                    token),
                token);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            suggestions = [];
        }
        finally
        {
            _isScanning = false;
            if (statusProject is not null)
                statusProject.IsBusy = false;
            else
                foreach (var project in _projects)
                    project.IsBusy = false;
            SetScanUi(scanning: false);
        }

        if (cancelled)
        {
            SetPageStatus("Suggestion search cancelled.", SemanticTone.Attention);
            return;
        }

        if (suggestions.Count == 0)
        {
            var emptyMessage = "No suggestions found.";
            if (statusProject is not null)
                statusProject.SetStatus(emptyMessage, SemanticTone.Informative);
            SetPageStatus(emptyMessage, SemanticTone.Informative);
            return;
        }

        var selected = await ShowSuggestionsDialogAsync(suggestions);
        if (selected is null || selected.Count == 0)
        {
            SetPageStatus(
                selected is null
                    ? "Suggestions dismissed."
                    : "No folders selected.",
                SemanticTone.Informative);
            return;
        }

        await AddSuggestedFoldersAsync(selected);
    }

    private async Task<IReadOnlyList<FolderSuggestion>?> ShowSuggestionsDialogAsync(
        IReadOnlyList<FolderSuggestion> suggestions)
    {
        var items = suggestions
            .Select(s => new SuggestionDialogItem(s))
            .ToList();

        var list = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            MaxHeight = 360,
            ItemsSource = items
        };
        list.ItemTemplate = CreateSuggestionItemTemplate();

        var dialog = new ContentDialog
        {
            Title = suggestions.Count == 1
                ? "1 suggestion"
                : $"{suggestions.Count} suggestions",
            Content = list,
            PrimaryButtonText = "Add selected",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return null;

        return items
            .Where(i => i.IsSelected)
            .Select(i => i.Suggestion)
            .ToList();
    }

    private static DataTemplate CreateSuggestionItemTemplate()
    {
        // Built in code so we stay page-local without a new XAML resource.
        const string xaml =
            """
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
              <CheckBox IsChecked="{Binding IsSelected, Mode=TwoWay}" Margin="0,4,0,4">
                <StackPanel Spacing="2">
                  <TextBlock Text="{Binding PathText}" TextWrapping="WrapWholeWords" />
                  <TextBlock Text="{Binding MetaText}" Opacity="0.7" FontSize="12" TextWrapping="WrapWholeWords" />
                </StackPanel>
              </CheckBox>
            </DataTemplate>
            """;

        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
    }

    private async Task AddSuggestedFoldersAsync(IReadOnlyList<FolderSuggestion> selected)
    {
        var added = new List<(OrganiserProjectViewModel Project, OrganiserFolderViewModel Folder)>();
        var byProject = selected.GroupBy(s => s.ProjectId);

        foreach (var group in byProject)
        {
            var project = _projects.FirstOrDefault(p => p.Model.Id == group.Key);
            if (project is null)
                continue;

            var addedCount = 0;
            var duplicateCount = 0;
            var errorCount = 0;
            var overlapCount = 0;
            string? firstError = null;

            foreach (var suggestion in group)
            {
                var result = AppServices.Organiser.AddFolder(project.Model.Id, suggestion.Path);
                if (!result.Added || result.Folder is null)
                {
                    if (result.Error is not null &&
                        result.Error.Contains("already part of this project", StringComparison.OrdinalIgnoreCase))
                        duplicateCount++;
                    else
                    {
                        errorCount++;
                        firstError ??= result.Error;
                    }

                    continue;
                }

                if (result.Warning is not null)
                    overlapCount++;

                var folderViewModel = new OrganiserFolderViewModel(project.Model.Id, result.Folder);
                project.Folders.Add(folderViewModel);
                added.Add((project, folderViewModel));
                addedCount++;
            }

            project.RefreshTotals(_drives);
            ApplyFolderSort(project);
            ApplyBatchAddStatus(project, addedCount, duplicateCount, overlapCount, errorCount, firstError);
        }

        if (added.Count > 0)
        {
            SetPageStatus($"Added {added.Count} suggested folder(s). Scanning…", SemanticTone.Good);
            await ScanFoldersAsync(added);
        }
        else
        {
            SetPageStatus("No suggested folders were added.", SemanticTone.Attention);
        }
    }

    private void CancelScanButton_Click(object sender, RoutedEventArgs e)
    {
        CancelScanButton.IsEnabled = false;
        if (_isFolderSizeScanning)
            _pendingScanItems.Clear();
        _scanCts?.Cancel();
    }

    private async Task ScanFoldersAsync(
        List<(OrganiserProjectViewModel Project, OrganiserFolderViewModel Folder)> items)
    {
        if (items.Count == 0)
            return;

        if (_isScanning && !_isFolderSizeScanning)
            return;

        if (!_isFolderSizeScanning)
        {
            _pendingScanItems.Clear();
            _queuedOrScanningFolderIds.Clear();
            _scanCompleted = 0;
            _scanTotal = 0;
        }

        var queuedCount = EnqueueScanItems(items);
        if (queuedCount == 0)
            return;

        if (_isFolderSizeScanning)
        {
            SetPageStatus(
                $"Queued {queuedCount} folder(s). Scanned {_scanCompleted} of {_scanTotal} folder(s)…",
                SemanticTone.Informative);
            return;
        }

        _isScanning = true;
        _isFolderSizeScanning = true;
        var scanCts = new CancellationTokenSource();
        _scanCts = scanCts;
        var token = scanCts.Token;

        SetScanUi(scanning: true);
        foreach (var project in _projects)
        {
            project.IsFolderScanActive = true;
            project.IsBusy = true;
        }

        var byteProgress = new Progress<string>(status => PageStatusText.Text = status);
        var processedItems = new List<(OrganiserProjectViewModel Project, OrganiserFolderViewModel Folder)>();

        void OnFolderDone(OrganiserFolderViewModel folder)
        {
            _queuedOrScanningFolderIds.Remove(folder.Model.Id);
            _scanCompleted++;
            SetPageStatus($"Scanned {_scanCompleted} of {_scanTotal} folder(s)…", SemanticTone.Informative);
        }

        var cancelled = false;
        try
        {
            while (_pendingScanItems.Count > 0)
            {
                token.ThrowIfCancellationRequested();

                var batch = _pendingScanItems.ToList();
                _pendingScanItems.Clear();
                processedItems.AddRange(batch);

                var groups = batch.GroupBy(
                    item => OrganiserPathHelper.GetDriveRoot(item.Folder.Model.Path) ?? "?",
                    StringComparer.OrdinalIgnoreCase);

                // One folder at a time per drive to avoid thrashing; drives run in parallel.
                await Task.WhenAll(groups.Select(group =>
                    ScanGroupAsync(group.ToList(), byteProgress, token, OnFolderDone)));
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        finally
        {
            _pendingScanItems.Clear();
            _queuedOrScanningFolderIds.Clear();
            _isFolderSizeScanning = false;
            _isScanning = false;
            foreach (var project in _projects)
            {
                project.IsBusy = false;
                project.IsFolderScanActive = false;
            }
            SetScanUi(scanning: false);
            RefreshDrivesPanel();

            foreach (var project in processedItems.Select(item => item.Project).Distinct())
                ApplyFolderSort(project);

            if (ReferenceEquals(_scanCts, scanCts))
                _scanCts = null;
            scanCts.Dispose();
        }

        if (cancelled)
        {
            SetPageStatus(
                $"Scan cancelled after {_scanCompleted} of {_scanTotal} folder(s). Remaining sizes are unchanged.",
                SemanticTone.Attention);
            return;
        }

        var issues = processedItems.Count(item => item.Folder.Model.LastScanState
            is FolderScanState.Offline or FolderScanState.Error or FolderScanState.Missing or FolderScanState.Partial);

        if (issues > 0)
            SetPageStatus(
                $"Scanned {_scanTotal} folder(s) — {issues} with warnings (offline, missing, or partly unreadable).",
                SemanticTone.Important);
        else
            SetPageStatus($"Scanned {_scanTotal} folder(s). All sizes are up to date.", SemanticTone.Good);
    }

    private int EnqueueScanItems(
        IEnumerable<(OrganiserProjectViewModel Project, OrganiserFolderViewModel Folder)> items)
    {
        var queuedCount = 0;
        foreach (var item in items)
        {
            if (!_queuedOrScanningFolderIds.Add(item.Folder.Model.Id))
                continue;

            _pendingScanItems.Add(item);
            _scanTotal++;
            queuedCount++;
        }

        return queuedCount;
    }

    private async Task ScanGroupAsync(
        List<(OrganiserProjectViewModel Project, OrganiserFolderViewModel Folder)> group,
        IProgress<string> byteProgress,
        CancellationToken token,
        Action<OrganiserFolderViewModel> onFolderDone)
    {
        foreach (var (project, folder) in group)
        {
            token.ThrowIfCancellationRequested();

            FolderMeasureResult result;
            try
            {
                result = await AppServices.FolderSize.MeasureAsync(folder.Model.Path, byteProgress, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result = new FolderMeasureResult(0, FolderScanState.Error, ex.Message);
            }

            AppServices.Organiser.UpdateFolderScan(project.Model.Id, folder.Model.Id, result);
            folder.Refresh();
            project.RefreshTotals(_drives);
            onFolderDone(folder);
        }
    }

    private void SetScanUi(bool scanning)
    {
        ScanProgressRing.IsActive = scanning;
        ScanProgressRing.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
        CancelScanButton.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
        CancelScanButton.IsEnabled = scanning;
        RefreshAllButton.IsEnabled = !scanning && _projects.Count > 0;
        SuggestionsAllButton.IsEnabled = !scanning && _projects.Count > 0;
        NewProjectButton.IsEnabled = !scanning;
        FolderSortComboBox.IsEnabled = !scanning;
    }

    // ---- Dialogs & pickers -----------------------------------------------

    private async Task<string?> PromptForNameAsync(string title, string primaryLabel, string initial = "")
    {
        var textBox = new TextBox
        {
            Text = initial,
            PlaceholderText = "Project name",
            SelectionStart = initial.Length
        };
        AutomationProperties.SetName(textBox, "Project name");

        var dialog = new ContentDialog
        {
            Title = title,
            Content = textBox,
            PrimaryButtonText = primaryLabel,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(initial),
            XamlRoot = XamlRoot
        };

        textBox.TextChanged += (_, _) =>
            dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(textBox.Text);

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? textBox.Text.Trim() : null;
    }

    private async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.WrapWholeWords },
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private sealed class SuggestionDialogItem(FolderSuggestion suggestion)
    {
        public FolderSuggestion Suggestion { get; } = suggestion;
        public bool IsSelected { get; set; } = true;
        public string PathText => Suggestion.Path;
        public string MetaText => $"{Suggestion.ProjectName} · {Suggestion.Reason}";
    }
}
