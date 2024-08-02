using System;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace DeleteNewline.ViewModel
{
    public partial class ViewModel_MainWindow : ObservableValidator
    {
        [ObservableProperty] private double mainWindowSize_width;
        [ObservableProperty] private double mainWindowSize_height;
        [ObservableProperty] private Visibility windowVisibility;
        [ObservableProperty] private object? currentPage;
        [ObservableProperty] private object? selectedNavigationItem;

        Settings setting;

        public ViewModel_MainWindow()
        {
            setting = Settings.GetInstance();

            WindowVisibility = Visibility.Hidden;
            InitNotifyIcon();
        }

        public void Action_ContextMenu_Exit(object? sender, EventArgs e)
        {
            // window 사이즈를 비롯한 setting 정보를 파일에 저장.
            setting.mainWindowSize_width = MainWindowSize_width;
            setting.mainWindowSize_height = MainWindowSize_height;

            Settings.ApplyLoadedSettings(setting);
            Settings.Save();

            System.Environment.Exit(0);
        }

        [RelayCommand]
        private void WindowLoaded(RoutedEventArgs e)
        {
            // 저장된 window 사이즈를 프로세스로 로드.
            MainWindowSize_width = setting.mainWindowSize_width;
            MainWindowSize_height = setting.mainWindowSize_height;

            // NavigationItem 기본설정을 Input_Text 로 지정.
            var mainWindow = Application.Current.MainWindow as View.Page_MainWindow;
            SelectedNavigationItem = mainWindow.NavigationViewItem_InputText;

            CurrentPage = App.GetService<View.Page_InputText>();
        }

        [RelayCommand]
        private void WindowClosing(CancelEventArgs e)
        {
            // 설정 저장
            setting.mainWindowSize_width = MainWindowSize_width;
            setting.mainWindowSize_height = MainWindowSize_height;

            Settings.ApplyLoadedSettings(setting);
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
                CurrentPage = App.GetService<View.Page_Setting>();
            }
            else if (args.InvokedItemContainer is ModernWpf.Controls.NavigationViewItem navigationViewItem)
            {
                switch(navigationViewItem.Tag)
                {
                    case "InputText":
                        CurrentPage = App.GetService<View.Page_InputText>();
                        break;
                }

                SelectedNavigationItem = navigationViewItem;
            }
        }

        private void InitNotifyIcon()
        {
            System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = Resource.favicon,
                Text = GlobalVariables.programName,
                Visible = true
            };

            // 더블클릭시 화면 Visible.
            notifyIcon.DoubleClick += (sender, eventArgs) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WindowVisibility = Visibility.Visible;
                });
            };

            // notifyIcon 내부 ContentMenu 설정
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Exit DeleteNewline", null, Action_ContextMenu_Exit);
        }
    }
}
