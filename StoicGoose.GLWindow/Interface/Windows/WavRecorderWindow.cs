using ImGuiNET;
using StoicGoose.Common.IO;
using StoicGoose.Common.Localization;
using StoicGoose.ImGuiCommon.Windows;
using System;
using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface.Windows
{
    public class WavRecorderWindow : WindowBase
    {
        readonly int sampleRate, numChannels;
        WaveFileWriter waveFileWriter;

        public string OutputPath { get; set; } = string.Empty;
        public bool IsRecording { get; private set; }
        public Action BrowseRequested { get; set; }

        public WavRecorderWindow(int sampleRate, int numChannels) : base("Save WAV", new(420f, 180f), ImGuiCond.FirstUseEver)
        {
            this.sampleRate = sampleRate;
            this.numChannels = numChannels;
        }

        public void EnqueueSamples(short[] samples)
        {
            if (IsRecording)
                waveFileWriter?.Write(samples);
        }

        public void StopRecording()
        {
            if (!IsRecording)
                return;

            waveFileWriter?.Save();
            waveFileWriter?.Dispose();
            waveFileWriter = null;
            IsRecording = false;
        }

        protected override void DrawWindow(object userData)
        {
            if (ImGui.Begin(Localizer.GetString("WavRecorder.Title"), ref isWindowOpen))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

                ImGui.Text(Localizer.GetString("WavRecorder.Path"));
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 110f);
                var path = OutputPath ?? string.Empty;
                if (ImGui.InputText("##wav-path", ref path, 512))
                    OutputPath = path;
                ImGui.SameLine();
                if (ImGui.Button(Localizer.GetString("WavRecorder.Browse"), new NumericsVector2(100f, 0f)))
                    BrowseRequested?.Invoke();

                ImGui.Dummy(new NumericsVector2(0f, 8f));
                ImGui.Text(IsRecording ? Localizer.GetString("WavRecorder.Recording") : Localizer.GetString("WavRecorder.Idle"));
                ImGui.Dummy(new NumericsVector2(0f, 8f));

                var canStart = !string.IsNullOrWhiteSpace(OutputPath) && !IsRecording;
                ImGui.BeginDisabled(!canStart);
                if (ImGui.Button(Localizer.GetString("WavRecorder.Start"), new NumericsVector2(120f, 0f)))
                {
                    waveFileWriter?.Dispose();
                    waveFileWriter = new WaveFileWriter(OutputPath, sampleRate, numChannels);
                    IsRecording = true;
                }
                ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.BeginDisabled(!IsRecording);
                if (ImGui.Button(Localizer.GetString("WavRecorder.Stop"), new NumericsVector2(120f, 0f)))
                    StopRecording();
                ImGui.EndDisabled();

                ImGui.PopStyleVar();
                EndWindow();
            }
        }
    }
}
