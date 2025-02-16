using Delete_Newline.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Delete_Newline.Views;

public sealed partial class HotKeyPage : Page
{
    public HotKeyViewModel ViewModel
    {
        get;
    }

    public HotKeyPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<HotKeyViewModel>();
        DataContext = ViewModel;
    }

    private void HotKeyPage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // 현재 포인터 이벤트 정보를 가져옵니다.
        var point = e.GetCurrentPoint(this);

        // XButton1Pressed는 보통 마우스의 '뒤로가기' 버튼입니다.
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.XButton1Pressed)
        {
            e.Handled = true;
            Frame.Navigate(typeof(HotKeyCollectPage));
        }
    }
}
