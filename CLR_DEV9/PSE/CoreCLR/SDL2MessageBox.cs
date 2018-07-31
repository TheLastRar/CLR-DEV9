using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SDL2
{
    enum MessageBoxFlags
    {
        Error = 0x00000010,
        Warning = 0x00000020,
        Information  = 0x00000040
    }

    static class MessageBox
    {
        [DllImport("SDL2")]
        private static extern int SDL_ShowSimpleMessageBox(uint flags, string title, string message, IntPtr window);

        public static int Show(MessageBoxFlags flags, string title, string message)
        {
            return SDL_ShowSimpleMessageBox((uint)flags, title, message, new IntPtr(0));
        }
    }
}