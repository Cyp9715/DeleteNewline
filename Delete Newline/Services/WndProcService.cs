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
    private readonly OCRService _ocrService;
    private readonly HotkeyCollectSaveService _hotkeyCollectSaveService;
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
            case WM_KEYUP:
                int vKey = wParam.ToInt32();
                
                // Skip ESC key (used for OCR window closure)
                if (vKey == 27) break;
                
                // Any other keyboard input breaks the OCR → Hotkey chain
                _ocrService.NotifyUserInputDetected();
                Debug.WriteLine($"Keyboard input detected (VKey: {vKey}) - OCR → Hotkey chain reset");
                break;

            case WM_HOTKEY:
                int hotkeyId = wParam.ToInt32();
                Debug.WriteLine($"=== WM_HOTKEY received: ID={hotkeyId} ===");
                
                // 1. OCR Hotkey 확인
                int? ocrHotkeyId = _hotkeyRegisterService.GetOcrHotkeyId();
                bool isOcrHotkey = ocrHotkeyId.HasValue && hotkeyId == ocrHotkeyId.Value;
                Debug.WriteLine($"OCR Hotkey ID: {ocrHotkeyId}, Is OCR Hotkey: {isOcrHotkey}");

                if (isOcrHotkey)
                {
                    // OCR Hotkey 처리 - OCR 단축키는 일반 단축키 처리를 하지 않음
                    Debug.WriteLine("Processing OCR Hotkey - launching OCR capture");
                    LaunchOcrCapture();
                    break;
                }

                // 2. 일반 Hotkey 확인
                var hotkeyStructure = _hotkeyCollectSaveService.GetHotkeyStructureById(hotkeyId);
                if (hotkeyStructure?.Hotkey == null)
                {
                    // Hotkey structure not found - log error and ignore
                    Debug.WriteLine($"ERROR: Hotkey structure not found for ID: {hotkeyId}");
                    break;
                }

                Debug.WriteLine($"Found hotkey structure: {hotkeyStructure.HotkeyName}");

                // 3. OCR 직후인지 확인 (OCR → Hotkey 처리)
                bool isOcrToHotkey = _ocrService.TryApplyOcrToHotkey(hotkeyStructure.Hotkey.Modifiers, hotkeyStructure.Hotkey.Key);
                Debug.WriteLine($"OCR → Hotkey result: {isOcrToHotkey}");

                if (isOcrToHotkey)
                {
                    // OCR → Hotkey 처리 성공 (Ctrl+C 불필요)
                    Debug.WriteLine("OCR → Hotkey processing successful - showing notification");
                    _ocrService.ShowOcrHotkeyNotification();
                    break;
                }

                // 4. 일반 Hotkey 처리 (Ctrl+C 필요)
                Debug.WriteLine("Processing normal hotkey with Ctrl+C");
                App.ActiveHotkeyIdForCopy = hotkeyId;
                VirtualInputHelper.SendCtrlC();
                break;
        }
        return CallWindowProc(_oldWndProc, hWnd, (int)msg, wParam, lParam);
    }

    private void LaunchOcrCapture()
    {
        try
        {
            var ocrViewModel = App.GetService<OCRViewModel>();
            ocrViewModel.LaunchOcrCapture();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error launching OCR capture: {ex.Message}");
        }
    }

    private static int LowWord(int value)
    {
        return value & 0xFFFF;
    }
}
