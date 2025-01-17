using Windows.System;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;

namespace Delete_Newline.Services;

public sealed partial class User32
{
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
    public static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
    public static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);
}


public sealed class HotKeyRegisterService : IHotKeyRegister
{
    private readonly IntPtr _hwnd;
    private readonly string _salt = "Delete Newline";

    public HotKeyRegisterService(IntPtr hwnd)
    {
        this._hwnd = hwnd;
    }

    private int HotKeyToHash((VirtualKeyModifiers, VirtualKey) HotKey)
    {
        string HotKeyString = $"{_salt}:{HotKey.Item1}:{HotKey.Item2}";

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(HotKeyString));
            int hashInt = BitConverter.ToInt32(hashBytes, 0);
            return Math.Abs(hashInt);
        }
    }

    public bool RegistHotKey((VirtualKeyModifiers, VirtualKey) HotKey)
    {
        int HotKeyId = HotKeyToHash(HotKey);
        bool result = User32.RegisterHotKey(_hwnd, HotKeyId, (uint)HotKey.Item1, (uint)HotKey.Item2);
        System.Diagnostics.Debug.WriteLine($"_hwnd : {_hwnd}");
        if (!result)
        {
            int errorCode = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"RegisterHotKey failed with error code: {errorCode}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"RegisterHotKey result: {result}");
        }
        return result;
    }

    public void UnregistHotKey((VirtualKeyModifiers, VirtualKey) HotKey)
    {
        while (User32.UnregisterHotKey(_hwnd, HotKeyToHash(HotKey)))
        {
        }
    }
}