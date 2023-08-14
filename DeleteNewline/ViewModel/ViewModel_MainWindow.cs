using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;



namespace DeleteNewline.ViewModel
{
    public class ViewModel_MainWindow : ObservableObject
    {
        InitialSetting initialSettings;

        private object _currentContent;
        public object CurrentContent
        {
            get => _currentContent;
            set => SetProperty(ref _currentContent, value);
        }

        public RelayCommand<string> NavigationCommand { get; }
        

        public ViewModel_MainWindow()
        {
            NavigationCommand = new RelayCommand<string>(Navigate);
        }


        public void Navigate(string navigationTarget)
        {
            switch (navigationTarget)
            {
                case "InputText":
                    CurrentContent = App.GetService<Page_InputText>();
                    break;
                    // 다른 페이지들의 경우도 추가
            }
        }

    }
}
