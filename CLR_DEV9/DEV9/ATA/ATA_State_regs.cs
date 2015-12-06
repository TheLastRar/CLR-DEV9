using System;

namespace CLRDEV9.DEV9.ATA
{
    partial class ATA_State
    {
        delegate bool Cmd();
        delegate void EndTransfer();

        UInt16 xfer_mode;
        int smart_on;

        bool sendIRQ = true;

        //UInt16 pio_count;
        //UInt16 pio_size;
        //byte[] pio_buf = new byte[256*2];

        //UInt16 feature; //Shars reg with error

        Cmd[] HDDcmds = new Cmd[256];
        bool[] HDDcmdDoesSeek = new bool[256];

        //bus
        UInt16 command;

        //Device 0
        uint unit = 0;
        //Device Kind
        //int cylinders, heads, sectors, chs_trans;
        //int64_t nb_sectors;
        //int mult_sectors = 0;
        //int identify_set;
        byte[] identify_data; //512 bytes in size
        //int drive_serial;
        //char drive_serial_str[21];
        //char drive_model_str[41];
        //uint64_t wwn;
        //
        byte feature;
        byte error;
        Int32 nsector;
        byte sector;
        byte lcyl;
        byte hcyl;
        /* other part of tf for lba48 support */
        byte hob_feature;
        byte hob_nsector;
        byte hob_sector;
        byte hob_lcyl;
        byte hob_hcyl;

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
        int req_nb_sectors; /* number of sectors per interrupt */
        EndTransfer end_transfer_func;
        int data_ptr;
        int data_end;
        byte[] pio_buffer = new byte[512];
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
        //byte smart_enabled;
        //byte smart_autosave;
        //int smart_errors;
        //byte smart_selftest_count;
        //uint8_t *smart_selftest_data;
        /* AHCI */
        //int ncq_queues;
    }
}
