using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Input;
using Windows;

namespace DeleteNewline.ViewModel
{
    public partial class ViewModel_InputText : ObservableValidator
    {
        ViewModel_Setting? vm_setting;

        [ObservableProperty] private string? textboxContent;

        [RelayCommand]
        private void MenuItem_Paste_Click()
        {
            if (ClipboardManager.ContainText() == true)
            {
                var originalText = ClipboardManager.GetText();
                TextboxContent += originalText;

                var regexAndReplace = vm_setting.GetAllRegexAndReplace();

                var (_, replacedText) = ClipboardManager.ReplaceText(regexAndReplace.Item1, regexAndReplace.Item2);
                ClipboardManager.SetText(replacedText);
            }
            else
            {
                Notification.Send("ERROR", "FAILED GET CLIPBOARD", Notification.SoundType.reminder, 300);
                return;
            }
        }

        [RelayCommand]
        private void Textbox_Ctrl_V_KeyUp(KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.V || Keyboard.IsKeyDown(Key.V) && e.Key == Key.LeftCtrl)
            {
                if (ClipboardManager.ContainText() == true)
                {
                    var regexAndReplace = vm_setting.GetAllRegexAndReplace();

                    var (_, replacedText) = ClipboardManager.ReplaceText(regexAndReplace.Item1, regexAndReplace.Item2);
                    ClipboardManager.SetText(replacedText);
                }
                else
                {
                    Notification.Send("ERROR", "FAILED GET CLIPBOARD", Notification.SoundType.reminder, 300);
                    return;
                }
            }
        }

        [RelayCommand]
        private void Page_Loaded()
        {
            vm_setting = App.GetService<ViewModel_Setting>();
        }

        [RelayCommand]
        private void MenuItem_Clear_Click()
        {
            TextboxContent = string.Empty;
        }
    }
}
