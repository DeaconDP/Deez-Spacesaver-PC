using Spacesaver.Models;

namespace Spacesaver.Services;

public sealed class DiskScanService
{
    private const long HighImpactBytes = 1L * 1024 * 1024 * 1024;
    private const long MediumImpactBytes = 100L * 1024 * 1024;
    private const double DiskPressureThreshold = 85.0;

    public async Task<DiskScanResult> ScanAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ScanCore(progress, cancellationToken), cancellationToken);
    }

    private static DiskScanResult ScanCore(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        progress?.Report("Reading drive information…");
        cancellationToken.ThrowIfCancellationRequested();

        var systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
        var driveInfo = new DriveInfo(systemDrive.TrimEnd('\\'));
        var totalBytes = driveInfo.IsReady ? driveInfo.TotalSize : 0;
        var freeBytes = driveInfo.IsReady ? driveInfo.AvailableFreeSpace : 0;
        var usedPercent = totalBytes > 0
            ? (totalBytes - freeBytes) * 100.0 / totalBytes
            : 0;

        var recommendations = new List<CleanupRecommendation>();

        progress?.Report("Scanning Recycle Bin…");
        cancellationToken.ThrowIfCancellationRequested();
        AddMeasured(recommendations, "recycle-bin", MeasureRecycleBinBytes(), ExecutionMode.AutoClean,
            bytes => $"Found {FormatBytes(bytes)} in Recycle Bin.");

        progress?.Report("Scanning user Temp folder…");
        cancellationToken.ThrowIfCancellationRequested();
        AddMeasured(recommendations, "user-temp", MeasureDirectoryBytes(Environment.GetEnvironmentVariable("TEMP")),
            ExecutionMode.AutoClean, bytes => $"Found {FormatBytes(bytes)} in your Temp folder.");

        progress?.Report("Checking for Windows.old…");
        cancellationToken.ThrowIfCancellationRequested();
        AddMeasured(recommendations, "windows-old", MeasureDirectoryBytes(@"C:\Windows.old"),
            ExecutionMode.LaunchTool, bytes => $"Previous Windows installation uses {FormatBytes(bytes)}.",
            forceHighImpact: true);

        progress?.Report("Checking hibernation file…");
        cancellationToken.ThrowIfCancellationRequested();
        AddMeasured(recommendations, "hibernation", MeasureFileBytes(@"C:\hiberfil.sys"),
            ExecutionMode.ManualOnly, bytes => $"Hibernation file uses {FormatBytes(bytes)}.",
            forceHighImpact: true);

        progress?.Report("Scanning delivery optimization cache…");
        cancellationToken.ThrowIfCancellationRequested();
        var deliveryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            @"ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache");
        AddMeasured(recommendations, "delivery-optimization", MeasureDirectoryBytes(deliveryPath),
            ExecutionMode.LaunchTool, bytes => $"Delivery optimization cache is {FormatBytes(bytes)}.");

        progress?.Report("Scanning browser caches…");
        cancellationToken.ThrowIfCancellationRequested();
        AddMeasured(recommendations, "browser-cache", MeasureBrowserCacheBytes(),
            ExecutionMode.LaunchTool, bytes => $"Browser caches total {FormatBytes(bytes)}.");

        progress?.Report("Scanning Downloads folder…");
        cancellationToken.ThrowIfCancellationRequested();
        var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        AddMeasured(recommendations, "downloads", MeasureDirectoryBytes(downloadsPath),
            ExecutionMode.ManualOnly, bytes => $"Downloads folder contains {FormatBytes(bytes)} — review before deleting.");

        if (usedPercent >= DiskPressureThreshold)
        {
            progress?.Report("Disk is nearly full — adding suggested tasks…");
            AddConfiguration(recommendations, "temp-files-settings", "Your drive is over 85% full. Temporary files cleanup is recommended.");
            AddConfiguration(recommendations, "disk-cleanup", "Running Disk Cleanup can recover space quickly on a full drive.");
            AddConfiguration(recommendations, "disk-cleanup-system", "System-level Disk Cleanup finds Windows Update and delivery caches.");
        }
        else
        {
            AddConfiguration(recommendations, "storage-sense", "Enable automatic cleanup to prevent future space issues.");
        }

        var applicable = recommendations
            .Where(r => r.IsApplicable)
            .OrderByDescending(r => r.Impact)
            .ThenByDescending(r => r.ReclaimableBytes)
            .ToList();

        return new DiskScanResult
        {
            DriveName = driveInfo.Name,
            TotalBytes = totalBytes,
            FreeBytes = freeBytes,
            UsedPercent = usedPercent,
            Recommendations = applicable
        };
    }

    private static void AddMeasured(
        List<CleanupRecommendation> list,
        string taskId,
        long bytes,
        ExecutionMode mode,
        Func<long, string> detailFactory,
        bool forceHighImpact = false)
    {
        var task = TaskCatalog.GetById(taskId);
        if (task is null)
            return;

        var effectiveMode = ResolveExecutionMode(task, mode);
        var applicable = bytes > 0 || effectiveMode == ExecutionMode.LaunchTool;
        if (!applicable && task.RiskLevel != RiskLevel.Safe)
            return;

        var impact = forceHighImpact && bytes > 0
            ? ImpactLevel.High
            : ClassifyImpact(bytes, task);

        var recommendation = new CleanupRecommendation
        {
            TaskId = taskId,
            Task = task,
            ReclaimableBytes = bytes,
            Impact = impact,
            ExecutionMode = effectiveMode,
            ScanDetail = bytes > 0 ? detailFactory(bytes) : "Nothing significant found — optional maintenance.",
            RiskSummary = CleanupRecommendation.BuildRiskSummary(task),
            IsApplicable = applicable || bytes > 0,
            IsSelected = CleanupRecommendation.DefaultSelected(task, effectiveMode, bytes) && bytes > 0
        };

        if (recommendation.IsApplicable)
            list.Add(recommendation);
    }

    private static void AddConfiguration(List<CleanupRecommendation> list, string taskId, string detail)
    {
        if (list.Any(r => r.TaskId.Equals(taskId, StringComparison.OrdinalIgnoreCase)))
            return;

        var task = TaskCatalog.GetById(taskId);
        if (task is null)
            return;

        list.Add(new CleanupRecommendation
        {
            TaskId = taskId,
            Task = task,
            ReclaimableBytes = 0,
            Impact = ImpactLevel.Low,
            ExecutionMode = ResolveExecutionMode(task, ExecutionMode.LaunchTool),
            ScanDetail = detail,
            RiskSummary = CleanupRecommendation.BuildRiskSummary(task),
            IsApplicable = true,
            IsSelected = false
        });
    }

    private static ExecutionMode ResolveExecutionMode(CleanupTask task, ExecutionMode requested)
    {
        if (task.RiskLevel == RiskLevel.Advanced || !string.IsNullOrWhiteSpace(task.Warning))
            return requested == ExecutionMode.AutoClean ? ExecutionMode.LaunchTool : requested;

        if (task.RiskLevel == RiskLevel.Caution && requested == ExecutionMode.AutoClean)
            return ExecutionMode.LaunchTool;

        return requested;
    }

    private static ImpactLevel ClassifyImpact(long bytes, CleanupTask task)
    {
        if (bytes >= HighImpactBytes)
            return ImpactLevel.High;

        if (bytes >= MediumImpactBytes)
            return ImpactLevel.Medium;

        if (task.Category == TaskCategory.LargeSpaceWins && bytes > 0)
            return ImpactLevel.High;

        return ImpactLevel.Low;
    }

    private static long MeasureRecycleBinBytes()
    {
        try
        {
            var recycleRoot = Path.Combine(
                Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\",
                "$Recycle.Bin");

            if (!Directory.Exists(recycleRoot))
                return 0;

            var userSid = GetCurrentUserSid();
            if (userSid is not null)
            {
                var userBin = Path.Combine(recycleRoot, userSid);
                if (Directory.Exists(userBin))
                    return MeasureDirectoryBytes(userBin);
            }

            return MeasureDirectoryBytes(recycleRoot);
        }
        catch
        {
            return 0;
        }
    }

    private static string? GetCurrentUserSid()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return identity.User?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static long MeasureBrowserCacheBytes()
    {
        var total = 0L;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var edgePaths = new[]
        {
            Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache"),
            Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Code Cache"),
            Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Service Worker\CacheStorage")
        };

        foreach (var path in edgePaths)
            total += MeasureDirectoryBytes(path);

        var chromePaths = new[]
        {
            Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"),
            Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Code Cache"),
            Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Service Worker\CacheStorage")
        };

        foreach (var path in chromePaths)
            total += MeasureDirectoryBytes(path);

        return total;
    }

    private static long MeasureFileBytes(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static long MeasureDirectoryBytes(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return 0;

        long total = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                    // Skip inaccessible files.
                }
            }
        }
        catch
        {
            // Skip inaccessible directories.
        }

        return total;
    }

    public static string FormatBytes(long bytes) => Helpers.ByteFormatHelper.FormatBytes(bytes);
}
