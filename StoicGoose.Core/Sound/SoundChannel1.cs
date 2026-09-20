namespace StoicGoose.Core.Sound
{
    /* Channel 1, no additional features */
    public sealed class SoundChannel1(WaveTableReadDelegate waveTableRead)
    {
        const int counterReload = 2048;

        ushort counter;
        byte pointer;

        public byte OutputLeft { get; set; }
        public byte OutputRight { get; set; }

        readonly WaveTableReadDelegate waveTableReadDelegate = waveTableRead;

        /* REG_SND_CH1_PITCH */
        public ushort Pitch { get; set; }
        /* REG_SND_CH1_VOL */
        public byte VolumeLeft { get; set; }
        public byte VolumeRight { get; set; }
        /* REG_SND_CTRL */
        public bool IsEnabled { get; set; }

        public void Reset()
        {
            counter = counterReload;
            pointer = 0;
            OutputLeft = OutputRight = 0;

            Pitch = 0;
            VolumeLeft = VolumeRight = 0;
            IsEnabled = false;
        }

        public void ExportState(System.IO.BinaryWriter writer)
        {
            writer.Write(counter);
            writer.Write(pointer);
            writer.Write(OutputLeft);
            writer.Write(OutputRight);
            writer.Write(Pitch);
            writer.Write(VolumeLeft);
            writer.Write(VolumeRight);
            writer.Write(IsEnabled);
        }

        public void ImportState(System.IO.BinaryReader reader)
        {
            counter = reader.ReadUInt16();
            pointer = reader.ReadByte();
            OutputLeft = reader.ReadByte();
            OutputRight = reader.ReadByte();
            Pitch = reader.ReadUInt16();
            VolumeLeft = reader.ReadByte();
            VolumeRight = reader.ReadByte();
            IsEnabled = reader.ReadBoolean();
        }

        public void Step()
        {
            counter--;
            if (counter == Pitch)
            {
                var data = waveTableReadDelegate((ushort)(pointer >> 1));
                if ((pointer & 0b1) == 0b1) data >>= 4;
                data &= 0x0F;

                OutputLeft = (byte)(data * VolumeLeft);
                OutputRight = (byte)(data * VolumeRight);

                pointer++;
                pointer &= 0b11111;
                counter = counterReload;
            }
        }
    }
}
