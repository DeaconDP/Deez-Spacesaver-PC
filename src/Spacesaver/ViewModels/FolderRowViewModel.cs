using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Spacesaver.Helpers;
using Spacesaver.Models;

namespace Spacesaver.ViewModels;

public sealed class FolderRowViewModel : INotifyPropertyChanged
{
    public FolderRowViewModel(FolderEntry entry)
    {
        Entry = entry;
        RefreshBrushes();
    }

    public FolderEntry Entry { get; }

    public string Name => Entry.Name;
    public string Path => Entry.Path;
    public string ClassificationReason => Entry.ClassificationReason;
    public string? RelatedTaskId => Entry.RelatedTaskId;

    public string FormattedSize => Entry.ScanState is FolderScanState.Pending or FolderScanState.Scanning
        ? "Calculating…"
        : Entry.ScanState == FolderScanState.Inaccessible && Entry.SizeBytes == 0
            ? "Access denied"
            : FileSystemHelper.FormatBytes(Entry.SizeBytes);

    public string PercentLabel => Entry.ScanState is FolderScanState.Pending or FolderScanState.Scanning
        ? "—"
        : $"{Entry.PercentOfParent:0.#}% of parent";

    public string ChildFolderLabel => Entry.ChildFolderCount == 1
        ? "1 subfolder"
        : $"{Entry.ChildFolderCount} subfolders";

    public string ImportanceLabel => Entry.Importance switch
    {
        FolderImportance.Critical => "Critical",
        FolderImportance.Important => "Important",
        FolderImportance.Reclaimable => "Reclaimable",
        _ => "Review"
    };

    public Visibility RelatedTaskVisibility => string.IsNullOrEmpty(Entry.RelatedTaskId)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility DrillDownVisibility => Entry.ChildFolderCount > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Brush ImportanceBrush { get; private set; } =
        SemanticColorHelper.GetAccentBrush(SemanticTone.Informative);

    public Brush ImportanceTintBrush { get; private set; } =
        SemanticColorHelper.GetTintBrush(SemanticTone.Informative);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshFromEntry()
    {
        OnPropertyChanged(nameof(FormattedSize));
        OnPropertyChanged(nameof(PercentLabel));
        OnPropertyChanged(nameof(ChildFolderLabel));
        OnPropertyChanged(nameof(ClassificationReason));
        OnPropertyChanged(nameof(RelatedTaskVisibility));
        RefreshBrushes();
    }

    public void RefreshBrushes()
    {
        var tone = SemanticColorHelper.ForFolderImportance(Entry.Importance);
        ImportanceBrush = SemanticColorHelper.GetAccentBrush(tone);
        ImportanceTintBrush = SemanticColorHelper.GetTintBrush(tone);
        OnPropertyChanged(nameof(ImportanceBrush));
        OnPropertyChanged(nameof(ImportanceTintBrush));
        OnPropertyChanged(nameof(ImportanceLabel));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
