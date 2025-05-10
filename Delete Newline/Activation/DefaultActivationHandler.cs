using Microsoft.UI.Xaml;

using Delete_Newline.Contracts.Services;
using Delete_Newline.ViewModels;
using Delete_Newline.Services;

namespace Delete_Newline.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;
    private readonly HotkeyCollectSaveService _HotkeyCollectSaveService;

    public DefaultActivationHandler(
        INavigationService navigationService, 
        HotkeyCollectSaveService HotkeyCollectSaveService)
    {
        _navigationService = navigationService;
        _HotkeyCollectSaveService = HotkeyCollectSaveService;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // Navigate to HotkeyCollectPage as the initial page
        _navigationService.NavigateTo(typeof(HotkeyCollectViewModel).FullName!, args.Arguments);

        await Task.CompletedTask;
    }
}