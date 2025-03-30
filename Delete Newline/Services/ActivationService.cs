using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Delete_Newline.Activation;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Views;
using Delete_Newline.Helpers;
using Delete_Newline.Core.Contracts.Services;

namespace Delete_Newline.Services;

public sealed class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingsService _settingsService;
    private readonly IFilePickerService _filePickerService;
    private readonly NotificationService _notificationService;
    private readonly HotKeyCollectSaveService _hotKeyCollectManagerService;
    private readonly HotKeyRegisterService _hotKeyRegisterService;
    private readonly WndProcService _wndProcService;
    private readonly TrayIconService _trayIconService;

    private UIElement? _shell = null;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler,
        IEnumerable<IActivationHandler> activationHandlers,
        IThemeSelectorService themeSelectorService,
        ILocalizationService localizationService,
        ISettingsService settingsService,
        IFilePickerService filePickerService,
        NotificationService notificationService,
        HotKeyCollectSaveService hotKeyCollectManagerService,
        HotKeyRegisterService hotKeyRegister,
        WndProcService wndProcService,
        TrayIconService trayIconService)
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
        _trayIconService = trayIconService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        _shell = App.GetService<ShellPage>();
        App.MainWindow.Content = _shell ?? new Frame();
        App.MainWindow.Activate();

        await _settingsService.InitializeAsync();

        Task activationTask = HandleActivationAsync(activationArgs);
        Task notificationTask = _notificationService.InitializeAsync();

        await Task.WhenAll(activationTask, notificationTask);

        _localizationService.Initialize();
        _themeSelectorService.Initialize();
        _hotKeyRegisterService.Initialize(MainWindow.hwnd);
        _wndProcService.Initialize(MainWindow.hwnd);
        _filePickerService.Initialize(MainWindow.hwnd);
        _trayIconService.Initialize(MainWindow.hwnd);
        TopMostHelper.Initialize(App.MainWindow);

        _hotKeyCollectManagerService.Initialize();
        _themeSelectorService.SetRequestedTheme();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        Task? handlerTask = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs))?.HandleAsync(activationArgs);
        Task? defaultHandlerTask = _defaultHandler.CanHandle(activationArgs) ? _defaultHandler.HandleAsync(activationArgs) : null;

        await Task.WhenAll(handlerTask ?? Task.CompletedTask, defaultHandlerTask ?? Task.CompletedTask);
    }
}
