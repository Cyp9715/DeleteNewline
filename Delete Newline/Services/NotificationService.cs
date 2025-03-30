using Delete_Newline.Contracts.Services;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Delete_Newline.Services;

public sealed class NotificationService
{
    private const string NotificationSettingsKey = "Notification";
    
    AppNotificationManager? notificationManager;

    private bool _enableNotification; // default true.

    private readonly ISettingsService _localSettingsService;

    public NotificationService(ISettingsService localSettingsService)
    {
        notificationManager = AppNotificationManager.Default;
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        bool? storedSetting = _localSettingsService.ReadSetting<bool?>(NotificationSettingsKey);

        // default setting
        if (storedSetting.HasValue is false)
        {
            _enableNotification = true;
            await _localSettingsService.SaveSettingAsync(NotificationSettingsKey, true);
        }
        else
        {
            _enableNotification = storedSetting.Value;
        }
    }

    public event EventHandler<bool>? EnableNotificationChanged;

    public async Task SetEnableNotificationAsync(bool enable)
    {
        _enableNotification = enable;
        await _localSettingsService.SaveSettingAsync(NotificationSettingsKey, enable);
        EnableNotificationChanged?.Invoke(this, enable); // Synchronize to change the ViewModel code as well.
    }

    public bool GetEnableNotification() => _enableNotification;

    public void ShowNotification(string title, string message, bool force = false, bool tag = true)
    {
        if (_enableNotification is false && force is false) 
            return;

        AppNotificationBuilder builder = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message);

        builder = tag ? builder.SetTag("Delete Newline") : builder;

        notificationManager!.Show(builder.BuildNotification());
    }
}
