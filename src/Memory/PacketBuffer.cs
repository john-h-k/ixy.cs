using System;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;

namespace IxyCs.Memory
{
    /*
        This class does not contain the actual packet buffer but is actually
        a wrapper containing the real buffer's address
        The reason for this is that the real buffer lives in DMA memory which is written to by
        the device and requires a very specific memory layout
     */
    public struct PacketBuffer
    {
        public const uint DataOffset = 64;
        //These buffers have 64 bytes of headroom so the actual data has an offset of 64 bytes
        /*
        Fields:
        0 - pointer Physical Address (64)
        64 - pointer mempool (64)
        128 - uint mempool index (32)
        160 - uint size (32)
        192 - byte[] more headroom (40 * 8)
        == 64 bytes
         */
        private readonly ulong _baseAddress;
        private readonly ulong _start;

        /// <summary>
        /// The virtual address of the actual Packet Buffer that this object wraps
        /// </summary>
        public ulong VirtualAddress => _baseAddress;

        /// <summary>
        /// If true, this buffer is not (successfully) initialized
        /// </summary>
        public bool IsNull => VirtualAddress == 0;

        public static PacketBuffer Null => new PacketBuffer(0);

        //Physical Address, 64 bits, offset 0
        public unsafe ulong PhysicalAddress
        {
            get => *((ulong*)_baseAddress);
            set => *((ulong*)_baseAddress) = value;
        }

        //This id is 64 bits to keep the data as similar to the C version as possible
        public unsafe long MempoolId
        {
            get => *((long*)(_baseAddress + 8));
            set => *((long*)(_baseAddress + 8)) = value;
        }

        //Size, 32 bits, offset 160 bits
        public unsafe uint Size
        {
            get => *((uint*)(_baseAddress + 20));
            set => *((uint*)(_baseAddress + 20)) = value;
        }

        public PacketBuffer(ulong baseAddr)
        {
            this._baseAddress = baseAddr;
            this._start = baseAddr + DataOffset;
        }

        /// <summary>
        /// Increments the second byte of the payload. Used for benchmarking
        /// </summary>
        public unsafe void Touch()
        {
            ((byte*)(_start + 1))[0]++;
        }

        //Sacrificing some code compactness for a nicer API
        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, int val)
        {
            int* ptr = (int*)(_start + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, short val)
        {
            short* ptr = (short*)(_start + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, IntPtr val)
        {
            IntPtr* ptr = (IntPtr*)(_start + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, long val)
        {
            long* ptr = (long*)(_start + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, byte val)
        {
            byte* ptr = (byte*)(_start + offset);
            *ptr = val;
        }

        /// <summary>
        /// Writes the value to the data segment of this buffer with the given offset (to which DataOffset is added)
        /// </summary>
        public unsafe void WriteData(uint offset, ReadOnlySpan<byte> val)
        {
            byte* targetPtr = (byte*)(_start + offset);
            
            val.CopyTo(new Span<byte>(targetPtr, int.MaxValue));
        }

        /// <summary>
        /// Returns one byte of data at the given offset. Used for debug/benchmark purposes
        /// </summary>
        public unsafe byte GetDataByte(uint i)
        {
            byte* b = (byte*)(_start + i);
            return *b;
        }


        /// <summary>
        /// Returns a copy of the buffer's payload
        /// </summary>
        public byte[] CopyData()
        {
            var buffer = new byte[Size];
            CopyData(buffer);
            return buffer;
        }

        public void CopyData(Span<byte> buffer, uint offset = 0, uint length = 0)
        {
            ReadOnlySpan<byte> source = Data.Slice((int)offset, (int)length);
            source.CopyTo(buffer);
        }

        public unsafe ReadOnlySpan<byte> Data => new ReadOnlySpan<byte>((void*)(_start), (int)Size);
    }
}