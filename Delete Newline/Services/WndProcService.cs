using System.Diagnostics;
using System.Runtime.InteropServices;
using Delete_Newline.Helpers;
using Delete_Newline.Views;
using Delete_Newline.ViewModels;
using Windows.System;

namespace Delete_Newline.Services;

public sealed class WndProcService
{
    private readonly OCRService _ocrService;
    private readonly HotkeyCollectSaveService _hotkeyCollectSaveService;
    private readonly HotkeyRegisterService _hotkeyRegisterService;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WM_HOTKEY = 0x0312;
    private const int WM_ACTIVATE = 0x0006;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int GWL_WNDPROC = -4;

    // Mouse hook constants
    private const int WH_MOUSE_LL = 14;
    private const int HC_ACTION = 0;

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    private static IntPtr _oldWndProc;
    private static WndProc? _newWndProc;
    private static IntPtr _mouseHookID = IntPtr.Zero;
    private static LowLevelMouseProc? _mouseProc;
    private static WndProcService? _instance;

    public WndProcService(OCRService ocrService, HotkeyCollectSaveService hotkeyCollectSaveService, HotkeyRegisterService hotkeyRegisterService)
    {
        _ocrService = ocrService;
        _hotkeyCollectSaveService = hotkeyCollectSaveService;
        _hotkeyRegisterService = hotkeyRegisterService;
        _instance = this;
    }

    public void Initialize(IntPtr _hwnd)
    {
        _newWndProc = NewWndProc;
        IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, newWndProcPtr);

        // Set up global mouse hook
        _mouseProc = MouseHookProc;
        _mouseHookID = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle("user32.dll"), 0);
        
        if (_mouseHookID == IntPtr.Zero)
        {
            Debug.WriteLine("Failed to install mouse hook");
        }
        else
        {
            Debug.WriteLine("Mouse hook installed successfully");
        }
    }

    public void Dispose()
    {
        if (_mouseHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookID);
            _mouseHookID = IntPtr.Zero;
            Debug.WriteLine("Mouse hook uninstalled");
        }
    }

    private static IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= HC_ACTION && _instance != null)
        {
            int message = wParam.ToInt32();
            
            // Detect mouse clicks
            if (message == WM_LBUTTONDOWN || message == WM_RBUTTONDOWN || message == WM_MBUTTONDOWN)
            {
                _instance._ocrService.NotifyUserInputDetected();
                Debug.WriteLine($"Global mouse click detected: {message}");
            }
        }
        
        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
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

            case WM_KEYDOWN:
            case WM_KEYUP:
                // Detect keyboard input that would disable OCR → Hotkey matching
                int vKey = wParam.ToInt32();
                if (vKey != 27) // Ignore ESC key (used for OCR window closure)
                {
                    _ocrService.NotifyUserInputDetected();
                }
                break;

            case WM_LBUTTONDOWN:
            case WM_RBUTTONDOWN:
            case WM_MBUTTONDOWN:
                // Detect mouse clicks that would disable OCR → Hotkey matching
                _ocrService.NotifyUserInputDetected();
                break;

            case WM_HOTKEY:
                int hotkeyId = wParam.ToInt32();
                Debug.WriteLine($"=== WM_Hotkey received ===");
                Debug.WriteLine($"Hotkey ID: {hotkeyId}");

                // Check if this is the OCR hotkey (dynamically determine OCR hotkey ID)
                int? ocrHotkeyId = _hotkeyRegisterService.GetOcrHotkeyId();
                bool isOcrHotkey = ocrHotkeyId.HasValue && hotkeyId == ocrHotkeyId.Value;
                
                Debug.WriteLine($"OCR Hotkey ID: {ocrHotkeyId}");
                Debug.WriteLine($"Is OCR Hotkey: {isOcrHotkey}");

                if (isOcrHotkey)
                {
                    Debug.WriteLine("=== OCR Hotkey detected - launching OCR capture ===");
                    LaunchOcrCapture();
                    Debug.WriteLine("=== OCR Hotkey processing completed - returning immediately ===");
                    return CallWindowProc(_oldWndProc, hWnd, (int)msg, wParam, lParam); // Immediately return after OCR processing
                }

                Debug.WriteLine("=== Processing regular hotkey ===");

                // Check for OCR → Hotkey matching for regular hotkeys
                var hotkeyStructure = _hotkeyCollectSaveService.GetHotkeyStructureById(hotkeyId);
                if (hotkeyStructure?.Hotkey != null)
                {
                    bool ocrApplied = _ocrService.TryApplyOcrToHotkey(hotkeyStructure.Hotkey.Modifiers, hotkeyStructure.Hotkey.Key);
                    if (ocrApplied)
                    {
                        Debug.WriteLine($"OCR → Hotkey matching applied for hotkey ID: {hotkeyId}");
                        break; // Skip normal hotkey processing
                    }
                }

                // Normal hotkey processing
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
