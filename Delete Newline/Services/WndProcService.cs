using System.Diagnostics;
using System.Runtime.InteropServices;
using Delete_Newline.Helpers;
using Delete_Newline.Views;
using Delete_Newline.ViewModels;
using Windows.System;
using System.Linq;

namespace Delete_Newline.Services;

public sealed class WndProcService
{
    private readonly RegexCollectSaveService _regexCollectSaveService;
    private readonly HotkeyRegisterService _hotkeyRegisterService;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WM_HOTKEY = 0x0312;
    private const int WM_ACTIVATE = 0x0006;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int GWL_WNDPROC = -4;

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    
    private static IntPtr _oldWndProc;
    private static WndProc? _newWndProc;
    private static WndProcService? _instance;

    public WndProcService(RegexCollectSaveService regexCollectSaveService, HotkeyRegisterService hotkeyRegisterService)
    {
        _regexCollectSaveService = regexCollectSaveService;
        _hotkeyRegisterService = hotkeyRegisterService;
        _instance = this;
    }

    public void Initialize(IntPtr _hwnd)
    {
        _newWndProc = NewWndProc;
        IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC, newWndProcPtr);
        
        Debug.WriteLine("WndProcService initialized successfully");
    }

    public void Dispose()
    {
        Debug.WriteLine("WndProcService disposed");
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
                break;

            case WM_KEYUP:
                break;

            case WM_HOTKEY:
                int hotkeyId = wParam.ToInt32();
                Debug.WriteLine($"=== WM_HOTKEY received: ID={hotkeyId} ===");
                
                // 1. Check OCR Hotkey
                int? ocrHotkeyId = _hotkeyRegisterService.GetOcrHotkeyId();
                bool isOcrHotkey = ocrHotkeyId.HasValue && hotkeyId == ocrHotkeyId.Value;
                Debug.WriteLine($"OCR Hotkey ID: {ocrHotkeyId}, Is OCR Hotkey: {isOcrHotkey}");

                if (isOcrHotkey)
                {
                    // Process OCR Hotkey - OCR hotkey doesn't follow normal hotkey processing
                    Debug.WriteLine("Processing OCR Hotkey - launching OCR capture");
                    App.GetService<OCRViewModel>().LaunchOcr();
                    break;
                }

                // 2. Check normal hotkey
                var hotkeyStructure = _regexCollectSaveService.GetRegexStructureByHotkeyId(hotkeyId);
                if (hotkeyStructure?.Hotkey == null)
                {
                    // Hotkey structure not found - log error and ignore
                    Debug.WriteLine($"ERROR: Hotkey structure not found for ID: {hotkeyId}");
                    break;
                }

                Debug.WriteLine($"Found hotkey structure: {hotkeyStructure.HotkeyName}");

                // 4. Process normal hotkey (Ctrl+C needed)
                Debug.WriteLine("Processing normal hotkey with Ctrl+C");
                App.ActiveHotkeyIdForCopy = hotkeyId;

                // 5. Goto OnClipboardContentChanged()
                VirtualInputHelper.SendCtrlC();
                break;
        }
        return CallWindowProc(_oldWndProc, hWnd, (int)msg, wParam, lParam);
    }

    private static int LowWord(int value)
    {
        return value & 0xFFFF;
    }
}
