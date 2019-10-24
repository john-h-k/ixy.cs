using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IxyCs;
using IxyCs.Ixgbe;
using IxyCs.Memory;

namespace IxyCs.Demo
{
    public class PacketGenerator
    {
        private const int BuffersCount = 2048;
        private const int PacketSize = 60;
        private const int BatchSize = 64;

        private readonly byte[] PacketData = new byte[] {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, //dst MAC
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, //src MAC
            0x08, 0x00, //ether type: IPv4
            0x45, 0x00, //Version IHL, TOS
            (PacketSize - 14) >> 8, //ip len excluding ethernet, high byte
            (PacketSize - 14) & 0xFF, //ip len excluding ethernet, low byte
            0x00, 0x00, 0x00, 0x00, //id,flags,fragmentation
            0x40, 0x11, 0x00, 0x00, //TTL(64), protocol (UDP), checksum
            0x0A, 0x00, 0x00, 0x01,             // src ip (10.0.0.1)
            0x0A, 0x00, 0x00, 0x02,             // dst ip (10.0.0.2)
            0x00, 0x2A, 0x05, 0x39,             // src and dst ports (42 -> 1337)
            (PacketSize - 20 - 14) >> 8,          // udp len excluding ip & ethernet, high byte
            (PacketSize - 20 - 14) & 0xFF,        // udp len exlucding ip & ethernet, low byte
            0x00, 0x00,                         // udp checksum, optional
            0x69, 0x78, 0x79                       // payload ("ixy")
            // rest of the payload is zero-filled because mempools guarantee empty bufs
        };

        private Mempool _mempool;

        public unsafe PacketGenerator(string pciAddr)
        {
            InitMempool();

            var dev = new IxgbeDevice(pciAddr, 1, 1);

            // TODO: switch to C# 7.3 and replace with Span<PacketBuffer> buffers = stackalloc PacketBuffer[BatchSize];
            var buffersArray = stackalloc PacketBuffer[BatchSize];
            var buffers = new Span<PacketBuffer>(buffersArray, BatchSize);

            var statsOld = new DeviceStats(dev);
            var statsNew = new DeviceStats(dev);
            ulong counter = 0;
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            int seqNum = 0;

            while(true)
            {
                var batchCount = _mempool.GetPacketBuffers(buffers);
                var batch = buffers.Slice(0, batchCount);
                foreach (var buf in batch)
                    buf.WriteData(PacketSize - 4, seqNum++);
                dev.TxBatchBusyWait(0, batch);

                if((counter++ & 0xFFF) == 0 && stopWatch.ElapsedMilliseconds > 100)
                {
                    stopWatch.Stop();
                    var nanos = stopWatch.ElapsedTicks;
                    dev.ReadStats(ref statsNew);
                    statsNew.PrintStatsDiff(ref statsOld, (ulong)nanos);
                    statsOld = statsNew;
                    counter = 0;
                    stopWatch.Restart();
                }
            }
        }

        private void InitMempool()
        {
            _mempool = MemoryHelper.AllocateMempool(BuffersCount);

            //Pre-fill all our packet buffers with some templates that can be modified later
            var buffers = new PacketBuffer[BuffersCount];
            for(int i = 0; i < BuffersCount; i++)
            {
                var buffer = _mempool.GetPacketBuffer();
                buffer.Size = (uint)PacketData.Length;
                buffer.WriteData(0, PacketData);
                var ipData = buffer.Data.Slice(14, 20);
                buffer.WriteData(24, (short)CalcIpChecksum(ipData));
                buffers[i] = buffer;
            }

            //Return them all to the mempool, all future allocations will return buffers with the data set above
            foreach(var buffer in buffers)
                _mempool.FreeBuffer(buffer);
        }

        private ushort CalcIpChecksum(ReadOnlySpan<byte> data)
        {
            if(data.Length % 2 != 0)
            {
                Log.Error("Odd sized checksums NYI");
                Environment.Exit(1);
            }
            uint checksum = 0;
            for(int i = 0; i < data.Length / 2; i++)
            {
                checksum += data[i];
                if(checksum > 0xFFFF)
                    checksum = (checksum & 0xFFFF) + 1; //16 bit one's complement
            }
            return (ushort)(~((ushort)checksum));
        }
    }
}