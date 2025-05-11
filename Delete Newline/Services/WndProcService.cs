using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using System;
using Delete_Newline.Helpers;

namespace Delete_Newline.Services;

public sealed class WndProcService
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WM_HOTKEY = 0x0312;
    private const int WM_ACTIVATE = 0x0006;
    private const int GWL_WNDPROC = -4;

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private static IntPtr _oldWndProc;
    private static WndProc? _newWndProc;
    private IRegexService? _regexService;
    
    public void Initialize(IntPtr _hwnd)
    {
        _newWndProc = NewWndProc;
        IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, newWndProcPtr);

        _regexService = App.GetService<IRegexService>();
    }

    private IntPtr NewWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_ACTIVATE:
                if(LowWord(wParam.ToInt32()) == 0)
                {
                    // Todo, Inactive Window
                }
                break;

            case WM_HOTKEY:
                int hotkeyId = wParam.ToInt32();
                Debug.WriteLine($"WM_Hotkey received! ID={hotkeyId}");

                // Set the active hotkey ID expecting a copy
                App.ActiveHotkeyIdForCopy = hotkeyId;

                // Simulate Ctrl+C
                VirtualInputHelper.SendCtrlC();
                Debug.WriteLine($"Simulated Ctrl+C for Hotkey ID: {hotkeyId}");

                // We no longer process clipboard directly here.
                // OnClipboardContentChanged in MainWindow (or other handler) will pick it up.
                break;
        }
        return CallWindowProc(_oldWndProc, hWnd, (int)msg, wParam, lParam);
    }

    private static int LowWord(int value)
    {
        return value & 0xFFFF;
    }
}
