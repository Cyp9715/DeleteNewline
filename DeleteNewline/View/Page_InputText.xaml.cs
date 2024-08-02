using DeleteNewline.ViewModel;

namespace DeleteNewline.View
{
    public partial class Page_InputText
    {
        public Page_InputText(ViewModel_InputText? vm_inputText)
        {
            InitializeComponent();
            DataContext = vm_inputText;
        }
    }
}
