using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.System;
using Delete_Newline.Helpers;

namespace Delete_Newline.Services;

[Flags]
public enum Win32Modifiers
{
    None = 0x0000,
    MOD_ALT = 0x0001,
    MOD_CONTROL = 0x0002,
    MOD_SHIFT = 0x0004,
    MOD_WIN = 0x0008,
    MOD_NOREPEAT = 0x4000,
}

public sealed class HotkeyRegisterService
{
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    private IntPtr _hwnd;
    private bool _isRegisteringHotkey = false;
    private readonly HashSet<(VirtualKeyModifiers, VirtualKey)> _registeredHotkeys = new();
    private readonly HashSet<int> _registeredHotkeyIds = new();
    
    // OCR hotkey management - now using same hash system as regular hotkeys
    private (VirtualKeyModifiers, VirtualKey)? _ocrHotkey = null;

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }
    
    public bool RegisterOcrHotkey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        var hotkey = (modifiers, key);
        
        if (HotkeyHelper.IsSystemHotkey(modifiers, key))
        {
            Debug.WriteLine($"Cannot register system hotkey for OCR: {modifiers} + {key}");
            return false;
        }

        if (IsHotkeyRegistered(hotkey))
        {
            Debug.WriteLine($"Hotkey already registered for text processing: {modifiers} + {key}");
            return false;
        }

        if (_ocrHotkey.HasValue)
        {
            UnregisterOcrHotkey();
        }

        Win32Modifiers win32Modifiers = MapVirtualModifiersToWin32(modifiers);
        int ocrHotkeyId = HotkeyHelper.GenerateHotkeyHash(modifiers, key);
        
        bool result = RegisterHotKey(_hwnd, ocrHotkeyId, (uint)win32Modifiers, (uint)key);

        if (!result)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.WriteLine($"RegisterOcrHotkey failed with error code: {errorCode}");
        }
        else
        {
            Debug.WriteLine($"RegisterOcrHotkey succeeded: {modifiers} + {key}, Hash ID: {ocrHotkeyId}");
            _ocrHotkey = hotkey;
        }
        
        return result;
    }
    
    public void UnregisterOcrHotkey()
    {
        if (_ocrHotkey.HasValue)
        {
            int ocrHotkeyId = HotkeyHelper.GenerateHotkeyHash(_ocrHotkey.Value);
            if (UnregisterHotKey(_hwnd, ocrHotkeyId))
            {
                Debug.WriteLine($"UnregisterOcrHotkey succeeded: {_ocrHotkey.Value.Item1} + {_ocrHotkey.Value.Item2}, Hash ID: {ocrHotkeyId}");
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                Debug.WriteLine($"UnregisterOcrHotkey failed with error code: {errorCode}");
            }
            _ocrHotkey = null;
        }
    }
    
    // OCR 핫키 조회
    public (VirtualKeyModifiers, VirtualKey)? GetOcrHotkey()
    {
        return _ocrHotkey;
    }
    
    // OCR 핫키가 등록되어 있는지 확인
    public bool IsOcrHotkeyRegistered()
    {
        return _ocrHotkey.HasValue;
    }

    // Get OCR hotkey hash ID (for WndProcService to identify OCR hotkey)
    public int? GetOcrHotkeyId()
    {
        return _ocrHotkey.HasValue ? HotkeyHelper.GenerateHotkeyHash(_ocrHotkey.Value) : null;
    }

    private Win32Modifiers MapVirtualModifiersToWin32(VirtualKeyModifiers virtualModifiers)
    {
        Win32Modifiers win32Modifiers = Win32Modifiers.None;

        if (virtualModifiers.HasFlag(VirtualKeyModifiers.Control))
            win32Modifiers |= Win32Modifiers.MOD_CONTROL;
        if (virtualModifiers.HasFlag(VirtualKeyModifiers.Menu))
            win32Modifiers |= Win32Modifiers.MOD_ALT;
        if (virtualModifiers.HasFlag(VirtualKeyModifiers.Shift))
            win32Modifiers |= Win32Modifiers.MOD_SHIFT;
        if (virtualModifiers.HasFlag(VirtualKeyModifiers.Windows))
            win32Modifiers |= Win32Modifiers.MOD_WIN;

        return win32Modifiers;
    }

    public bool IsSystemHotkey((VirtualKeyModifiers, VirtualKey) hotkey)
    {
        return HotkeyHelper.IsSystemHotkey(hotkey.Item1, hotkey.Item2);
    }

    public bool IsHotkeyRegistered((VirtualKeyModifiers, VirtualKey) hotkey)
    {
        return _registeredHotkeys.Contains(hotkey);
    }

    // unregister all hotkeys
    public void StartHotkeyRegistration()
    {
        _isRegisteringHotkey = true;
        foreach (var hotkey in _registeredHotkeys.ToList())
        {
            int hotkeyId = HotkeyHelper.GenerateHotkeyHash(hotkey);
            if (UnregisterHotKey(_hwnd, hotkeyId))
            {
                _registeredHotkeyIds.Add(hotkeyId);
            }
        }
        
        // OCR 핫키도 임시 해제
        if (_ocrHotkey.HasValue)
        {
            int ocrHotkeyId = HotkeyHelper.GenerateHotkeyHash(_ocrHotkey.Value);
            UnregisterHotKey(_hwnd, ocrHotkeyId);
        }
    }

    // register all hotkeys
    public void EndHotkeyRegistration()
    {
        _isRegisteringHotkey = false;
        foreach (var hotkey in _registeredHotkeys.ToList())
        {
            int hotkeyId = HotkeyHelper.GenerateHotkeyHash(hotkey);
            if (_registeredHotkeyIds.Contains(hotkeyId))
            {
                Win32Modifiers win32Modifiers = MapVirtualModifiersToWin32(hotkey.Item1);
                RegisterHotKey(_hwnd, hotkeyId, (uint)win32Modifiers, (uint)hotkey.Item2);
                _registeredHotkeyIds.Remove(hotkeyId);
            }
        }
        
        // OCR 핫키 다시 등록
        if (_ocrHotkey.HasValue)
        {
            int ocrHotkeyId = HotkeyHelper.GenerateHotkeyHash(_ocrHotkey.Value);
            Win32Modifiers win32Modifiers = MapVirtualModifiersToWin32(_ocrHotkey.Value.Item1);
            RegisterHotKey(_hwnd, ocrHotkeyId, (uint)win32Modifiers, (uint)_ocrHotkey.Value.Item2);
        }
    }

    public bool IsRegisteringHotkey()
    {
        return _isRegisteringHotkey;
    }

    public bool RegisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        if (HotkeyHelper.IsSystemHotkey(Hotkey.Item1, Hotkey.Item2))
        {
            Debug.WriteLine($"Cannot register system hotkey: {Hotkey.Item1} + {Hotkey.Item2}");
            return false;
        }

        if (IsHotkeyRegistered(Hotkey))
        {
            Debug.WriteLine($"Hotkey already registered: {Hotkey.Item1} + {Hotkey.Item2}");
            return false;
        }
        
        // OCR 핫키와 충돌하는지 확인
        if (_ocrHotkey.HasValue && _ocrHotkey.Value.Equals(Hotkey))
        {
            Debug.WriteLine($"Hotkey conflicts with OCR hotkey: {Hotkey.Item1} + {Hotkey.Item2}");
            return false;
        }

        int HotkeyId = HotkeyHelper.GenerateHotkeyHash(Hotkey);
        Win32Modifiers win32Modifiers = MapVirtualModifiersToWin32(Hotkey.Item1);
        
        // Try to unregister any existing Hotkey with this ID first
        UnregisterHotKey(_hwnd, HotkeyId);
        
        bool result = RegisterHotKey(_hwnd, HotkeyId, (uint)win32Modifiers, (uint)Hotkey.Item2);

        if (!result)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.WriteLine($"RegisterHotkey failed with error code: {errorCode}, ID: {HotkeyId}, _hwnd: {_hwnd}");
        }
        else
        {
            Debug.WriteLine($"RegisterHotkey succeeded. ID={HotkeyId}");
            _registeredHotkeys.Add(Hotkey);
        }
        return result;
    }

    public void UnRegisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        int HotkeyId = HotkeyHelper.GenerateHotkeyHash(Hotkey);
        if (UnregisterHotKey(_hwnd, HotkeyId) == false)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.WriteLine($"UnregisterHotkey failed with error code: {errorCode}, ID: {HotkeyId}, _hwnd: {_hwnd}");
        }
        else
        {
            Debug.WriteLine($"UnregisterHotkey succeeded. ID={HotkeyId}");
            _registeredHotkeys.Remove(Hotkey);
        }
    }
}
