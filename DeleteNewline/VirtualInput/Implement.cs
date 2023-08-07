using System.Threading;
using System.Windows.Input;

namespace VirtualInput
{
    class Implement
    {
        public static void TypeKeyboard_Copy()
        {
            /*
             * [COM 객체 Thread 문제로 인한 STA Thread 분기.]
             * 
             * Ctrl + C 동작은 Sleep 없이 즉발로 발동됨.
             * ClipboardManager 내부의 Sleep 과 어느정도 연동성이 존재함. (때문에 조심히 다뤄야 함.)
             * 
             * 해당 부분에서 Sleep 를 운용할 경우
             * RetryClipboardAction() 부분의 Sleep 를 더 넉넉하게 부여해야 함.
             * 
            */
            Thread thread = new Thread(() =>
            {
                VirtualInput.Keyboard.Reset();
                VirtualInput.Keyboard.Press(Key.LeftCtrl);
                VirtualInput.Keyboard.Type(Key.C);
                VirtualInput.Keyboard.Release(Key.LeftCtrl);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}