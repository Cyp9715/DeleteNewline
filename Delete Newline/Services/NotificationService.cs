using Delete_Newline.Contracts.Services;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Delete_Newline.Helpers; // For LocalizationHelper

namespace Delete_Newline.Services;

public sealed class NotificationService
{
    private const string NotificationSettingsKey = "Notification";
    
    AppNotificationManager? notificationManager;

    private bool _enableNotification; // default true.

    private readonly SettingsService _localSettingsService;

    public NotificationService(SettingsService localSettingsService)
    {
        notificationManager = AppNotificationManager.Default;
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        bool? storedSetting = _localSettingsService.ReadSetting<bool?>(NotificationSettingsKey);

        // default setting
        if (storedSetting.HasValue == false)
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

    public void ShowSystemNotification(string titleKey, string messageKey, bool force = false, bool addTag = true, params object[]? messageArgs)
    {
        if (_enableNotification == false && force == false) 
            return;

        string title = LocalizationHelper.GetLocalizedString(titleKey);
        string message = LocalizationHelper.GetLocalizedString(messageKey, messageArgs ?? System.Array.Empty<object>());

        AppNotificationBuilder builder = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message);

        builder = addTag ? builder.SetTag(LocalizationHelper.GetLocalizedString("AppDisplayName")) : builder;

        notificationManager!.Show(builder.BuildNotification());
    }
}
