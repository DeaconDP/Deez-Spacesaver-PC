using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Dialogs;
using Spacesaver.Helpers;
using Spacesaver.Models;
using Spacesaver.Services;
using Spacesaver.ViewModels;

namespace Spacesaver.Coordinators;

public sealed class CleanupWizardCoordinator
{
    private readonly ObservableCollection<RecommendationRowViewModel> _rows = [];
    private DiskScanResult? _scanResult;
    private CancellationTokenSource? _scanCts;
    private bool _isApplying;

    public ObservableCollection<RecommendationRowViewModel> Rows => _rows;

    public DiskScanResult? ScanResult => _scanResult;

    public bool IsApplying => _isApplying;

    public event Action? SelectionChanged;

    public event Action<string>? ScanStatusChanged;

    public event Action<DiskScanResult>? ScanCompleted;

    public void CancelScan() => _scanCts?.Cancel();

    public async Task RunScanAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _scanCts?.Cancel();
        _scanCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _scanCts.Token;

        _rows.Clear();
        ScanStatusChanged?.Invoke("Scanning your drive…");

        try
        {
            _scanResult = await AppServices.Scan.ScanAsync(progress, token);
            token.ThrowIfCancellationRequested();

            foreach (var recommendation in _scanResult.Recommendations)
            {
                var row = new RecommendationRowViewModel(recommendation);
                row.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(RecommendationRowViewModel.IsSelected))
                        SelectionChanged?.Invoke();
                };
                _rows.Add(row);
            }

            ScanStatusChanged?.Invoke($"Found {_rows.Count} recommendation(s). Select what you want to apply.");
            ScanCompleted?.Invoke(_scanResult);
        }
        catch (OperationCanceledException)
        {
            ScanStatusChanged?.Invoke("Scan cancelled.");
        }
        catch (Exception ex)
        {
            ScanStatusChanged?.Invoke($"Scan failed: {ex.Message}");
        }
        finally
        {
            SelectionChanged?.Invoke();
        }
    }

    public IReadOnlyList<CleanupRecommendation> GetSelectedRecommendations() =>
        _rows.Where(r => r.IsSelected).Select(r => r.Recommendation).ToList();

    public (int Count, long Bytes) GetSelectionSummary()
    {
        var selected = _rows.Where(r => r.IsSelected).ToList();
        return (selected.Count, selected.Sum(r => r.Recommendation.ReclaimableBytes));
    }

    public async Task<IReadOnlyList<ApplyActionResult>> ApplySelectedAsync(
        IProgress<(string TaskId, string Status)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var selected = GetSelectedRecommendations();
        if (selected.Count == 0)
            return [];

        _isApplying = true;
        try
        {
            return await AppServices.Cleanup.ApplyAsync(selected, progress, cancellationToken);
        }
        finally
        {
            _isApplying = false;
            SelectionChanged?.Invoke();
        }
    }

    public static async Task<bool> ConfirmApplyAsync(
        XamlRoot xamlRoot,
        IReadOnlyList<CleanupRecommendation> selected)
    {
        return await ConfirmCleanupDialog.ShowAsync(xamlRoot, selected);
    }

    public static async Task ShowResultsAsync(
        XamlRoot xamlRoot,
        IReadOnlyList<ApplyActionResult> results)
    {
        await CleanupResultsDialog.ShowAsync(xamlRoot, results);
    }
}
