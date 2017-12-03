using System;
using System.Runtime.InteropServices;

//In Place to Test .Net Core Port
namespace PSE
{
    public class DllExport : Attribute
    {
        public DllExport(string natName) { }
        public CallingConvention CallingConvention;
    }
}
