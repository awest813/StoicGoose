using System.IO;
using System.Text;

namespace StoicGoose.Core.SaveStates
{
    internal static class SaveStateIO
    {
        public const string Magic = "SGST";
        public const ushort Version = 1;

        public static void WriteHeader(BinaryWriter writer, string machineType, uint romCrc32)
        {
            writer.Write(Encoding.ASCII.GetBytes(Magic));
            writer.Write(Version);
            writer.Write(machineType ?? string.Empty);
            writer.Write(romCrc32);
        }

        public static bool TryReadHeader(BinaryReader reader, string expectedMachineType, uint expectedRomCrc32)
        {
            var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != Magic)
                return false;

            var version = reader.ReadUInt16();
            if (version != Version)
                return false;

            var machineType = reader.ReadString();
            if (machineType != expectedMachineType)
                return false;

            var romCrc32 = reader.ReadUInt32();
            return romCrc32 == expectedRomCrc32;
        }

        public static void WriteBytes(BinaryWriter writer, byte[] data)
        {
            var length = data?.Length ?? 0;
            writer.Write(length);
            if (length > 0)
                writer.Write(data);
        }

        public static byte[] ReadBytes(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            return length <= 0 ? [] : reader.ReadBytes(length);
        }

        public static void WriteUIntArray(BinaryWriter writer, uint[] data)
        {
            var length = data?.Length ?? 0;
            writer.Write(length);
            if (data == null)
                return;

            for (var i = 0; i < length; i++)
                writer.Write(data[i]);
        }

        public static void ReadUIntArray(BinaryReader reader, uint[] destination)
        {
            var length = reader.ReadInt32();
            for (var i = 0; i < length; i++)
            {
                var value = reader.ReadUInt32();
                if (i < destination.Length)
                    destination[i] = value;
            }
        }

        public static void WriteBoolArray(BinaryWriter writer, bool[] data)
        {
            var length = data?.Length ?? 0;
            writer.Write(length);
            if (data == null)
                return;

            for (var i = 0; i < length; i++)
                writer.Write(data[i]);
        }

        public static void ReadBoolArray(BinaryReader reader, bool[] destination)
        {
            var length = reader.ReadInt32();
            for (var i = 0; i < length; i++)
            {
                var value = reader.ReadBoolean();
                if (i < destination.Length)
                    destination[i] = value;
            }
        }
    }
}
