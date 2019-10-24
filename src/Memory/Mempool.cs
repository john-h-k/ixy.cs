using System;
using System.Collections.Generic;
using System.Linq;

namespace IxyCs.Memory
{
    public struct Mempool
    {
        //Static mempool management - this is necessary because PacketBuffers need to reference
        //mempools and we can't save references to managed memory in DMA
        public static readonly List<Mempool> Pools = new List<Mempool>();

        public static Mempool? FindPool(long id)
        {
            int index = Pools.FindIndex(pool => pool.Id == id);
            return index == -1 ? (Mempool?)null : Pools[index];
        }

        public bool IsDefault => BaseAddress == 0;

        public static void FreePool(Mempool pool) { Pools.Remove(pool); }
        public static void AddPool(Mempool pool)
        {
            long i = 0;
            while (!ValidId(i))
                i++;
            pool.Id = i;
            Pools.Add(pool);
        }
        private static bool ValidId(long id) { return FindPool(id) == null; }
        //---End of static management

        public readonly ulong BaseAddress;
        public readonly uint BufferSize, NumEntries;
        /// <summary>
        /// Is used to identify the mempool so that PacketBuffers can reference them
        /// </summary>
        public long Id
        {
            get => _id;
            set
            {
                if (ValidId(value))
                    _id = value;
                else
                    throw new ArgumentException("This mempool id is already in use");
            }
        }

        private long _id;
        //Pre-allocated buffer objects for this mempool
        private FixedStack _buffers;

        public Mempool(ulong baseAddr, uint bufSize, uint numEntries)
        {
            this.BaseAddress = baseAddr;
            this.BufferSize = bufSize;
            this.NumEntries = numEntries;
            _id = default;
            _buffers = default;
            //Register this mempool to static list of pools
            AddPool(this);
        }

        public void PreallocateBuffers()
        {
            _buffers = new FixedStack(NumEntries);
            for (int i = (int)NumEntries - 1; i >= 0; i--)
            {
                var virtAddr = BaseAddress + (uint)i * BufferSize;
                var buffer = new PacketBuffer(virtAddr)
                {
                    MempoolId = Id,
                    PhysicalAddress = MemoryHelper.VirtToPhys(virtAddr),
                    Size = 0
                };
                _buffers.Push(buffer);
            }
        }

        public PacketBuffer GetPacketBuffer()
        {
            if (_buffers.Count < 1)
            {
                Log.Warning("Memory pool is out of free buffers - ignoring request for allocation");
                return PacketBuffer.Null;
            }
            return _buffers.Pop();
        }

        internal PacketBuffer GetPacketBufferFast()
        {
            return _buffers.Pop();
        }

        public int GetPacketBuffers(Span<PacketBuffer> buffers)
        {
            var num = buffers.Length;

            if (_buffers.Count < num)
            {
                Log.Warning($"Mempool only has {_buffers.Count} free buffers, requested {num}");
                num = _buffers.Count;
            }

            _buffers.GetUnderlyingArray().AsSpan().CopyTo(buffers);

            return num;
        }

        /// <summary>
        /// Returns the given buffer to the top of the mempool stack
        /// </summary>
        public void FreeBuffer(PacketBuffer buffer)
        {
            if (_buffers.Count < _buffers.Capacity)
                _buffers.Push(buffer);
            else
                Log.Warning("Cannot free buffer because mempool stack is full");
        }

        /// <summary>
        /// Fast version of FreeBuffer, which does not do any bounds checking. For internal use only!
        /// </summary>
        internal void FreeBufferFast(PacketBuffer buffer)
        {
            _buffers.Push(buffer);
        }
    }
}