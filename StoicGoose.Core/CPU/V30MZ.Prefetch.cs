namespace StoicGoose.Core.CPU
{
    public sealed partial class V30MZ
    {
        const int PrefetchQueueSize = 16;

        readonly byte[] prefetchQueue = new byte[PrefetchQueueSize];
        uint prefetchBaseAddress;
        int prefetchIndex;
        int prefetchRemaining;

        private void InvalidatePrefetch()
        {
            prefetchRemaining = 0;
            prefetchIndex = 0;
        }

        private void EnsurePrefetch()
        {
            var currentAddress = (uint)((cs << 4) + ip);
            if (prefetchRemaining == 0 || currentAddress != prefetchBaseAddress + prefetchIndex)
            {
                prefetchBaseAddress = currentAddress;
                prefetchIndex = 0;
                prefetchRemaining = PrefetchQueueSize;

                for (var i = 0; i < PrefetchQueueSize; i++)
                    prefetchQueue[i] = machine.ReadMemory((uint)(prefetchBaseAddress + i));
            }
        }

        private byte FetchByte()
        {
            EnsurePrefetch();

            var value = prefetchQueue[prefetchIndex++];
            prefetchRemaining--;
            ip++;

            return value;
        }

        private ushort FetchWord()
        {
            var low = FetchByte();
            var high = FetchByte();
            return (ushort)((high << 8) | low);
        }
    }
}
