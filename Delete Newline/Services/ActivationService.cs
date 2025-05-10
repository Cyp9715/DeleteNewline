using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Delete_Newline.Activation;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Views;
using Delete_Newline.Helpers;
using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.ViewModels;
using WinUIEx;

namespace Delete_Newline.Services;

public sealed class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalizationService _localizationService;
    private readonly IFilePickerService _filePickerService;
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;
    private readonly HotkeyCollectSaveService _HotkeyCollectManagerService;
    private readonly HotkeyRegisterService _HotkeyRegisterService;
    private readonly WndProcService _wndProcService;
    private readonly TopMostService _topMostService;
    private readonly TrayIconService _trayIconService;

    private UIElement? _shell = null;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler,
        IEnumerable<IActivationHandler> activationHandlers,
        IThemeSelectorService themeSelectorService,
        ILocalizationService localizationService,
        IFilePickerService filePickerService,
        SettingsService settingsService,
        NotificationService notificationService,
        HotkeyCollectSaveService HotkeyCollectManagerService,
        HotkeyRegisterService HotkeyRegister,
        WndProcService wndProcService,
        TopMostService topMostService,
        TrayIconService trayIconService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _localizationService = localizationService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _filePickerService = filePickerService;
        _HotkeyCollectManagerService = HotkeyCollectManagerService;
        _HotkeyRegisterService = HotkeyRegister;
        _wndProcService = wndProcService;
        _topMostService = topMostService;
        _trayIconService = trayIconService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        // UI initialization (must run first)
        _shell = App.GetService<ShellPage>();
        App.MainWindow.Content = _shell ?? new Frame();

        // Initialize essential settings service
        await _settingsService.InitializeAsync();

        // Parallel execution of non-blocking async tasks
        var asyncTasks = new List<Task>
        {
            HandleActivationAsync(activationArgs),
            _notificationService.InitializeAsync()
        };
        await Task.WhenAll(asyncTasks);

        // Initialize services that depend on window handle
        var hwnd = MainWindow.hwnd;
        _HotkeyRegisterService.Initialize(hwnd);
        _wndProcService.Initialize(hwnd);
        _filePickerService.Initialize(hwnd);
        _trayIconService.Initialize(hwnd);

        // Initialize remaining services
        _localizationService.Initialize();
        _themeSelectorService.Initialize();
        _topMostService.Initialize(App.MainWindow);
        
        // Initialize HotkeyCollectSaveService after HotkeyRegisterService
        _HotkeyCollectManagerService.Initialize();

        // Apply theme (executed last as it affects UI appearance)
        _themeSelectorService.SetRequestedTheme();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        Task? handlerTask = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs))?.HandleAsync(activationArgs);
        Task? defaultHandlerTask = _defaultHandler.CanHandle(activationArgs) ? _defaultHandler.HandleAsync(activationArgs) : null;

        await Task.WhenAll(handlerTask ?? Task.CompletedTask, defaultHandlerTask ?? Task.CompletedTask);
    }
}
