using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;
using Windows.System;

using Delete_Newline.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;

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
        args.Handled = true;
    }
}
