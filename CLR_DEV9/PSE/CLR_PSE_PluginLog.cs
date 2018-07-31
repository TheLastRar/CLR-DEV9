using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CLRDEV9;

namespace PSE
{
    internal class CLR_PSE_PluginLog
    {
        //Constants
        const int UNKOWN = -1;
        const int ERRTRAP = -2;
        const SourceLevels consoleStdLevel = SourceLevels.Information & ~(SourceLevels.Error);
        const SourceLevels consoleErrLevel = SourceLevels.Error;
        const SourceLevels FileLevel = SourceLevels.Error;

        //current path to detech if log path has changed
        static string currentLogPath = "";
        //all then loggers
        static Dictionary<int, TraceSource> sources = null;
        static TraceListener fileAll; //both out and error go to same file
        static TraceListener stdOut;
        static TraceListener stdErr;

        //Enable AutoFlush
        static CLR_PSE_PluginLog()
        {
            Trace.AutoFlush = true;
        }

        //Set filter of Source
        public static void SetSourceLogLevel(SourceLevels eLevel, int logSource)
        {
            if (sources.ContainsKey(logSource))
            {
                sources[logSource].Switch.Level = eLevel;
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
        public static void SetSourceUseStdOut(bool use, int logSource)
        {
            if (sources.ContainsKey(logSource))
            {
                if (!use)
                {
                    sources[logSource].Listeners.Remove(stdOut);
                }
                else if (!sources[logSource].Listeners.Contains(stdOut))
                {
                    sources[logSource].Listeners.Add(stdOut);
                }
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
        //Change filter of Listerners (effects all connected sources)
        public static void SetStdOutLevel(SourceLevels eLevel)
        {
            if (stdOut != null)
                stdOut.Filter = new EventTypeFilter(eLevel);
        }
        public static void SetStdErrLevel(SourceLevels eLevel)
        {
            if (stdErr != null)
                stdErr.Filter = new EventTypeFilter(eLevel);
        }
        public static void SetFileLevel(SourceLevels eLevel)
        {
            if (fileAll != null)
                fileAll.Filter = new EventTypeFilter(eLevel);
        }

        //Add Source to sources, only used in Open()
        private static void AddSource(int id, string name, string prefix)
        {
            TraceSource newSource = new TraceSource(prefix + ":" + name);
            newSource.Switch = new SourceSwitch(prefix + ":" + name + ".SS");
            newSource.Switch.Level = SourceLevels.Information;
            newSource.Listeners.Remove("Default");
            newSource.Listeners.Add(fileAll);
            newSource.Listeners.Add(stdErr);

            sources.Add(id, newSource);
        }
        public static void Open(string logFolderPath, string logFileName, string prefix, Dictionary<ushort, string> sourceIDs)
        {
            logFolderPath = logFolderPath.TrimEnd(Path.DirectorySeparatorChar);
            logFolderPath = logFolderPath.TrimEnd(Path.AltDirectorySeparatorChar);
            if (sourceIDs == null)
            {
                throw new NullReferenceException();
            }
            if (sources == null || (currentLogPath != logFolderPath + Path.DirectorySeparatorChar + logFileName))
            {
                Close();

                if (File.Exists(logFolderPath + Path.DirectorySeparatorChar + logFileName))
                {
                    try
                    {
                        File.Delete(logFolderPath + Path.DirectorySeparatorChar + logFileName);
                    }
                    catch
                    {
                    }
                }
                //Console Normal
                if (CLR_PSE_Utils.IsWindows())
                {
                    stdOut = new TextWriterTraceListener(new CLR_PSE_NativeLoggerWin(false));
                }
                else
                {
                    stdOut = new TextWriterTraceListener(Console.Out);
                }
                stdOut.Filter = new EventTypeFilter(consoleStdLevel); //information
                stdOut.Name = "StdOut";
                //Console Error
                if (CLR_PSE_Utils.IsWindows())
                {
                    stdErr = new TextWriterTraceListener(new CLR_PSE_NativeLoggerWin(true));
                }
                else
                {
                    stdErr = new TextWriterTraceListener(Console.Error);
                }
                stdErr.Filter = new EventTypeFilter(consoleErrLevel);
                stdErr.Name = "StdErr";
                currentLogPath = logFolderPath + Path.DirectorySeparatorChar + logFileName;
                //Text File
                try
                {
                    fileAll = new TextWriterTraceListener(currentLogPath);
                    fileAll.Filter = new EventTypeFilter(FileLevel);
                    fileAll.Name = "File";
                    //defualtSource.Listeners.Add(textListener);
                }
                catch (Exception e)
                {
                    //Console.Error.WriteLine("Failed to Open Log File :" + e.ToString());
                    stdErr.WriteLine("Failed to Open Log File :" + e.ToString());
                }
                //Create sources
                sources = new Dictionary<int, TraceSource>();
                //Defualt Sources
                AddSource(UNKOWN, "UnkownSource", prefix);
                SetSourceLogLevel(SourceLevels.All, UNKOWN);
                SetSourceUseStdOut(true, UNKOWN);
                AddSource(ERRTRAP, "ErrorTrapper", prefix);
                SetSourceUseStdOut(true, ERRTRAP);

                foreach (KeyValuePair<ushort, string> sourceID in sourceIDs)
                {
                    AddSource(sourceID.Key, sourceID.Value, prefix);
                }
            }
        }
        public static void Close()
        {
            if (sources == null) return;
            foreach (KeyValuePair<int, TraceSource> source in sources)
            {
                //will close all listerners
                source.Value.Close();
            }
            sources.Clear();
            sources = null;
        }

        public static void WriteLine(TraceEventType eType, int logSource, string str)
        {
            if (sources == null)
                return;
            if (DEV9Header.config != null)
                if  ((!DEV9Header.config.EnableLogging.Error && eType == TraceEventType.Error) ||
                    (!DEV9Header.config.EnableLogging.Verbose && eType == TraceEventType.Verbose) ||
                    (!DEV9Header.config.EnableLogging.Information && eType == TraceEventType.Information))
                    return;
            if (sources.ContainsKey(logSource))
            {
                sources[logSource].TraceEvent(eType, logSource, str);
            }
            else
            {
                sources[UNKOWN].TraceEvent(eType, logSource, str);
            }
        }

        public static void MsgBoxErrorTrapper(Exception e)
        {
            Console.Error.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
#if NETCOREAPP2_0
            SDL2.MessageBox.Show(SDL2.MessageBoxFlags.Error, "Fatal Error", "Encounted Exception! : " + e.Message + Environment.NewLine + e.StackTrace);
#else
            System.Windows.Forms.MessageBox.Show("Encounted Exception! : " + e.Message + Environment.NewLine + e.StackTrace);
#endif
            try
            {
                if (sources != null)
                {
                    WriteLine(TraceEventType.Critical, ERRTRAP, e.Message + Environment.NewLine + e.StackTrace);
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
    }
}
