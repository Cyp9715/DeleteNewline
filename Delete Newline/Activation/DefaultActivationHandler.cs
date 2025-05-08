using Microsoft.UI.Xaml;

using Delete_Newline.Contracts.Services;
using Delete_Newline.ViewModels;
using Delete_Newline.Services;

namespace Delete_Newline.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;
    private readonly HotKeyCollectSaveService _hotKeyCollectSaveService;

    public DefaultActivationHandler(
        INavigationService navigationService, 
        HotKeyCollectSaveService hotKeyCollectSaveService)
    {
        _navigationService = navigationService;
        _hotKeyCollectSaveService = hotKeyCollectSaveService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // Initialize HotKeyCollectSaveService to load saved hotkey configurations
        _hotKeyCollectSaveService.Initialize();
        
        // Navigate to HotKeyCollectPage as the initial page
        _navigationService.NavigateTo(typeof(HotKeyCollectViewModel).FullName!, args.Arguments);

        await Task.CompletedTask;
    }
}