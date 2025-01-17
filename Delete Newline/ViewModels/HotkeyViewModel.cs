using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;
using Windows.System;

namespace Delete_Newline.ViewModels;
public partial class HotKeyViewModel : ObservableRecipient
{
    [ObservableProperty]
    private HotKeyPageStructure? _currentHotKeyConfig;

    [RelayCommand]
    private void AddRegexItem()
    {
        if (CurrentHotKeyConfig is null || CurrentHotKeyConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentHotKeyConfig is null.");
        }

        CurrentHotKeyConfig.RegexChain.AddChainItem();
    }

    [RelayCommand]
    private void RemoveRegexItem(ChainItem item)
    {
        if (CurrentHotKeyConfig is null || CurrentHotKeyConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentHotKeyConfig is null.");
        }

        CurrentHotKeyConfig.RegexChain.ChainItems.Remove(item);
    }

    [RelayCommand]
    public void HandleKeyboardAccelerator(KeyboardAcceleratorEventArgs args)
    {
        var HotKeyManager = App.GetService<IHotKeyRegister>();

        if (HotKeyManager.RegistHotKey((args.Modifiers, args.Key)))
        {
            if (CurrentHotKeyConfig?.HotKey != null)
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

                CurrentHotKeyConfig.HotKey.Modifiers = tempModifiers;
                CurrentHotKeyConfig.HotKey.Key = args.Key;

                //App.GetService<HotKeySaver>().SaveHoykey(CurrentHotKeyConfig);
            }
        }
        else
        {
            // Add Toast Message.
        }
    }

}

