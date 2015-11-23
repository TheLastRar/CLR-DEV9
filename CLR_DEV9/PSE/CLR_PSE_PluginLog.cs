using System;

namespace PSE
{
    class CLR_PSE_PluginLog
    {
        public void ErrorWrite(string str)
        {
            Console.Error.Write(str);
            //log to stderr
            //this.LogWrite(str);
        }
        public void ErrorWriteLine(string str)
        {
            Console.Error.WriteLine(str);
            //log to stderr
            //this.LogWriteLine(str);
        }

    }
}
