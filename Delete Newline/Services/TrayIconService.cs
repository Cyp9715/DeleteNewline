using Delete_Newline.Contracts.Services;
using System.Runtime.InteropServices;
namespace Delete_Newline.Services;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct NOTIFYICONDATA
{
    public int cbSize;
    public IntPtr hWnd;
    public uint uID;
    public uint uFlags;
    public uint uCallbackMessage;
    public IntPtr hIcon;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szTip;
    public int dwState;
    public int dwStateMask;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string szInfo;
    public uint uTimeoutOrVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string szInfoTitle;
    public int dwInfoFlags;
}

public sealed class TrayIconService
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hInstance, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern bool Shell_NotifyIcon(uint dwMessage, in NOTIFYICONDATA lpData);

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint WM_TRAYICON = 0x8000;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;

    private IntPtr _hwnd;
    private readonly NotificationService _notificationService;

    public TrayIconService(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void Initialize(IntPtr hwnd)
    {
        if(hwnd == IntPtr.Zero)
        {
            // just debugging.
            System.Diagnostics.Debug.WriteLine("Failed to initialize tray icon. hwnd is null.");
            return;
        }

        _hwnd = hwnd;
        AddTrayIcon();
    }

    private IntPtr LoadIcon()
    {
        string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "favicon_64x64.ico");
        return LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 64, 64, LR_LOADFROMFILE);
    }

    public void AddTrayIcon()
    {
        if (_hwnd == IntPtr.Zero)
        {
            // just debugging.
            System.Diagnostics.Debug.WriteLine("Failed to add tray icon. _hwnd is null.");
            return;
        }

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE,
            uCallbackMessage = WM_TRAYICON,
            hIcon = LoadIcon(),
            szTip = "Delete Newline"
        };

        bool success = Shell_NotifyIcon(NIM_ADD, nid);
        if (success == false)
        {
            // just debugging.
            System.Diagnostics.Debug.WriteLine("Failed to add tray icon.");
        }
    }

    public void RemoveTrayIcon()
    {
        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1
        };

        Shell_NotifyIcon(NIM_DELETE, nid);
    }


    /* 
     * Context Menu Sector.
     */
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    // Add POINT structure and DllImports
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    private const uint MF_CHECKED = 0x00000008;
    private const uint MF_STRING = 0x00000000;

    public const int ID_EXIT = 1;
    public const int ID_NOTIFICATION = 2;

    public void ShowContextMenu()
    {
        IntPtr hMenu = CreatePopupMenu();
        AppendMenu(hMenu, 0, (IntPtr)ID_EXIT, "Exit Delete Newline");

        // Notification 메뉴 추가 (체크 상태 적용)
        uint notificationFlags = MF_STRING | (_notificationService.GetEnableNotification() ? MF_CHECKED : 0);
        AppendMenu(hMenu, notificationFlags, (IntPtr)ID_NOTIFICATION, "Notification");

        GetCursorPos(out POINT pt);
        SetForegroundWindow(_hwnd);

        TrackPopupMenuEx(hMenu, 0, pt.X, pt.Y, _hwnd, IntPtr.Zero);
        DestroyMenu(hMenu);
    }
}
