namespace Spacesaver.Models;

public enum FolderScanState
{
    NotScanned,
    Scanned,

    /// <summary>Scan finished but some entries were unreadable; size is a lower bound.</summary>
    Partial,

    /// <summary>The folder's drive is not connected; cached size is preserved.</summary>
    Offline,

    /// <summary>Drive is connected but the folder no longer exists.</summary>
    Missing,
    Error
}
