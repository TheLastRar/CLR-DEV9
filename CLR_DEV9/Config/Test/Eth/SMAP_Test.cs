//using CLRDEV9.DEV9.SMAP;
//using System;
//using System.Collections.Generic;
//using System.Collections.Concurrent;
//using System.Linq;
//using System.Text;
//using CLRDEV9.DEV9;
//using CLRDEV9.DEV9.SMAP.Data;
//using System.Threading;

//namespace CLRDEV9.Config.Test
//{
//    class SMAP_Test : SMAP_State
//    {
//        public SMAP_Test() : base(new DEV9_Test()) { }

//        public AutoResetEvent ReceivedEvent = new AutoResetEvent(false);
//        public ConcurrentQueue<NetPacket> ReceivedPackets = new ConcurrentQueue<NetPacket>();

//        public override bool RxFifoCanRx()
//        {
//            return true;
//        }
//        public override void RxProcess(ref NetPacket pk)
//        {
//            ReceivedPackets.Enqueue(pk);
//            ReceivedEvent.Set();
//        }

//        public void TxProcess(ref NetPacket pk)
//        {
//            adapter.net.TxPut(ref pk);
//        }

//        public byte[] GetHWAddress()
//        {
//            return adapter.net.GetPS2HWAddress();
//        }
//    }
//}
