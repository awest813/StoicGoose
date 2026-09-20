using System.IO;

namespace StoicGoose.Core.CPU
{
    public sealed partial class V30MZ
    {
        public void ExportState(BinaryWriter writer)
        {
            writer.Write(ax.Word);
            writer.Write(bx.Word);
            writer.Write(cx.Word);
            writer.Write(dx.Word);
            writer.Write(sp);
            writer.Write(bp);
            writer.Write(si);
            writer.Write(di);
            writer.Write(cs);
            writer.Write(ds);
            writer.Write(ss);
            writer.Write(es);
            writer.Write(ip);
            writer.Write((ushort)flags);
            writer.Write(halted);
            writer.Write(opCycles);
            writer.Write(intCycles);
            writer.Write(prevMulOverflow);

            writer.Write(prefetchBaseAddress);
            writer.Write(prefetchIndex);
            writer.Write(prefetchRemaining);
            writer.Write(prefetchQueue);

            writer.Write((byte)prefixSegOverride);
            writer.Write(prefixHasRepeat);
            writer.Write(prefixRepeatOnNotEqual);

            writer.Write(modRm.IsSet);
            writer.Write(modRm.Raw);
            writer.Write(modRm.Segment);
            writer.Write(modRm.Offset);
        }

        public void ImportState(BinaryReader reader)
        {
            ax.Word = reader.ReadUInt16();
            bx.Word = reader.ReadUInt16();
            cx.Word = reader.ReadUInt16();
            dx.Word = reader.ReadUInt16();
            sp = reader.ReadUInt16();
            bp = reader.ReadUInt16();
            si = reader.ReadUInt16();
            di = reader.ReadUInt16();
            cs = reader.ReadUInt16();
            ds = reader.ReadUInt16();
            ss = reader.ReadUInt16();
            es = reader.ReadUInt16();
            ip = reader.ReadUInt16();
            flags = (Flags)reader.ReadUInt16();
            halted = reader.ReadBoolean();
            opCycles = reader.ReadInt32();
            intCycles = reader.ReadInt32();
            prevMulOverflow = reader.ReadBoolean();

            prefetchBaseAddress = reader.ReadUInt32();
            prefetchIndex = reader.ReadInt32();
            prefetchRemaining = reader.ReadInt32();
            reader.Read(prefetchQueue, 0, PrefetchQueueSize);

            prefixSegOverride = (SegmentNumber)reader.ReadByte();
            prefixHasRepeat = reader.ReadBoolean();
            prefixRepeatOnNotEqual = reader.ReadBoolean();

            var modRmSet = reader.ReadBoolean();
            var modRmRaw = reader.ReadByte();
            if (modRmSet)
                modRm.Set(modRmRaw);
            else
                modRm.Reset();
            modRm.Segment = reader.ReadUInt16();
            modRm.Offset = reader.ReadUInt16();
        }
    }
}
