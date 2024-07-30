
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Windows;

namespace DeleteNewline.ViewModel
{
    public partial class ViewModel_MainWindow : ObservableValidator
    {
        [ObservableProperty]
        private double mainWindowSize_width;

        [ObservableProperty]
        private double mainWindowSize_height;

        // 실제 TopMost 옵션의 적용은 Page_Setting.ViewModel에서 진행.
        [ObservableProperty]
        private bool? topMost;

        [ObservableProperty]
        private Visibility windowVisibility = Visibility.Hidden;

        [ObservableProperty]
        private object? currentPage;

        [ObservableProperty]
        private object? selectedItem;

        Settings setting;

        public ViewModel_MainWindow()
        {
            InitialSetting.Init();
            setting = Settings.GetInstance();
        }

        [RelayCommand]
        private void WindowLoaded(RoutedEventArgs e)
        {
            MainWindowSize_width = setting.mainWindowSize_width;
            MainWindowSize_height = setting.mainWindowSize_height;
            TopMost = setting.topMost;

            CurrentPage = App.GetService<Page_InputText>();
            SelectedItem = "InputText";
        }

        public void Action_ContextMenu_Exit(object? sender, EventArgs e)
        {
            // window 사이즈를 비롯한 setting 정보를 파일에 저장.
            setting.mainWindowSize_width = MainWindowSize_width;
            setting.mainWindowSize_height = MainWindowSize_height;
            Settings.Apply(setting);
            Settings.Save();

            System.Environment.Exit(0);
        }

        [RelayCommand]
        private void WindowClosing(CancelEventArgs e)
        {
            // 설정 저장
            setting.mainWindowSize_width = MainWindowSize_width;
            setting.mainWindowSize_height = MainWindowSize_height;
            Settings.Apply(setting);
            Settings.Save();

            // 창 닫기 취소(프로세스 종료방지) 및 숨기기
            e.Cancel = true;
            WindowVisibility = Visibility.Hidden;
        }

        [RelayCommand]
        private void Navigate(ModernWpf.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                CurrentPage = App.GetService<Page_Setting>();
            }
            else if (args.InvokedItemContainer is ModernWpf.Controls.NavigationViewItem navigationViewItem)
            {
                switch(navigationViewItem.Tag)
                {
                    case "InputText":
                        CurrentPage = App.GetService<Page_InputText>();
                        break;
                }

                SelectedItem = args.InvokedItemContainer;
            }
        }
    }
}
