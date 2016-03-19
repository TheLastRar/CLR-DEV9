//using System;
//using System.Runtime.InteropServices;
//using CLRDEV9.DEV9.SMAP.Tap.INetCfgCOM;

//namespace CLRDEV9.DEV9.SMAP.Tap
//{
//    class Bridge
//    {
//        //Reg Helper
//        [DllImport("advapi32.dll", SetLastError = true)]
//        static extern uint RegQueryValueEx(
//            [In, MarshalAs(UnmanagedType.SysUInt)] UIntPtr hKey,
//            [In, MarshalAs(UnmanagedType.LPStr)] string lpValueName,
//            int lpReserved,
//            [Out, Optional, MarshalAs(UnmanagedType.I4)] out Microsoft.Win32.RegistryValueKind lpType,
//            [Optional, MarshalAs(UnmanagedType.SysInt)] IntPtr lpData,
//            [Optional] ref int lpcbData);

//        [DllImport("advapi32.dll", SetLastError = true)]
//        public static extern int RegCloseKey(
//            [In] UIntPtr hKey);

//        public static int GetInstance(out INetCfg netCfg, out INetCfgLock netCfgLock)
//        {
//            object objp;
//            int hr = Ole32Methods.CoCreateInstance(ref INetCfg_Guid.CLSID_CNetCfg, null, Ole32Methods.CLSCTX_INPROC_SERVER, ref INetCfg_Guid.IID_INetCfg, out objp);
//            netCfg = objp as INetCfg;
//            if (hr == 0)
//            {
//                netCfgLock = netCfg as INetCfgLock;
//            }
//            else
//            {
//                netCfgLock = null;
//            }
//            return hr;
//        }

//        public static int GetMiniportDriverComponent(INetCfg NetCfg, out INetCfgComponent component)
//        {
//            object objp;
//            //int hr = NetCfg.FindComponent("ms_bridgemp", out objp); //COMPOSITEBUS\MS_IMPLAT_MP
//            int hr = NetCfg.FindComponent(@"COMPOSITEBUS\MS_IMPLAT_MP", out objp); //COMPOSITEBUS\MS_IMPLAT_MP
//            component = objp as INetCfgComponent;
//            return hr;
//        }
//        public static int GetDriverComponent(INetCfg NetCfg, out INetCfgComponent component)
//        {
//            object objp;
//            int hr = NetCfg.FindComponent("ms_bridge", out objp);
//            component = objp as INetCfgComponent;
//            return hr;
//        }
//        // GetMiniportAdapterComponent
//        public static int GetAdapterById(INetCfg netCfg, string nodeIdOrIndex, bool isIndex, out INetCfgComponent outComponent)
//        {
//            outComponent = null;
//            object objp;
//            int hr = -1;
//            IEnumNetCfgComponent enumerator;
//            INetCfgComponent component;

//            hr = netCfg.EnumComponents(ref INetCfg_Guid.IID_DevClassNet, out objp);
//            enumerator = objp as IEnumNetCfgComponent;

//            if (hr == 0)
//            {
//                uint count;
//                uint i = 0;

//                while (enumerator.Next(1, out objp, out count) == 0)
//                {
//                    component = objp as INetCfgComponent;
//                    int physicalMediaType = 1;
//                    UIntPtr hKey;

//                    if (component.OpenParamKey(out hKey) == 0)
//                    {
//                        int DataSize = sizeof(int);
//                        IntPtr memPtr = Marshal.AllocHGlobal(DataSize);
//                        Marshal.WriteInt32(memPtr, 0);
//                        Microsoft.Win32.RegistryValueKind regKind;
//                        uint ret = RegQueryValueEx(hKey, "*PhysicalMediaType", 0, out regKind, memPtr, ref DataSize);
//                        physicalMediaType = Marshal.ReadInt32(memPtr, 0);
//                        Marshal.FreeHGlobal(memPtr);
//                        RegCloseKey(hKey);
//                    }

//                    string nodeId;
//                    bool isFound = false;

//                    if (component.GetPnpDevNodeId(out nodeId) == 0)
//                    {
//                        if (physicalMediaType > 0 && physicalMediaType < 19)
//                        {
//                            bool isAvailable = true;
//                            //Is Available? (Skipping)
//                            if (isAvailable)
//                            {
//                                if (isIndex)
//                                {
//                                    int compareNetLuidIndex = int.Parse(nodeIdOrIndex);
//                                    if (compareNetLuidIndex == i)
//                                    {
//                                        //hr = BridgeToAdapter(netCfg, component, bridge);
//                                        outComponent = component;
//                                        isFound = true;
//                                    }
//                                }
//                                else
//                                {
//                                    if (nodeId == nodeIdOrIndex)
//                                    {
//                                        //hr = BridgeToAdapter(netCfg, component, bridge);
//                                        outComponent = component;
//                                        isFound = true;
//                                    }
//                                }
//                            }
//                        }
//                    }
//                    i++;
//                    if (isFound) break;
//                    Marshal.ReleaseComObject(component);
//                }
//                Marshal.ReleaseComObject(enumerator);
//            }
//            else
//            {
//                outComponent = null;
//                return 1; //S_FALSE
//            }
//            if (outComponent == null)
//            {
//                return 1; //S_FALSE
//            }
//            else
//            {
//                return 0;
//            }
//        }

//        public static bool IsAdapterInstalled(INetCfg netCfg)
//        {
//            INetCfgComponent component;

//            if (GetMiniportDriverComponent(netCfg, out component) == 0)
//            {
//                Marshal.ReleaseComObject(component);
//                return true;
//            }
//            return false;
//        }
//        public static bool IsDriverInstalled(INetCfg netCfg)
//        {
//            INetCfgComponent component;

//            if (GetDriverComponent(netCfg, out component) == 0)
//            {
//                Marshal.ReleaseComObject(component);
//                return true;
//            }
//            return false;
//        }
//        //RestartAdapter
//        //UninstallDriver
//        //InstallDriver

//        public static int BindTo(Guid guid, INetCfg netCfg, INetCfgComponent netComponent, bool isBinding, string[] ignoredProtocols = null)
//        {
//            object objp = null;
//            int hr = 1;
//            IEnumNetCfgComponent enumerator;
//            INetCfgComponent component;

//            hr = netCfg.EnumComponents(ref guid, out objp);
//            enumerator = objp as IEnumNetCfgComponent;

//            if (hr == 0)
//            {
//                uint Count = 0;
//                while (enumerator.Next(1, out objp, out Count) == 0)
//                {
//                    component = objp as INetCfgComponent;
//                    string Id;

//                    if (component.GetId(out Id) == 0)
//                    {
//                        bool Ignored = false;

//                        if (Ignored == false && ignoredProtocols != null)
//                        {
//                            int ignoredProtocolsIndex = 0;

//                            while (ignoredProtocolsIndex < ignoredProtocols.Length)
//                            {
//                                string ignoredProtocolsStart = ignoredProtocols[ignoredProtocolsIndex];
//                                if (Id == ignoredProtocolsStart)
//                                {
//                                    Ignored = true;
//                                    break;
//                                }
//                                ignoredProtocolsIndex += 1;
//                            }
//                        }

//                        if (Ignored == false)
//                        {
//                            INetCfgComponentBindings bindings;
//                            bindings = component as INetCfgComponentBindings;

//                            if (isBinding)
//                            {
//                                if (bindings.IsBindableTo(netComponent) == 0)
//                                {
//                                    hr = bindings.BindTo(netComponent);
//                                }
//                            }
//                            else {
//                                if (bindings.IsBoundTo(netComponent) == 0)
//                                {
//                                    hr = bindings.UnbindFrom(netComponent);
//                                }
//                            }
//                            Marshal.ReleaseComObject(bindings);
//                        }
//                    }
//                    Marshal.ReleaseComObject(component);
//                }
//                Marshal.ReleaseComObject(enumerator);
//            }
//            return hr;
//        }

//        public static int BridgeToAdapter(INetCfg netCfg, INetCfgComponent netComponent, bool bridge = true)
//        {
//            int hr = 1;
//            INetCfgComponent bridgeComponent;

//            if (GetDriverComponent(netCfg, out bridgeComponent) == 0)
//            {
//                INetCfgComponentBindings bindings = bridgeComponent as INetCfgComponentBindings;

//                if (bindings.IsBindableTo(netComponent) == 0)
//                {
//                    if (bridge)
//                    {
//                        hr = bindings.BindTo(netComponent);
//                        if (hr == 0)
//                        {
//                            hr = BindTo(INetCfg_Guid.IID_DevClassNetTrans, netCfg, netComponent, false, new string[] { "ms_bridge", "ms_ndisuio" }); // Unbind other protocols
//                            hr = BindTo(INetCfg_Guid.IID_DevClassNetService, netCfg, netComponent, false, null);
//                            hr = BindTo(INetCfg_Guid.IID_DevClassNetClient, netCfg, netComponent, false, null);
//                        }
//                    }
//                    else
//                    {
//                        hr = bindings.UnbindFrom(netComponent);
//                        if (hr == 0)
//                        {
//                            hr = BindTo(INetCfg_Guid.IID_DevClassNetTrans, netCfg, netComponent, true, new string[] { "ms_bridge" }); // Bind other protocols
//                            hr = BindTo(INetCfg_Guid.IID_DevClassNetService, netCfg, netComponent, true, null);
//                            hr = BindTo(INetCfg_Guid.IID_DevClassNetClient, netCfg, netComponent, true, null);
//                        }
//                    }
//                }
//                Marshal.ReleaseComObject(bindings);
//                Marshal.ReleaseComObject(bridgeComponent);
//            }
//            return hr;
//        }

//        public static int BridgeToAdapterById(INetCfg netCfg, string nodeIdOrIndex, bool isIndex, bool bridge)
//        {
//            int hr = 1;
//            INetCfgComponent component;

//            if (GetAdapterById(netCfg, nodeIdOrIndex, isIndex, out component) == 0)
//            {
//                hr = BridgeToAdapter(netCfg, component, bridge);
//            }

//            return hr;
//            //object objp;
//            //int hr = -1;
//            //IEnumNetCfgComponent enumerator;
//            //INetCfgComponent component;

//            //hr = netCfg.EnumComponents(ref INetCfg_Guid.IID_DevClassNet, out objp);
//            //enumerator = objp as IEnumNetCfgComponent;

//            //if (hr == 0)
//            //{
//            //    int count;
//            //    uint i = 0;

//            //    while (enumerator.Next(1, out objp, out count) == 0)
//            //    {
//            //        component = objp as INetCfgComponent;
//            //        int physicalMediaType = 0;
//            //        IntPtr hKey;

//            //        if (component.OpenParamKey(out hKey) == 0)
//            //        {
//            //            int DataSize = sizeof(int);
//            //            IntPtr memPtr = Marshal.AllocHGlobal(DataSize);
//            //            Microsoft.Win32.RegistryValueKind regKind = Microsoft.Win32.RegistryValueKind.Unknown;
//            //            RegQueryValueEx(hKey, "PhysicalMediaType", 0, ref regKind, memPtr, ref DataSize);
//            //            physicalMediaType = Marshal.ReadInt32(memPtr, 0);
//            //            Marshal.FreeHGlobal(memPtr);
//            //            RegCloseKey(hKey);
//            //        }

//            //        string nodeId;
//            //        bool isFound = false;

//            //        if (component.GetPnpDevNodeId(out nodeId) == 0)
//            //        {
//            //            if (physicalMediaType > 0 && physicalMediaType < 19)
//            //            {
//            //                bool isAvailable = true;
//            //                //Is Available? (Skipping)
//            //                if (isAvailable)
//            //                {
//            //                    if (isIndex)
//            //                    {
//            //                        int compareNetLuidIndex = int.Parse(nodeIdOrIndex);
//            //                        if (compareNetLuidIndex == i)
//            //                        {
//            //                            hr = BridgeToAdapter(netCfg, component, bridge);
//            //                            isFound = true;
//            //                        }
//            //                    }
//            //                    else
//            //                    {
//            //                        if (nodeId == nodeIdOrIndex)
//            //                        {
//            //                            hr = BridgeToAdapter(netCfg, component, bridge);
//            //                            isFound = true;
//            //                        }
//            //                    }
//            //                }
//            //            }
//            //        }
//            //        Marshal.ReleaseComObject(component);
//            //        i++;
//            //        if (isFound) break;
//            //    }
//            //    Marshal.ReleaseComObject(enumerator);
//            //}
//            //else
//            //{
//            //    return 1; //S_FALSE
//            //}
//            //return hr;
//        }

//        public static bool IsBridgedToAdapter(INetCfg netCfg, INetCfgComponent netComponent)
//        {
//            int hr = 1;
//            INetCfgComponent bridgeComponent;

//            if (GetDriverComponent(netCfg, out bridgeComponent) == 0)
//            {
//                INetCfgComponentBindings bindings = bridgeComponent as INetCfgComponentBindings;

//                if (bindings.IsBindableTo(netComponent) == 0)
//                {
//                    hr = bindings.IsBoundTo(netComponent);
//                }
//                Marshal.ReleaseComObject(bindings);
//                Marshal.ReleaseComObject(bridgeComponent);
//            }
//            if (hr == 1)
//            {
//                return false;
//            }
//            else
//            {
//                return true;
//            }
//        }

//        public static bool IsBridgedToAdapterById(INetCfg netCfg, string nodeIdOrIndex, bool isIndex)
//        {
//            bool hr = false;
//            INetCfgComponent component;

//            if (GetAdapterById(netCfg, nodeIdOrIndex, isIndex, out component) == 0)
//            {
//                hr = IsBridgedToAdapter(netCfg, component);
//                Marshal.ReleaseComObject(component);
//            }

//            return hr;
//        }
//    }
//}
