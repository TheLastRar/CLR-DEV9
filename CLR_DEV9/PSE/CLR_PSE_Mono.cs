using System;
using System.Runtime.InteropServices;
using PSE.CLR_PSE_Callbacks;

namespace PSE
{
    class CLR_PSE_Mono
    {
        //Can't figure out how to do this in mono-embed
        static CLR_CyclesCallback CyclesCallbackFromFunctionPointer(IntPtr func)
        {
            return (CLR_CyclesCallback)Marshal.GetDelegateForFunctionPointer(func, typeof(CLR_CyclesCallback));
        }

        //Less messy to do here than in mono-embed
        static IntPtr FunctionPointerFromIRQHandler(CLR_IRQHandler func)
        {
            return Marshal.GetFunctionPointerForDelegate(func);
        }
    }
}
