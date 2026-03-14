using System.Diagnostics;
using System.Runtime.InteropServices;
using Delete_Newline.Helpers;
using Delete_Newline.Helpers.Hotkeys;
using Delete_Newline.ViewModels;

namespace Delete_Newline.Services;

public sealed class WndProcService
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WM_HOTKEY = 0x0312;
    private const int WM_POWERBROADCAST = 0x0218;
    private const int GWL_WNDPROC = -4;
    private const int PBT_APMSUSPEND = 0x0004;
    private const int PBT_APMRESUMESUSPEND = 0x0007;
    private const int PBT_APMRESUMEAUTOMATIC = 0x0012;

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    
    private static IntPtr _oldWndProc;
    private static WndProc? _newWndProc;

    public void Initialize(IntPtr _hwnd)
    {
        _newWndProc = NewWndProc;
        IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, newWndProcPtr);
        
        Debug.WriteLine("WndProcService initialized successfully");
    }

    private IntPtr NewWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_HOTKEY:
                int hotkeyId = wParam.ToInt32();
                Debug.WriteLine($"=== WM_HOTKEY received: ID={hotkeyId} ===");
                
                // Check OCR Hotkey
                int? ocrHotkeyId = HotkeyRegister.GetOcrHotkeyId();

                if (ocrHotkeyId.HasValue && hotkeyId == ocrHotkeyId.Value)
                {
                    // Process OCR Hotkey - OCR hotkey doesn't follow normal hotkey processing
                    Debug.WriteLine("Processing OCR Hotkey - launching OCR capture");
                    App.GetService<OCRViewModel>().LaunchOcr();
                    break;
                }

                App.SetActiveHotkeyIdForCopy(hotkeyId);

                // Goto OnClipboardContentChanged()
                Debug.WriteLine("Processing normal hotkey with Ctrl+C");
                if (!VirtualInputHelper.SendCtrlC())
                {
                    // Prevent stale state if synthetic copy key injection failed.
                    App.ClearActiveHotkeyIdForCopy();
                }
                break;

            case WM_POWERBROADCAST:
                HandlePowerBroadcast(wParam.ToInt32());
                break;
        }
        return CallWindowProc(_oldWndProc, hWnd, (int)msg, wParam, lParam);
    }

    private static void HandlePowerBroadcast(int powerEvent)
    {
        switch (powerEvent)
        {
            case PBT_APMSUSPEND:
                App.ClearActiveHotkeyIdForCopy();
                Debug.WriteLine("[WndProcService] Suspend detected. Cleared pending hotkey state.");
                break;

            case PBT_APMRESUMESUSPEND:
            case PBT_APMRESUMEAUTOMATIC:
                Debug.WriteLine("[WndProcService] Resume detected. Scheduling service recovery.");
                App.GetService<ResumeRecoveryService>().ScheduleResumeRecovery();
                break;
        }
    }
}
