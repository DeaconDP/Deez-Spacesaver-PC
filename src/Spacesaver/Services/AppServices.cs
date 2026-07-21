namespace Spacesaver.Services;

public static class AppServices
{
    public static ProgressService Progress { get; } = new();
    public static WindowsLauncherService Launcher { get; } = new();
    public static DiskScanService Scan { get; } = new();
    public static CleanupExecutionService Cleanup { get; } = new();
    public static ProjectOrganiserService Organiser { get; } = new();
    public static FolderSizeService FolderSize { get; } = new();
    public static DriveInventoryService Drives { get; } = new();
    public static AppSettingsService Settings { get; } = new();
    public static FolderSuggestionService Suggestions { get; } = new();
}
