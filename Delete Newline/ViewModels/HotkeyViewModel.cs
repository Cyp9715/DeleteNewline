using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;
using Windows.System;

namespace Delete_Newline.ViewModels;
public partial class HotkeyViewModel : ObservableRecipient
{
    [ObservableProperty]
    private HotkeyPageStructure? _currentHotkeyConfig;

    [RelayCommand]
    private void AddRegexItem()
    {
        if (CurrentHotkeyConfig is null || CurrentHotkeyConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentHotkeyConfig is null.");
        }

        CurrentHotkeyConfig.RegexChain.AddChainItem();
    }

    [RelayCommand]
    private void RemoveRegexItem(ChainItem item)
    {
        if (CurrentHotkeyConfig is null || CurrentHotkeyConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentHotkeyConfig is null.");
        }

        CurrentHotkeyConfig.RegexChain.ChainItems.Remove(item);
    }

    [RelayCommand]
    public void HandleKeyboardAccelerator(KeyboardAcceleratorEventArgs args)
    {
        var hotkeyManager = App.GetService<IHotkeyRegister>();

        if (hotkeyManager.RegisterHotkey(1, (args.Modifiers, args.Key)))
        {
            if (CurrentHotkeyConfig?.Hotkey != null)
            {
                VirtualKeyModifiers tempModifiers = VirtualKeyModifiers.None;

                if (args.Modifiers.HasFlag(VirtualKeyModifiers.Control))
                    tempModifiers |= VirtualKeyModifiers.Control;
                if (args.Modifiers.HasFlag(VirtualKeyModifiers.Menu))
                    tempModifiers |= VirtualKeyModifiers.Menu;
                if (args.Modifiers.HasFlag(VirtualKeyModifiers.Shift))
                    tempModifiers |= VirtualKeyModifiers.Shift;
                if (args.Modifiers.HasFlag(VirtualKeyModifiers.Windows))
                    tempModifiers |= VirtualKeyModifiers.Windows;

                CurrentHotkeyConfig.Hotkey.Modifiers = tempModifiers;
                CurrentHotkeyConfig.Hotkey.Key = args.Key;

                //App.GetService<HotkeySaver>().SaveHoykey(CurrentHotkeyConfig);
            }
        }
        else
        {
            // Add Toast Message.
        }
    }

}

