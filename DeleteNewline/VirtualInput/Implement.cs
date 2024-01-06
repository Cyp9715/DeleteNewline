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
             * Virtual Key 입력의 즉발성으로 인해 어느정도의 유휴시간이 필요함.
            */
            Thread thread = new Thread(() =>
            {
                VirtualInput.Keyboard.Reset();
                VirtualInput.Keyboard.Press(Key.LeftCtrl);
                VirtualInput.Keyboard.Type(Key.C);
                VirtualInput.Keyboard.Release(Key.LeftCtrl);

                Thread.Sleep(200);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}