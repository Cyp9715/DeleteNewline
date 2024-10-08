using Delete_Newline.Contracts.Services;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Delete_Newline.Services;

class NotificationService : INotificationService
{
    private const string NotificationSettingsKey = "Notification";
    
    AppNotificationManager notificationManager;

    private bool _enableNotification; // default true.

    private readonly ILocalSettingsService _localSettingsService;

    public NotificationService(ILocalSettingsService localSettingsService)
    {
        notificationManager = AppNotificationManager.Default;
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        bool? storedSetting = await _localSettingsService.ReadSettingAsync<bool?>(NotificationSettingsKey);

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

    public async Task SetEnableNotificationAsync(bool enable)
    {
        _enableNotification = enable;

        await _localSettingsService.SaveSettingAsync(NotificationSettingsKey, enable);
    }
    
    public bool GetEnableNotification() => _enableNotification;

    public void ShowNotification(string title, string message, bool force=false)
    {
        if (_enableNotification is false && force is false) 
            return;

        AppNotificationBuilder builder = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message);

        notificationManager.Show(builder.BuildNotification());
    }
}
