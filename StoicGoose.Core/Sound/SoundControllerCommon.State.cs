using System.IO;

namespace StoicGoose.Core.Sound
{
    public abstract partial class SoundControllerCommon
    {
        public virtual void ExportState(BinaryWriter writer)
        {
            writer.Write(cycleCount);
            writer.Write(waveTableBase);
            writer.Write(speakerEnable);
            writer.Write(headphoneEnable);
            writer.Write(headphonesConnected);
            writer.Write(speakerVolumeShift);
            writer.Write(masterVolume);

            channel1.ExportState(writer);
            channel2.ExportState(writer);
            channel3.ExportState(writer);
            channel4.ExportState(writer);
        }

        public virtual void ImportState(BinaryReader reader)
        {
            cycleCount = reader.ReadInt32();
            waveTableBase = reader.ReadByte();
            speakerEnable = reader.ReadBoolean();
            headphoneEnable = reader.ReadBoolean();
            headphonesConnected = reader.ReadBoolean();
            speakerVolumeShift = reader.ReadByte();
            masterVolume = reader.ReadByte();

            channel1.ImportState(reader);
            channel2.ImportState(reader);
            channel3.ImportState(reader);
            channel4.ImportState(reader);

            FlushSamples();
        }
    }
}
