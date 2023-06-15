using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using VirtualInput;

namespace VirtualInput
{
    class InputImplement
    {
        public static void TypeKeyboard_Copy()
        {
            /*
             * [COM 객체 Thread 문제로 인한 STA Thread 분기.]
             * Thread 를 생성하지 않고 그냥 Sleep 을 진행하면
             * Clipboared.GetObjectData() 함수의 호출 부분에서 문제가 발행함 (OpenClipboard Failed)
             * 
             * 문제는 쓰레드 밖에서 사용한 Thread.Sleep() 으로 인해 발생한다.
             * Thread.Sleep 과 Thread.Join() 의 내부적인 구현 형태에 대해서는 정확히 모르겠지만
             * Thread.Sleep() 을 통한대기는 COM 개체와 통신이 불가능한 반면
             * Thread.Join() 을 통해서는 COM 개체와 통신이 가능한것으로 판단됨.
             * Thread 내부에 Sleep 을 둠으로서 안정성 확보...
             * 
             * 단순 클립보드를 복사해오는 시간을 버는것이 목적이므로 적절한 200ms 정도로 설정하였음.
             * 개선할 여지가 분명히 있으나 일단 보류.
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