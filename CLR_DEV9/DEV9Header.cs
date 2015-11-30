using System;

namespace CLRDEV9
{
    static class DEV9Header
    {
        public const string ETH_DEF = "winsock";//"eth0";
        public const string HDD_DEF = "DEV9hdd.raw";

        public struct Config
        {
            public string Eth;
            public string Hdd;
            public int HddSize;

            //public int hddEnable;
            public int ethEnable;
        }

        public static Config config;

        public static PSE.CLR_PSE_Callbacks.CLR_CyclesCallback DEV9irq;

        //EEPROM states
        public const byte EEPROM_READY = 0;
        public const byte EEPROM_OPCD0 = 1; //waiting for first bit of opcode
        public const byte EEPROM_OPCD1 = 2; //waiting for second bit of opcode
        public const byte EEPROM_ADDR0 = 3;	//waiting for address bits
        public const byte EEPROM_ADDR1 = 4;
        public const byte EEPROM_ADDR2 = 5;
        public const byte EEPROM_ADDR3 = 6;
        public const byte EEPROM_ADDR4 = 7;
        public const byte EEPROM_ADDR5 = 8;
        public const byte EEPROM_TDATA = 9;	//ready to send/receive data

        public const uint DEV9_R_REV = 0x1f80146e;

        /*
         * SPEED (ASIC on SMAP) register definitions.
         */

        public const uint SPD_REGBASE = 0x10000000;

        public const uint SPD_R_REV = (SPD_REGBASE + 0x00);
        public const uint SPD_R_REV_1 = (SPD_REGBASE + 0x02);
        // bit 0: smap
        // bit 1: hdd
        // bit 5: flash
        public const uint SPD_R_REV_3 = (SPD_REGBASE + 0x04);
        public const uint SPD_R_0e = (SPD_REGBASE + 0x0e);

        public const uint SPD_R_DMA_CTRL = (SPD_REGBASE + 0x24);
        public const uint SPD_R_INTR_STAT = (SPD_REGBASE + 0x28);
        public const uint SPD_R_INTR_MASK = (SPD_REGBASE + 0x2a);
        public const uint SPD_R_PIO_DIR = (SPD_REGBASE + 0x2c);
        public const uint SPD_R_PIO_DATA = (SPD_REGBASE + 0x2e);
        public const uint SPD_PP_DOUT = (1 << 4);	/* Data output, read port */
        public const uint SPD_PP_DIN = (1 << 5);	/* Data input,  write port */
        public const uint SPD_PP_SCLK = (1 << 6);	/* Clock,       write port */
        public const uint SPD_PP_CSEL = (1 << 7);	/* Chip select, write port */
        /* Operation codes */
        public const uint SPD_PP_OP_READ = 2;
        public const uint SPD_PP_OP_WRITE = 1;
        public const uint SPD_PP_OP_EWEN = 0;
        public const uint SPD_PP_OP_EWDS = 0;

        public const uint SPD_R_XFR_CTRL = (SPD_REGBASE + 0x32);
        public const uint SPD_R_38 = (SPD_REGBASE + 0x38);
        public const uint SPD_R_IF_CTRL = (SPD_REGBASE + 0x64);
        public const uint SPD_IF_ATA_RESET = 0x80;
        public const uint SPD_IF_DMA_ENABLE = 0x04;
        public const uint SPD_R_PIO_MODE = (SPD_REGBASE + 0x70);
        public const uint SPD_R_MWDMA_MODE = (SPD_REGBASE + 0x72);
        public const uint SPD_R_UDMA_MODE = (SPD_REGBASE + 0x74);

        /*
         * SMAP (PS2 Network Adapter) register definitions.
         */

        /* SMAP interrupt status bits (selected from the SPEED device).  */
        public const int SMAP_INTR_EMAC3 = (1 << 6);
        public const int SMAP_INTR_RXEND = (1 << 5);
        public const int SMAP_INTR_TXEND = (1 << 4);
        public const int SMAP_INTR_RXDNV = (1 << 3);	/* descriptor not valid */
        public const int SMAP_INTR_TXDNV = (1 << 2);	/* descriptor not valid */
        public const int SMAP_INTR_CLR_ALL = (SMAP_INTR_RXEND | SMAP_INTR_TXEND | SMAP_INTR_RXDNV);
        public const int SMAP_INTR_ENA_ALL = (SMAP_INTR_EMAC3 | SMAP_INTR_CLR_ALL);
        //public const int SMAP_INTR_BITMSK = 0x7C;

        /* SMAP Register Definitions.  */
        public const uint SMAP_REGBASE = (SPD_REGBASE + 0x100);

        public const uint SMAP_R_BD_MODE = (SMAP_REGBASE + 0x02);
        public const uint SMAP_BD_SWAP = (1 << 0);

        public const uint SMAP_R_INTR_CLR = (SMAP_REGBASE + 0x28);

        /* SMAP FIFO Registers.  */
        public const uint SMAP_R_TXFIFO_CTRL = (SMAP_REGBASE + 0xf00);
        public const uint SMAP_TXFIFO_RESET = (1 << 0);
        public const uint SMAP_TXFIFO_DMAEN = (1 << 1);
        public const uint SMAP_R_TXFIFO_WR_PTR = (SMAP_REGBASE + 0xf04);
        public const uint SMAP_R_TXFIFO_SIZE = (SMAP_REGBASE + 0xf08);
        public const uint SMAP_R_TXFIFO_FRAME_CNT = (SMAP_REGBASE + 0xf0C);
        public const uint SMAP_R_TXFIFO_FRAME_INC = (SMAP_REGBASE + 0xf10);
        public const uint SMAP_R_TXFIFO_DATA = (SMAP_REGBASE + 0x1000);

        public const uint SMAP_R_RXFIFO_CTRL = (SMAP_REGBASE + 0xf30);
        public const uint SMAP_RXFIFO_RESET = (1 << 0);
        public const uint SMAP_RXFIFO_DMAEN = (1 << 1);
        public const uint SMAP_R_RXFIFO_RD_PTR = (SMAP_REGBASE + 0xf34);
        public const uint SMAP_R_RXFIFO_SIZE = (SMAP_REGBASE + 0xf38);
        public const uint SMAP_R_RXFIFO_FRAME_CNT = (SMAP_REGBASE + 0xf3C);
        public const uint SMAP_R_RXFIFO_FRAME_DEC = (SMAP_REGBASE + 0xf40);
        public const uint SMAP_R_RXFIFO_DATA = (SMAP_REGBASE + 0x1100);

        /* EMAC3 Registers.  */
        public const uint SMAP_EMAC3_REGBASE = (SMAP_REGBASE + 0x1f00);

        public const uint SMAP_R_EMAC3_MODE0_L = (SMAP_EMAC3_REGBASE + 0x00);
        public const uint SMAP_E3_RXMAC_IDLE = unchecked((uint)(1 << (15 + 16)));
        public const uint SMAP_E3_TXMAC_IDLE = (1 << (14 + 16));
        public const uint SMAP_E3_SOFT_RESET = (1 << (13 + 16));
        public const uint SMAP_E3_TXMAC_ENABLE = (1 << (12 + 16));
        public const uint SMAP_E3_RXMAC_ENABLE = (1 << (11 + 16));
        public const uint SMAP_E3_WAKEUP_ENABLE = (1 << (10 + 16));
        public const uint SMAP_R_EMAC3_MODE0_H = (SMAP_EMAC3_REGBASE + 0x02);

        public const uint SMAP_R_EMAC3_MODE1 = (SMAP_EMAC3_REGBASE + 0x04);
        public const uint SMAP_R_EMAC3_MODE1_L = (SMAP_EMAC3_REGBASE + 0x04);
        public const uint SMAP_R_EMAC3_MODE1_H = (SMAP_EMAC3_REGBASE + 0x06);
        public const uint SMAP_E3_FDX_ENABLE = unchecked((uint)(1 << 31));
        public const uint SMAP_E3_INLPBK_ENABLE = (1 << 30);	/* internal loop back */
        public const uint SMAP_E3_VLAN_ENABLE = (1 << 29);
        public const uint SMAP_E3_FLOWCTRL_ENABLE = (1 << 28);  /* integrated flow ctrl(pause frame) */
        public const uint SMAP_E3_ALLOW_PF = (1 << 27);         /* allow pause frame */
        public const uint SMAP_E3_ALLOW_EXTMNGIF = (1 << 25);   /* allow external management IF */
        public const uint SMAP_E3_IGNORE_SQE = (1 << 24);
        public const uint SMAP_E3_MEDIA_FREQ_BITSFT = (22);
        public const uint SMAP_E3_MEDIA_10M = (0 << 22);
        public const uint SMAP_E3_MEDIA_100M = (1 << 22);
        public const uint SMAP_E3_MEDIA_1000M = (2 << 22);
        public const uint SMAP_E3_MEDIA_MSK = (3 << 22);
        public const uint SMAP_E3_RXFIFO_SIZE_BITSFT = (20);
        public const uint SMAP_E3_RXFIFO_512 = (0 << 20);
        public const uint SMAP_E3_RXFIFO_1K = (1 << 20);
        public const uint SMAP_E3_RXFIFO_2K = (2 << 20);
        public const uint SMAP_E3_RXFIFO_4K = (3 << 20);
        public const uint SMAP_E3_TXFIFO_SIZE_BITSFT = (18);
        public const uint SMAP_E3_TXFIFO_512 = (0 << 18);
        public const uint SMAP_E3_TXFIFO_1K = (1 << 18);
        public const uint SMAP_E3_TXFIFO_2K = (2 << 18);
        public const uint SMAP_E3_TXREQ0_BITSFT = (15);
        public const uint SMAP_E3_TXREQ0_SINGLE = (0 << 15);
        public const uint SMAP_E3_TXREQ0_MULTI = (1 << 15);
        public const uint SMAP_E3_TXREQ0_DEPEND = (2 << 15);
        public const uint SMAP_E3_TXREQ1_BITSFT = (13);
        public const uint SMAP_E3_TXREQ1_SINGLE = (0 << 13);
        public const uint SMAP_E3_TXREQ1_MULTI = (1 << 13);
        public const uint SMAP_E3_TXREQ1_DEPEND = (2 << 13);
        public const uint SMAP_E3_JUMBO_ENABLE = (1 << 12);

        public const uint SMAP_R_EMAC3_TxMODE0_L = (SMAP_EMAC3_REGBASE + 0x08);
        public const uint SMAP_E3_TX_GNP_0 = unchecked((uint)(1 << (15 + 16)));/* get new packet */
        public const uint SMAP_E3_TX_GNP_1 = (1 << (14 + 16));	    /* get new packet */
        public const uint SMAP_E3_TX_GNP_DEPEND = (1 << (13 + 16));	/* get new packet */
        public const uint SMAP_E3_TX_FIRST_CHANNEL = (1 << (12 + 16));
        public const uint SMAP_R_EMAC3_TxMODE0_H = (SMAP_EMAC3_REGBASE + 0x0A);

        public const uint SMAP_R_EMAC3_TxMODE1_L = (SMAP_EMAC3_REGBASE + 0x0C);
        public const uint SMAP_R_EMAC3_TxMODE1_H = (SMAP_EMAC3_REGBASE + 0x0E);
        public const uint SMAP_E3_TX_LOW_REQ_MSK = (0x1F);	/* low priority request */
        public const uint SMAP_E3_TX_LOW_REQ_BITSFT = (27);	/* low priority request */
        public const uint SMAP_E3_TX_URG_REQ_MSK = (0xFF);	/* urgent priority request */
        public const uint SMAP_E3_TX_URG_REQ_BITSFT = (16);	/* urgent priority request */

        public const uint SMAP_R_EMAC3_RxMODE = (SMAP_EMAC3_REGBASE + 0x10);
        public const uint SMAP_R_EMAC3_RxMODE_L = (SMAP_EMAC3_REGBASE + 0x10);
        public const uint SMAP_R_EMAC3_RxMODE_H = (SMAP_EMAC3_REGBASE + 0x12);
        public const uint SMAP_E3_RX_STRIP_PAD = unchecked((uint)(1 << 31));
        public const uint SMAP_E3_RX_STRIP_FCS = (1 << 30);
        public const uint SMAP_E3_RX_RX_RUNT_FRAME = (1 << 29);
        public const uint SMAP_E3_RX_RX_FCS_ERR = (1 << 28);
        public const uint SMAP_E3_RX_RX_TOO_LONG_ERR = (1 << 27);
        public const uint SMAP_E3_RX_RX_IN_RANGE_ERR = (1 << 26);
        public const uint SMAP_E3_RX_PROP_PF = (1 << 25);/* propagate pause frame */
        public const uint SMAP_E3_RX_PROMISC = (1 << 24);
        public const uint SMAP_E3_RX_PROMISC_MCAST = (1 << 23);
        public const uint SMAP_E3_RX_INDIVID_ADDR = (1 << 22);
        public const uint SMAP_E3_RX_INDIVID_HASH = (1 << 21);
        public const uint SMAP_E3_RX_BCAST = (1 << 20);
        public const uint SMAP_E3_RX_MCAST = (1 << 19);

        public const uint SMAP_R_EMAC3_INTR_STAT = (SMAP_EMAC3_REGBASE + 0x14);
        public const uint SMAP_R_EMAC3_INTR_STAT_L = (SMAP_EMAC3_REGBASE + 0x14);
        public const uint SMAP_R_EMAC3_INTR_STAT_H = (SMAP_EMAC3_REGBASE + 0x16);
        public const uint SMAP_R_EMAC3_INTR_ENABLE = (SMAP_EMAC3_REGBASE + 0x18);
        public const uint SMAP_R_EMAC3_INTR_ENABLE_L = (SMAP_EMAC3_REGBASE + 0x18);
        public const uint SMAP_R_EMAC3_INTR_ENABLE_H = (SMAP_EMAC3_REGBASE + 0x1A);
        public const uint SMAP_E3_INTR_OVERRUN = (1 << 25);/* this bit does NOT WORKED */
        public const uint SMAP_E3_INTR_PF = (1 << 24);
        public const uint SMAP_E3_INTR_BAD_FRAME = (1 << 23);
        public const uint SMAP_E3_INTR_RUNT_FRAME = (1 << 22);
        public const uint SMAP_E3_INTR_SHORT_EVENT = (1 << 21);
        public const uint SMAP_E3_INTR_ALIGN_ERR = (1 << 20);
        public const uint SMAP_E3_INTR_BAD_FCS = (1 << 19);
        public const uint SMAP_E3_INTR_TOO_LONG = (1 << 18);
        public const uint SMAP_E3_INTR_OUT_RANGE_ERR = (1 << 17);
        public const uint SMAP_E3_INTR_IN_RANGE_ERR = (1 << 16);
        public const uint SMAP_E3_INTR_DEAD_DEPEND = (1 << 9);
        public const uint SMAP_E3_INTR_DEAD_0 = (1 << 8);
        public const uint SMAP_E3_INTR_SQE_ERR_0 = (1 << 7);
        public const uint SMAP_E3_INTR_TX_ERR_0 = (1 << 6);
        public const uint SMAP_E3_INTR_DEAD_1 = (1 << 5);
        public const uint SMAP_E3_INTR_SQE_ERR_1 = (1 << 4);
        public const uint SMAP_E3_INTR_TX_ERR_1 = (1 << 3);
        public const uint SMAP_E3_INTR_MMAOP_SUCCESS = (1 << 1);
        public const uint SMAP_E3_INTR_MMAOP_FAIL = (1 << 0);
        public const uint SMAP_E3_INTR_ALL =
            (SMAP_E3_INTR_OVERRUN | SMAP_E3_INTR_PF | SMAP_E3_INTR_BAD_FRAME |
             SMAP_E3_INTR_RUNT_FRAME | SMAP_E3_INTR_SHORT_EVENT |
             SMAP_E3_INTR_ALIGN_ERR | SMAP_E3_INTR_BAD_FCS |
             SMAP_E3_INTR_TOO_LONG | SMAP_E3_INTR_OUT_RANGE_ERR |
             SMAP_E3_INTR_IN_RANGE_ERR |
             SMAP_E3_INTR_DEAD_DEPEND | SMAP_E3_INTR_DEAD_0 |
             SMAP_E3_INTR_SQE_ERR_0 | SMAP_E3_INTR_TX_ERR_0 |
             SMAP_E3_INTR_DEAD_1 | SMAP_E3_INTR_SQE_ERR_1 |
             SMAP_E3_INTR_TX_ERR_1 |
             SMAP_E3_INTR_MMAOP_SUCCESS | SMAP_E3_INTR_MMAOP_FAIL);
        public const uint SMAP_E3_DEAD_ALL =
            (SMAP_E3_INTR_DEAD_DEPEND | SMAP_E3_INTR_DEAD_0 |
             SMAP_E3_INTR_DEAD_1);

        public const uint SMAP_R_EMAC3_ADDR_HI = (SMAP_EMAC3_REGBASE + 0x1C);
        public const uint SMAP_R_EMAC3_ADDR_LO = (SMAP_EMAC3_REGBASE + 0x20);
        public const uint SMAP_R_EMAC3_ADDR_HI_L = (SMAP_EMAC3_REGBASE + 0x1C);
        public const uint SMAP_R_EMAC3_ADDR_HI_H = (SMAP_EMAC3_REGBASE + 0x1E);
        public const uint SMAP_R_EMAC3_ADDR_LO_L = (SMAP_EMAC3_REGBASE + 0x20);
        public const uint SMAP_R_EMAC3_ADDR_LO_H = (SMAP_EMAC3_REGBASE + 0x22);

        public const uint SMAP_R_EMAC3_VLAN_TPID = (SMAP_EMAC3_REGBASE + 0x24);
        public const uint SMAP_E3_VLAN_ID_MSK = 0xFFFF;

        public const uint SMAP_R_EMAC3_PAUSE_TIMER = (SMAP_EMAC3_REGBASE + 0x2C);
        public const uint SMAP_R_EMAC3_PAUSE_TIMER_L = (SMAP_EMAC3_REGBASE + 0x2C);
        public const uint SMAP_R_EMAC3_PAUSE_TIMER_H = (SMAP_EMAC3_REGBASE + 0x2E);
        public const uint SMAP_E3_PTIMER_MSK = 0xFFFF;

        public const uint SMAP_R_EMAC3_INDIVID_HASH1 = (SMAP_EMAC3_REGBASE + 0x30);
        public const uint SMAP_R_EMAC3_INDIVID_HASH2 = (SMAP_EMAC3_REGBASE + 0x34);
        public const uint SMAP_R_EMAC3_INDIVID_HASH3 = (SMAP_EMAC3_REGBASE + 0x38);
        public const uint SMAP_R_EMAC3_INDIVID_HASH4 = (SMAP_EMAC3_REGBASE + 0x3C);
        public const uint SMAP_R_EMAC3_GROUP_HASH1 = (SMAP_EMAC3_REGBASE + 0x40);
        public const uint SMAP_R_EMAC3_GROUP_HASH2 = (SMAP_EMAC3_REGBASE + 0x44);
        public const uint SMAP_R_EMAC3_GROUP_HASH3 = (SMAP_EMAC3_REGBASE + 0x48);
        public const uint SMAP_R_EMAC3_GROUP_HASH4 = (SMAP_EMAC3_REGBASE + 0x4C);
        public const uint SMAP_E3_HASH_MSK = 0xFFFF;

        public const uint SMAP_R_EMAC3_LAST_SA_HI = (SMAP_EMAC3_REGBASE + 0x50);
        public const uint SMAP_R_EMAC3_LAST_SA_LO = (SMAP_EMAC3_REGBASE + 0x54);

        public const uint SMAP_R_EMAC3_INTER_FRAME_GAP = (SMAP_EMAC3_REGBASE + 0x58);
        public const uint SMAP_R_EMAC3_INTER_FRAME_GAP_L = (SMAP_EMAC3_REGBASE + 0x58);
        public const uint SMAP_R_EMAC3_INTER_FRAME_GAP_H = (SMAP_EMAC3_REGBASE + 0x5A);
        public const uint SMAP_E3_IFGAP_MSK = 0x3F;

        public const uint SMAP_R_EMAC3_STA_CTRL_L = (SMAP_EMAC3_REGBASE + 0x5C);
        public const uint SMAP_R_EMAC3_STA_CTRL_H = (SMAP_EMAC3_REGBASE + 0x5E);
        public const uint SMAP_E3_PHY_DATA_MSK = (0xFFFF);
        public const uint SMAP_E3_PHY_DATA_BITSFT = (16);
        public const uint SMAP_E3_PHY_OP_COMP = (1 << 15);/* operation complete */
        public const uint SMAP_E3_PHY_ERR_READ = (1 << 14);
        public const uint SMAP_E3_PHY_STA_CMD_BITSFT = (12);
        public const uint SMAP_E3_PHY_READ = (1 << 12);
        public const uint SMAP_E3_PHY_WRITE = (2 << 12);
        public const uint SMAP_E3_PHY_OPBCLCK_BITSFT = (10);
        public const uint SMAP_E3_PHY_50M = (0 << 10);
        public const uint SMAP_E3_PHY_66M = (1 << 10);
        public const uint SMAP_E3_PHY_83M = (2 << 10);
        public const uint SMAP_E3_PHY_100M = (3 << 10);
        public const uint SMAP_E3_PHY_ADDR_MSK = (0x1F);
        public const uint SMAP_E3_PHY_ADDR_BITSFT = (5);
        public const uint SMAP_E3_PHY_REG_ADDR_MSK = (0x1F);

        public const uint SMAP_R_EMAC3_TX_THRESHOLD = (SMAP_EMAC3_REGBASE + 0x60);
        public const uint SMAP_R_EMAC3_TX_THRESHOLD_L = (SMAP_EMAC3_REGBASE + 0x60);
        public const uint SMAP_R_EMAC3_TX_THRESHOLD_H = (SMAP_EMAC3_REGBASE + 0x62);
        public const uint SMAP_E3_TX_THRESHLD_MSK = (0x1F);
        public const uint SMAP_E3_TX_THRESHLD_BITSFT = (27);

        public const uint SMAP_R_EMAC3_RX_WATERMARK = (SMAP_EMAC3_REGBASE + 0x64);
        public const uint SMAP_R_EMAC3_RX_WATERMARK_L = (SMAP_EMAC3_REGBASE + 0x64);
        public const uint SMAP_R_EMAC3_RX_WATERMARK_H = (SMAP_EMAC3_REGBASE + 0x66);
        public const uint SMAP_E3_RX_LO_WATER_MSK = (0x1FF);
        public const uint SMAP_E3_RX_LO_WATER_BITSFT = (23);
        public const uint SMAP_E3_RX_HI_WATER_MSK = (0x1FF);
        public const uint SMAP_E3_RX_HI_WATER_BITSFT = (7);

        public const uint SMAP_R_EMAC3_TX_OCTETS = (SMAP_EMAC3_REGBASE + 0x68);
        public const uint SMAP_R_EMAC3_RX_OCTETS = (SMAP_EMAC3_REGBASE + 0x6C);
        public const uint SMAP_EMAC3_REGEND = (SMAP_EMAC3_REGBASE + 0x6C + 4);

        /* Buffer descriptors.  */
        public const uint SMAP_BD_REGBASE = (SMAP_REGBASE + 0x2f00);
        public const uint SMAP_BD_TX_BASE = (SMAP_BD_REGBASE + 0x0000);
        public const uint SMAP_BD_RX_BASE = (SMAP_BD_REGBASE + 0x0200);
        public const uint SMAP_BD_SIZE = 512;
        public const uint SMAP_BD_MAX_ENTRY = 64;

        public const uint SMAP_TX_BASE = (SMAP_REGBASE + 0x1000);
        public const uint SMAP_TX_BUFSIZE = 4096;

        /* Tx Control */
        public const uint SMAP_BD_TX_READY = (1 << 15); /* set:driver, clear:HW */
        public const uint SMAP_BD_TX_GENFCS = (1 << 9); /* generate FCS */
        public const uint SMAP_BD_TX_GENPAD = (1 << 8); /* generate padding */
        public const uint SMAP_BD_TX_INSSA = (1 << 7);  /* insert source address */
        public const uint SMAP_BD_TX_RPLSA = (1 << 6);  /* replace source address */
        public const uint SMAP_BD_TX_INSVLAN = (1 << 5);/* insert VLAN Tag */
        public const uint SMAP_BD_TX_RPLVLAN = (1 << 4);/* replace VLAN Tag */

        /* TX Status */
        //public const uint SMAP_BD_TX_READY = (1 << 15); /* set:driver, clear:HW */
        public const uint SMAP_BD_TX_BADFCS = (1 << 9);	/* bad FCS */
        public const uint SMAP_BD_TX_BADPKT = (1 << 8);	/* bad previous pkt in dependent mode */
        public const uint SMAP_BD_TX_LOSSCR = (1 << 7);	/* loss of carrior sense */
        public const uint SMAP_BD_TX_EDEFER = (1 << 6);	/* excessive deferal */
        public const uint SMAP_BD_TX_ECOLL = (1 << 5);	/* excessive collision */
        public const uint SMAP_BD_TX_LCOLL = (1 << 4);	/* late collision */
        public const uint SMAP_BD_TX_MCOLL = (1 << 3);	/* multiple collision */
        public const uint SMAP_BD_TX_SCOLL = (1 << 2);	/* single collision */
        public const uint SMAP_BD_TX_UNDERRUN = (1 << 1);/* underrun */
        public const uint SMAP_BD_TX_SQE = (1 << 0);	/* SQE */

        public const uint SMAP_BD_TX_ERROR = (SMAP_BD_TX_LOSSCR | SMAP_BD_TX_EDEFER | SMAP_BD_TX_ECOLL |
            SMAP_BD_TX_LCOLL | SMAP_BD_TX_UNDERRUN);

        /* RX Control */
        public const uint SMAP_BD_RX_EMPTY = (1 << 15);	/* set:driver, clear:HW */

        /* RX Status */
        //public const uint SMAP_BD_RX_EMPTY = (1 << 15);	/* set:driver, clear:HW */
        public const uint SMAP_BD_RX_OVERRUN = (1 << 9);/* overrun */
        public const uint SMAP_BD_RX_PFRM = (1 << 8);	/* pause frame */
        public const uint SMAP_BD_RX_BADFRM = (1 << 7);	/* bad frame */
        public const uint SMAP_BD_RX_RUNTFRM = (1 << 6);/* runt frame */
        public const uint SMAP_BD_RX_SHORTEVNT = (1 << 5);/* short event */
        public const uint SMAP_BD_RX_ALIGNERR = (1 << 4);/* alignment error */
        public const uint SMAP_BD_RX_BADFCS = (1 << 3); /* bad FCS */
        public const uint SMAP_BD_RX_FRMTOOLONG = (1 << 2);/* frame too long */
        public const uint SMAP_BD_RX_OUTRANGE = (1 << 1);/* out of range error */
        public const uint SMAP_BD_RX_INRANGE = (1 << 0);/* in range error */

        public const uint SMAP_BD_RX_ERROR = (SMAP_BD_RX_OVERRUN | SMAP_BD_RX_RUNTFRM | SMAP_BD_RX_SHORTEVNT |
            SMAP_BD_RX_ALIGNERR | SMAP_BD_RX_BADFCS | SMAP_BD_RX_FRMTOOLONG |
            SMAP_BD_RX_OUTRANGE | SMAP_BD_RX_INRANGE);

        /* PHY registers (National Semiconductor DP83846A).  */

        public const uint SMAP_NS_OUI = 0x080017;
        public const uint SMAP_DsPHYTER_ADDRESS = 0x1;

        public const uint SMAP_DsPHYTER_BMCR = 0x00;
        public const uint SMAP_PHY_BMCR_RST = (1 << 15); /* ReSeT */
        public const uint SMAP_PHY_BMCR_LPBK = (1 << 14);/* LooPBacK */
        public const uint SMAP_PHY_BMCR_100M = (1 << 13);/* speed select, 1:100M, 0:10M */
        public const uint SMAP_PHY_BMCR_10M = (0 << 13); /* speed select, 1:100M, 0:10M */
        public const uint SMAP_PHY_BMCR_ANEN = (1 << 12);/* Auto-Negotiation ENable */
        public const uint SMAP_PHY_BMCR_PWDN = (1 << 11);/* PoWer DowN */
        public const uint SMAP_PHY_BMCR_ISOL = (1 << 10);/* ISOLate */
        public const uint SMAP_PHY_BMCR_RSAN = (1 << 9); /* ReStart Auto-Negotiation */
        public const uint SMAP_PHY_BMCR_DUPM = (1 << 8); /* DUPlex Mode, 1:FDX, 0:HDX */
        public const uint SMAP_PHY_BMCR_COLT = (1 << 7); /* COLlision Test */

        public const UInt16 SMAP_DsPHYTER_BMSR = 0x01;
        public const UInt16 SMAP_PHY_BMSR_ANCP = (1 << 5);	/* Auto-Negotiation ComPlete */
        public const UInt16 SMAP_PHY_BMSR_LINK = (1 << 2); /* LINK status */

        public const uint SMAP_DsPHYTER_PHYIDR1 = 0x02;
        public const uint SMAP_PHY_IDR1_VAL = (((SMAP_NS_OUI << 2) >> 8) & 0xffff);

        public const uint SMAP_DsPHYTER_PHYIDR2 = 0x03;
        public const uint SMAP_PHY_IDR2_VMDL = 0x2;		/* Vendor MoDeL number */
        public const uint SMAP_PHY_IDR2_VAL =
            (((SMAP_NS_OUI << 10) & 0xFC00) | ((SMAP_PHY_IDR2_VMDL << 4) & 0x3F0));
        public const uint SMAP_PHY_IDR2_MSK = 0xFFF0;
        public const uint SMAP_PHY_IDR2_REV_MSK = 0x000F;

        public const uint SMAP_DsPHYTER_ANAR = 0x04;
        public const uint SMAP_DsPHYTER_ANLPAR = 0x05;
        public const uint SMAP_DsPHYTER_ANLPARNP = 0x05;
        public const uint SMAP_DsPHYTER_ANER = 0x06;
        public const uint SMAP_DsPHYTER_ANNPTR = 0x07;

        /* Extended registers.  */
        public const uint SMAP_DsPHYTER_PHYSTS = 0x10;
        public const UInt16 SMAP_PHY_STS_REL = (1 << 13);  /* Receive Error Latch */
        public const UInt16 SMAP_PHY_STS_POST = (1 << 12); /* POlarity STatus */
        public const UInt16 SMAP_PHY_STS_FCSL = (1 << 11); /* False Carrier Sense Latch */
        public const UInt16 SMAP_PHY_STS_SD = (1 << 10);   /* 100BT unconditional Signal Detect */
        public const UInt16 SMAP_PHY_STS_DSL = (1 << 9);   /* 100BT DeScrambler Lock */
        public const UInt16 SMAP_PHY_STS_PRCV = (1 << 8);  /* Page ReCeiVed */
        public const UInt16 SMAP_PHY_STS_RFLT = (1 << 6);  /* Remote FauLT */
        public const UInt16 SMAP_PHY_STS_JBDT = (1 << 5);  /* JaBber DetecT */
        public const UInt16 SMAP_PHY_STS_ANCP = (1 << 4);  /* Auto-Negotiation ComPlete */
        public const UInt16 SMAP_PHY_STS_LPBK = (1 << 3);  /* LooPBacK status */
        public const UInt16 SMAP_PHY_STS_DUPS = (1 << 2);  /* DUPlex Status,1:FDX,0:HDX */
        public const UInt16 SMAP_PHY_STS_FDX = (1 << 2);   /* Full Duplex */
        public const UInt16 SMAP_PHY_STS_HDX = (0 << 2);   /* Half Duplex */
        public const UInt16 SMAP_PHY_STS_SPDS = (1 << 1);  /* SPeeD Status */
        public const UInt16 SMAP_PHY_STS_10M = (1 << 1);   /* 10Mbps */
        public const UInt16 SMAP_PHY_STS_100M = (0 << 1);  /* 100Mbps */
        public const UInt16 SMAP_PHY_STS_LINK = (1 << 0);  /* LINK status */
        public const UInt16 SMAP_DsPHYTER_FCSCR = 0x14;
        public const UInt16 SMAP_DsPHYTER_RECR = 0x15;
        public const UInt16 SMAP_DsPHYTER_PCSR = 0x16;
        public const UInt16 SMAP_DsPHYTER_PHYCTRL = 0x19;
        public const UInt16 SMAP_DsPHYTER_10BTSCR = 0x1A;
        public const UInt16 SMAP_DsPHYTER_CDCTRL = 0x1B;

        /*
         * ATA hardware types and definitions.
        */

        public const uint ATA_DEV9_HDD_BASE = (SPD_REGBASE + 0x40);
        /* AIF on T10Ks - Not supported yet.  */
        public const uint ATA_AIF_HDD_BASE = (SPD_REGBASE + 0x4000000 + 0x60);

        public const uint ATA_R_DATA = (ATA_DEV9_HDD_BASE + 0x00);
        public const uint ATA_R_ERROR = (ATA_DEV9_HDD_BASE + 0x02);   //On Read
        public const uint ATA_R_FEATURE = (ATA_DEV9_HDD_BASE + 0x02); //On Write (from MegaDev9)
        public const uint ATA_R_NSECTOR = (ATA_DEV9_HDD_BASE + 0x04);
        public const uint ATA_R_SECTOR = (ATA_DEV9_HDD_BASE + 0x06);
        public const uint ATA_R_LCYL = (ATA_DEV9_HDD_BASE + 0x08);
        public const uint ATA_R_HCYL = (ATA_DEV9_HDD_BASE + 0x0a);
        public const uint ATA_R_SELECT = (ATA_DEV9_HDD_BASE + 0x0c);
        public const uint ATA_R_STATUS = (ATA_DEV9_HDD_BASE + 0x0e); //On Read
        public const uint ATA_R_CMD = (ATA_DEV9_HDD_BASE + 0x0e);    //On Write (from MegaDev9)
        public const uint ATA_R_ALT_STATUS = (ATA_DEV9_HDD_BASE + 0x1c);//On Read (from MegaDev9)
        public const uint ATA_R_CONTROL = (ATA_DEV9_HDD_BASE + 0x1c);//On Write
        public const uint ATA_DEV9_INT = (0x01);
        public const uint ATA_DEV9_INT_DMA = (0x02); //not sure rly
        public const uint ATA_DEV9_HDD_END = (ATA_R_CONTROL + 4);

        /* 
         * From MagaDev9
         */

        public const UInt16 ATA_ERR_MARK = 0x01;
        public const UInt16 ATA_ERR_TRACK0 = 0x02;
        public const UInt16 ATA_ERR_ABORT = 0x04;
        public const UInt16 ATA_ERR_MCR = 0x08;
        public const UInt16 ATA_ERR_ID = 0x10;
        public const UInt16 ATA_ERR_MC = 0x20;
        public const UInt16 ATA_ERR_ECC = 0x40;
        public const UInt16 ATA_ERR_ICRC = 0x80;

        public const UInt16 ATA_STAT_ERR = 0x01;
        public const UInt16 ATA_STAT_INDEX = 0x02;
        public const UInt16 ATA_STAT_ECC = 0x04;
        public const UInt16 ATA_STAT_DRQ = 0x08;
        public const UInt16 ATA_STAT_SEEK = 0x10;
        public const UInt16 ATA_STAT_WRERR = 0x20;
        public const UInt16 ATA_STAT_READY = 0x40;
        public const UInt16 ATA_STAT_BUSY = 0x80;

        /*
         * NAND Flash via Dev9 driver definitions
         */

        public const uint FLASH_ID_64MBIT = 0xe6;
        public const uint FLASH_ID_128MBIT = 0x73;
        public const uint FLASH_ID_256MBIT = 0x75;
        public const uint FLASH_ID_512MBIT = 0x76;
        public const uint FLASH_ID_1024MBIT = 0x79;

        /* SmartMedia commands.  */
        public const uint SM_CMD_READ1 = 0x00;
        public const uint SM_CMD_READ2 = 0x01;
        public const uint SM_CMD_READ3 = 0x50;
        public const uint SM_CMD_RESET = 0xff;
        public const uint SM_CMD_WRITEDATA = 0x80;
        public const uint SM_CMD_PROGRAMPAGE = 0x10;
        public const uint SM_CMD_ERASEBLOCK = 0x60;
        public const uint SM_CMD_ERASECONFIRM = 0xd0;
        public const uint SM_CMD_GETSTATUS = 0x70;
        public const uint SM_CMD_READID = 0x90;

        public const uint FLASH_REGBASE = 0x10004800;

        public const uint FLASH_R_DATA = (FLASH_REGBASE + 0x00);
        public const uint FLASH_R_CMD = (FLASH_REGBASE + 0x04);
        public const uint FLASH_R_ADDR = (FLASH_REGBASE + 0x08);
        public const uint FLASH_R_CTRL = (FLASH_REGBASE + 0x0C);
        public const uint FLASH_PP_READY = (1 << 0);	// r/w	/BUSY
        public const uint FLASH_PP_WRITE = (1 << 7);	// -/w	WRITE data
        public const uint FLASH_PP_CSEL = (1 << 8);	// -/w	CS
        public const uint FLASH_PP_READ = (1 << 11);	// -/w	READ data
        public const uint FLASH_PP_NOECC = (1 << 12);// -/w	ECC disabled
        public const uint FLASH_R_ID = (FLASH_REGBASE + 0x14);

        public const uint FLASH_REGSIZE = 0x20;
    }
}
