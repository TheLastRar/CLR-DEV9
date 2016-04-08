using System;
using System.Runtime.InteropServices;

namespace PSE
{
    internal class CLR_PSE_SudoPtr : SafeBuffer
    {
        public CLR_PSE_SudoPtr(IntPtr ptr) : base(true)
        {
            SetHandle(ptr);
        }

        protected override bool ReleaseHandle()
        {
            handle = IntPtr.Zero;
            return true;
        }
    }
}
