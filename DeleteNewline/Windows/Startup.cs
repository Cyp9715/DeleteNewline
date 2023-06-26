using System.Windows.Forms;

namespace Windows
{
    /* 
     * 해당 코드는 고전적인 .exe 파일에서만 적용할 수 있습니다.
     * 명시적인 저장공간이 제공되지 않는 MSIX 파일에서는 적용할 수 없는 코드입니다.
     * 해당 코드는 MSIX 로 제공되는 DeleteNewline 특성상 실제로 사용되지 않습니다.
     */
    static class Startup
    {
        public static Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        public const string name = DeleteNewline.Global.programName;

        public static void Registered()
        {
            key.SetValue(name, Application.ExecutablePath);
        }

        public static void Unregistered()
        {
            key.DeleteValue(name, false);
        }
    }
}
