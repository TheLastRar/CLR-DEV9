﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace CLRDEV9.DEV9.SMAP
{
    class BridgeHelper
    {
        static string oldBridge = "BridgeMP";
        static string newBridge = "NdisImPlatformMp";

        public static bool IsBridge(string guid)
        {
            //find adapter in WMI and compare servicename
            ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\cimv2");

            ObjectQuery query = new ObjectQuery("SELECT ServiceName, GUID FROM Win32_NetworkAdapter Where GUID = '" + guid + "'");
            using (ManagementObjectSearcher netSearcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection netQueryCollection = netSearcher.Get())
                {
                    using (ManagementObjectCollection.ManagementObjectEnumerator netMOEn = netQueryCollection.GetEnumerator())
                    {
                        if (netMOEn.MoveNext())
                        {
                            ManagementObject netMO = (ManagementObject)netMOEn.Current;
                            if (netMO["ServiceName"] == null)
                                return false;

                            string srvName = (string)netMO["ServiceName"];

                            if (srvName == oldBridge | srvName == newBridge)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static string GetGUIDFromFriendlyName(string name)
        {
            ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\cimv2");

            ObjectQuery query = new ObjectQuery("SELECT NetConnectionID, GUID FROM Win32_NetworkAdapter Where NetConnectionID = '" + name + "'");
            using (ManagementObjectSearcher netSearcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection netQueryCollection = netSearcher.Get())
                {
                    using (ManagementObjectCollection.ManagementObjectEnumerator netMOEn = netQueryCollection.GetEnumerator())
                    {
                        if (netMOEn.MoveNext())
                        {
                            ManagementObject netMO = (ManagementObject)netMOEn.Current;

                            return (string)netMO["GUID"];
                        }
                    }
                }
            }
            return null;
        }

        public static string[] ListBridgedAdapters()
        {
            List<string> guids = new List<string>();

            Netsh("bridge show adapter");

            if (outBuffer.Count > 3)
            {
                int adtrCount = outBuffer.Count - 3 - 1;
                //Have atleast one adapter
                for (int adtrIndex = 0; adtrIndex < adtrCount; adtrIndex++)
                {
                    string[] split = outBuffer[3 + adtrIndex].Split(new char[] { ' ' });
                    //ID (a number) is the 1st non-null entry
                    //compatmode is last non-null entry (enabled, disabled, unkown) (and seems to be one entry is size)
                    //god knows if this will work in non-english
                    //but i can't find an api for this
                    string name = "";
                    bool namestart = false;
                    for (int i = 0; i < split.Length - 1; i++)
                    {
                        if (namestart == false)
                        {
                            if (split[i] != "")
                            {
                                namestart = true;
                            }
                        }
                        else
                        {
                            name += split[i] + " ";
                        }
                    }
                    name = name.TrimEnd();
                    guids.Add(GetGUIDFromFriendlyName(name));
                }
            }
            else
            {
                outBuffer = null;
                return null;
            }

            outBuffer = null;
            return guids.ToArray();
        }

        public static bool IsInBridge(string guid)
        {
            return ListBridgedAdapters().Contains(guid);
        }

        public static string GetBridgeGUID()
        {
            //find adapter in WMI and compare servicename
            ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\cimv2");

            ObjectQuery query = new ObjectQuery("SELECT ServiceName, GUID FROM Win32_NetworkAdapter Where ServiceName = '" + oldBridge + "' OR ServiceName = '" + newBridge + "'");
            using (ManagementObjectSearcher netSearcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection netQueryCollection = netSearcher.Get())
                {
                    using (ManagementObjectCollection.ManagementObjectEnumerator netMOEn = netQueryCollection.GetEnumerator())
                    {
                        if (netMOEn.MoveNext())
                        {
                            ManagementObject netMO = (ManagementObject)netMOEn.Current;

                            return (string)netMO["GUID"];
                        }
                    }
                }
            }
            return "";
        }

        static List<string> outBuffer = null;
        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Collect the sort command output.
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                outBuffer.Add(outLine.Data);
            }
        }

        private static void Netsh(string Args)
        {
            outBuffer = new List<string>();

            ProcessStartInfo netshSI = new ProcessStartInfo("netsh", Args);
            netshSI.RedirectStandardError = true;
            netshSI.RedirectStandardOutput = true;
            netshSI.RedirectStandardInput = true;
            netshSI.UseShellExecute = false;
            netshSI.CreateNoWindow = true;

            Process netsh = new Process();
            netsh.StartInfo = netshSI;
            netsh.OutputDataReceived += OutputHandler;
            netsh.ErrorDataReceived += OutputHandler;

            netsh.Start();
            netsh.BeginOutputReadLine();
            netsh.BeginErrorReadLine();
            netsh.WaitForExit();
        }
    }
}