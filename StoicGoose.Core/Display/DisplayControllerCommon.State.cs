using StoicGoose.Core.SaveStates;
using System.IO;

namespace StoicGoose.Core.Display
{
    public abstract partial class DisplayControllerCommon
    {
        public virtual void ExportState(BinaryWriter writer)
        {
            SaveStateIO.WriteUIntArray(writer, spriteData);
            SaveStateIO.WriteUIntArray(writer, spriteDataNextFrame);
            SaveStateIO.WriteUIntArray(writer, activeSpritesOnLine);
            SaveStateIO.WriteBoolArray(writer, isUsedBySCR2);

            writer.Write(spriteCountNextFrame);
            writer.Write(activeSpriteCountOnLine);
            writer.Write(cycleCount);

            writer.Write(scr1Enable);
            writer.Write(scr2Enable);
            writer.Write(sprEnable);
            writer.Write(sprWindowEnable);
            writer.Write(scr2WindowDisplayOutside);
            writer.Write(scr2WindowEnable);
            writer.Write(backColorIndex);
            writer.Write(lineCurrent);
            writer.Write(lineCompare);
            writer.Write(sprBase);
            writer.Write(sprFirst);
            writer.Write(sprCount);
            writer.Write(scr1Base);
            writer.Write(scr2Base);
            writer.Write(scr2WinX0);
            writer.Write(scr2WinY0);
            writer.Write(scr2WinX1);
            writer.Write(scr2WinY1);
            writer.Write(sprWinX0);
            writer.Write(sprWinY0);
            writer.Write(sprWinX1);
            writer.Write(sprWinY1);
            writer.Write(scr1ScrollX);
            writer.Write(scr1ScrollY);
            writer.Write(scr2ScrollX);
            writer.Write(scr2ScrollY);
            writer.Write(lcdActive);
            writer.Write(iconSleep);
            writer.Write(iconVertical);
            writer.Write(iconHorizontal);
            writer.Write(iconAux1);
            writer.Write(iconAux2);
            writer.Write(iconAux3);
            writer.Write(vtotal);
            writer.Write(vsync);

            writer.Write(palMonoPools);
            writer.Write(palMonoData.Length);
            for (var i = 0; i < palMonoData.Length; i++)
                writer.Write(palMonoData[i]);

            hBlankTimer.ExportState(writer);
            vBlankTimer.ExportState(writer);
        }

        public virtual void ImportState(BinaryReader reader)
        {
            SaveStateIO.ReadUIntArray(reader, spriteData);
            SaveStateIO.ReadUIntArray(reader, spriteDataNextFrame);
            SaveStateIO.ReadUIntArray(reader, activeSpritesOnLine);
            SaveStateIO.ReadBoolArray(reader, isUsedBySCR2);

            spriteCountNextFrame = reader.ReadInt32();
            activeSpriteCountOnLine = reader.ReadInt32();
            cycleCount = reader.ReadInt32();

            scr1Enable = reader.ReadBoolean();
            scr2Enable = reader.ReadBoolean();
            sprEnable = reader.ReadBoolean();
            sprWindowEnable = reader.ReadBoolean();
            scr2WindowDisplayOutside = reader.ReadBoolean();
            scr2WindowEnable = reader.ReadBoolean();
            backColorIndex = reader.ReadByte();
            lineCurrent = reader.ReadInt32();
            lineCompare = reader.ReadInt32();
            sprBase = reader.ReadInt32();
            sprFirst = reader.ReadInt32();
            sprCount = reader.ReadInt32();
            scr1Base = reader.ReadInt32();
            scr2Base = reader.ReadInt32();
            scr2WinX0 = reader.ReadInt32();
            scr2WinY0 = reader.ReadInt32();
            scr2WinX1 = reader.ReadInt32();
            scr2WinY1 = reader.ReadInt32();
            sprWinX0 = reader.ReadInt32();
            sprWinY0 = reader.ReadInt32();
            sprWinX1 = reader.ReadInt32();
            sprWinY1 = reader.ReadInt32();
            scr1ScrollX = reader.ReadInt32();
            scr1ScrollY = reader.ReadInt32();
            scr2ScrollX = reader.ReadInt32();
            scr2ScrollY = reader.ReadInt32();
            lcdActive = reader.ReadBoolean();
            iconSleep = reader.ReadBoolean();
            iconVertical = reader.ReadBoolean();
            iconHorizontal = reader.ReadBoolean();
            iconAux1 = reader.ReadBoolean();
            iconAux2 = reader.ReadBoolean();
            iconAux3 = reader.ReadBoolean();
            vtotal = reader.ReadInt32();
            vsync = reader.ReadInt32();

            reader.Read(palMonoPools, 0, palMonoPools.Length);
            var palCount = reader.ReadInt32();
            for (var i = 0; i < palCount; i++)
            {
                var palette = reader.ReadBytes(4);
                if (i < palMonoData.Length)
                    palMonoData[i] = palette;
            }

            hBlankTimer.ImportState(reader);
            vBlankTimer.ImportState(reader);
        }
    }
}
