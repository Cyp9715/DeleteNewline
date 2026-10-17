namespace Delete_Newline.Services;

// A failed Ctrl+C must not transform an unrelated clipboard update minutes later.
public sealed class PendingHotkeyCopy(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly object _sync = new();
    private int? _hotkeyId;
    private long _started;

    public void Set(int hotkeyId)
    {
        lock (_sync)
        {
            _hotkeyId = hotkeyId;
            _started = _timeProvider.GetTimestamp();
        }
    }

    public bool TryConsume(out int hotkeyId)
    {
        lock (_sync)
        {
            hotkeyId = _hotkeyId ?? -1;
            bool valid = _hotkeyId.HasValue && _timeProvider.GetElapsedTime(_started) < TimeSpan.FromSeconds(2);
            _hotkeyId = null;
            return valid;
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _hotkeyId = null;
        }
    }
}
