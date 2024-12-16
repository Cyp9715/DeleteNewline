using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Delete_Newline.Services;

namespace Delete_Newline.Views;

public sealed partial class KeybindPage : Page
{
    public KeybindViewModel ViewModel
    {
        get;
    }

    public KeybindPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<KeybindViewModel>();
        DataContext = ViewModel;
    }

    private void Keybind_KeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        if (Hotkey.Validate(args.Modifiers, args.Key))
        {
            // Todo : make id Manager
            //HotkeyManager.RegisterHotkey(Int ,args.Modifiers, args.Key);
        }

        args.Handled = true;
    }
}
