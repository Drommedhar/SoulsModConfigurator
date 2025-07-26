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

namespace SoulsConfigurator.Mods.DS3
{
    public class DS3Mod_FogGate : IMod, IConfigurableMod
    {
        public string Name => "DS3 Fog Gate Randomizer";
        public string ModFile => "DS3_FogGate_Randomizer.zip";

        private ModConfiguration? _configuration;
        private Dictionary<string, object>? _savedConfiguration;
        private string? _selectedPreset;
        private string? _executablePath; // Store the executable path

        public DS3Mod_FogGate()
        {
            InitializeConfiguration();
        }

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "DS3", ModFile);
            return File.Exists(sourcePath);
        }

        private void InitializeConfiguration()
        {
            _configuration = new ModConfiguration
            {
                ModName = Name,
                ExecutablePath = "FogMod.exe",
                WindowTitle = "DS3 Fog Gate Randomizer v0.2",
                Options = new List<ModConfigurationOption>
                {
                    // Randomized entrances group
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
                        Description = "Randomize warp destinations, like to DLCs",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "warp",
                        DefaultValue = true,
                        GroupName = "Randomized entrances"
                    },
                    new ModConfigurationOption
                    {
                        Name = "pvp",
                        DisplayName = "PvP fog gates",
                        Description = "Enable and randomize fog gates separating PvP zones",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "pvp",
                        DefaultValue = true,
                        GroupName = "Randomized entrances"
                    },
                    new ModConfigurationOption
                    {
                        Name = "lords",
                        DisplayName = "Require Cinders of a Lord",
                        Description = "Don't randomize warps in Kiln",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "lords",
                        DefaultValue = true,
                        GroupName = "Randomized entrances"
                    },
                    new ModConfigurationOption
                    {
                        Name = "dlc1",
                        DisplayName = "DLC 1",
                        Description = "Randomize warps to and from Ariandel",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "dlc1",
                        DefaultValue = true,
                        GroupName = "Randomized entrances"
                    },
                    new ModConfigurationOption
                    {
                        Name = "dlc2",
                        DisplayName = "DLC 2",
                        Description = "Randomize warps to and from Dreg Heap and Ringed City",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "dlc2",
                        DefaultValue = true,
                        GroupName = "Randomized entrances"
                    },

                    // Warping between bonfires group
                    new ModConfigurationOption
                    {
                        Name = "earlywarp",
                        DisplayName = "Coiled Sword available early",
                        Description = "Firelink Shrine and Coiled Sword are routed in early. Balanced start",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "earlywarp",
                        DefaultValue = true,
                        GroupName = "Warping between bonfires",
                        RadioButtonGroup = new List<string> { "earlywarp", "latewarp", "instawarp" }
                    },
                    new ModConfigurationOption
                    {
                        Name = "latewarp",
                        DisplayName = "Coiled Sword can be anywhere",
                        Description = "Firelink is still early, but Coiled Sword is like Lordvessel in Dark Souls. Slower start",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "latewarp",
                        DefaultValue = false,
                        GroupName = "Warping between bonfires",
                        RadioButtonGroup = new List<string> { "earlywarp", "latewarp", "instawarp" }
                    },
                    new ModConfigurationOption
                    {
                        Name = "instawarp",
                        DisplayName = "Coiled Sword not required",
                        Description = "Firelink Shrine, and warping between bonfires, is available immediately. Easy start",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "instawarp",
                        DefaultValue = false,
                        GroupName = "Warping between bonfires",
                        RadioButtonGroup = new List<string> { "earlywarp", "latewarp", "instawarp" }
                    },

                    // Misc options group
                    new ModConfigurationOption
                    {
                        Name = "scale",
                        DisplayName = "Scale enemies and bosses",
                        Description = "Increase or decrease enemy health and damage based on distance from start",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "scale",
                        DefaultValue = true,
                        GroupName = "Misc options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "pacifist",
                        DisplayName = "Pacifist Mode",
                        Description = "Allow escaping boss fights without defeating bosses",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "pacifist",
                        DefaultValue = false,
                        GroupName = "Misc options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "treeskip",
                        DisplayName = "Tree skip",
                        Description = "Logic assumes you can jump to Firelink Shrine roof from the tree",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "treeskip",
                        DefaultValue = false,
                        GroupName = "Misc options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "unconnected",
                        DisplayName = "Disconnected fog gates",
                        Description = "Entering a fog gate you just exited can send you to a different fixed location",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "unconnected",
                        DefaultValue = false,
                        GroupName = "Misc options"
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
                    },
                    new ModConfigurationOption
                    {
                        Name = "exe",
                        DisplayName = "Merge with other mod",
                        Description = "Select Data0.bdt from another mod to merge (leave blank to run this mod by itself)",
                        ControlType = ModControlType.TextBox,
                        ControlName = "exe",
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
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS3", ModFile), destPath);
                
                // If we have saved configuration, run the mod with it
                if (_savedConfiguration != null)
                {
                    RunWithConfiguration(_savedConfiguration, destPath);
                    ModAutomationHelper.ModifyModEngineIni(destPath, "fog");
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
                statusUpdater?.Invoke("Extracting DS3 Fog Gate Randomizer files...");
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS3", ModFile), destPath);
                
                // If we have saved configuration, run the mod with it
                if (_savedConfiguration != null)
                {
                    statusUpdater?.Invoke("Running DS3 Fog Gate Randomizer with configuration...");
                    statusUpdater?.Invoke("Please wait while the randomizer configures and runs...");
                    
                    bool result = await Task.Run(() => RunWithConfiguration(_savedConfiguration, destPath));
                    ModAutomationHelper.ModifyModEngineIni(destPath, "fog");
                    
                    if (result)
                    {
                        statusUpdater?.Invoke("DS3 Fog Gate Randomizer completed successfully!");
                    }
                    else
                    {
                        statusUpdater?.Invoke("DS3 Fog Gate Randomizer installation failed.");
                    }
                    
                    return result;
                }

                statusUpdater?.Invoke("DS3 Fog Gate Randomizer files extracted successfully!");
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
                Directory.Delete(Path.Combine(destPath, "fog"), true);
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

                // Find the executable
                string exePath = Path.Combine(destPath, "fog", _configuration.ExecutablePath);
                if (!File.Exists(exePath))
                {
                    // Alternative path
                    exePath = Path.Combine(destPath, _configuration.ExecutablePath);
                    if (!File.Exists(exePath))
                        return false;
                }

                // Store the executable path for later use
                _executablePath = exePath;

                // Modify the FogMod settings before launching
                if (!SetFogModSettings(configuration, destPath))
                {
                    return false;
                }

                // Launch the mod executable in UI mode (it will read the pre-configured settings)
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
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

        private string GenerateCommandLineArgs(Dictionary<string, object> configuration)
        {
            if (_configuration == null)
                return "";

            List<string> args = new List<string>();

            // Add boolean options that are enabled
            foreach (var option in _configuration.Options)
            {
                if (configuration.TryGetValue(option.Name, out object? value) && value != null)
                {
                    switch (option.ControlType)
                    {
                        case ModControlType.CheckBox:
                        case ModControlType.RadioButton:
                            if (Convert.ToBoolean(value))
                            {
                                args.Add(option.Name);
                            }
                            break;
                        case ModControlType.TextBox:
                            string textValue = value.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(textValue))
                            {
                                // For FogMod, special handling of seed and merge options
                                if (option.Name == "fixedseed")
                                {
                                    if (uint.TryParse(textValue, out uint seed))
                                    {
                                        args.Add(seed.ToString());
                                    }
                                }
                                else if (option.Name == "exe")
                                {
                                    // This is the merge mod option - FogMod expects "mergemods" flag when a merge path is provided
                                    args.Add("mergemods");
                                }
                            }
                            break;
                    }
                }
            }

            // Add default seed if no fixed seed was provided
            if (!configuration.ContainsKey("fixedseed") || 
                string.IsNullOrWhiteSpace(configuration["fixedseed"]?.ToString()))
            {
                args.Add(new Random().Next().ToString());
            }

            return string.Join(" ", args);
        }

        private bool SetFogModSettings(Dictionary<string, object> configuration, string destPath)
        {
            try
            {
                // Generate the options string that FogMod expects
                string optionsString = GenerateFogModOptionsString(configuration);
                
                // Get the merge mod path
                string mergePath = Path.Combine(destPath, "randomizer", "Data0.bdt");
                if(!File.Exists(mergePath))
                {
                    mergePath = "";
                }

                // Create the exe.config file with our settings
                string exePath = GetExecutablePath();
                if (string.IsNullOrEmpty(exePath))
                {
                    return false;
                }

                // Modify the FogMod user.config file to set the Options and Exe settings
                return ModifyFogModUserConfig(optionsString, mergePath, exePath);
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
                string[] possiblePaths = {
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

            // Add seed at the end
            string seed = "";
            if (configuration.TryGetValue("fixedseed", out object? seedValue) && seedValue != null)
            {
                string seedText = seedValue.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(seedText) && uint.TryParse(seedText, out uint parsedSeed))
                {
                    seed = parsedSeed.ToString();
                }
            }

            return string.Join(" ", options) + " " + seed;
        }

        private bool ModifyFogModUserConfig(string optionsString, string mergePath, string exePath)
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

                return CreateFogModSettings(optionsString, mergePath, exePath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool UpdateUserConfigFile(string configFilePath, string optionsString, string exePath)
        {
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.Load(configFilePath);

                // Find or create the FogMod.Properties.Settings section
                var userSettings = doc.SelectSingleNode("//userSettings");
                if (userSettings == null)
                {
                    userSettings = doc.CreateElement("userSettings");
                    doc.DocumentElement?.AppendChild(userSettings);
                }

                var fogModSettings = userSettings.SelectSingleNode("FogMod.Properties.Settings");
                if (fogModSettings == null)
                {
                    fogModSettings = doc.CreateElement("FogMod.Properties.Settings");
                    userSettings.AppendChild(fogModSettings);
                }

                // Update or create the Options setting
                UpdateSettingValue(doc, fogModSettings, "Options", optionsString);
                
                // Update or create the Exe setting
                UpdateSettingValue(doc, fogModSettings, "Exe", exePath);

                doc.Save(configFilePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void UpdateSettingValue(System.Xml.XmlDocument doc, System.Xml.XmlNode settingsNode, string settingName, string value)
        {
            var settingNode = settingsNode.SelectSingleNode($"setting[@name='{settingName}']");
            if (settingNode == null)
            {
                settingNode = doc.CreateElement("setting");
                var nameAttr = doc.CreateAttribute("name");
                nameAttr.Value = settingName;
                settingNode.Attributes?.Append(nameAttr);
                
                var serializeAttr = doc.CreateAttribute("serializeAs");
                serializeAttr.Value = "String";
                settingNode.Attributes?.Append(serializeAttr);
                
                settingsNode.AppendChild(settingNode);
            }

            var valueNode = settingNode.SelectSingleNode("value");
            if (valueNode == null)
            {
                valueNode = doc.CreateElement("value");
                settingNode.AppendChild(valueNode);
            }

            valueNode.InnerText = value;
        }

        private bool CreateFogModSettings(string optionsString, string mergePath, string exePath)
        {
            try
            {
                string configPath = exePath + ".config";

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
                <value>{System.Security.SecurityElement.Escape(mergePath)}</value>
            </setting>
            <setting name=""Language"" serializeAs=""String"">
                <value></value>
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

        private string GetRandomHash()
        {
            return Guid.NewGuid().ToString("N")[..8];
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
                IntPtr randomizeButton = ModAutomationHelper.FindControlByText(mainWindow, "Randomize new run!");
                if (randomizeButton == IntPtr.Zero)
                {
                    randomizeButton = ModAutomationHelper.FindControlByText(mainWindow, "Run with fixed seed");
                }
                if (randomizeButton == IntPtr.Zero)
                {
                    randomizeButton = ModAutomationHelper.FindControlByText(mainWindow, "Randomize!");
                }

                if (randomizeButton != IntPtr.Zero)
                {
                    ModAutomationHelper.SendMessage(randomizeButton, ModAutomationHelper.BM_CLICK, IntPtr.Zero, IntPtr.Zero);

                    // Monitor the status bar for completion
                    bool success = ModAutomationHelper.WaitForRandomizationComplete(mainWindow);

                    // Close the window
                    ModAutomationHelper.PostMessage(mainWindow, ModAutomationHelper.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                    // Wait a bit before cleaning up
                    Thread.Sleep(2000);

                    // Clean up: Remove the exe.config file and clear any AppData settings
                    ClearFogModSettings();

                    return success;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
