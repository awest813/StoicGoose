using StoicGoose.Common.Attributes;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Cartridges;
using StoicGoose.Core.CPU;
using StoicGoose.Core.Display;
using StoicGoose.Core.EEPROMs;
using StoicGoose.Core.Interfaces;
using StoicGoose.Core.SaveStates;
using StoicGoose.Core.Serial;
using StoicGoose.Core.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Machines
{
    public abstract class MachineCommon : IMachine, IMemoryAccessComponent, IPortAccessComponent
    {
        // http://daifukkat.su/docs/wsman/

        public abstract string Manufacturer { get; }
        public abstract string Model { get; }

        public int ScreenWidth => DisplayControllerCommon.ScreenWidth;
        public int ScreenHeight => DisplayControllerCommon.ScreenHeight;
        public double RefreshRate => DisplayControllerCommon.VerticalClock;
        public string GameControls => "Start, B, A, X1, X2, X3, X4, Y1, Y2, Y3, Y4";
        public string HardwareControls => "Volume";
        public string VerticalControlRemap => "X1=Y2, X2=Y3, X3=Y4, X4=Y1, Y1=X2, Y2=X3, Y3=X4, Y4=X1";

        public abstract string InternalEepromDefaultUsername { get; }
        public abstract Dictionary<ushort, byte> InternalEepromDefaultData { get; }

        public virtual bool IsSphinxCpu => isWSCOrGreater;

        public const double MasterClock = 12288000.0; /* 12.288 MHz xtal */
        public const double CpuClock = MasterClock / 4.0; /* /4 = 3.072 MHz */

        public abstract int InternalRamSize { get; }
        public uint InternalRamMask { get; protected set; } = 0;
        public byte[] InternalRam { get; protected set; } = default;

        public Cartridge Cartridge { get; protected set; } = new();
        public V30MZ Cpu { get; protected set; } = default;
        public DisplayControllerCommon DisplayController { get; protected set; } = default;
        public SoundControllerCommon SoundController { get; protected set; } = default;
        public EEPROM InternalEeprom { get; protected set; } = default;
        public Bootstrap BootstrapRom { get; protected set; } = default;
        public SerialPort Serial { get; protected set; } = default;

        public abstract int InternalEepromSize { get; }
        public abstract int InternalEepromAddressBits { get; }

        public abstract int BootstrapRomAddress { get; }
        public abstract int BootstrapRomSize { get; }

        public Func<(List<string> buttonsPressed, List<string> buttonsHeld)> ReceiveInput { get; set; } = default;

        public Func<uint, byte, byte> ReadMemoryCallback { get; set; } = default;
        public Action<uint, byte> WriteMemoryCallback { get; set; } = default;
        public Func<ushort, byte, byte> ReadPortCallback { get; set; } = default;
        public Action<ushort, byte> WritePortCallback { get; set; } = default;
        public Func<bool> RunStepCallback { get; set; } = default;

        protected bool cancelFrameExecution = false;
        public bool IsPoweredOff { get; private set; }

        public int CurrentClockCyclesInLine { get; protected set; } = 0;
        public int CurrentClockCyclesInFrame { get; protected set; } = 0;

        public int TotalClockCyclesInFrame { get; protected set; } = 0;

        /* REG_HW_FLAGS */
        protected bool cartEnable, isWSCOrGreater, is16BitExtBus, cartRom1CycleSpeed, builtInSelfTestOk;
        /* REG_KEYPAD */
        protected bool keypadYEnable, keypadXEnable, keypadButtonEnable;
        /* REG_INT_xxx */
        protected byte interruptBase, interruptEnable, interruptStatus;

        public bool IsBootstrapLoaded { get; private set; } = false;
        public bool UseBootstrap { get; set; } = false;

        public virtual void Initialize()
        {
            if (InternalRamSize == -1) throw new Exception("Internal RAM size not set");

            InternalRamMask = (uint)(InternalRamSize - 1);
            InternalRam = new byte[InternalRamSize];

            Cpu = new(this);
            InternalEeprom = new(InternalEepromSize, InternalEepromAddressBits);
            BootstrapRom = new(BootstrapRomSize);
            Serial = new();

            InitializeEepromToDefaults();

            Log.WriteEvent(LogSeverity.Information, this, "Machine initialized.");
        }

        public virtual void Reset()
        {
            for (var i = 0; i < InternalRam.Length; i++) InternalRam[i] = 0;

            Cartridge?.Reset();
            Cpu?.Reset();
            DisplayController?.Reset();
            SoundController?.Reset();
            InternalEeprom?.Reset();
            Serial?.Reset();

            CurrentClockCyclesInFrame = 0;
            CurrentClockCyclesInLine = 0;
            TotalClockCyclesInFrame = (int)Math.Round(CpuClock / DisplayControllerCommon.VerticalClock);

            ResetRegisters();

            IsPoweredOff = false;

            Log.WriteEvent(LogSeverity.Information, this, "Machine reset.");
        }

        public virtual void ResetRegisters()
        {
            cartEnable = !IsBootstrapLoaded || !UseBootstrap;
            is16BitExtBus = true;
            cartRom1CycleSpeed = false;
            builtInSelfTestOk = true;

            keypadYEnable = keypadXEnable = keypadButtonEnable = false;

            interruptBase = interruptEnable = interruptStatus = 0;
        }

        public virtual void Shutdown()
        {
            Cartridge?.Shutdown();
            Cpu?.Shutdown();
            DisplayController?.Shutdown();
            SoundController?.Shutdown();
            InternalEeprom?.Shutdown();
            Serial?.Shutdown();

            Log.WriteEvent(LogSeverity.Information, this, "Machine shutdown.");
        }

        protected virtual void InitializeEepromToDefaults()
        {
            var data = ConvertUsernameForEeprom(InternalEepromDefaultUsername);
            for (var i = 0; i < data.Length; i++) InternalEeprom.Program(0x60 + i, data[i]); // Username (0x60-0x6F, max 16 characters)
            foreach (var (address, value) in InternalEepromDefaultData) InternalEeprom.Program(address, value);
            AssignSwanIdIfUnset();
        }

        protected void AssignSwanIdIfUnset()
        {
            var contents = InternalEeprom.GetContents();
            if (contents[0x7A] != 0 || contents[0x7B] != 0) return;

            var swanId = Random.Shared.Next(1, 0x10000);
            InternalEeprom.Program(0x7A, (byte)(swanId & 0xFF));
            InternalEeprom.Program(0x7B, (byte)(swanId >> 8));
        }

        private static byte[] ConvertUsernameForEeprom(string name)
        {
            var data = new byte[16];
            for (var i = 0; i < data.Length; i++)
            {
                var c = i < name.Length ? name[i] : ' ';
                if (c == ' ') data[i] = (byte)(c - ' ' + 0x00);
                else if (c >= '0' && c <= '9') data[i] = (byte)(c - '0' + 0x01);
                else if (c >= 'A' && c <= 'Z') data[i] = (byte)(c - 'A' + 0x0B);
                else if (c >= 'a' && c <= 'b') data[i] = (byte)(c - 'A' + 0x0B);
                else if (c == '♥') data[i] = (byte)(c - '♥' + 0x25);
                else if (c == '♪') data[i] = (byte)(c - '♪' + 0x26);
                else if (c == '+') data[i] = (byte)(c - '+' + 0x27);
                else if (c == '-') data[i] = (byte)(c - '-' + 0x28);
                else if (c == '?') data[i] = (byte)(c - '?' + 0x29);
                else if (c == '.') data[i] = (byte)(c - '.' + 0x2A);
                else data[i] = 0x00;
            }
            return data;
        }

        public void RunFrame()
        {
            while (CurrentClockCyclesInFrame < TotalClockCyclesInFrame)
            {
                RunLine();

                if (cancelFrameExecution) return;
            }

            CurrentClockCyclesInFrame -= TotalClockCyclesInFrame;

            _ = ReceiveInput?.Invoke();
        }

        public void RunLine()
        {
            while (CurrentClockCyclesInLine < DisplayControllerCommon.HorizontalTotal)
            {
                RunStep();

                if (cancelFrameExecution) return;
            }

            CurrentClockCyclesInFrame += CurrentClockCyclesInLine;
            CurrentClockCyclesInLine = 0;
        }

        public abstract void RunStep();

        protected void RaiseInterrupt(int number)
        {
            if (!IsBitSet(interruptEnable, number)) return;
            ChangeBit(ref interruptStatus, number, true);
        }

        protected void LowerInterrupt(int number)
        {
            ChangeBit(ref interruptStatus, number, false);
        }

        protected void PowerOff()
        {
            IsPoweredOff = true;
            Cpu.IsHalted = true;
            cancelFrameExecution = true;
        }

        protected void HandleInterrupts()
        {
            if (!Cpu.IsFlagSet(V30MZ.Flags.InterruptEnable)) return;

            for (var i = 7; i >= 0; i--)
            {
                if (!IsBitSet(interruptStatus, i)) continue;

                Cpu.IsHalted = false;
                Cpu.Interrupt((interruptBase & 0b11111000) | i);
                return;
            }
        }

        public void LoadBootstrap(byte[] data)
        {
            BootstrapRom.LoadRom(data);

            IsBootstrapLoaded = true;
        }

        public void LoadInternalEeprom(byte[] data)
        {
            InternalEeprom.LoadContents(data);
            AssignSwanIdIfUnset();
        }

        public void LoadRom(byte[] data)
        {
            Cartridge.LoadRom(data);
        }

        public void LoadSaveData(byte[] data)
        {
            if (Cartridge.Metadata.IsSramSave)
                Cartridge.LoadSram(data);
            else if (Cartridge.Metadata.IsEepromSave)
                Cartridge.LoadEeprom(data);
        }

        public void LoadRtcState(byte[] data) => Cartridge.LoadRtcState(data);

        public bool HasRtcSave => Cartridge.HasRtc;

        public byte[] GetInternalEeprom()
        {
            return InternalEeprom.GetContents();
        }

        public byte[] GetSaveData()
        {
            if (Cartridge.Metadata != null)
            {
                if (Cartridge.Metadata.IsSramSave)
                    return Cartridge.GetSram();
                else if (Cartridge.Metadata.IsEepromSave)
                    return Cartridge.GetEeprom();
            }

            return [];
        }

        public byte[] GetRtcState() => Cartridge.GetRtcState() ?? [];

        public byte[] GetSaveState()
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                SaveStateIO.WriteHeader(writer, GetType().FullName, Cartridge?.Crc32 ?? 0);
                ExportState(writer);
            }
            return stream.ToArray();
        }

        public bool LoadSaveState(byte[] data)
        {
            if (data == null || data.Length == 0)
                return false;

            try
            {
                using var stream = new MemoryStream(data, false);
                using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, false);
                if (!SaveStateIO.TryReadHeader(reader, GetType().FullName, Cartridge?.Crc32 ?? 0))
                    return false;

                ImportState(reader);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteEvent(LogSeverity.Error, this, $"Failed to load save state: {ex.Message}");
                return false;
            }
        }

        protected virtual void ExportState(BinaryWriter writer)
        {
            writer.Write(CurrentClockCyclesInLine);
            writer.Write(CurrentClockCyclesInFrame);
            writer.Write(TotalClockCyclesInFrame);
            writer.Write(IsPoweredOff);
            writer.Write(cancelFrameExecution);
            writer.Write(UseBootstrap);
            writer.Write(cartEnable);
            writer.Write(isWSCOrGreater);
            writer.Write(is16BitExtBus);
            writer.Write(cartRom1CycleSpeed);
            writer.Write(builtInSelfTestOk);
            writer.Write(keypadYEnable);
            writer.Write(keypadXEnable);
            writer.Write(keypadButtonEnable);
            writer.Write(interruptBase);
            writer.Write(interruptEnable);
            writer.Write(interruptStatus);

            SaveStateIO.WriteBytes(writer, InternalRam);
            Cpu.ExportState(writer);
            DisplayController.ExportState(writer);
            SoundController.ExportState(writer);
            InternalEeprom.ExportState(writer);
            Serial.ExportState(writer);
            Cartridge.ExportState(writer);
        }

        protected virtual void ImportState(BinaryReader reader)
        {
            CurrentClockCyclesInLine = reader.ReadInt32();
            CurrentClockCyclesInFrame = reader.ReadInt32();
            TotalClockCyclesInFrame = reader.ReadInt32();
            IsPoweredOff = reader.ReadBoolean();
            cancelFrameExecution = reader.ReadBoolean();
            UseBootstrap = reader.ReadBoolean();
            cartEnable = reader.ReadBoolean();
            isWSCOrGreater = reader.ReadBoolean();
            is16BitExtBus = reader.ReadBoolean();
            cartRom1CycleSpeed = reader.ReadBoolean();
            builtInSelfTestOk = reader.ReadBoolean();
            keypadYEnable = reader.ReadBoolean();
            keypadXEnable = reader.ReadBoolean();
            keypadButtonEnable = reader.ReadBoolean();
            interruptBase = reader.ReadByte();
            interruptEnable = reader.ReadByte();
            interruptStatus = reader.ReadByte();

            var ram = SaveStateIO.ReadBytes(reader);
            if (InternalRam != null && ram.Length != 0)
                Buffer.BlockCopy(ram, 0, InternalRam, 0, Math.Min(ram.Length, InternalRam.Length));

            Cpu.ImportState(reader);
            DisplayController.ImportState(reader);
            SoundController.ImportState(reader);
            InternalEeprom.ImportState(reader);
            Serial.ImportState(reader);
            Cartridge.ImportState(reader);

            Cpu.IsHalted = IsPoweredOff || Cpu.IsHalted;
        }

        public byte ReadMemory(uint address)
        {
            byte retVal;

            if (!cartEnable && BootstrapRom != null && address >= BootstrapRomAddress)
            {
                /* Bootstrap enabled */
                retVal = BootstrapRom.ReadMemory(address);
            }
            else
            {
                address &= 0xFFFFF;

                if ((address & 0xF0000) == 0x00000)
                {
                    /* Internal RAM -- returns 0x90 if unmapped */
                    if (address < InternalRamSize)
                        retVal = InternalRam[address & InternalRamMask];
                    else
                        retVal = 0x90;
                }
                else
                {
                    /* Cartridge */
                    retVal = Cartridge.ReadMemory(address);
                }
            }

            if (ReadMemoryCallback != null)
                retVal = ReadMemoryCallback.Invoke(address, retVal);

            return retVal;
        }

        public void WriteMemory(uint address, byte value)
        {
            WriteMemoryCallback?.Invoke(address, value);

            address &= 0xFFFFF;
            if ((address & 0xF0000) == 0x00000)
            {
                /* Internal RAM -- no effect if unmapped */
                if (address < InternalRamSize)
                    InternalRam[address & InternalRamMask] = value;
            }
            else if ((address & 0xF0000) == 0x10000)
            {
                /* Cartridge -- SRAM only, other writes not emitted */
                Cartridge.WriteMemory(address, value);
            }
        }

        public abstract byte ReadPort(ushort port);
        public abstract void WritePort(ushort port, byte value);

        [Port("REG_HW_FLAGS", 0x0A0)]
        [BitDescription("BIOS lockout; is cartridge mapped?", 0)]
        public bool CartEnable => cartEnable;
        [Port("REG_HW_FLAGS", 0x0A0)]
        [BitDescription("System type; is WSC or greater?", 1)]
        public bool IsWSCOrGreater => isWSCOrGreater;
        [Port("REG_HW_FLAGS", 0x0A0)]
        [BitDescription("External bus width; is 16-bit bus?", 2)]
        public bool Is16BitExtBus => is16BitExtBus;
        [Port("REG_HW_FLAGS", 0x0A0)]
        [BitDescription("Cartridge ROM speed; is 1-cycle?", 3)]
        public bool CartRom1CycleSpeed => cartRom1CycleSpeed;
        [Port("REG_HW_FLAGS", 0x0A0)]
        [BitDescription("Built-in self test passed", 7)]
        public bool BuiltInSelfTestOk => builtInSelfTestOk;

        [Port("REG_KEYPAD", 0x0B5)]
        [BitDescription("Y keys check enabled", 4)]
        public bool KeypadYEnable => keypadYEnable;
        [Port("REG_KEYPAD", 0x0B5)]
        [BitDescription("X keys check enabled", 5)]
        public bool KeypadXEnable => keypadXEnable;
        [Port("REG_KEYPAD", 0x0B5)]
        [BitDescription("Button check enabled", 6)]
        public bool KeypadButtonEnable => keypadButtonEnable;

        [Port("REG_INT_BASE", 0x0B0)]
        public abstract byte InterruptBase { get; }
        [Port("REG_INT_ENABLE", 0x0B2)]
        [BitDescription("Interrupt enable bitmask", 4)]
        [Format("X2")]
        public byte InterruptEnable => interruptEnable;
        [Port("REG_INT_STATUS", 0x0B4)]
        [BitDescription("Interrupt status bitmask", 4)]
        [Format("X2")]
        public byte InterruptStatus => interruptStatus;
    }
}
