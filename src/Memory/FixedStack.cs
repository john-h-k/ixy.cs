using System.Collections;

namespace IxyCs.Memory
{
    /// <summary>
    /// Very fast fixed-size stack implementation for PacketBuffers. Could easily be turned into a generic stack
    /// Bounds checking is delegated to Mempool or application. Fairly unsafe solution,
    /// but this does provide a large performance increase and as this driver is not meant for production
    /// environments, the trade-off is acceptable.
    /// </summary>
    public struct FixedStack
    {
        private readonly PacketBuffer[] _buffers;
        private int _top;

        public int Count => _top + 1;
        public uint Capacity { get; }

        public int Free => (int)(Capacity - _top - 1);

        public FixedStack(uint size)
        {
            Capacity = size;
            _top = -1;
            _buffers = new PacketBuffer[Capacity];
        }

        public void Push(PacketBuffer pb)
        {
            _buffers[++_top] = pb;
        }

        public PacketBuffer Pop()
        {
            return _buffers[_top--];
        }

        internal PacketBuffer[] GetUnderlyingArray() => _buffers;
    }
}