using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using PSE;

namespace CLRDEV9
{
    static class Config
    {
        public static void SaveConf()
        {
            //RegistryKey myKey = Registry.CurrentUser.CreateSubKey("Software\\PS2Eplugin\\DEV9\\DEV9linuz");
            //myKey.SetValue("Eth", DEV9Header.config.Eth);
            //myKey.SetValue("Hdd", DEV9Header.config.Hdd);
            //myKey.SetValue("HddSize", DEV9Header.config.HddSize);
            //myKey.SetValue("ethEnable", DEV9Header.config.ethEnable);
            //myKey.SetValue("hddEnable", DEV9Header.config.hddEnable);
            //myKey.Close();
        }

        public static void LoadConf()
        {
            DEV9Header.config = new DEV9Header.Config();
            DEV9Header.config.Hdd = DEV9Header.HDD_DEF;
            DEV9Header.config.HddSize = 8 * 1024;
            DEV9Header.config.Eth = DEV9Header.ETH_DEF;

            //RegistryKey myKey = Registry.CurrentUser.OpenSubKey("Software\\PS2Eplugin\\DEV9\\DEV9linuz");
            //if (myKey == null)
            //{
            //    SaveConf(); return;
            //}
            //DEV9Header.config.Eth = (string)myKey.GetValue("Eth", DEV9Header.config.Eth);
            DEV9Header.config.Eth = "winsock";
            //DEV9Header.config.Hdd = (string)myKey.GetValue("Hdd", DEV9Header.config.Hdd);
            //DEV9Header.config.HddSize = (int)myKey.GetValue("HddSize", DEV9Header.config.HddSize);
            //DEV9Header.config.ethEnable = (int)myKey.GetValue("ethEnable", DEV9Header.config.ethEnable);
            DEV9Header.config.ethEnable = 1;
            //DEV9Header.config.hddEnable = (int)myKey.GetValue("hddEnable", DEV9Header.config.hddEnable);
            DEV9Header.config.hddEnable = 1;
            //myKey.Close();
        }
    }
}
