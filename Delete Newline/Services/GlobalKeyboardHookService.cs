using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Delete_Newline.Services;

public sealed class GlobalKeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;

    private readonly WndProcService _wndProcService;
    private IntPtr _hookHandle = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _hookProc;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    public GlobalKeyboardHookService(WndProcService wndProcService)
    {
        _wndProcService = wndProcService;
        _hookProc = HookCallback;
        InstallHook();
    }

    private void InstallHook()
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        if (module != null)
        {
            var moduleHandle = GetModuleHandle(module.ModuleName);
            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, moduleHandle, 0);
            
            if (_hookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Debug.WriteLine($"Failed to install keyboard hook. Error code: {errorCode}");
            }
            else
            {
                Debug.WriteLine("Global keyboard hook installed successfully");
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            
            // Check for key up events
            if ((int)wParam == WM_KEYUP || (int)wParam == WM_SYSKEYUP)
            {
                // Skip ESC key
                if (vkCode != 27)
                {
                    _wndProcService.ResetOcrRegexState();
                    Debug.WriteLine($"Global key up detected (VKey: {vkCode}) - OCR state reset");
                }
            }
        }
        
        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
            Debug.WriteLine("Global keyboard hook removed");
        }
    }
} 