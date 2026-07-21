using Spacesaver.Models;

namespace Spacesaver.Services;

public sealed class DriveInventoryService
{
    public IReadOnlyList<DriveSnapshot> GetDrives()
    {
        var drives = new List<DriveSnapshot>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                var ready = drive.IsReady;
                drives.Add(new DriveSnapshot
                {
                    Root = drive.Name,
                    VolumeLabel = ready ? drive.VolumeLabel : null,
                    DriveType = drive.DriveType,
                    IsReady = ready,
                    TotalBytes = ready ? drive.TotalSize : 0,
                    FreeBytes = ready ? drive.AvailableFreeSpace : 0
                });
            }
            catch
            {
                drives.Add(new DriveSnapshot { Root = drive.Name, IsReady = false });
            }
        }

        return drives;
    }
}
