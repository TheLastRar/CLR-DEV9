using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PSE
{
    class CLR_PSE_PluginLog
    {
        static TraceSource mySource = null;
        static SourceSwitch defualtSwicth = new SourceSwitch("Default");
        static SourceLevels ConsoleStdLevel = SourceLevels.All & ~(SourceLevels.Error);
        static SourceLevels ConsoleErrLevel = SourceLevels.Error;
        static SourceLevels FileLevel = SourceLevels.All;
        static string currentLogPath = "";

        static CLR_PSE_PluginLog()
        {
            defualtSwicth.Level = SourceLevels.Error;
            Trace.AutoFlush = true;
            SetLogLevel(SourceLevels.Critical, -1);
        }

        static Dictionary<int, SourceSwitch> enabledLogLevels = new Dictionary<int, SourceSwitch>();

        public static void MsgBoxError(Exception e)
        {
            Console.Error.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
            System.Windows.Forms.MessageBox.Show("Encounted Exception! : " + Environment.NewLine + e.Message);
            try
            {
                //System.IO.File.WriteAllLines(logPath + "\\" + libraryName + " ERR.txt", new string[] { e.Message + Environment.NewLine + e.StackTrace });
                if (mySource != null)
                {
                    WriteLine(TraceEventType.Critical, -1, "ERROR", e.Message + Environment.NewLine + e.StackTrace);
                }
                else
                {
                    throw new Exception("Error Before Log Open");
                }
            }
            catch
            {
                Console.Error.WriteLine("Error while writing ErrorLog");
            }
        }

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

        public static void SetStdOutLevel(SourceLevels eLevel)
        {
            ConsoleStdLevel = eLevel;
            if (mySource != null)
            {
                mySource.Listeners["StdOut"].Filter = new EventTypeFilter(ConsoleStdLevel);
            }
        }
        public static void SetStdErrLevel(SourceLevels eLevel)
        {
            ConsoleErrLevel = eLevel;
            if (mySource != null)
            {
                mySource.Listeners["StdErr"].Filter = new EventTypeFilter(ConsoleErrLevel);
            }
        }
        public static void SetFileLevel(SourceLevels eLevel)
        {
            FileLevel = eLevel;
            if (mySource != null)
            {
                mySource.Listeners["File"].Filter = new EventTypeFilter(FileLevel);
            }
        }

        public static void Open(string logFolderPath , string logFileName)
        {
            if (currentLogPath != logFolderPath + "\\" + logFileName)
            {
                Close();

                if (File.Exists(logFolderPath + "\\" + logFileName))
                {
                    try
                    {
                        File.Delete(logFolderPath + "\\" + logFileName);
                    } catch
                    {
                    }
                }

                mySource = new TraceSource("CLR_DEV9");
                mySource.Switch = new SourceSwitch("Accept All");
                mySource.Listeners.Remove("Default");
                mySource.Switch.Level = SourceLevels.All;

                currentLogPath = logFolderPath + "\\" + logFileName;
                //Text File
                try
                {
                    TextWriterTraceListener textListener = new TextWriterTraceListener(currentLogPath);
                    textListener.Filter = new EventTypeFilter(FileLevel);
                    textListener.Name = "File";
                    mySource.Listeners.Add(textListener);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Failed to Open Log File :" + e.ToString());
                }
                //Console Normal
                ConsoleTraceListener consoleLog = new ConsoleTraceListener(true);
                consoleLog.Filter = new EventTypeFilter(ConsoleStdLevel); //information
                consoleLog.Name = "StdOut";
                //Console Error
                ConsoleTraceListener consoleError = new ConsoleTraceListener(true);
                consoleError.Filter = new EventTypeFilter(ConsoleErrLevel);
                consoleError.Name = "StdErr";
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
            {
                mySource.TraceEvent(eType, logSource, "[" + prefix + "] " + str);
            }
        }
        public static void WriteLine(TraceEventType eType, int logSource, string prefix, string str)
        {
            if (mySource == null) return;
            if ((enabledLogLevels.ContainsKey(logSource) && enabledLogLevels[logSource].ShouldTrace(eType)) ||
                    defualtSwicth.ShouldTrace(eType))
            {
                mySource.TraceEvent(eType, logSource, "[" + prefix + "] " + str);
            }
        }
    }
}
