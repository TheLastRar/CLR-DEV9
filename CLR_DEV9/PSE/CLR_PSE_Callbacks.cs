using System.Runtime.InteropServices;

namespace PSE.CLR_PSE_Callbacks
{
    //Async callback
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void CLR_CyclesCallback(int cycles);
    //PCSX2 Handler
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int CLR_IRQHandler();
}
