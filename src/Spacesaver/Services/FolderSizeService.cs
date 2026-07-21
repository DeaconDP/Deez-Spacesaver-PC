using Spacesaver.Helpers;
using Spacesaver.Models;

namespace Spacesaver.Services;

/// <summary>
/// Recursively measures folder sizes off the UI thread with cancellation and progress.
/// A disconnected drive reports Offline (not zero bytes); unreadable entries mark the result Partial.
/// </summary>
public sealed class FolderSizeService
{
    private const int CancellationCheckInterval = 256;

    public Task<FolderMeasureResult> MeasureAsync(
        string path,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => MeasureCore(path, progress, cancellationToken), cancellationToken);
    }

    private static FolderMeasureResult MeasureCore(
        string path,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var root = OrganiserPathHelper.GetDriveRoot(path);
        if (root is null || !IsDriveReady(root))
            return new FolderMeasureResult(0, FolderScanState.Offline, "Drive is not connected.");

        if (!Directory.Exists(path))
            return new FolderMeasureResult(0, FolderScanState.Missing, "Folder no longer exists on this drive.");

        long total = 0;
        var partial = false;
        var fileCount = 0;

        var pending = new Stack<string>();
        pending.Push(path);

        try
        {
            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Pop();

                try
                {
                    foreach (var directory in Directory.EnumerateDirectories(current))
                        pending.Push(directory);

                    foreach (var file in Directory.EnumerateFiles(current))
                    {
                        if (++fileCount % CancellationCheckInterval == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            progress?.Report($"Measuring… {ByteFormatHelper.FormatBytes(total)} so far");
                        }

                        try
                        {
                            total += new FileInfo(file).Length;
                        }
                        catch (IOException)
                        {
                            partial = true;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            partial = true;
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    partial = true;
                }
                catch (DirectoryNotFoundException)
                {
                    partial = true;
                }
                catch (IOException)
                {
                    if (!IsDriveReady(root))
                        return new FolderMeasureResult(total, FolderScanState.Offline, "Drive was disconnected during the scan.");
                    partial = true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (!IsDriveReady(root))
                return new FolderMeasureResult(total, FolderScanState.Offline, "Drive was disconnected during the scan.");
            return new FolderMeasureResult(total, FolderScanState.Error, ex.Message);
        }

        return partial
            ? new FolderMeasureResult(total, FolderScanState.Partial, "Some entries could not be read; size is a lower bound.")
            : new FolderMeasureResult(total, FolderScanState.Scanned, null);
    }

    private static bool IsDriveReady(string root)
    {
        try
        {
            // UNC roots have no DriveInfo; reachability check is the best signal.
            if (root.StartsWith(@"\\", StringComparison.Ordinal))
                return Directory.Exists(root);

            return new DriveInfo(root).IsReady;
        }
        catch
        {
            return false;
        }
    }
}
