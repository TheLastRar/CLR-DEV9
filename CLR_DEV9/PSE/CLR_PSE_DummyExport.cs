#if SKIP_DLLEXPORT
using System;
using System.Runtime.InteropServices;

//In Place to Test .Net Core Port
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NativeCallableAttribute : Attribute
    {
        public string EntryPoint;
        public CallingConvention CallingConvention;
        public NativeCallableAttribute(string function) { EntryPoint = function; }
    }
}

namespace PSE
{
    public class DllExport : Attribute
    {
        public DllExport(string natName) { }
        public CallingConvention CallingConvention;
    }
}
#endif
