using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Spacesaver.Controls;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.ViewModels;

public sealed class OrganiserProjectViewModel : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isBusy;
    private bool _isAddingFolders;
    private bool _isFolderScanActive;
    private bool _isSelectingFolders;
    private string _statusText = string.Empty;
    private SemanticTone _statusTone = SemanticTone.Informative;
    private readonly HashSet<OrganiserFolderViewModel> _subscribedFolders = [];

    public OrganiserProjectViewModel(OrganiserProject model)
    {
        Model = model;
        Folders.CollectionChanged += Folders_CollectionChanged;
        foreach (var folder in model.Folders)
            Folders.Add(new OrganiserFolderViewModel(model.Id, folder));
        RefreshTotals(null);
    }

    public OrganiserProject Model { get; }

    public ObservableCollection<OrganiserFolderViewModel> Folders { get; } = [];

    public bool IsSelectingFolders
    {
        get => _isSelectingFolders;
        private set
        {
            if (_isSelectingFolders == value)
                return;

            _isSelectingFolders = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NormalActionsVisibility));
            OnPropertyChanged(nameof(SelectionActionsVisibility));
        }
    }

    public Visibility NormalActionsVisibility =>
        IsSelectingFolders ? Visibility.Collapsed : Visibility.Visible;

    public Visibility SelectionActionsVisibility =>
        IsSelectingFolders ? Visibility.Visible : Visibility.Collapsed;

    public int SelectedFolderCount => Folders.Count(folder => folder.IsSelected);

    public string SelectedFolderCountText => SelectedFolderCount switch
    {
        0 => "No folders selected",
        1 => "1 folder selected",
        var count => $"{count} folders selected"
    };

    public bool CanRemoveSelected => SelectedFolderCount > 0 && !IsBusy;
    public bool CanBeginFolderSelection => Folders.Count > 0 && !IsBusy;

    /// <summary>Reorders the UI folder list without changing persisted model order.</summary>
    public void ApplyFolderSort(OrganiserFolderSortMode mode)
    {
        if (Folders.Count <= 1)
            return;

        var sorted = OrganiserFolderSortHelper
            .OrderByMode(Folders, folder => folder.Model, mode)
            .ToList();

        Folders.Clear();
        foreach (var folder in sorted)
            Folders.Add(folder);
    }

    public string Name => Model.Name;

    public string FolderCountText => Model.Folders.Count switch
    {
        0 => "No folders yet",
        1 => "1 folder",
        var n => $"{n} folders"
    };

    public string TotalSizeText { get; private set; } = "—";
    public string DriveBreakdownText { get; private set; } = string.Empty;
    public IReadOnlyList<BarSegment> Segments { get; private set; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value)
                return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BusyVisibility));
            OnPropertyChanged(nameof(ActionsEnabled));
            OnPropertyChanged(nameof(CanAddFolders));
            OnPropertyChanged(nameof(CanRemoveSelected));
            OnPropertyChanged(nameof(CanBeginFolderSelection));
        }
    }

    public Visibility BusyVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;
    public bool ActionsEnabled => !IsBusy;

    public bool IsAddingFolders
    {
        get => _isAddingFolders;
        set
        {
            if (_isAddingFolders == value)
                return;

            _isAddingFolders = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanAddFolders));
            OnPropertyChanged(nameof(AddFoldersButtonText));
        }
    }

    public bool IsFolderScanActive
    {
        get => _isFolderScanActive;
        set
        {
            if (_isFolderScanActive == value)
                return;

            _isFolderScanActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanAddFolders));
        }
    }

    public bool CanAddFolders => !IsAddingFolders && (!IsBusy || IsFolderScanActive);
    public string AddFoldersButtonText => IsAddingFolders ? "Opening picker…" : "Add folders";

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
                return;
            _statusText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusVisibility));
        }
    }

    public Visibility StatusVisibility =>
        string.IsNullOrEmpty(StatusText) ? Visibility.Collapsed : Visibility.Visible;

    public Microsoft.UI.Xaml.Media.Brush StatusBrush =>
        SemanticColorHelper.GetForegroundBrush(_statusTone);

    public Visibility EmptyHintVisibility =>
        Model.Folders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public string AccessibleName =>
        $"Project {Name}, {TotalSizeText} total, {FolderCountText}" +
        (string.IsNullOrEmpty(DriveBreakdownText) ? string.Empty : $", {DriveBreakdownText}");

    public event PropertyChangedEventHandler? PropertyChanged;

    public void BeginFolderSelection()
    {
        if (!CanBeginFolderSelection)
            return;

        foreach (var folder in Folders)
        {
            folder.IsSelected = false;
            folder.SetSelectionVisible(true);
        }

        IsSelectingFolders = true;
        NotifySelectionChanged();
    }

    public void CancelFolderSelection()
    {
        foreach (var folder in Folders)
        {
            folder.IsSelected = false;
            folder.SetSelectionVisible(false);
        }

        IsSelectingFolders = false;
        NotifySelectionChanged();
    }

    public void SelectAllFolders()
    {
        if (!IsSelectingFolders)
            return;

        foreach (var folder in Folders)
            folder.IsSelected = true;
    }

    public IReadOnlyList<OrganiserFolderViewModel> GetSelectedFolders() =>
        Folders.Where(folder => folder.IsSelected).ToList();

    public void SetStatus(string text, SemanticTone tone = SemanticTone.Informative)
    {
        _statusTone = tone;
        StatusText = text;
        OnPropertyChanged(nameof(StatusBrush));
    }

    public void ClearStatus() => SetStatus(string.Empty);

    /// <summary>Recomputes totals, per-drive breakdown text, and the segmented bar from cached folder sizes.</summary>
    public void RefreshTotals(IReadOnlyList<DriveSnapshot>? drives)
    {
        var total = ProjectSizeCalculator.TotalBytes(Model);
        var hasAnyMeasurement = Model.Folders.Any(f => f.LastMeasuredBytes is not null);

        TotalSizeText = hasAnyMeasurement ? ByteFormatHelper.FormatBytes(total) : "—";

        var byDrive = ProjectSizeCalculator.ByDrive(Model);
        var segments = new List<BarSegment>(byDrive.Count);
        var breakdownParts = new List<string>(byDrive.Count);

        foreach (var usage in byDrive)
        {
            var label = OrganiserPathHelper.GetDriveLabel(usage.Root);
            var offline = drives is not null
                && !drives.Any(d => d.IsReady && string.Equals(d.Root, usage.Root, StringComparison.OrdinalIgnoreCase))
                && !usage.Root.StartsWith(@"\\", StringComparison.Ordinal);

            var text = $"{label} {ByteFormatHelper.FormatBytes(usage.Bytes)}" + (offline ? " (offline)" : string.Empty);
            breakdownParts.Add(text);
            segments.Add(new BarSegment(usage.Bytes, DriveColorHelper.ForDriveRoot(usage.Root), text));
        }

        DriveBreakdownText = string.Join(" · ", breakdownParts);
        Segments = segments;

        OnPropertyChanged(nameof(TotalSizeText));
        OnPropertyChanged(nameof(DriveBreakdownText));
        OnPropertyChanged(nameof(Segments));
        OnPropertyChanged(nameof(FolderCountText));
        OnPropertyChanged(nameof(EmptyHintVisibility));
        OnPropertyChanged(nameof(AccessibleName));
    }

    public void RefreshName()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(AccessibleName));
    }

    private void Folders_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var current = Folders.ToHashSet();

        foreach (var folder in _subscribedFolders.Where(folder => !current.Contains(folder)).ToList())
        {
            folder.PropertyChanged -= Folder_PropertyChanged;
            _subscribedFolders.Remove(folder);
        }

        foreach (var folder in current.Where(folder => !_subscribedFolders.Contains(folder)))
        {
            folder.PropertyChanged += Folder_PropertyChanged;
            folder.SetSelectionVisible(IsSelectingFolders);
            _subscribedFolders.Add(folder);
        }

        OnPropertyChanged(nameof(CanBeginFolderSelection));
        NotifySelectionChanged();
    }

    private void Folder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrganiserFolderViewModel.IsSelected))
            NotifySelectionChanged();
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedFolderCount));
        OnPropertyChanged(nameof(SelectedFolderCountText));
        OnPropertyChanged(nameof(CanRemoveSelected));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
