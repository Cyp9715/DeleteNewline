using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Delete_Newline.Activation;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Views;
using Delete_Newline.Helpers;
using Delete_Newline.Core.Contracts.Services;

namespace Delete_Newline.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly IFilePickerService _filePickerService;
    private readonly HotKeyCollectSaveService _hotKeyCollectManagerService;
    private readonly HotKeyRegisterService _hotKeyRegisterService;
    private readonly WndProcService _wndProcService;

    private UIElement? _shell = null;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, 
        IEnumerable<IActivationHandler> activationHandlers,
        IThemeSelectorService themeSelectorService,
        ILocalizationService localizationService,
        ISettingsService settingsService,
        INotificationService notificationService,
        IFilePickerService filePickerService,
        HotKeyCollectSaveService hotKeyCollectManagerService,
        HotKeyRegisterService hotKeyRegister,
        WndProcService wndProcService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _localizationService = localizationService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _filePickerService = filePickerService;
        _hotKeyCollectManagerService = hotKeyCollectManagerService;
        _hotKeyRegisterService = hotKeyRegister;
        _wndProcService = wndProcService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // Execute tasks before activation.
        await InitializeAsync();

        // Set the MainWindow Content.
        _shell = App.GetService<ShellPage>();
        App.MainWindow.Content = _shell ?? new Frame();

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);

        // Activate the MainWindow.
        App.MainWindow.Activate();

        // Register Window Handle. is synchronized.
        _hotKeyRegisterService.Initialize(MainWindow.hwnd);
        _wndProcService.Initialize(MainWindow.hwnd);
        _filePickerService.Initialize(MainWindow.hwnd);

        // Execute tasks after activation.
        await StartupAsync();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _localizationService.InitializeAsync().ConfigureAwait(false);
        await _notificationService.InitializeAsync().ConfigureAwait(false);
        _themeSelectorService.Initialize();
    }

    private async Task StartupAsync()
    {
        await TopMostHelper.InitializeAsync(App.MainWindow); // TopMostHelper is static class.
        _themeSelectorService.SetRequestedTheme();
        _hotKeyCollectManagerService.Initialize();
    }
}
