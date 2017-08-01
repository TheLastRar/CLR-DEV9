
namespace CLRDEV9.DEV9.SMAP.Data
{
    class NetPacket
    {
        public NetPacket()
        {
            size = 0;
        }
        public NetPacket(byte[] bytes, int offset, int sz)
        {
            size = sz;
            Utils.memcpy(buffer, 0, bytes, offset, sz);
        }

        public int size;
        public byte[] buffer = new byte[2048 - sizeof(int)];//1536 is realy needed, just pad up to 2048 bytes :)
    }
}
