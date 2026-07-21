using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Spacesaver.Helpers;
using Spacesaver.Models;

namespace Spacesaver.ViewModels;

public sealed class OrganiserFolderViewModel(Guid projectId, OrganiserFolder model) : INotifyPropertyChanged
{
    private bool _isSelected;
    private Visibility _selectionVisibility = Visibility.Collapsed;

    public Guid ProjectId { get; } = projectId;
    public OrganiserFolder Model { get; } = model;

    public string Path => Model.Path;

    public string TypeLabel => OrganiserDisplayHelper.GetTypeLabel(Model.FolderType);
    public Brush TypeBrush => OrganiserDisplayHelper.GetTypeBrush(Model.FolderType);
    public Brush TypeTintBrush => OrganiserDisplayHelper.GetTypeTintBrush(Model.FolderType);

    public string DriveLabel =>
        OrganiserPathHelper.GetDriveLabel(OrganiserPathHelper.GetDriveRoot(Model.Path));

    public Brush DriveBrush =>
        DriveColorHelper.GetBrush(OrganiserPathHelper.GetDriveRoot(Model.Path));

    public string SizeText => Model.LastMeasuredBytes is { } bytes
        ? ByteFormatHelper.FormatBytes(bytes)
        : "—";

    public string MetaText =>
        $"{DriveLabel} · created {OrganiserDisplayHelper.FormatRelativeAge(Model.CreatedUtc)} · modified {OrganiserDisplayHelper.FormatRelativeAge(Model.ModifiedUtc)}";

    public string StateLabel => OrganiserDisplayHelper.GetStateLabel(Model.LastScanState) ?? string.Empty;
    public Brush StateBrush => OrganiserDisplayHelper.GetStateBrush(Model.LastScanState);

    public Visibility StateVisibility => Model.LastScanState is FolderScanState.Scanned
        ? Visibility.Collapsed
        : Visibility.Visible;

    public string AccessibleName =>
        $"{TypeLabel} folder {Model.Path}, {SizeText}, created {OrganiserDisplayHelper.FormatRelativeAge(Model.CreatedUtc)}, modified {OrganiserDisplayHelper.FormatRelativeAge(Model.ModifiedUtc)}" +
        (StateVisibility == Visibility.Visible ? $", {StateLabel}" : string.Empty);

    public string SelectionAccessibleName => $"Select folder {Model.Path}";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public Visibility SelectionVisibility
    {
        get => _selectionVisibility;
        private set
        {
            if (_selectionVisibility == value)
                return;

            _selectionVisibility = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NormalActionsVisibility));
        }
    }

    public Visibility NormalActionsVisibility =>
        SelectionVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetSelectionVisible(bool visible) =>
        SelectionVisibility = visible ? Visibility.Visible : Visibility.Collapsed;

    public void Refresh()
    {
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(TypeBrush));
        OnPropertyChanged(nameof(TypeTintBrush));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(MetaText));
        OnPropertyChanged(nameof(StateLabel));
        OnPropertyChanged(nameof(StateBrush));
        OnPropertyChanged(nameof(StateVisibility));
        OnPropertyChanged(nameof(AccessibleName));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
