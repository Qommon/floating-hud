using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WindowsPoint = System.Windows.Point;

namespace FloatingHud;

internal sealed class HudNativeWindowController(
    Window window,
    Func<bool> isHudVisible,
    Func<Rect> getHudScreenBounds) : IDisposable
{
    private const int GwlExStyle = -20;
    private const int GwlpHwndParent = -8;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const int SwpNoSize = 0x0001;
    private const int SwpNoMove = 0x0002;
    private const int SwpNoActivate = 0x0010;
    private const int SwpNoOwnerZOrder = 0x0200;
    private const int WmNcHitTest = 0x0084;
    private const int HtTransparent = -1;
    private static readonly nint HwndTopmost = new(-1);

    private HwndSource? hwndSource;
    private nint taskbarHandle;
    private nint windowHandle;

    public void Initialize()
    {
        WindowInteropHelper helper = new(window);
        windowHandle = helper.Handle;
        AttachToTaskbarOwner();

        int extendedStyle = GetWindowLong(windowHandle, GwlExStyle);
        SetWindowLong(windowHandle, GwlExStyle, extendedStyle | WsExToolWindow | WsExNoActivate);
        SetTopmostState(HwndTopmost);

        hwndSource = HwndSource.FromHwnd(windowHandle);
        hwndSource?.AddHook(WindowMessageHook);
    }

    public void Dispose()
    {
        hwndSource?.RemoveHook(WindowMessageHook);
        hwndSource = null;
    }

    public static WindowsPoint GetCursorScreenPoint()
    {
        return GetCursorPos(out NativePoint point)
            ? new WindowsPoint(point.X, point.Y)
            : new WindowsPoint(0, 0);
    }

    private void AttachToTaskbarOwner()
    {
        if (taskbarHandle == 0 || !IsWindow(taskbarHandle))
        {
            taskbarHandle = FindWindow("Shell_TrayWnd", null);
        }

        if (taskbarHandle != 0)
        {
            SetWindowLongPtr(windowHandle, GwlpHwndParent, taskbarHandle);
        }
    }

    private void SetTopmostState(nint insertAfter)
    {
        if (windowHandle == 0)
        {
            return;
        }

        SetWindowPos(
            windowHandle,
            insertAfter,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate | SwpNoOwnerZOrder);
    }

    private nint WindowMessageHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != WmNcHitTest)
        {
            return 0;
        }

        if (!isHudVisible())
        {
            handled = true;
            return HtTransparent;
        }

        WindowsPoint screenPoint = GetScreenPointFromLParam(lParam);
        if (getHudScreenBounds().Contains(screenPoint))
        {
            return 0;
        }

        handled = true;
        return HtTransparent;
    }

    private static WindowsPoint GetScreenPointFromLParam(nint lParam)
    {
        long value = lParam.ToInt64();
        int x = unchecked((short)(value & 0xFFFF));
        int y = unchecked((short)((value >> 16) & 0xFFFF));
        return new WindowsPoint(x, y);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", EntryPoint = "FindWindowW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        int uFlags);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
