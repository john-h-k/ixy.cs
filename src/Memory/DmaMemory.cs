using System;

namespace IxyCs.Memory
{
    public readonly struct DmaMemory
    {
        public readonly ulong VirtualAddress, PhysicalAddress;

        public DmaMemory(ulong virt, ulong phys)
        {
            VirtualAddress = virt;
            PhysicalAddress = phys;
        }
    }
}