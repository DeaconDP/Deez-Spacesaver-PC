using Spacesaver.Helpers;
using Spacesaver.Models;

namespace Spacesaver.Services;

public sealed class FolderScanService
{
    private const int MaxParallelSizing = 4;

    public async Task<IReadOnlyList<FolderEntry>> ScanChildrenAsync(
        string rootPath,
        IProgress<string>? progress = null,
        IProgress<FolderEntry>? entryProgress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(
            () => ScanChildrenCore(rootPath, progress, entryProgress, cancellationToken),
            cancellationToken);
    }

    private static List<FolderEntry> ScanChildrenCore(
        string rootPath,
        IProgress<string>? progress,
        IProgress<FolderEntry>? entryProgress,
        CancellationToken cancellationToken)
    {
        progress?.Report($"Listing folders in {rootPath}…");
        cancellationToken.ThrowIfCancellationRequested();

        var childPaths = FileSystemHelper.EnumerateChildFolderPaths(rootPath);
        if (childPaths.Count == 0)
        {
            progress?.Report("No subfolders found.");
            return [];
        }

        var entries = new List<FolderEntry>(childPaths.Count);
        foreach (var childPath in childPaths)
        {
            var (importance, reason, relatedTaskId) = FolderClassifier.Classify(childPath);
            var entry = new FolderEntry
            {
                Path = childPath,
                Name = Path.GetFileName(childPath) ?? childPath,
                ChildFolderCount = FileSystemHelper.CountChildFolders(childPath),
                Importance = importance,
                ClassificationReason = reason,
                RelatedTaskId = relatedTaskId,
                ScanState = FolderScanState.Pending
            };
            entries.Add(entry);
            entryProgress?.Report(entry);
        }

        var completed = 0;
        var lockObj = new object();

        Parallel.ForEach(
            entries,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxParallelSizing,
                CancellationToken = cancellationToken
            },
            entry =>
            {
                lock (lockObj)
                {
                    completed++;
                    progress?.Report($"Sizing {completed} of {entries.Count}: {entry.Name}…");
                }

                entry.ScanState = FolderScanState.Scanning;
                entryProgress?.Report(entry);

                try
                {
                    var result = FileSystemHelper.MeasureDirectory(entry.Path, cancellationToken);
                    entry.SizeBytes = result.Bytes;
                    entry.ScanState = result.IsInaccessible
                        ? FolderScanState.Inaccessible
                        : result.IsPartial
                            ? FolderScanState.Partial
                            : FolderScanState.Complete;

                    if (result.IsInaccessible && entry.SizeBytes == 0)
                        entry.ClassificationReason += " Access denied — size may be incomplete.";
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    entry.ScanState = FolderScanState.Inaccessible;
                    entry.ClassificationReason += " Could not measure this folder.";
                }

                entryProgress?.Report(entry);
            });

        var totalBytes = entries.Sum(e => e.SizeBytes);
        if (totalBytes > 0)
        {
            foreach (var entry in entries)
                entry.PercentOfParent = entry.SizeBytes * 100.0 / totalBytes;
        }

        entries.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
        progress?.Report($"Found {entries.Count} folder(s).");

        foreach (var entry in entries)
            entryProgress?.Report(entry);

        return entries;
    }
}
