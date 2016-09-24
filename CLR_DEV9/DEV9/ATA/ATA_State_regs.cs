using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        const bool lba48Supported = false;

        delegate bool Cmd();
        delegate void EndTransfer();

        UInt16 xferMode;
        int pioMode = -1;
        int sdmaMode = -1;
        int mdmaMode = -1;
        int udmaMode = 5;

        bool sendIRQ = true;

        //UInt16 pio_count;
        //UInt16 pio_size;
        //byte[] pio_buf = new byte[256*2];

        //UInt16 feature; //Shars reg with error

        Cmd[] hddCmds = new Cmd[256];
        bool[] hddCmdDoesSeek = new bool[256];

        //bus
        UInt16 command;

        //Device 0
        uint unit = 0;
        //Device Kind
        //int cylinders, heads, sectors, chs_trans;
        //int64_t nb_sectors;
        //int mult_sectors = 0;
        //int identify_set;
        byte[] identifyData; //512 bytes in size
        //int drive_serial;
        //char drive_serial_str[21];
        //char drive_model_str[41];
        //uint64_t wwn;
        //
        byte feature;
        byte error;
        Int32 nsector; //sector count
        byte sector; //sector number
        byte lcyl;
        byte hcyl;
        /* other part of tf for lba48 support */
        byte hobFeature;
        byte hobNsector;
        byte hobSector;
        byte hobLcyl;
        byte hobHcyl;

        byte select;
        byte status;

        /* set for lba48 access */
        bool lba48 = false;
        //BlockBackend *blk;
        //char version[9];
        /* ATAPI specific */
        //struct unreported_events events;
        //uint8_t sense_key;
        //uint8_t asc;
        //bool tray_open;
        //bool tray_locked;
        //uint8_t cdrom_changed;
        //int packet_transfer_size;
        //int elementary_transfer_size;
        //int32_t io_buffer_index;
        //int lba;
        //int cd_sector_size;
        //int atapi_dma; /* true if dma is requested for the packet cmd */
        //BlockAcctCookie acct;
        //BlockAIOCB *pio_aiocb;
        //struct iovec iov;
        //QEMUIOVector qiov;
        //QLIST_HEAD(, IDEBufferedRequest) buffered_requests;
        /* ATA DMA state */
        //UInt64 io_buffer_offset;
        //Int32 io_buffer_size;
        //QEMUSGList sg;
        /* PIO transfer handling */
        int reqNbSectors; /* number of sectors per interrupt */
        EndTransfer endTransferFunc;
        int dataPtr;
        int dataEnd;
        byte[] pioBuffer = new byte[512];
        /* PIO save/restore */
        //Int32 io_buffer_total_len;
        //Int32 cur_io_buffer_offset;
        //Int32 cur_io_buffer_len;
        //byte end_transfer_fn_idx;
        //QEMUTimer *sector_write_timer; /* only used for win2k install hack */
        //uint32_t irq_count; /* counts IRQs when using win2k install hack */
        /* CF-ATA extended error */
        //byte ext_error;
        /* CF-ATA metadata storage */
        //UInt32 mdata_size;
        //uint8_t *mdata_storage;
        //int media_changed;
        //enum ide_dma_cmd dma_cmd;

        /* SMART */
#pragma warning disable CS0414 // The field 'ATA_State.smartAutosave' is assigned but its value is never used
        bool smartAutosave = true;
#pragma warning restore CS0414 // The field 'ATA_State.smartAutosave' is assigned but its value is never used
        bool smartErrors = false;
        byte smartSelfTestCount = 0;
        //uint8_t *smart_selftest_data;
        /* AHCI */
        //int ncq_queues;

        //Enable/disable features
        bool fetSmartEnabled = true;
        bool fetSecurityEnabled = false;
        bool fetWriteCacheEnabled = false; //WriteCache off
        bool fetHostProtectedAreaEnabled = false;
    }
}
