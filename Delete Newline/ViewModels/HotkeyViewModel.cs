using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using System.Diagnostics;
using Windows.System;

namespace Delete_Newline.ViewModels;
public partial class HotkeyViewModel : ObservableRecipient
{
    [ObservableProperty]
    private HotkeyPageConfiguration? _currentHotkeyConfig;

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
        // HotkeyManager를 사용하여 핫키 등록 로직 구현
        bool success = true;

        if (success)
        {
            // 성공적으로 등록된 경우 Hotkey 설정 업데이트
            if (CurrentHotkeyConfig?.Hotkey != null)
            {
                VirtualKeyModifiers tempModifiers = VirtualKeyModifiers.None;

                // 2) 현재 이벤트에서 전달된 Modifier 키들을 조합해서 OR 연산으로 설정
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
            }
        }
        else
        {
            // 핫키 등록 실패 시 처리 로직 추가 (예: 사용자에게 알림)
            Debug.WriteLine("핫키 등록에 실패했습니다.");
        }
    }

}

