using Windows.System;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;

namespace Delete_Newline.Services;

public sealed partial class User32
{
    public const int WM_Hotkey = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotkey(
        IntPtr hWnd, 
        int id, 
        uint fsModifiers, 
        uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotkey(
        IntPtr hWnd, 
        int id);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CallWindowProc(
        IntPtr lpPrevWndFunc, 
        IntPtr hWnd, 
        uint Msg, 
        IntPtr wParam, 
        IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowSubclass(
        IntPtr hWnd,
        SUBCLASSPROC pfnSubclass,
        IntPtr uIdSubclass,
        IntPtr dwRefData);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RemoveWindowSubclass(
        IntPtr hWnd,
        SUBCLASSPROC pfnSubclass,
        IntPtr uIdSubclass);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr DefSubclassProc(
        IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    public delegate IntPtr SUBCLASSPROC(
        IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
        IntPtr uIdSubclass, IntPtr dwRefData);
}


public sealed class HotkeyRegister : IHotkeyRegister
{
    private readonly IntPtr _hwnd;
    private readonly string _salt = "Delete Newline";

    public HotkeyRegister(IntPtr hwnd)
    {
        this._hwnd = hwnd;
    }

    private int CreateHashHotkey((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        string HotkeyString = $"{_salt}:{Hotkey.Item1}:{Hotkey.Item2}";

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(HotkeyString));
            int hashInt = BitConverter.ToInt32(hashBytes, 0);
            return Math.Abs(hashInt);
        }
    }

    public bool RegisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        bool result = User32.RegisterHotkey(_hwnd, CreateHashHotkey(Hotkey), (uint)Hotkey.Item1, (uint)Hotkey.Item2);
        System.Diagnostics.Debug.WriteLine($"RegisterHotkey result: {result}");
        return result;
    }

    public void UnregisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        while (User32.UnregisterHotkey(_hwnd, CreateHashHotkey(Hotkey)))
        {
        }
    }
}

public class HotkeyProcessor : IDisposable
{
    private readonly IntPtr _hWnd;
    private readonly User32.SUBCLASSPROC? _subclassProc;
    private readonly IntPtr _subclassId = (IntPtr)1;
    private bool _isSubclassed = false;

    public event Action<int> HotkeyPressed;

    public HotkeyProcessor(IntPtr hWnd)
    {
        _hWnd = hWnd;
        _subclassProc = new User32.SUBCLASSPROC(WndProc);
        SubclassWindow();
    }

    private void SubclassWindow()
    {
        if (_isSubclassed) return;

        bool success = User32.SetWindowSubclass(_hWnd, _subclassProc, _subclassId, IntPtr.Zero);
        System.Diagnostics.Debug.WriteLine($"SetWindowSubclass result: {success}");

        if (!success)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "SetWindowSubclass failed.");
        }

        _isSubclassed = true;
    }

    private void RemoveSubclass()
    {
        if (!_isSubclassed) return;

        bool success = User32.RemoveWindowSubclass(_hWnd, _subclassProc, _subclassId);
        System.Diagnostics.Debug.WriteLine($"RemoveWindowSubclass result: {success}");

        if (!success)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "RemoveWindowSubclass failed.");
        }

        _isSubclassed = false;
    }

    private IntPtr WndProc(
        IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
        IntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (msg == User32.WM_Hotkey)
        {
            int HotkeyId = wParam.ToInt32();
            System.Diagnostics.Debug.WriteLine($"Hotkey pressed with ID: {HotkeyId}");
            HotkeyPressed?.Invoke(HotkeyId);
        }

        return User32.DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        RemoveSubclass();
    }
}
