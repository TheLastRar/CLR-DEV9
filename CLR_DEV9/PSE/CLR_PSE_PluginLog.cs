using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PSE
{
    class CLR_PSE_PluginLog
    {
        static TraceSource mySource = null;
        static SourceSwitch defualtSwicth = new SourceSwitch("Default");
        static string currentLogPath = "";

        static CLR_PSE_PluginLog()
        {
            defualtSwicth.Level = SourceLevels.Error;
        }

        static Dictionary<int, SourceSwitch> enabledLogLevels = new Dictionary<int, SourceSwitch>();
        public static void SetLogLevel(SourceLevels eLevel, int logSource)
        {
            if (enabledLogLevels.ContainsKey(logSource))
            {
                enabledLogLevels[logSource].Level = eLevel;
            } 
            else
            {
                SourceSwitch newSwicth = new SourceSwitch("ID: " + logSource);
                newSwicth.Level = eLevel;
                enabledLogLevels.Add(logSource, newSwicth);
            }
        }

        public static void Open(string logFolderPath , string logFileName)
        {
            if (currentLogPath != logFolderPath + "\\" + logFileName)
            {
                Close();

                mySource = new TraceSource("CLR_DEV9");
                mySource.Switch = new SourceSwitch("Accept All");
                mySource.Listeners.Remove("Default");
                mySource.Switch.Level = SourceLevels.All;

                currentLogPath = logFolderPath + "\\" + logFileName;
                //Text File
                try
                {
                    TextWriterTraceListener textListener = new TextWriterTraceListener(currentLogPath);
                    textListener.Filter = new EventTypeFilter(SourceLevels.All);
                    mySource.Listeners.Add(textListener);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Failed to Open Log File");
                }
                //Console Normal
                ConsoleTraceListener consoleLog = new ConsoleTraceListener(true);
                consoleLog.Filter = new EventTypeFilter(SourceLevels.Information & ~ (SourceLevels.Error));
                //Console Error
                ConsoleTraceListener consoleError = new ConsoleTraceListener(true);
                consoleError.Filter = new EventTypeFilter(SourceLevels.Error);

                //Add Sources
                
                mySource.Listeners.Add(consoleLog);
                mySource.Listeners.Add(consoleError);
            }
        }

        public static void Close()
        {
            if (mySource == null) return;
            mySource.Close();
            mySource = null;
        }

        public static void Write(TraceEventType eType, int logSource, string prefix, string str)
        {
            if (mySource == null) return;
            if ((enabledLogLevels.ContainsKey(logSource) && enabledLogLevels[logSource].ShouldTrace(eType)) ||
                    defualtSwicth.ShouldTrace(eType))
                mySource.TraceEvent(eType, logSource, "[" + prefix + "] " + str);
        }
        public static void WriteLine(TraceEventType eType, int logSource, string prefix, string str)
        {
            if (mySource == null) return;
            if ((enabledLogLevels.ContainsKey(logSource) && enabledLogLevels[logSource].ShouldTrace(eType)) ||
                    defualtSwicth.ShouldTrace(eType))
                mySource.TraceEvent(eType, logSource, "[" + prefix + "] " + str);
        }
    }
}
