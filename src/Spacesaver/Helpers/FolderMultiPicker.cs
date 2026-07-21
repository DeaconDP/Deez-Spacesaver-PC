using System.Runtime.InteropServices;

namespace Spacesaver.Helpers;

/// <summary>
/// Result from the native multi-select folder dialog.
/// </summary>
/// <param name="Cancelled">User dismissed the dialog.</param>
/// <param name="Paths">Selected folder paths (empty when cancelled or failed).</param>
/// <param name="Error">Non-null when the dialog could not return a selection.</param>
public sealed record PickFoldersResult(bool Cancelled, IReadOnlyList<string> Paths, string? Error);

/// <summary>
/// Native Windows multi-select folder dialog via IFileOpenDialog.
/// WinUI's FolderPicker only supports a single folder; this exposes FOS_ALLOWMULTISELECT.
/// Call from the UI (STA) thread — Show is modal and pumps its own message loop.
/// </summary>
public static class FolderMultiPicker
{
    private const int FOS_PICKFOLDERS = 0x00000020;
    private const int FOS_FORCEFILESYSTEM = 0x00000040;
    private const int FOS_ALLOWMULTISELECT = 0x00000200;
    private const int FOS_PATHMUSTEXIST = 0x00000800;
    private const int SIGDN_FILESYSPATH = unchecked((int)0x80058000);
    private const int S_OK = 0;
    private const int HRESULT_CANCELLED = unchecked((int)0x800704C7);

    private static readonly Guid ClsidFileOpenDialog = new("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");

    public static Task<PickFoldersResult> PickFoldersAsync(nint ownerHwnd) =>
        Task.FromResult(PickFolders(ownerHwnd));

    public static PickFoldersResult PickFolders(nint ownerHwnd)
    {
        try
        {
            var dialog = (IFileOpenDialog)Activator.CreateInstance(Type.GetTypeFromCLSID(ClsidFileOpenDialog)!)!;

            try
            {
                dialog.GetOptions(out var options);
                dialog.SetOptions(
                    options | FOS_PICKFOLDERS | FOS_ALLOWMULTISELECT | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST);
                dialog.SetTitle("Select folders");

                var hr = dialog.Show(ownerHwnd);
                if (hr == HRESULT_CANCELLED)
                    return new PickFoldersResult(true, [], null);

                if (hr != S_OK)
                    return new PickFoldersResult(false, [], FormatHResult(hr));

                dialog.GetResults(out var results);
                if (results is null)
                    return new PickFoldersResult(false, [], "Folder dialog returned no selection.");

                try
                {
                    results.GetCount(out var count);
                    if (count == 0)
                        return new PickFoldersResult(false, [], "No folders were selected.");

                    var paths = new List<string>((int)count);

                    for (uint i = 0; i < count; i++)
                    {
                        results.GetItemAt(i, out var item);
                        if (item is null)
                            continue;

                        try
                        {
                            item.GetDisplayName(SIGDN_FILESYSPATH, out var path);
                            if (!string.IsNullOrWhiteSpace(path))
                                paths.Add(path);
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(item);
                        }
                    }

                    if (paths.Count == 0)
                        return new PickFoldersResult(false, [], "Could not read the selected folder path(s).");

                    return new PickFoldersResult(false, paths, null);
                }
                finally
                {
                    Marshal.ReleaseComObject(results);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }
        catch (Exception ex)
        {
            return new PickFoldersResult(false, [], $"Folder picker failed: {ex.Message}");
        }
    }

    private static string FormatHResult(int hr) =>
        $"Folder picker failed (0x{hr & 0xFFFFFFFF:X8}).";

    // IID_IShellItem = {43826D1E-E718-42EE-BC55-A1E261C37BFE}
    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
        void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void GetDisplayName(int sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [ComImport]
    [Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray
    {
        void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppvOut);
        void GetPropertyStore(int flags, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
        void GetPropertyDescriptionList([In] IntPtr keyType, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
        void GetAttributes(int AttribFlags, uint sfgaoMask, out uint psfgaoAttribs);
        void GetCount(out uint pdwNumItems);
        void GetItemAt(uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void EnumItems(out IntPtr ppenumShellItems);
    }

    /// <summary>
    /// Full IFileOpenDialog vtable (IModalWindow → IFileDialog → IFileOpenDialog).
    /// Methods we do not call are stubs with the correct signatures for layout.
    /// </summary>
    [ComImport]
    [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        // IModalWindow
        [PreserveSig]
        int Show(IntPtr parent);

        // IFileDialog
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(int fos);
        void GetOptions(out int pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        void AddPlace(IShellItem psi, int fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);

        // IFileOpenDialog
        void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);
        void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);
    }
}
