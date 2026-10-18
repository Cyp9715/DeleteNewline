using Hotkey = (Windows.System.VirtualKeyModifiers Modifiers, Windows.System.VirtualKey Key);

namespace Delete_Newline.Helpers.Hotkeys;

public enum HotkeyType
{
    Regex,
    Ocr
}

// All calls, including window activation, run on the UI thread. Configured shortcuts
// survive the temporary release needed while a shortcut recorder has focus.
internal sealed class HotkeyRegistrationManager(Func<Hotkey, bool> register, Action<Hotkey> unregister)
{
    private readonly Dictionary<Hotkey, HotkeyType> _configured = [];
    private readonly HashSet<Hotkey> _active = [];
    private object? _recorder;
    private bool _windowActive;

    public bool Register(Hotkey hotkey, HotkeyType type)
    {
        if (_configured.ContainsKey(hotkey) || !register(hotkey)) return false;
        _configured.Add(hotkey, type);
        _active.Add(hotkey);
        Reconcile();
        return true;
    }

    public void Unregister(Hotkey hotkey, HotkeyType type)
    {
        if (!_configured.TryGetValue(hotkey, out HotkeyType owner) || owner != type) return;
        if (_active.Remove(hotkey)) unregister(hotkey);
        _configured.Remove(hotkey);
    }

    public bool Contains(Hotkey hotkey, HotkeyType? type = null) =>
        _configured.TryGetValue(hotkey, out HotkeyType owner) && (type == null || owner == type);

    public Hotkey? GetHotkey(HotkeyType type) =>
        _configured.Where(entry => entry.Value == type).Select(entry => (Hotkey?)entry.Key).FirstOrDefault();

    public void SetRecording(object recorder, bool recording)
    {
        if (recording) _recorder = recorder;
        else if (ReferenceEquals(_recorder, recorder)) _recorder = null;
        Reconcile();
    }

    public void SetWindowActive(bool active)
    {
        _windowActive = active;
        Reconcile();
    }

    public void Refresh()
    {
        ReleaseActive();
        Reconcile();
    }

    private void Reconcile()
    {
        if (_windowActive && _recorder != null)
        {
            ReleaseActive();
            return;
        }

        foreach (Hotkey hotkey in _configured.Keys)
        {
            // Repeated RegisterHotKey calls for the same window/id can leave
            // additional native registrations, so activate each shortcut once.
            if (!_active.Contains(hotkey) && register(hotkey)) _active.Add(hotkey);
        }
    }

    private void ReleaseActive()
    {
        foreach (Hotkey hotkey in _active) unregister(hotkey);
        _active.Clear();
    }
}
