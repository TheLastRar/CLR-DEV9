//using System;
//using System.Runtime.InteropServices;

//namespace CLRDEV9.DEV9.SMAP.Tap.INetCfgCOM
//{
//    public class Ole32Methods
//    {
//        [DllImport("ole32.Dll")]
//        //Why ref?
//        static public extern int CoCreateInstance(ref Guid clsid,
//           [MarshalAs(UnmanagedType.IUnknown)] object inner,
//           uint context,
//           ref Guid uuid,
//           [Out, MarshalAs(UnmanagedType.IUnknown)] out object rReturnedComObject);
//        public static uint CLSCTX_INPROC_SERVER = 1;
//    }
//    public abstract class INetCfg_Guid
//    {
//        public static Guid CLSID_CNetCfg = new Guid("5B035261-40F9-11D1-AAEC-00805FC1270E");
//        public static Guid IID_INetCfg = new Guid("C0E8AE93-306E-11D1-AACF-00805FC1270E");
//        public static Guid IID_DevClassNet = new Guid(0x4d36e972, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18);
//        public static Guid IID_DevClassNetClient = new Guid(0x4d36e973, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18);
//        public static Guid IID_DevClassNetService = new Guid(0x4d36e974, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18);
//        public static Guid IID_DevClassNetTrans = new Guid(0x4d36e975, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18);
//        public static Guid IID_INetCfgClass = new Guid("C0E8AE97-306E-11D1-AACF-00805FC1270E");
//    }

//    [Guid("C0E8AE93-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface INetCfg
//    {
//        [PreserveSig]
//        int Initialize(IntPtr pvReserved);

//        [PreserveSig]
//        int Uninitialize();

//        [PreserveSig]
//        int Apply();

//        [PreserveSig]
//        int Cancel();

//        [PreserveSig]
//        int EnumComponents(ref Guid pguidClass,
//            [Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*IEnumNetCfgComponent*/object ppenumComponent);

//        [PreserveSig]
//        int FindComponent([In, MarshalAs(UnmanagedType.LPWStr)]  string pszwInfId,
//            [Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*INetCfgComponent*/ object pComponent);

//        [PreserveSig]
//        int QueryNetCfgClass([In]ref Guid pguidClass,
//            ref Guid riid,
//            [Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);
//    };

//    [Guid("C0E8AE9F-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface INetCfgLock
//    {
//        [PreserveSig]
//        int AcquireWriteLock([In, MarshalAs(UnmanagedType.U4)] uint cmsTimeout,
//            [In, MarshalAs(UnmanagedType.LPWStr)] string pszwClientDescription,
//            [Out, Optional, MarshalAs(UnmanagedType.LPWStr)] string ppszwClientDescription);

//        [PreserveSig]
//        int ReleaseWriteLock();

//        [PreserveSig]
//        int IsWriteLocked([Out, MarshalAs(UnmanagedType.LPWStr)] string ppszwClientDescription);
//    };

//    [Guid("C0E8AE97-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface INetCfgClass
//    {
//        [PreserveSig]
//        int FindComponent([In, MarshalAs(UnmanagedType.LPWStr)] string pszwInfId,
//            [Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*INetCfgComponent*/object ppnccItem);

//        [PreserveSig]
//        int EnumComponents([Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*IEnumNetCfgComponent*/object ppenumComponent);
//    };

//    [Guid("C0E8AE9D-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface INetCfgClassSetup
//    {
//        [Obsolete]
//        [PreserveSig]
//        int SelectAndInstall([In] IntPtr hwndParent,
//            [In, Optional] /*OBO_TOKEN*/IntPtr pOboToken,
//            [Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*INetCfgComponent***/object ppnccItem);

//        [PreserveSig]
//        int Install([In, MarshalAs(UnmanagedType.LPWStr)] string pszwInfId,
//            [In, Optional] /*OBO_TOKEN*/IntPtr pOboToken,
//            [In, Optional, MarshalAs(UnmanagedType.U4)] int dwSetupFlags,
//            [In, Optional, MarshalAs(UnmanagedType.U4)] int dwUpgradeFromBuildNo,
//            [In, Optional, MarshalAs(UnmanagedType.LPWStr)] string pszwAnswerFile,
//            [In, Optional, MarshalAs(UnmanagedType.LPWStr)] string pszwAnswerSections,
//            [Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*INetCfgComponent***/ object ppnccItem);

//        [PreserveSig]
//        int DeInstall([Out, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgComponent**/object pComponent,
//            [In, Optional] /*OBO_TOKEN*/IntPtr pOboToken,
//            [Out, Optional, MarshalAs(UnmanagedType.LPWStr)] out string pmszwRefs);
//    };

//    [Guid("C0E8AE96-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface INetCfgBindingPath
//    {
//        [PreserveSig]
//        int IsSamePathAs([In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgBindingPath*/ object pPath);

//        [PreserveSig]
//        int IsSubPathOf([In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgBindingPath*/ object pPath);

//        [PreserveSig]
//        int IsEnabled();

//        [PreserveSig]
//        int Enable([MarshalAs(UnmanagedType.Bool)] bool fEnable);

//        [PreserveSig]
//        int GetPathToken([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszwPathToken);

//        [PreserveSig]
//        int GetOwner([Out, MarshalAs(UnmanagedType.IUnknown)] out /*INetCfgBindingPath*/ object ppComponent);

//        [PreserveSig]
//        int GetDepth([Out, MarshalAs(UnmanagedType.U4)] out uint pcInterfaces);

//        [PreserveSig]
//        int EnumBindingInterfaces([Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*IEnumNetCfgBindingInterface*/object ppenumInterface);
//    };

//    [Guid("C0E8AE94-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface INetCfgBindingInterface
//    {
//        [PreserveSig]
//        int GetName([Out, MarshalAs(UnmanagedType.LPWStr)] string ppszwInterfaceName);

//        [PreserveSig]
//        int GetUpperComponent([Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*INetCfgComponent*/object ppnccItem);

//        [PreserveSig]
//        int GetLowerComponent([Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*INetCfgComponent*/object ppnccItem);
//    };

//    [Guid("C0E8AE99-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface INetCfgComponent
//    {
//        [PreserveSig]
//        int GetDisplayName([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszwDisplayName);

//        [PreserveSig]
//        int SetDisplayName([In, MarshalAs(UnmanagedType.LPWStr)] string pszwDisplayName);

//        [PreserveSig]
//        int GetHelpText([Out, MarshalAs(UnmanagedType.LPWStr)] out string pszwHelpText);

//        [PreserveSig]
//        int GetId([Out, MarshalAs(UnmanagedType.LPWStr)]out string ppszwId);

//        [PreserveSig]
//        int GetCharacteristics([Out, MarshalAs(UnmanagedType.U4)] out uint pdwCharacteristics);

//        [PreserveSig]
//        int GetInstanceGuid([Out] Guid pGuid);

//        [PreserveSig]
//        int GetPnpDevNodeId([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszwDevNodeId);

//        [PreserveSig]
//        int GetClassGuid([Out]  Guid pGuid);

//        [PreserveSig]
//        int GetBindName([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszwBindName);

//        [PreserveSig]
//        int GetDeviceStatus([Out, MarshalAs(UnmanagedType.U4)] out int pulStatus);

//        [PreserveSig]
//        int OpenParamKey([Out, Optional, MarshalAs(UnmanagedType.SysUInt)] out UIntPtr phkey);

//        [PreserveSig]
//        int RaisePropertyUi([In, Optional] IntPtr hwndParent,
//            [In, MarshalAs(UnmanagedType.U4)] int dwFlags, /* NCRP_FLAGS */
//            [In, Optional, MarshalAs(UnmanagedType.IUnknown)] object punkContext);
//    };

//    [Guid("C0E8AE9E-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface INetCfgComponentBindings
//    {
//        [PreserveSig]
//        int BindTo([In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgComponent**/object pnccItem);

//        [PreserveSig]
//        int UnbindFrom([In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgComponent**/object pnccItem);

//        [PreserveSig]
//        int SupportsBindingInterface([In, MarshalAs(UnmanagedType.U4)] uint dwFlags,
//                                    [In, MarshalAs(UnmanagedType.LPWStr)] string pszwInterfaceName);

//        [PreserveSig]
//        int IsBoundTo(
//        [In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgComponent**/object pnccItem);

//        [PreserveSig]
//        int IsBindableTo(
//        [In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgComponent**/object pnccItem);

//        [PreserveSig]
//        int EnumBindingPaths([In, MarshalAs(UnmanagedType.U4)] int dwFlags,
//                            [Out, Optional, MarshalAs(UnmanagedType.IUnknown)] out /*IEnumNetCfgBindingPath***/object ppIEnum);

//        [Obsolete]
//        [PreserveSig]
//        int MoveBefore([In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgBindingPath**/ object pncbItemSrc,
//                    [In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgBindingPath**/ object pncbItemDest);

//        [Obsolete]
//        [PreserveSig]
//        int MoveAfter([In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgBindingPath**/ object pncbItemSrc,
//                    [In, MarshalAs(UnmanagedType.IUnknown)] /*INetCfgBindingPath**/ object pncbItemDest);
//    };

//    //[Guid("C0E8AE98-306E-11D1-AACF-00805FC1270E"),
//    //InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    //[ComVisible(true)]
//    //interface INetCfgSysPrep
//    //{
//    //    int HrSetupSetFirstDword(
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pwszSection,
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pwszKey,
//    //    [In, MarshalAs(UnmanagedType.U4)] int dwValue);

//    //    int HrSetupSetFirstString(
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pwszSection,
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pwszKey,
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pwszValue);

//    //    int HrSetupSetFirstStringAsBool(
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pwszSection,
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pwszKey,
//    //    [MarshalAs(UnmanagedType.Bool)] bool fValue);

//    //    int HrSetupSetFirstMultiSzField(
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pwszSection,
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pwszKey,
//    //    [In, MarshalAs(UnmanagedType.LPWStr)] string pmszValue);
//    //};

//    //Enums
//    [Guid("C0E8AE92-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface IEnumNetCfgComponent
//    {
//        [PreserveSig]
//        int Next([In, MarshalAs(UnmanagedType.U4)] uint celt,
//            [Out, MarshalAs(UnmanagedType.IUnknown)] out object rgelt,
//            [Out, MarshalAs(UnmanagedType.U4)] out uint pceltFetched);

//        [PreserveSig]
//        int Skip([In, MarshalAs(UnmanagedType.U4)] uint celt);

//        [PreserveSig]
//        int Reset();

//        [PreserveSig]
//        void Clone([Out, MarshalAs(UnmanagedType.IUnknown)] out /*IEnumNetCfgComponent*/object ppenum);
//    };

//    [Guid("C0E8AE90-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    public interface IEnumNetCfgBindingInterface
//    {
//        [PreserveSig]
//        int Next([In, MarshalAs(UnmanagedType.U4)] uint celt,
//            [Out, MarshalAs(UnmanagedType.IUnknown)] out object rgelt,
//            [Out, Optional, MarshalAs(UnmanagedType.U4)] out uint pceltFetched);

//        [PreserveSig]
//        int Skip([In, MarshalAs(UnmanagedType.U4)] uint celt);

//        [PreserveSig]
//        int Reset();

//        [PreserveSig]
//        void Clone([Out, MarshalAs(UnmanagedType.IUnknown)] out /*IEnumNetCfgBindingInterface*/object ppenum);
//    }

//    [Guid("C0E8AE91-306E-11D1-AACF-00805FC1270E"),
//    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
//    [ComVisible(true)]
//    interface IEnumNetCfgBindingPath
//    {
//        [PreserveSig]
//        void Next([In, MarshalAs(UnmanagedType.U4)] uint celt,
//            [Out, MarshalAs(UnmanagedType.IUnknown)] out object rgelt,
//            [Out, MarshalAs(UnmanagedType.U4)] out uint pceltFetched);

//        [PreserveSig]
//        void Skip([In, MarshalAs(UnmanagedType.U4)] int celt);

//        [PreserveSig]
//        void Reset();

//        [PreserveSig]
//        void Clone([Out, MarshalAs(UnmanagedType.IUnknown)] out /*IEnumNetCfgBindingPath*/object ppenum);
//    }

//    //enum COMPONENT_CHARACTERISTICS
//    //{
//    //    NCF_VIRTUAL = 0x00000001,
//    //    NCF_SOFTWARE_ENUMERATED = 0x00000002,
//    //    NCF_PHYSICAL = 0x00000004,
//    //    NCF_HIDDEN = 0x00000008,
//    //    NCF_NO_SERVICE = 0x00000010,
//    //    NCF_NOT_USER_REMOVABLE = 0x00000020,
//    //    NCF_MULTIPORT_INSTANCED_ADAPTER = 0x00000040, // This adapter has separate instances for each port 
//    //    NCF_HAS_UI = 0x00000080,
//    //    NCF_SINGLE_INSTANCE = 0x00000100,
//    //    // = 0x00000200, // filter device 
//    //    NCF_FILTER = 0x00000400, // filter component 
//    //    NCF_DONTEXPOSELOWER = 0x00001000,
//    //    NCF_HIDE_BINDING = 0x00002000, // don't show in binding page 
//    //    NCF_NDIS_PROTOCOL = 0x00004000, // Needs UNLOAD notifications 
//    //                                    // = 0x00008000, 
//    //                                    // = 0x00010000, // pnp notifications forced through service controller 
//    //    NCF_FIXED_BINDING = 0x00020000 // UI ability to change binding is disabled 
//    //}

//    //enum NCRP_FLAGS
//    //{
//    //    NCRP_QUERY_PROPERTY_UI = 0x00000001,
//    //    NCRP_SHOW_PROPERTY_UI = 0x00000002
//    //}

//    enum OBO_TOKEN_TYPE
//    {
//        OBO_USER = 1,
//        OBO_COMPONENT = 2,
//        OBO_SOFTWARE = 3,
//    };

//    struct OBO_TOKEN
//    {
//        [MarshalAs(UnmanagedType.I4)]
//        OBO_TOKEN_TYPE Type;

//        // Type == OBO_COMPONENT 
//        // 
//        [MarshalAs(UnmanagedType.IUnknown)]
//        /*INetCfgComponent**/
//        object pncc;

//        // Type == OBO_SOFTWARE 
//        // 
//        [MarshalAs(UnmanagedType.LPWStr)]
//        string pszwManufacturer;
//        [MarshalAs(UnmanagedType.LPWStr)]
//        string pszwProduct;
//        [MarshalAs(UnmanagedType.LPWStr)]
//        string pszwDisplayName;

//        // The following fields must be initialized to zero 
//        // by users of OBO_TOKEN. 
//        // 
//        [MarshalAs(UnmanagedType.Bool)]
//        bool fRegistered;
//    };

//    //enum SUPPORTS_BINDING_INTERFACE_FLAGS
//    //{
//    //    NCF_LOWER = 0x01,
//    //    NCF_UPPER = 0x02
//    //};

//    //enum ENUM_BINDING_PATHS_FLAGS
//    //{
//    //    EBP_ABOVE = 0x01,
//    //    EBP_BELOW = 0x02,
//    //};
//}
