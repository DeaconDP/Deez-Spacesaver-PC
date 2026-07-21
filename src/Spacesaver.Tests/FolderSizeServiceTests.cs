using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Tests;

public class FolderSizeServiceTests : IDisposable
{
    private readonly string _tempDir;

    public FolderSizeServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SpacesaverTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Temp cleanup best-effort.
        }
    }

    [Fact]
    public async Task MeasureAsync_SumsFilesRecursively()
    {
        File.WriteAllBytes(Path.Combine(_tempDir, "a.bin"), new byte[100]);
        var sub = Directory.CreateDirectory(Path.Combine(_tempDir, "sub"));
        File.WriteAllBytes(Path.Combine(sub.FullName, "b.bin"), new byte[250]);

        var result = await new FolderSizeService().MeasureAsync(_tempDir);

        Assert.Equal(350, result.Bytes);
        Assert.Equal(FolderScanState.Scanned, result.State);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task MeasureAsync_MissingFolderOnReadyDrive_ReportsMissing()
    {
        var missing = Path.Combine(_tempDir, "does-not-exist");
        var result = await new FolderSizeService().MeasureAsync(missing);

        Assert.Equal(FolderScanState.Missing, result.State);
        Assert.Equal(0, result.Bytes);
    }

    [Fact]
    public async Task MeasureAsync_UnavailableDrive_ReportsOffline()
    {
        // Find a drive letter that doesn't exist on this machine.
        var used = DriveInfo.GetDrives()
            .Select(d => char.ToUpperInvariant(d.Name[0]))
            .ToHashSet();
        var freeLetter = "ZYXWVUTSRQPONMLKJIHGFEDCBA".FirstOrDefault(c => !used.Contains(c));
        Assert.NotEqual('\0', freeLetter);

        var result = await new FolderSizeService().MeasureAsync($@"{freeLetter}:\Backups\Alpha");

        Assert.Equal(FolderScanState.Offline, result.State);
        Assert.Equal(0, result.Bytes);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task MeasureAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new FolderSizeService().MeasureAsync(_tempDir, null, cts.Token));
    }
}
