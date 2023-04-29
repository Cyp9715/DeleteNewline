using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using VirtualInput;

namespace VirtualInput
{
    class InputImplement
    {
        public static void PressKeyboard_Copy(int delayTime_ms)
        {
            // 워낙 빠른시간내에 코드가 동작하다 보니 Reset 하지 않으면 복사를 입력받지 못함.
            VirtualInput.Keyboard.Reset();
            VirtualInput.Keyboard.Press(Key.LeftCtrl);
            VirtualInput.Keyboard.Type(Key.C);
            VirtualInput.Keyboard.Release(Key.LeftCtrl);

            // Ctrl + C 를 입력하기 위한 Sleep.
            // 해당 부분의 Sleep 으로 인한 문제가 발생함.
            // Clipboard.GetObjectData()...
            Thread.Sleep(delayTime_ms);
        }
    }
}