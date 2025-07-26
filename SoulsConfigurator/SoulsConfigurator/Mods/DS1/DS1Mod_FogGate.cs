using SoulsConfigurator.Helpers;
using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Models;
using SoulsConfigurator.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace SoulsConfigurator.Mods.DS1
{
    public class DS1Mod_FogGate : IMod, IConfigurableMod
    {
        public string Name => "DS1 Fog Gate Randomizer";
        public string ModFile => "DS1_FogGate_Randomizer.zip";

        private ModConfiguration? _configuration;
        private Dictionary<string, object>? _savedConfiguration;
        private string? _selectedPreset;
        private string? _executablePath; // Store the executable path

        public DS1Mod_FogGate()
        {
            InitializeConfiguration();
        }

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "DS1", ModFile);
            return File.Exists(sourcePath);
        }

        private void InitializeConfiguration()
        {
            _configuration = new ModConfiguration
            {
                ModName = Name,
                ExecutablePath = "FogMod.exe",
                WindowTitle = "DS1 Fog Gate Randomizer v0.3",
                Options = new List<ModConfigurationOption>
                {
                    // Randomized entrances group
                    new ModConfigurationOption
                    {
                        Name = "world",
                        DisplayName = "Traversable fog gates (non-boss)",
                        Description = "Randomize two-way fog gates",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "world",
                        DefaultValue = true,
                        GroupName = "Randomized entrances"
                    },
                    new ModConfigurationOption
                    {
                        Name = "boss",
                        DisplayName = "Boss fog gates",
                        Description = "Randomize fog gates to and from bosses",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "boss",
                        DefaultValue = true,
                        GroupName = "Randomized entrances"
                    },
                    new ModConfigurationOption
                    {
                        Name = "warp",
                        DisplayName = "Warps between areas",
                        Description = "Randomize warp destinations, like to/from Painted World",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "warp",
                        DefaultValue = true,
                        GroupName = "Randomized entrances"
                    },
                    new ModConfigurationOption
                    {
                        Name = "major",
                        DisplayName = "Major PvP fog gates",
                        Description = "Enable and randomize invasion fog gates separating major areas",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "major",
                        DefaultValue = false,
                        GroupName = "Randomized entrances"
                    },
                    new ModConfigurationOption
                    {
                        Name = "minor",
                        DisplayName = "Minor PvP fog gates",
                        Description = "Enable and randomize invasion fog gates usually separating off smaller areas",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "minor",
                        DefaultValue = false,
                        GroupName = "Randomized entrances"
                    },
                    new ModConfigurationOption
                    {
                        Name = "lordvessel",
                        DisplayName = "Lordvessel gates",
                        Description = "Randomize golden fog gates, in which case they are never dispelled",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "lordvessel",
                        DefaultValue = false,
                        GroupName = "Randomized entrances"
                    },

                    // Options group
                    new ModConfigurationOption
                    {
                        Name = "scale",
                        DisplayName = "Scale enemies and bosses",
                        Description = "Increase or decrease enemy health and damage based on distance from start",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "scale",
                        DefaultValue = true,
                        GroupName = "Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "lords",
                        DisplayName = "Require Lord Souls",
                        Description = "Require opening the kiln door to access Gwyn",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "lords",
                        DefaultValue = true,
                        GroupName = "Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "pacifist",
                        DisplayName = "Pacifist Mode",
                        Description = "Allow escaping boss fights without defeating bosses",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "pacifist",
                        DefaultValue = false,
                        GroupName = "Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "hard",
                        DisplayName = "Glitched logic",
                        Description = "Various glitches may be required, similar to Race Mode+ in item randomizer",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "hard",
                        DefaultValue = false,
                        GroupName = "Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "bboc",
                        DisplayName = "No-Fall Bed of Chaos",
                        Description = "BoC floor no longer crumbles. Not related to randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "bboc",
                        DefaultValue = false,
                        GroupName = "Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "unconnected",
                        DisplayName = "Disconnected fog gates",
                        Description = "If enabled, entering a fog gate you just exited can send you somewhere else",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "unconnected",
                        DefaultValue = false,
                        GroupName = "Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "start",
                        DisplayName = "Random start outside of Asylum",
                        Description = "Immediately warp away from Asylum, returning later through a fog gate",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "start",
                        DefaultValue = false,
                        GroupName = "Options"
                    },

                    // Other controls
                    new ModConfigurationOption
                    {
                        Name = "fixedseed",
                        DisplayName = "Seed",
                        Description = "Leave seed blank for a random seed",
                        ControlType = ModControlType.TextBox,
                        ControlName = "fixedseed",
                        DefaultValue = "",
                        GroupName = "General"
                    }
                }
            };
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                string fogDataPath = Path.Combine("Data", "DS1", "fog");
                
                // Create the fog directory if it doesn't exist
                if (!Directory.Exists(fogDataPath))
                {
                    Directory.CreateDirectory(fogDataPath);
                }
                
                // Extract to the fog subdirectory in Data folder
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS1", ModFile), fogDataPath);
                
                // If we have saved configuration, run the mod with it
                if (_savedConfiguration != null)
                {
                    RunWithConfiguration(_savedConfiguration, destPath);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Async version of TryInstallMod with status reporting capability
        /// </summary>
        public async Task<bool> TryInstallModAsync(string destPath, Action<string>? statusUpdater = null)
        {
            try
            {
                statusUpdater?.Invoke("Extracting DS1 Fog Gate Randomizer files...");
                
                string fogDataPath = Path.Combine("Data", "DS1", "fog");
                
                // Create the fog directory if it doesn't exist
                if (!Directory.Exists(fogDataPath))
                {
                    Directory.CreateDirectory(fogDataPath);
                }
                
                // Extract to the fog subdirectory in Data folder
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS1", ModFile), fogDataPath);
                
                // If we have saved configuration, run the mod with it
                if (_savedConfiguration != null)
                {
                    statusUpdater?.Invoke("Running Fog Gate Randomizer with configuration...");
                    statusUpdater?.Invoke("Please wait while the randomizer configures and runs...");
                    
                    bool result = await Task.Run(() => RunWithConfiguration(_savedConfiguration, destPath));
                    
                    if (result)
                    {
                        statusUpdater?.Invoke("Fog Gate Randomizer completed successfully!");
                    }
                    else
                    {
                        statusUpdater?.Invoke("Fog Gate Randomizer installation failed.");
                    }
                    
                    return result;
                }

                statusUpdater?.Invoke("Fog Gate Randomizer files extracted successfully!");
                return true;
            }
            catch (Exception ex)
            {
                statusUpdater?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }

        public bool TryRemoveMod(string destPath)
        {
            try
            {
                // First, run the executable and click "Restore backups" before deleting files
                if (!RestoreBackups())
                {
                    // If restore fails, still try to delete the directory
                    System.Diagnostics.Debug.WriteLine("Failed to restore backups, proceeding with directory deletion");
                }

                Process.GetProcessesByName("FogMod").ToList().ForEach(p => p.WaitForExit());

                string fogDataPath = Path.Combine("Data", "DS1", "fog");
                if (Directory.Exists(fogDataPath))
                {
                    Directory.Delete(fogDataPath, true);
                }
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public ModConfiguration GetUIConfiguration()
        {
            return _configuration ?? throw new InvalidOperationException("Configuration not initialized");
        }

        public List<UserPreset> GetUserPresets()
        {
            return UserPresetService.Instance.LoadPresets(Name);
        }

        public void SaveConfiguration(Dictionary<string, object> configuration)
        {
            _savedConfiguration = new Dictionary<string, object>(configuration);
        }

        public Dictionary<string, object>? GetSavedConfiguration()
        {
            return _savedConfiguration;
        }

        public string? GetSelectedPreset()
        {
            return _selectedPreset;
        }

        public void SetSelectedPreset(string? presetName)
        {
            _selectedPreset = presetName;
            
            // If a preset is selected, load its configuration
            if (!string.IsNullOrEmpty(presetName))
            {
                var preset = UserPresetService.Instance.GetPreset(Name, presetName);
                if (preset != null)
                {
                    SaveConfiguration(preset.OptionValues);
                }
            }
        }

        public bool RunWithConfiguration(Dictionary<string, object> configuration, string destPath)
        {
            try
            {
                if (_configuration == null)
                    return false;

                // Find the executable in the fog data directory
                string fogDataPath = Path.Combine("Data", "DS1", "fog");
                string exePath = Path.Combine(fogDataPath, _configuration.ExecutablePath);
                if (!File.Exists(exePath))
                {
                    // Try to find it recursively in the fog directory
                    if (Directory.Exists(fogDataPath))
                    {
                        string[] foundExes = Directory.GetFiles(fogDataPath, _configuration.ExecutablePath, SearchOption.AllDirectories);
                        if (foundExes.Length > 0)
                        {
                            exePath = foundExes[0];
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                // Store the executable path for later use
                var appExecutablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(appExecutablePath))
                    return false;
                    
                _executablePath = Path.Combine(appExecutablePath, exePath);

                // Modify the FogMod settings before launching
                if (!SetFogModSettings(configuration, destPath))
                {
                    return false;
                }

                // Launch the mod executable in UI mode (it will read the pre-configured settings)
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _executablePath,
                    WorkingDirectory = Path.GetDirectoryName(_executablePath),
                    UseShellExecute = true
                };

                using (Process? process = Process.Start(startInfo))
                {
                    if (process == null)
                        return false;

                    // Wait a moment for the application to start
                    Thread.Sleep(2000);

                    // Try to automate the randomize button click
                    return AutomateRandomizeButton(process);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ApplyUserPreset(string presetName, string destPath)
        {
            var preset = UserPresetService.Instance.GetPreset(Name, presetName);
            if (preset == null)
                return false;

            return RunWithConfiguration(preset.OptionValues, destPath);
        }

        private bool SetFogModSettings(Dictionary<string, object> configuration, string destPath)
        {
            try
            {
                // Generate the options string that FogMod expects
                string optionsString = GenerateFogModOptionsString(configuration);
                
                // Create the exe.config file with our settings
                string exePath = GetExecutablePath();
                if (string.IsNullOrEmpty(exePath))
                {
                    return false;
                }

                // Modify the FogMod user.config file to set the Options and Exe settings
                return ModifyFogModUserConfig(optionsString, exePath, Path.Combine(destPath, "DarkSoulsRemastered.exe"));
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GetExecutablePath()
        {
            try
            {
                // Use stored executable path if available
                if (!string.IsNullOrEmpty(_executablePath) && File.Exists(_executablePath))
                {
                    return _executablePath;
                }

                if (_configuration == null)
                    return "";

                // Try to find the executable in common locations
                string fogDataPath = Path.Combine("Data", "fog");
                string[] possiblePaths = {
                    Path.Combine(fogDataPath, _configuration.ExecutablePath),
                    Path.Combine(Environment.CurrentDirectory, "randomizer", _configuration.ExecutablePath),
                    Path.Combine(Environment.CurrentDirectory, _configuration.ExecutablePath)
                };

                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                // Try to find it recursively in the fog directory
                if (Directory.Exists(fogDataPath))
                {
                    string[] foundExes = Directory.GetFiles(fogDataPath, _configuration.ExecutablePath, SearchOption.AllDirectories);
                    if (foundExes.Length > 0)
                    {
                        return foundExes[0];
                    }
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        private string GenerateFogModOptionsString(Dictionary<string, object> configuration)
        {
            if (_configuration == null)
                return "";

            List<string> options = new List<string>();

            // Add boolean options that are enabled
            foreach (var option in _configuration.Options)
            {
                if (configuration.TryGetValue(option.Name, out object? value) && value != null)
                {
                    if (option.ControlType == ModControlType.CheckBox || option.ControlType == ModControlType.RadioButton)
                    {
                        if (Convert.ToBoolean(((JsonElement)value).ValueKind.ToString()))
                        {
                            options.Add(option.Name);
                        }
                    }
                }
            }

            return string.Join(" ", options);
        }

        private bool ModifyFogModUserConfig(string optionsString, string exePath, string pathDS1)
        {
            try
            {
                // FogMod stores settings in the standard .NET user settings location
                // We need to find and modify ALL user.config files for FogMod
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string fogModConfigPath = Path.Combine(appDataPath, "FogMod");

                // Find all user.config files and update them (mod might create new folders)
                if (Directory.Exists(fogModConfigPath))
                {
                    string[] configFiles = Directory.GetFiles(fogModConfigPath, "user.config", SearchOption.AllDirectories);
                    foreach (string configFilePath in configFiles)
                    {
                        File.Delete(configFilePath); // Delete existing user.config to avoid conflicts
                    }
                }

                return CreateFogModSettings(optionsString, exePath, pathDS1);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool CreateFogModSettings(string optionsString, string exePath, string pathDS1)
        {
            try
            {
                string configPath = exePath + ".config";

                // Get the seed
                string seed = "";
                if (_savedConfiguration != null && _savedConfiguration.TryGetValue("fixedseed", out object? seedValue) && seedValue != null)
                {
                    string seedText = seedValue.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(seedText) && uint.TryParse(seedText, out uint parsedSeed))
                    {
                        seed = parsedSeed.ToString();
                    }
                }
                
                // If no fixed seed, generate a random one
                if (string.IsNullOrEmpty(seed))
                {
                    seed = new Random().Next().ToString();
                }

                string configContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <configSections>
        <sectionGroup name=""userSettings"" type=""System.Configuration.UserSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" >
            <section name=""FogMod.Properties.Settings"" type=""System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" allowExeDefinition=""MachineToLocalUser"" requirePermission=""false"" />
        </sectionGroup>
    </configSections>
    <userSettings>
        <FogMod.Properties.Settings>
            <setting name=""Options"" serializeAs=""String"">
                <value>{System.Security.SecurityElement.Escape(optionsString)}</value>
            </setting>
            <setting name=""Exe"" serializeAs=""String"">
                <value>{pathDS1}</value>
            </setting>
            <setting name=""Language"" serializeAs=""String"">
                <value>ENGLISH</value>
            </setting>
        </FogMod.Properties.Settings>
    </userSettings>
</configuration>";

                File.WriteAllText(configPath, configContent);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ClearFogModSettings()
        {
            try
            {
                // Clear all FogMod settings after the mod has been applied
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string fogModConfigPath = Path.Combine(appDataPath, "FogMod");

                if (Directory.Exists(fogModConfigPath))
                {
                    // Delete the entire FogMod settings directory
                    Directory.Delete(fogModConfigPath, true);
                }
            }
            catch (Exception)
            {
                // Silently ignore errors when clearing settings
                // This is not critical to the mod functionality
            }
        }

        private bool IsRandomizationComplete(string runDir)
        {
            try
            {
                if (!Directory.Exists(runDir))
                    return false;

                // Get all log files in the runs directory
                var logFiles = Directory.GetFiles(runDir, "*.txt", SearchOption.TopDirectoryOnly);
                if (logFiles.Length == 0)
                    return false;

                // Get the most recent log file
                var latestLogFile = logFiles
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (latestLogFile == null || !latestLogFile.Exists)
                    return false;

                // Read the last line of the log file
                string lastLine = "";
                using (var reader = new StreamReader(latestLogFile.FullName))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            lastLine = line.Trim();
                    }
                }

                // Check if the last line starts with "Writing messages to"
                return lastLine.StartsWith("Writing messages to ", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool AutomateRandomizeButton(Process process)
        {
            try
            {
                // Wait for the window to be ready
                process.WaitForInputIdle();
                Thread.Sleep(2000);

                IntPtr mainWindow = process.MainWindowHandle;
                if (mainWindow == IntPtr.Zero)
                    return false;

                // Find and click the randomize button
                IntPtr randomizeButton = ModAutomationHelper.FindControlByText(mainWindow, "Randomize!");
                
                if (randomizeButton != IntPtr.Zero)
                {
                    ModAutomationHelper.SendMessage(randomizeButton, ModAutomationHelper.BM_CLICK, IntPtr.Zero, IntPtr.Zero);

                    if (string.IsNullOrEmpty(_executablePath))
                        return false;

                    var executableDir = Path.GetDirectoryName(_executablePath);
                    if (string.IsNullOrEmpty(executableDir))
                        return false;

                    var runDir = Path.Combine(executableDir, "runs");
                    
                    // Wait for the runs directory to exist and for the randomization to complete
                    while (!IsRandomizationComplete(runDir))
                    {
                        Thread.Sleep(200);
                    }

                    // Close the window
                    ModAutomationHelper.PostMessage(mainWindow, ModAutomationHelper.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                    // Wait a bit before cleaning up
                    Thread.Sleep(2000);

                    // Clean up: Remove the exe.config file and clear any AppData settings
                    ClearFogModSettings();

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool RestoreBackups()
        {
            try
            {
                if (_configuration == null)
                    return false;

                // Find the executable in the fog data directory
                string fogDataPath = Path.Combine("Data", "DS1", "fog");
                string exePath = Path.Combine(fogDataPath, _configuration.ExecutablePath);
                if (!File.Exists(exePath))
                {
                    // Try to find it recursively in the fog directory
                    if (Directory.Exists(fogDataPath))
                    {
                        string[] foundExes = Directory.GetFiles(fogDataPath, _configuration.ExecutablePath, SearchOption.AllDirectories);
                        if (foundExes.Length > 0)
                        {
                            exePath = foundExes[0];
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                // Store the executable path for later use
                var appExecutablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(appExecutablePath))
                    return false;
                    
                _executablePath = Path.Combine(appExecutablePath, exePath);

                // Launch the mod executable in UI mode
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _executablePath,
                    WorkingDirectory = Path.GetDirectoryName(_executablePath),
                    UseShellExecute = true
                };

                using (Process? process = Process.Start(startInfo))
                {
                    if (process == null)
                        return false;

                    // Wait a moment for the application to start
                    Thread.Sleep(2000);

                    // Try to automate the restore backups button click
                    return AutomateRestoreBackupsButton(process);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool AutomateRestoreBackupsButton(Process process)
        {
            try
            {
                // Wait for the window to be ready
                process.WaitForInputIdle();
                Thread.Sleep(2000);

                IntPtr mainWindow = process.MainWindowHandle;
                if (mainWindow == IntPtr.Zero)
                    return false;

                // Find and click the restore backups button
                IntPtr restoreButton = ModAutomationHelper.FindControlByText(mainWindow, "Restore backups");
                
                if (restoreButton != IntPtr.Zero)
                {
                    ModAutomationHelper.SendMessage(restoreButton, ModAutomationHelper.BM_CLICK, IntPtr.Zero, IntPtr.Zero);

                    // Wait for the button to be disabled (indicating completion)
                    const int maxWaitTime = 30000; // 30 seconds max wait
                    const int checkInterval = 200; // Check every 200ms
                    int elapsedTime = 0;

                    while (elapsedTime < maxWaitTime)
                    {
                        // Check if button is disabled
                        if (IsButtonDisabled(restoreButton))
                        {
                            // Button is disabled, restoration is complete
                            break;
                        }

                        Thread.Sleep(checkInterval);
                        elapsedTime += checkInterval;
                    }

                    // Wait a moment for any message box to appear
                    Thread.Sleep(1000);

                    // Send Enter key to accept any message box that appears
                    SendEnterKey();

                    // Wait a bit more to ensure the message box is handled
                    Thread.Sleep(1000);

                    // Close the window
                    ModAutomationHelper.PostMessage(mainWindow, ModAutomationHelper.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsButtonDisabled(IntPtr buttonHandle)
        {
            try
            {
                const int GWL_STYLE = -16;
                const int WS_DISABLED = 0x08000000;

                int style = GetWindowLong(buttonHandle, GWL_STYLE);
                return (style & WS_DISABLED) != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SendEnterKey()
        {
            try
            {
                const byte VK_RETURN = 0x0D;
                const int KEYEVENTF_KEYUP = 0x0002;

                // Send key down
                keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);
                Thread.Sleep(50);
                // Send key up
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception)
            {
                // Silently ignore errors in key sending
            }
        }

        // Windows API imports needed for the restore functionality
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, UIntPtr dwExtraInfo);
    }
}
