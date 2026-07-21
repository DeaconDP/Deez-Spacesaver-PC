using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Spacesaver.Helpers;

/// <summary>
/// Enforces a minimum client size via WM_GETMINMAXINFO (WinAppSDK has no OverlappedPresenter.SetMinSize).
/// </summary>
internal static class WindowMinSizeHelper
{
    private const int GwlpWndProc = -4;
    private const uint WmGetMinMaxInfo = 0x0024;

    private static readonly Dictionary<nint, MinSizeState> States = new();

    public static void SetMinSize(Window window, int minWidthDip, int minHeightDip)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        if (hwnd == nint.Zero)
            return;

        if (States.TryGetValue(hwnd, out var existing))
        {
            existing.MinWidthDip = minWidthDip;
            existing.MinHeightDip = minHeightDip;
            return;
        }

        var state = new MinSizeState
        {
            MinWidthDip = minWidthDip,
            MinHeightDip = minHeightDip
        };

        state.NewWndProc = (h, msg, wParam, lParam) =>
        {
            if (!States.TryGetValue(h, out var s))
                return nint.Zero;

            if (msg == WmGetMinMaxInfo)
            {
                var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
                var scale = GetDpiScale(h);
                info.ptMinTrackSize.X = (int)Math.Ceiling(s.MinWidthDip * scale);
                info.ptMinTrackSize.Y = (int)Math.Ceiling(s.MinHeightDip * scale);
                Marshal.StructureToPtr(info, lParam, false);
            }

            return CallWindowProc(s.OldWndProc, h, msg, wParam, lParam);
        };

        state.OldWndProc = SetWindowLongPtr(hwnd, GwlpWndProc,
            Marshal.GetFunctionPointerForDelegate(state.NewWndProc));
        States[hwnd] = state;
    }

    private static double GetDpiScale(nint hwnd)
    {
        var dpi = GetDpiForWindow(hwnd);
        return dpi <= 0 ? 1.0 : dpi / 96.0;
    }

    private delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

    private sealed class MinSizeState
    {
        public nint OldWndProc;
        public WndProc NewWndProc = null!;
        public int MinWidthDip;
        public int MinHeightDip;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point ptReserved;
        public Point ptMaxSize;
        public Point ptMaxPosition;
        public Point ptMinTrackSize;
        public Point ptMaxTrackSize;
    }

    [DllImport("user32.dll")]
    private static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hWnd);
}
