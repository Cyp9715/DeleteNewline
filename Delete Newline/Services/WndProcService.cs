using System.Diagnostics;
using System.Runtime.InteropServices;
using Delete_Newline.Helpers;
using Delete_Newline.Views;
using Delete_Newline.ViewModels;

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
    
    // OCR hotkey ID (Fix value)
    private const int OCR_HOTKEY_ID = 9999;
    
    public void Initialize(IntPtr _hwnd)
    {
        _newWndProc = NewWndProc;
        IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, newWndProcPtr);
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

                // Check if this is the OCR hotkey
                if (hotkeyId == OCR_HOTKEY_ID)
                {
                    Debug.WriteLine("OCR Hotkey detected - launching OCR capture");
                    LaunchOcrCapture();
                    break;
                }

                // Set the active hotkey ID expecting a copy (기존 텍스트 처리 핫키)
                App.ActiveHotkeyIdForCopy = hotkeyId;

                // Simulate Ctrl+C
                VirtualInputHelper.SendCtrlC();
                Debug.WriteLine($"Simulated Ctrl+C for Hotkey ID: {hotkeyId}");

                // We no longer process clipboard directly here.
                // check ClipboardMonitorService.cs
                break;
        }
        return CallWindowProc(_oldWndProc, hWnd, (int)msg, wParam, lParam);
    }

    private void LaunchOcrCapture()
    {
        try
        {
            Debug.WriteLine("Starting OCR capture from hotkey...");
            
            // Get OCRViewModel instance and call its launch method
            var ocrViewModel = App.GetService<OCRViewModel>();
            ocrViewModel.LaunchOcrCapture();
            
            Debug.WriteLine("OCR capture launched successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error launching OCR capture: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static int LowWord(int value)
    {
        return value & 0xFFFF;
    }
}
