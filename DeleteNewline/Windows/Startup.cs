using System.Windows.Forms;

namespace Windows
{
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
