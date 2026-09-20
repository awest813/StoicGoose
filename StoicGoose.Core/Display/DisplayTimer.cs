using System.IO;

namespace StoicGoose.Core.Display
{
    public class DisplayTimer
    {
        bool enable;

        public bool Enable
        {
            get => enable;
            set
            {
                if (value && !enable && Frequency != 0)
                    Counter = Frequency;
                enable = value;
            }
        }

        public bool Repeating { get; set; }
        public ushort Frequency { get; set; }

        public ushort Counter { get; set; }

        public DisplayTimer()
        {
            Reset();
        }

        public void Reset()
        {
            enable = Repeating = false;
            Frequency = Counter = 0;
        }

        public void Reload()
        {
            Counter = Frequency;
        }

        public bool Step()
        {
            if (!enable || Counter == 0)
                return false;

            Counter--;
            var expired = Counter == 0;
            if (expired && Repeating)
                Reload();
            return expired;
        }

        public void ExportState(BinaryWriter writer)
        {
            writer.Write(enable);
            writer.Write(Repeating);
            writer.Write(Frequency);
            writer.Write(Counter);
        }

        public void ImportState(BinaryReader reader)
        {
            enable = reader.ReadBoolean();
            Repeating = reader.ReadBoolean();
            Frequency = reader.ReadUInt16();
            Counter = reader.ReadUInt16();
        }
    }
}
