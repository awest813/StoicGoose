using StoicGoose.Common.Localization;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Machines;
using StoicGoose.GLWindow.Interface.Windows;
using StoicGoose.ImGuiCommon.Handlers;
using StoicGoose.ImGuiCommon.Widgets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace StoicGoose.GLWindow
{
    partial class MainWindow
    {
        const int maxRecentFiles = 15;
        const int maxScreenSizeFactor = 5;

        InputSettingsWindow inputSettingsWindow = default;
        DisplayWindow displayWindow = default;
        DisassemblerWindow disassemblerWindow = default;
        SystemControllerStatusWindow systemControllerStatusWindow = default;
        DisplayControllerStatusWindow displayControllerStatusWindow = default;
        SoundControllerStatusWindow soundControllerStatusWindow = default;
        MemoryPatchWindow memoryPatchWindow = default;
        BreakpointWindow breakpointWindow = default;
        MemoryEditorWindow memoryEditorWindow = default;
        TilemapViewerWindow tilemapViewerWindow = default;
        WavRecorderWindow wavRecorderWindow = default;

        MenuItem fileMenu = default, emulationMenu = default, windowsMenu = default, optionsMenu = default, cheatsMenu = default, helpMenu = default;
        MenuItem recentFilesMenu = default;
        MessageBox aboutMessageBox = default, breakpointHitMessageBox = default;
        StatusBarItem statusMessageItem = default, statusRunningItem = default, statusFpsItem = default;
        FileDialog openRomDialog = default, selectBootstrapRomDialog = default, saveWavDialog = default;

        BackgroundLogo backgroundGoose = default;

        MenuHandler menuHandler = default;
        MessageBoxHandler messageBoxHandler = default;
        StatusBarHandler statusBarHandler = default;
        FileDialogHandler fileDialogHandler = default;

        private void InitializeUI()
        {
            inputSettingsWindow = new();

            displayWindow = new DisplayWindow() { WindowScale = Program.Configuration.DisplaySize };

            disassemblerWindow = new DisassemblerWindow();
            disassemblerWindow.PauseEmulation += (s, e) => isPaused = true;
            disassemblerWindow.UnpauseEmulation += (s, e) => isPaused = false;

            systemControllerStatusWindow = new();
            displayControllerStatusWindow = new();
            soundControllerStatusWindow = new();

            memoryPatchWindow = new();
            breakpointWindow = new();
            memoryEditorWindow = new();

            tilemapViewerWindow = new();
            wavRecorderWindow = new(44100, 2)
            {
                BrowseRequested = () =>
                {
                    if (!string.IsNullOrEmpty(wavRecorderWindow.OutputPath))
                    {
                        saveWavDialog.InitialDirectory = Path.GetDirectoryName(wavRecorderWindow.OutputPath);
                        saveWavDialog.InitialFilename = Path.GetFileName(wavRecorderWindow.OutputPath);
                    }
                    saveWavDialog.IsOpen = true;
                }
            };

            void reinitMachineIfRunning()
            {
                if (machine != null && isRunning)
                {
                    CreateMachine(Program.Configuration.PreferredSystem);
                    LoadAndRunCartridge(Program.Configuration.LastRomLoaded);
                }
            }

            void initBootstrapRomDialog(Type machineType)
            {
                if (Program.Configuration.BootstrapFiles.TryGetValue(machineType.FullName, out string value))
                {
                    selectBootstrapRomDialog.InitialDirectory = Path.GetDirectoryName(value);
                    selectBootstrapRomDialog.InitialFilename = Path.GetFileName(value);
                }

                selectBootstrapRomDialog.Callback = (res, fn) =>
                {
                    if (res == ImGuiFileDialogResult.Okay)
                    {
                        Program.Configuration.BootstrapFiles[machineType.FullName] = fn;
                        reinitMachineIfRunning();
                    }
                };
            }

            recentFilesMenu = new(localization: "MainWindow.Menus.RecentFiles");
            RebuildRecentFilesMenu();

            fileMenu = new(localization: "MainWindow.Menus.File")
            {
                SubItems =
                [
                    new(localization: "MainWindow.Menus.Open", clickAction: (_) =>
                    {
                        if (!string.IsNullOrEmpty(Program.Configuration.LastRomLoaded))
                        {
                            openRomDialog.InitialDirectory = Path.GetDirectoryName(Program.Configuration.LastRomLoaded);
                            openRomDialog.InitialFilename = Path.GetFileName(Program.Configuration.LastRomLoaded);
                        }
                        openRomDialog.IsOpen = true;
                    })
                    { Shortcut = "Ctrl+O" },
                    new(localization: "MainWindow.Menus.SaveWAV",
                    clickAction: (_) => { wavRecorderWindow.IsWindowOpen = !wavRecorderWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = wavRecorderWindow.IsWindowOpen; })
                    { Shortcut = "Ctrl+W" },
                    new("-"),
                    recentFilesMenu,
                    new("-"),
                    new(localization: "MainWindow.Menus.Exit", clickAction: (_) => { Close(); })
                ]
            };

            emulationMenu = new(localization: "MainWindow.Menus.Emulation")
            {
                SubItems =
                [
                    new(localization: "MainWindow.Menus.Pause",
                    clickAction: (_) => { isPaused = !isPaused; if (isPaused) SaveCartridgeRam(); },
                    updateAction: (s) => { s.IsEnabled = isRunning; s.IsChecked = isPaused; })
                    { Shortcut = "Ctrl+P" },
                    new(localization: "MainWindow.Menus.Reset",
                    clickAction: (_) => { if (isRunning) { SaveVolatileData(); machine?.Reset(); } },
                    updateAction: (s) => { s.IsEnabled = isRunning; })
                    { Shortcut = "Ctrl+R" },
                    new("-"),
                    new(localization: "MainWindow.Menus.SaveState",
                    clickAction: (_) => SaveEmulatorState(),
                    updateAction: (s) => { s.IsEnabled = isRunning; })
                    { Shortcut = "F5" },
                    new(localization: "MainWindow.Menus.LoadState",
                    clickAction: (_) => LoadEmulatorState(),
                    updateAction: (s) => { s.IsEnabled = isRunning; })
                    { Shortcut = "F7" },
                    new("-"),
                    new(localization: "MainWindow.Menus.Shutdown",
                    clickAction: (_) => { if (isRunning) { SaveVolatileData(); machine?.Shutdown(); displayTexture.Fill(0, 0, 0, 255); statusMessageItem.Label = Localizer.GetString("MainWindow.StatusMessageShutdown"); isRunning = false; } },
                    updateAction: (s) => { s.IsEnabled = isRunning; })
                ]
            };

            windowsMenu = new(localization: "MainWindow.Menus.Windows")
            {
                SubItems =
                [
                    new(localization: "MainWindow.Menus.Display",
                    clickAction: (_) => { displayWindow.IsWindowOpen = !displayWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = displayWindow.IsWindowOpen; }),
                    new("-"),
                    new(localization: "MainWindow.Menus.Disassembler",
                    clickAction: (_) => { disassemblerWindow.IsWindowOpen = !disassemblerWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = disassemblerWindow.IsWindowOpen; }),
                    new(localization: "MainWindow.Menus.MemoryEditor",
                    clickAction : (_) => { memoryEditorWindow.IsWindowOpen = !memoryEditorWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = memoryEditorWindow.IsWindowOpen; }),
                    new(localization: "MainWindow.Menus.SystemControllers")
                    {
                        SubItems =
                        [
                            new(localization: "MainWindow.Menus.SystemController",
                            clickAction : (_) => { systemControllerStatusWindow.IsWindowOpen = !systemControllerStatusWindow.IsWindowOpen; },
                            updateAction: (s) => { s.IsChecked = systemControllerStatusWindow.IsWindowOpen; }),
                            new(localization: "MainWindow.Menus.DisplayController",
                            clickAction : (_) => { displayControllerStatusWindow.IsWindowOpen = !displayControllerStatusWindow.IsWindowOpen; },
                            updateAction: (s) => { s.IsChecked = displayControllerStatusWindow.IsWindowOpen; }),
                            new(localization: "MainWindow.Menus.SoundController",
                            clickAction : (_) => { soundControllerStatusWindow.IsWindowOpen = !soundControllerStatusWindow.IsWindowOpen; },
                            updateAction: (s) => { s.IsChecked = soundControllerStatusWindow.IsWindowOpen; })
                        ]
                    },
                    new("-"),
                    new(localization: "MainWindow.Menus.TilemapViewer",
                    clickAction : (_) => { tilemapViewerWindow.IsWindowOpen = !tilemapViewerWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = tilemapViewerWindow.IsWindowOpen; }),
                    new("-"),
                    new(localization: "MainWindow.Menus.Breakpoints",
                    clickAction : (_) => { breakpointWindow.IsWindowOpen = !breakpointWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = breakpointWindow.IsWindowOpen; }),
                    new(localization: "MainWindow.Menus.MemoryPatches",
                    clickAction : (_) => { memoryPatchWindow.IsWindowOpen = !memoryPatchWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = memoryPatchWindow.IsWindowOpen; }),
                    new("-"),
                    new(localization: "MainWindow.Menus.Log",
                    clickAction: (_) => { logWindow.IsWindowOpen = !logWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = logWindow.IsWindowOpen; })
                ]
            };

            optionsMenu = new(localization: "MainWindow.Menus.Options")
            {
                SubItems =
                [
                    new(localization: "MainWindow.Menus.Language")
                    {
                        SubItems = [.. Localizer.GetSupportedLanguages().Select(x =>
                            new MenuItem(label: x.NativeName,
                            clickAction: (_) => { Thread.CurrentThread.CurrentUICulture = new(Program.Configuration.Language = x.TwoLetterISOLanguageName); LocalizeUI(true); },
                            updateAction: (s) => { s.IsChecked = Program.Configuration.Language == x.TwoLetterISOLanguageName; })
                        )]
                    },
                    new("-"),
                    new(localization: "MainWindow.Menus.PreferredSystem")
                    {
                        SubItems =
                        [
                            new(localization: "MainWindow.Menus.WonderSwan",
                            clickAction: (_) => { Program.Configuration.PreferredSystem = typeof(WonderSwan).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
                            updateAction: (s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwan).FullName; }),
                            new(localization: "MainWindow.Menus.WonderSwanColor",
                            clickAction: (_) => { Program.Configuration.PreferredSystem = typeof(WonderSwanColor).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
                            updateAction: (s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwanColor).FullName; })
                        ]
                    },
                    new("-"),
                    new(localization: "MainWindow.Menus.ScreenSize")
                    {
                        SubItems = [.. Enumerable.Range(1, maxScreenSizeFactor).Select(scale =>
                            new MenuItem(label: $"{scale}x",
                            clickAction: (_) => { displayWindow.WindowScale = Program.Configuration.DisplaySize = scale; },
                            updateAction: (s) => { s.IsChecked = displayWindow.WindowScale == scale; }))
                        ]
                    },
                    new(localization: "MainWindow.Menus.RotateScreen",
                    clickAction: (_) => { isVerticalOrientation = !isVerticalOrientation; inputHandler?.SetVerticalOrientation(isVerticalOrientation); },
                    updateAction: (s) => { s.IsChecked = isVerticalOrientation; s.IsEnabled = isRunning; }),
                    new("-"),
                    new(localization: "MainWindow.Menus.LimitFPS",
                    clickAction: (_) => { Program.Configuration.LimitFps = !Program.Configuration.LimitFps; },
                    updateAction: (s) => { s.IsChecked = Program.Configuration.LimitFps; }),
                    new(localization: "MainWindow.Menus.Mute",
                    clickAction: (_) => { soundHandler.SetMute(Program.Configuration.Mute = !Program.Configuration.Mute); },
                    updateAction: (s) => { s.IsChecked = Program.Configuration.Mute; }),
                    new(localization: "MainWindow.Menus.LowPassFilter",
                    clickAction: (_) => { soundHandler.SetLowPassFilter(Program.Configuration.LowPassFilter = !Program.Configuration.LowPassFilter); },
                    updateAction: (s) => { s.IsChecked = Program.Configuration.LowPassFilter; }),
                    new("-"),
                    new(localization: "MainWindow.Menus.UseBootstrapROMs",
                    clickAction: (_) => { Program.Configuration.UseBootstrap = !Program.Configuration.UseBootstrap; reinitMachineIfRunning(); },
                    updateAction: (s) => { s.IsChecked = Program.Configuration.UseBootstrap; }),
                    new(localization: "MainWindow.Menus.SelectBootstrapROM")
                    {
                        SubItems =
                        [
                            new(localization: "MainWindow.Menus.WonderSwan", clickAction: (_) =>
                            {
                                initBootstrapRomDialog(typeof(WonderSwan));
                                selectBootstrapRomDialog.IsOpen = true;
                            }),
                            new(localization: "MainWindow.Menus.WonderSwanColor", clickAction: (_) =>
                            {
                                initBootstrapRomDialog(typeof(WonderSwanColor));
                                selectBootstrapRomDialog.IsOpen = true;
                            })
                        ]
                    },
                    new("-"),
                    new(localization: "MainWindow.Menus.EnableBreakpoints",
                    clickAction: (_) => { ApplyMachineBreakpointHandlers(Program.Configuration.EnableBreakpoints = !Program.Configuration.EnableBreakpoints); },
                    updateAction: (s) => { s.IsChecked = Program.Configuration.EnableBreakpoints; }),
                    new(localization: "MainWindow.Menus.EnableMemoryPatches",
                    clickAction: (_) =>
                    {
                        Program.Configuration.EnablePatchCallbacks = !Program.Configuration.EnablePatchCallbacks;
                        Program.Configuration.EnableCheats = Program.Configuration.EnablePatchCallbacks;
                        ApplyMachinePatchHandlers(Program.Configuration.EnablePatchCallbacks);
                    },
                    updateAction: (s) => { s.IsChecked = Program.Configuration.EnablePatchCallbacks; }),
                    new("-"),
                    new(localization: "MainWindow.Menus.EnableAutoRemap",
                    clickAction: (_) => { inputHandler.SetEnableRemapping(Program.Configuration.AutoRemap = !Program.Configuration.AutoRemap); },
                    updateAction: (s) => { s.IsChecked = Program.Configuration.AutoRemap; }),
                    new(localization: "MainWindow.Menus.InputSettings",
                    clickAction: (_) => { inputSettingsWindow.IsWindowOpen = !inputSettingsWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = inputSettingsWindow.IsWindowOpen; })
                ]
            };

            cheatsMenu = new(localization: "MainWindow.Menus.Cheats")
            {
                SubItems =
                [
                    new(localization: "MainWindow.Menus.EnableCheats",
                    clickAction: (_) =>
                    {
                        Program.Configuration.EnableCheats = !Program.Configuration.EnableCheats;
                        Program.Configuration.EnablePatchCallbacks = Program.Configuration.EnableCheats;
                        ApplyMachinePatchHandlers(Program.Configuration.EnablePatchCallbacks);
                    },
                    updateAction: (s) => { s.IsChecked = Program.Configuration.EnableCheats; }),
                    new("-"),
                    new(localization: "MainWindow.Menus.CheatList",
                    clickAction: (_) => { memoryPatchWindow.IsWindowOpen = !memoryPatchWindow.IsWindowOpen; },
                    updateAction: (s) => { s.IsChecked = memoryPatchWindow.IsWindowOpen; s.IsEnabled = isRunning; })
                ]
            };

            helpMenu = new(localization: "MainWindow.Menus.Help")
            {
                SubItems =
                [
                    new(localization: "MainWindow.Menus.OpenDataFolder", clickAction: (_) =>
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = $"{Program.DataPath.TrimEnd(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}",
                            UseShellExecute = true
                        });
                    }),
                    new("-"),
                    new(localization: "MainWindow.Menus.About", clickAction: (_) => { aboutMessageBox.IsOpen = true; })
                ]
            };

            aboutMessageBox = new(
                "About",
                $"{Program.ProductName} {Program.GetVersionString(true)}\r\n" +
                $"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description}\r\n" +
                "\r\n" +
                $"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}\r\n" +
                $"{ThisAssembly.Git.RepositoryUrl}",
                "OK");

            breakpointHitMessageBox = new("Breakpoint Hit", string.Empty, "OK");

            statusMessageItem = new() { ShowSeparator = false };
            statusRunningItem = new() { Width = 100f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };
            statusFpsItem = new() { Width = 90f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };

            openRomDialog = new(ImGuiFileDialogType.Open)
            {
                Callback = (res, fn) =>
                {
                    if (res == ImGuiFileDialogResult.Okay && !string.IsNullOrEmpty(fn))
                        LoadAndRunCartridge(fn);
                }
            };

            selectBootstrapRomDialog = new(ImGuiFileDialogType.Open);

            saveWavDialog = new(ImGuiFileDialogType.Save)
            {
                Callback = (res, fn) =>
                {
                    if (res == ImGuiFileDialogResult.Okay && !string.IsNullOrEmpty(fn))
                    {
                        wavRecorderWindow.OutputPath = fn;
                        wavRecorderWindow.IsWindowOpen = true;
                    }
                }
            };

            backgroundGoose = new()
            {
                Texture = new(Resources.GetEmbeddedRgbaFile("Assets.Goose-Logo.rgba")),
                Positioning = BackgroundLogoPositioning.BottomRight,
                Offset = new(-32f),
                Scale = new(0.5f),
                Alpha = 32
            };

            menuHandler = new(fileMenu, emulationMenu, windowsMenu, optionsMenu, cheatsMenu, helpMenu);
            messageBoxHandler = new(aboutMessageBox, breakpointHitMessageBox);
            statusBarHandler = new();
            fileDialogHandler = new(openRomDialog, selectBootstrapRomDialog, saveWavDialog);

            Log.WriteEvent(LogSeverity.Information, this, "User interface initialized.");
        }

        private void RebuildRecentFilesMenu()
        {
            var items = new List<MenuItem>
            {
                new(localization: "MainWindow.Menus.ClearRecent",
                    clickAction: (_) =>
                    {
                        Program.Configuration.RecentFiles.Clear();
                        RebuildRecentFilesMenu();
                        LocalizeUI();
                    },
                    updateAction: (s) => { s.IsEnabled = Program.Configuration.RecentFiles.Count != 0; }),
                new("-")
            };

            if (Program.Configuration.RecentFiles.Count == 0)
                items.Add(new(label: "-") { IsEnabled = false });
            else
            {
                foreach (var file in Program.Configuration.RecentFiles)
                {
                    var filename = file;
                    items.Add(new(label: filename, clickAction: (_) => LoadAndRunCartridge(filename)));
                }
            }

            recentFilesMenu.SubItems = [.. items];
            recentFilesMenu.Label = Localizer.GetString(recentFilesMenu.Localization);
            foreach (var item in recentFilesMenu.SubItems.Where(x => !string.IsNullOrEmpty(x.Localization)))
                item.Label = Localizer.GetString(item.Localization);
        }

        private static void AddToRecentFiles(string filename)
        {
            if (Program.Configuration.RecentFiles.Contains(filename))
            {
                Program.Configuration.RecentFiles.Remove(filename);
                Program.Configuration.RecentFiles.Insert(0, filename);
            }
            else
            {
                Program.Configuration.RecentFiles.Insert(0, filename);
                if (Program.Configuration.RecentFiles.Count > maxRecentFiles)
                    Program.Configuration.RecentFiles.RemoveAt(Program.Configuration.RecentFiles.Count - 1);
            }
        }

        private void LocalizeUI(bool announceLanguageChange = false)
        {
            static void localizeMenus(params MenuItem[] menuItems)
            {
                foreach (var menuItem in menuItems.Where(x => !string.IsNullOrEmpty(x.Localization)))
                {
                    menuItem.Label = Localizer.GetString(menuItem.Localization);
                    localizeMenus([.. menuItem.SubItems.Where(x => !string.IsNullOrEmpty(x.Localization))]);
                }
            }

            localizeMenus(fileMenu, emulationMenu, windowsMenu, optionsMenu, cheatsMenu, helpMenu);

            if (announceLanguageChange)
                statusMessageItem.Label = Localizer.GetString("MainWindow.LanguageChanged", new { Language = Thread.CurrentThread.CurrentUICulture.NativeName });

            aboutMessageBox.Title = Localizer.GetString("MainWindow.AboutTitle");
            aboutMessageBox.Buttons = [Localizer.GetString("MainWindow.OkayButton")];
            breakpointHitMessageBox.Title = Localizer.GetString("MainWindow.BreakpointHitTitle");
            breakpointHitMessageBox.Buttons = [Localizer.GetString("MainWindow.OkayButton")];

            openRomDialog.Title = Localizer.GetString("MainWindow.Dialogs.OpenROMTitle");
            openRomDialog.Filter = Localizer.GetString("MainWindow.Dialogs.ROMFilter");

            selectBootstrapRomDialog.Title = Localizer.GetString("MainWindow.Dialogs.SelectBootstrapROMTitle");
            selectBootstrapRomDialog.Filter = Localizer.GetString("MainWindow.Dialogs.ROMFilter");

            saveWavDialog.Title = Localizer.GetString("MainWindow.Dialogs.SaveWAVTitle");
            saveWavDialog.Filter = Localizer.GetString("MainWindow.Dialogs.WAVFilter");

            FileDialogHandler.OpenButtonLabel = Localizer.GetString("FileDialogHandler.OpenButton");
            FileDialogHandler.SaveButtonLabel = Localizer.GetString("FileDialogHandler.SaveButton");
            FileDialogHandler.CancelButtonLabel = Localizer.GetString("FileDialogHandler.CancelButton");

            Log.WriteEvent(LogSeverity.Information, this, $"UI localization to {Thread.CurrentThread.CurrentUICulture.DisplayName} applied.");
        }
    }
}
