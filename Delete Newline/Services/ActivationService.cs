using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Delete_Newline.Activation;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Views;
using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.ViewModels;

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
    private readonly RegexCollectSaveService _regexCollectSaveService;
    private readonly WndProcService _wndProcService;
    private readonly TopMostService _topMostService;
    private readonly TrayIconService _trayIconService;
    private readonly OCRViewModel _ocrViewModel;

    private UIElement? _shell = null;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler,
        IEnumerable<IActivationHandler> activationHandlers,
        IThemeSelectorService themeSelectorService,
        ILocalizationService localizationService,
        IFilePickerService filePickerService,
        SettingsService settingsService,
        NotificationService notificationService,
        RegexCollectSaveService regexCollectSaveService,
        WndProcService wndProcService,
        TopMostService topMostService,
        TrayIconService trayIconService,
        OCRViewModel ocrViewModel)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _localizationService = localizationService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _filePickerService = filePickerService;
        _regexCollectSaveService = regexCollectSaveService;
        _wndProcService = wndProcService;
        _topMostService = topMostService;
        _trayIconService = trayIconService;
        _ocrViewModel = ocrViewModel;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        try
        {            
            InitializeUI();
            await HandleActivationAsync(activationArgs);
            await InitializeEssentialServicesAsync();
            InitializeWindowDependentServices();
            InitializeRemainingServices();

            _themeSelectorService.SetRequestedTheme();
        }
        catch (Exception ex)
        {
            _notificationService.ShowSystemNotification("Application Error", "Application Initialization Failed: {0}", true, false, ex.Message);
            throw;
        }
    }

    private void InitializeUI()
    {
        _shell = App.GetService<ShellPage>();
        App.MainWindow.Content = _shell ?? new Frame();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        Task? handlerTask = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs))?.HandleAsync(activationArgs);
        Task? defaultHandlerTask = _defaultHandler.CanHandle(activationArgs) ? _defaultHandler.HandleAsync(activationArgs) : null;

        await Task.WhenAll(handlerTask ?? Task.CompletedTask, defaultHandlerTask ?? Task.CompletedTask);
    }

    private async Task InitializeEssentialServicesAsync()
    {
        await _settingsService.InitializeAsync();
        await _notificationService.InitializeAsync();
    }

    private void InitializeWindowDependentServices()
    {
        var hwnd = MainWindow.hwnd;
        _wndProcService.Initialize(hwnd);
        _filePickerService.Initialize(hwnd);
        _trayIconService.Initialize(hwnd);
    }

    private void InitializeRemainingServices()
    {
        _localizationService.Initialize();
        _themeSelectorService.Initialize();
        _topMostService.Initialize();
        _regexCollectSaveService.Initialize();
        _ocrViewModel.Initialize();
    }
}
