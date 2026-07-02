namespace Spacesaver.Services;

public static class AppServices
{
    public static ProgressService Progress { get; } = new();
    public static AppSettingsService Settings { get; } = new();
    public static WindowsLauncherService Launcher { get; } = new();
    public static DiskScanService Scan { get; } = new();
    public static CleanupExecutionService Cleanup { get; } = new();
    public static FolderScanService FolderScan { get; } = new();
}
