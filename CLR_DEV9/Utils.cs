using System;

namespace CLRDEV9
{
    static class Utils
    {
        public static void memcpy(byte[] target, int targetstartindex, byte[] source, int sourcestartindex, int num)
        {
            Array.Copy(source, sourcestartindex, target, targetstartindex, num);
        }
        public static void memset(ref byte[] data, int offset, byte value, int length)
        {
            //Can be faster using unsafe fill http://techmikael.blogspot.co.uk/2009/12/filling-array-with-default-value.html
            //Is that really needed?
            for (int x = offset; x <= (offset + length) - 1; x++)
            {
                data[x] = value;
            }
        }
        public static bool memcmp(byte[] target, int targetstartindex, byte[] source, int sourcestartindex, int num)
        {
            bool match = true;
            for (int x = 0; x <= num - 1; x++)
            {
                if (!(target[targetstartindex + x] == source[sourcestartindex + x]))
                {
                    match = false;
                    return match;
                }
            }
            return match;
        }
        public static bool memcmp(byte[] target, int targetstartindex, ushort[] source, int sourcestartindex, int num)
        {
            //TODO change this to use pointers for casting?
            bool match = true;
            for (int x = 0; x <= num - 1; x++)
            {
                if (!(target[targetstartindex + x] == source[sourcestartindex + x]))
                {
                    match = false;
                    return match;
                }
            }
            return match;
        }
    }
}
