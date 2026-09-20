namespace StoicGoose.Core.Sound
{
    /* Channel 2, supports PCM voice */
    public sealed class SoundChannel2(WaveTableReadDelegate waveTableRead)
    {
        const int counterReload = 2048;

        ushort counter;
        byte pointer;

        public byte OutputLeft { get; set; }
        public byte OutputRight { get; set; }

        readonly WaveTableReadDelegate waveTableReadDelegate = waveTableRead;

        /* REG_SND_CH2_PITCH */
        public ushort Pitch { get; set; }
        /* REG_SND_CH2_VOL */
        public byte VolumeLeft { get; set; }
        public byte VolumeRight { get; set; }
        /* REG_SND_CTRL */
        public bool IsEnabled { get; set; }
        public bool IsVoiceEnabled { get; set; }

        /* REG_SND_VOICE_CTRL */
        public bool PcmRightFull { get; set; }
        public bool PcmRightHalf { get; set; }
        public bool PcmLeftFull { get; set; }
        public bool PcmLeftHalf { get; set; }

        public void Reset()
        {
            counter = counterReload;
            pointer = 0;
            OutputLeft = OutputRight = 0;

            Pitch = 0;
            VolumeLeft = VolumeRight = 0;
            IsEnabled = false;

            IsVoiceEnabled = false;

            PcmRightFull = PcmRightHalf = PcmLeftFull = PcmLeftHalf = false;
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
            writer.Write(IsVoiceEnabled);
            writer.Write(PcmRightFull);
            writer.Write(PcmRightHalf);
            writer.Write(PcmLeftFull);
            writer.Write(PcmLeftHalf);
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
            IsVoiceEnabled = reader.ReadBoolean();
            PcmRightFull = reader.ReadBoolean();
            PcmRightHalf = reader.ReadBoolean();
            PcmLeftFull = reader.ReadBoolean();
            PcmLeftHalf = reader.ReadBoolean();
        }

        public void Step()
        {
            if (IsVoiceEnabled)
            {
                var pcm = (ushort)(VolumeLeft << 4 | VolumeRight);
                OutputLeft = (byte)(PcmLeftFull ? pcm : (PcmLeftHalf ? pcm >> 1 : 0));
                OutputRight = (byte)(PcmRightFull ? pcm : (PcmRightHalf ? pcm >> 1 : 0));
            }
            else
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
}
