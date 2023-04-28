using System.Windows.Input;
using VirtualInput;

namespace VirtualInput
{
    class InputImplement
    {
        public static void PressKeyboard_Copy()
        {
            VirtualInput.Keyboard.Reset(); // 워낙 빠른시간내에 코드가 동작하다 보니 Reset 하지 않으면 복사를 입력받지 못함.
            VirtualInput.Keyboard.Press(Key.LeftCtrl);
            VirtualInput.Keyboard.Type(Key.C);
            VirtualInput.Keyboard.Release(Key.LeftCtrl);
        }
    }
}
