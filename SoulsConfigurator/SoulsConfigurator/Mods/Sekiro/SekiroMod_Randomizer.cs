using SoulsConfigurator.Helpers;
using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Models;
using SoulsConfigurator.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace SoulsConfigurator.Mods.Sekiro
{
    public class SekiroMod_Randomizer : IMod, IConfigurableMod
    {
        public string Name => "Sekiro Enemy and Item Randomizer";
        public string ModFile => "Sekiro_Randomizer.zip";

        private ModConfiguration? _configuration;
        private Dictionary<string, object>? _savedConfiguration;
        private string? _selectedPreset;
        private string? _executablePath;

        public SekiroMod_Randomizer()
        {
            InitializeConfiguration();
        }

        private void InitializeConfiguration()
        {
            _configuration = new ModConfiguration
            {
                ModName = Name,
                ExecutablePath = "SekiroRandomizer.exe",
                WindowTitle = "Sekiro Enemy and Item Randomizer",
                Options = new List<ModConfigurationOption>
                {
                    // Main toggles
                    new ModConfigurationOption
                    {
                        Name = "enemy",
                        DisplayName = "Enemy Randomizer",
                        Description = "Enable enemy randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "enemy",
                        DefaultValue = true,
                        GroupName = "Main"
                    },
                    new ModConfigurationOption
                    {
                        Name = "item",
                        DisplayName = "Item Randomizer",
                        Description = "Enable item randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "item",
                        DefaultValue = true,
                        GroupName = "Main"
                    },

                    // === ITEM RANDOMIZER TAB ===

                    // Bias group
                    new ModConfigurationOption
                    {
                        Name = "difficulty",
                        DisplayName = "Bias",
                        Description = "Location selection bias - higher values favor difficult/late locations for better rewards",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "difficulty",
                        DefaultValue = 50,
                        GroupName = "Bias",
                        TabName = "Item",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 100 } }
                    },

                    // Key items randomized group
                    new ModConfigurationOption
                    {
                        Name = "defaultA",
                        DisplayName = "To anywhere",
                        Description = "Key items can be placed anywhere",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "defaultA",
                        DefaultValue = true,
                        GroupName = "Key items randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultA", "racemode", "norandom" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "racemode",
                        DisplayName = "To important locations",
                        Description = "Key items only placed in important locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "racemode",
                        DefaultValue = false,
                        GroupName = "Key items randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultA", "racemode", "norandom" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "norandom",
                        DisplayName = "No",
                        Description = "Key items remain in original locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "norandom",
                        DefaultValue = false,
                        GroupName = "Key items randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultA", "racemode", "norandom" },
                        Order = 3
                    },

                    // Memories randomized group
                    new ModConfigurationOption
                    {
                        Name = "defaultB",
                        DisplayName = "To anywhere",
                        Description = "Memories can be placed anywhere",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "defaultB",
                        DefaultValue = true,
                        GroupName = "Memories randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultB", "racemode_dmg", "norandom_dmg" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "racemode_dmg",
                        DisplayName = "To important locations",
                        Description = "Memories only placed in important locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "racemode_dmg",
                        DefaultValue = false,
                        GroupName = "Memories randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultB", "racemode_dmg", "norandom_dmg" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "norandom_dmg",
                        DisplayName = "No",
                        Description = "Memories remain in original locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "norandom_dmg",
                        DefaultValue = false,
                        GroupName = "Memories randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultB", "racemode_dmg", "norandom_dmg" },
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "weaponprogression",
                        DisplayName = "Item availability similar to base game",
                        Description = "Comparable difficulty to base game",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "weaponprogression",
                        DefaultValue = true,
                        GroupName = "Memories randomized?",
                        TabName = "Item",
                        Order = 4
                    },

                    // Health and healing upgrades randomized group
                    new ModConfigurationOption
                    {
                        Name = "defaultC",
                        DisplayName = "To anywhere",
                        Description = "Health upgrades can be placed anywhere",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "defaultC",
                        DefaultValue = true,
                        GroupName = "Health and healing upgrades randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultC", "racemode_health", "norandom_health" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "racemode_health",
                        DisplayName = "To important locations",
                        Description = "Health upgrades only placed in important locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "racemode_health",
                        DefaultValue = false,
                        GroupName = "Health and healing upgrades randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultC", "racemode_health", "norandom_health" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "norandom_health",
                        DisplayName = "No",
                        Description = "Health upgrades remain in original locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "norandom_health",
                        DefaultValue = false,
                        GroupName = "Health and healing upgrades randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultC", "racemode_health", "norandom_health" },
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "healthprogression",
                        DisplayName = "Item availability similar to base game",
                        Description = "Comparable difficulty to base game",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "healthprogression",
                        DefaultValue = true,
                        GroupName = "Health and healing upgrades randomized?",
                        TabName = "Item",
                        Order = 4
                    },

                    // Skills and prosthetics randomized group
                    new ModConfigurationOption
                    {
                        Name = "defaultD",
                        DisplayName = "To anywhere",
                        Description = "Skills and prosthetics can be placed anywhere",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "defaultD",
                        DefaultValue = true,
                        GroupName = "Skills and prosthetics randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultD", "racemode_skills", "norandom_skills" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "racemode_skills",
                        DisplayName = "To important locations",
                        Description = "Skills and prosthetics only placed in important locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "racemode_skills",
                        DefaultValue = false,
                        GroupName = "Skills and prosthetics randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultD", "racemode_skills", "norandom_skills" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "norandom_skills",
                        DisplayName = "No",
                        Description = "Skills and prosthetics remain in original locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "norandom_skills",
                        DefaultValue = false,
                        GroupName = "Skills and prosthetics randomized?",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "defaultD", "racemode_skills", "norandom_skills" },
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "skillprogression",
                        DisplayName = "Usually available before enemy roadblocks",
                        Description = "Skills made available before challenging enemies that might need them",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "skillprogression",
                        DefaultValue = true,
                        GroupName = "Skills and prosthetics randomized?",
                        TabName = "Item",
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "splitskills",
                        DisplayName = "Replace Esoteric Texts with direct skill drops",
                        Description = "Instead of getting texts that unlock skills, get skills directly",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "splitskills",
                        DefaultValue = false,
                        GroupName = "Skills and prosthetics randomized?",
                        TabName = "Item",
                        Order = 5
                    },

                    // Other item options
                    new ModConfigurationOption
                    {
                        Name = "headlessignore",
                        DisplayName = "Make Headless not required",
                        Description = "Don't place key items there (only without 'Randomize Headless')",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "headlessignore",
                        DefaultValue = true,
                        GroupName = "Other item options",
                        TabName = "Item",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "carpsanity",
                        DisplayName = "Carpsanity",
                        Description = "Shuffle treasure carp drops with other items",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "carpsanity",
                        DefaultValue = false,
                        GroupName = "Other item options",
                        TabName = "Item",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "earlyhirata",
                        DisplayName = "Quick Hirata",
                        Description = "Bell charm always available before Chained Ogre",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "earlyhirata",
                        DefaultValue = true,
                        GroupName = "Other item options",
                        TabName = "Item",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "veryearlyhirata",
                        DisplayName = "Allow very early Hirata",
                        Description = "Shinobi Prosthetic can be placed in Hirata (does not softlock)",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "veryearlyhirata",
                        DefaultValue = true,
                        GroupName = "Other item options",
                        TabName = "Item",
                        Order = 4
                    },

                    // === ENEMY RANDOMIZER TAB ===

                    // Randomness group
                    new ModConfigurationOption
                    {
                        Name = "bosses",
                        DisplayName = "Randomize bosses",
                        Description = "Enemies with boss health bars replace each other",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "bosses",
                        DefaultValue = true,
                        GroupName = "Randomness",
                        TabName = "Enemy",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "minibosses",
                        DisplayName = "Randomize minibosses",
                        Description = "Enemies with miniboss health bars replace each other",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "minibosses",
                        DefaultValue = true,
                        GroupName = "Randomness",
                        TabName = "Enemy",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "headlessmove",
                        DisplayName = "Randomize Headless",
                        Description = "Headless are also included in miniboss pool and randomized",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "headlessmove",
                        DefaultValue = false,
                        GroupName = "Randomness",
                        TabName = "Enemy",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "enemies",
                        DisplayName = "Randomize regular enemies",
                        Description = "All other enemies can replace each other",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "enemies",
                        DefaultValue = true,
                        GroupName = "Randomness",
                        TabName = "Enemy",
                        Order = 4
                    },

                    // Progression group
                    new ModConfigurationOption
                    {
                        Name = "phases",
                        DisplayName = "Bosses keep a similar number of total phases",
                        Description = "For instance, try to prevent any fight from being SSI + Demon of Hatred",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "phases",
                        DefaultValue = true,
                        GroupName = "Progression",
                        TabName = "Enemy",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "phasebuff",
                        DisplayName = "Endgame boss phases are considered longer than early ones",
                        Description = "Try to prevent early bosses from being seriously unfun without scaling",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "phasebuff",
                        DefaultValue = true,
                        GroupName = "Progression",
                        TabName = "Enemy",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "earlyreq",
                        DisplayName = "More manageable early roadblocks",
                        Description = "Try to prevent early minibosses from being seriously unfun without scaling",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "earlyreq",
                        DefaultValue = true,
                        GroupName = "Progression",
                        TabName = "Enemy",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "scale",
                        DisplayName = "Scale up/down enemy health/damage",
                        Description = "Make enemies easier/harder when moved significantly earlier/later",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "scale",
                        DefaultValue = true,
                        GroupName = "Progression",
                        TabName = "Enemy",
                        Order = 4
                    },

                    // Enemy/Item interaction
                    new ModConfigurationOption
                    {
                        Name = "enemytoitem",
                        DisplayName = "Use randomized enemy placements for skill/prosthetic placements",
                        Description = "For instance, if Guardian Ape is the first boss, make spear available early. Make item randomizer depend on enemy randomizer.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "enemytoitem",
                        DefaultValue = true,
                        GroupName = "Enemy/Item interaction",
                        RadioButtonGroup = new List<string> { "enemytoitem", "defaultAllowReroll" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "defaultAllowReroll",
                        DisplayName = "Allow rerolling enemy rando in the middle of a run",
                        Description = "Make enemy and item randomizer completely independent",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "defaultAllowReroll",
                        DefaultValue = false,
                        GroupName = "Enemy/Item interaction",
                        RadioButtonGroup = new List<string> { "enemytoitem", "defaultAllowReroll" },
                        Order = 2
                    },

                    // General options
                    new ModConfigurationOption
                    {
                        Name = "mergemods",
                        DisplayName = "Merge mods from normal 'mods' directory",
                        Description = "Attempt to merge with other installed mods",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "mergemods",
                        DefaultValue = false,
                        GroupName = "General Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "headlesswalk",
                        DisplayName = "Remove Headless-induced slow walk",
                        Description = "Remove the slow walk effect when near Headless enemies",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "headlesswalk",
                        DefaultValue = false,
                        GroupName = "General Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "edittext",
                        DisplayName = "Edit in-game text (rename bosses and add hints)",
                        Description = "Modify item descriptions and add hints for randomized content",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "edittext",
                        DefaultValue = true,
                        GroupName = "General Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "openstart",
                        DisplayName = "Open Bell Demon's Temple doors at the start",
                        Description = "Temple doors are opened from the beginning",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "openstart",
                        DefaultValue = true,
                        GroupName = "General Options"
                    },

                    // Seeds
                    new ModConfigurationOption
                    {
                        Name = "fixedseed",
                        DisplayName = "Fixed seed",
                        Description = "Fixed seed for reproducible results",
                        ControlType = ModControlType.TextBox,
                        ControlName = "fixedseed",
                        DefaultValue = "",
                        GroupName = "Seeds"
                    },
                    new ModConfigurationOption
                    {
                        Name = "enemyseed",
                        DisplayName = "Enemy seed",
                        Description = "Separate seed for enemy randomization (leave empty to use same as fixed seed)",
                        ControlType = ModControlType.TextBox,
                        ControlName = "enemyseed",
                        DefaultValue = "",
                        GroupName = "Seeds"
                    }
                }
            };
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(Path.Combine("Data", "Sekiro", ModFile), destPath, true);

                // If we have saved configuration, run the mod with it
                if (_savedConfiguration != null)
                {
                    RunWithConfiguration(_savedConfiguration, destPath);
                    ModAutomationHelper.ModifyModEngineIni(destPath, "randomizer");
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
                statusUpdater?.Invoke("Extracting Sekiro Randomizer files...");
                ZipFile.ExtractToDirectory(Path.Combine("Data", "Sekiro", ModFile), destPath, true);

                // If we have saved configuration, run the mod with it
                if (_savedConfiguration != null)
                {
                    statusUpdater?.Invoke("Running Sekiro Randomizer with configuration...");
                    statusUpdater?.Invoke("Please wait while the randomizer configures and runs...");
                    
                    bool result = await Task.Run(() => RunWithConfiguration(_savedConfiguration, destPath));
                    ModAutomationHelper.ModifyModEngineIni(destPath, "randomizer");
                    
                    if (result)
                    {
                        statusUpdater?.Invoke("Sekiro Randomizer completed successfully!");
                    }
                    else
                    {
                        statusUpdater?.Invoke("Sekiro Randomizer installation failed.");
                    }
                    
                    return result;
                }

                statusUpdater?.Invoke("Sekiro Randomizer files extracted successfully!");
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
                // Remove randomizer directory and files
                string randomizerPath = Path.Combine(destPath, "randomizer");
                if (Directory.Exists(randomizerPath))
                {
                    Directory.Delete(randomizerPath, true);
                }

                // Remove any direct files that might have been placed
                string[] filesToRemove = {
                    "SekiroRandomizer.exe",
                    "RandomizerCommon.dll"
                };

                foreach (string file in filesToRemove)
                {
                    string fullPath = Path.Combine(destPath, file);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
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

        public bool ApplyUserPreset(string presetName, string destPath)
        {
            var preset = UserPresetService.Instance.GetPreset(Name, presetName);
            if (preset == null)
                return false;

            return RunWithConfiguration(preset.OptionValues, destPath);
        }

        public bool RunWithConfiguration(Dictionary<string, object> configuration, string destPath)
        {
            try
            {
                if (_configuration == null)
                    return false;

                // Save configuration for potential use during installation
                _savedConfiguration = configuration;

                // Find the executable
                string exePath = Path.Combine(destPath, "randomizer", _configuration.ExecutablePath);
                if (!File.Exists(exePath))
                {
                    // Alternative path
                    exePath = Path.Combine(destPath, _configuration.ExecutablePath);
                    if (!File.Exists(exePath))
                        return false;
                }

                // Store the executable path for later use
                _executablePath = exePath;

                // Modify the Sekiro Randomizer settings before launching
                if (!SetSekiroRandomizerSettings(configuration))
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

                    // Try to automate the randomize button click
                    return AutomateRandomizeButton(process);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SetSekiroRandomizerSettings(Dictionary<string, object> configuration)
        {
            try
            {
                // Generate the options string that Sekiro Randomizer expects
                string optionsString = GenerateSekiroOptionsString(configuration);
                                
                // Debug logging to see what we're generating
                System.Diagnostics.Debug.WriteLine($"Sekiro Randomizer Options String: '{optionsString}'");
                
                // Clear any existing AppData configs first
                ClearSekiroRandomizerSettings();
                
                // Create the exe.config file with our settings
                string exePath = GetExecutablePath();
                if (string.IsNullOrEmpty(exePath))
                {
                    return false;
                }
                
                return CreateSekiroRandomizerExeConfig(exePath, optionsString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetSekiroRandomizerSettings error: {ex.Message}");
                return false;
            }
        }

        private string GenerateSekiroOptionsString(Dictionary<string, object> configuration)
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

            // Add version flag that the randomizer expects
            options.Add("v4");

            // Add difficulty setting
            int difficulty = 50; // default
            if (configuration.TryGetValue("difficulty", out object? diffValue) && diffValue != null)
            {
                if (int.TryParse(diffValue.ToString(), out int parsedDiff))
                {
                    difficulty = parsedDiff;
                }
            }
            options.Add(difficulty.ToString());

            // Add main seed
            uint seed = 0;
            if (configuration.TryGetValue("fixedseed", out object? seedValue) && seedValue != null)
            {
                string seedText = seedValue.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(seedText) && uint.TryParse(seedText, out uint parsedSeed))
                {
                    seed = parsedSeed;
                }
            }
            
            if (seed == 0)
            {
                seed = (uint)new Random().Next();
            }
            options.Add(seed.ToString());

            return string.Join(" ", options);
        }

        private bool CreateSekiroRandomizerExeConfig(string exePath, string optionsString)
        {
            try
            {
                string configPath = exePath + ".config";
                
                string configContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <configSections>
        <sectionGroup name=""userSettings"" type=""System.Configuration.UserSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" >
            <section name=""RandomizerCommon.Properties.Settings"" type=""System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" allowExeDefinition=""MachineToLocalUser"" requirePermission=""false"" />
        </sectionGroup>
    </configSections>
    <userSettings>
        <RandomizerCommon.Properties.Settings>
            <setting name=""Options"" serializeAs=""String"">
                <value>{System.Security.SecurityElement.Escape(optionsString)}</value>
            </setting>
        </RandomizerCommon.Properties.Settings>
    </userSettings>
</configuration>";

                File.WriteAllText(configPath, configContent);
                System.Diagnostics.Debug.WriteLine($"Created SekiroRandomizer.exe.config at: {configPath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateSekiroRandomizerExeConfig error: {ex.Message}");
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
                while (mainWindow == IntPtr.Zero)
                {
                    Thread.Sleep(100);
                    mainWindow = process.MainWindowHandle;
                }
                    

                // Find and click the randomize button - Sekiro uses different button text
                IntPtr randomizeButton = ModAutomationHelper.FindControlByText(mainWindow, "Randomize new run!");
                if (randomizeButton == IntPtr.Zero)
                {
                    randomizeButton = ModAutomationHelper.FindControlByText(mainWindow, "Run with fixed seed");
                }
                if (randomizeButton == IntPtr.Zero)
                {
                    randomizeButton = ModAutomationHelper.FindControlByText(mainWindow, "Reroll");
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
                    CleanupSekiroRandomizerConfig();
                    
                    return success;
                }

                return false;
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

        private void ClearSekiroRandomizerSettings()
        {
            try
            {
                // Clear all Sekiro Randomizer settings in AppData
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                // Try multiple possible directory names for Sekiro Randomizer
                string[] possiblePaths = {
                    Path.Combine(appDataPath, "SekiroRandomizer"),
                    Path.Combine(appDataPath, "Sekiro_Randomizer"),
                    Path.Combine(appDataPath, "RandomizerCommon")
                };

                foreach (string randomizerConfigPath in possiblePaths)
                {
                    if (Directory.Exists(randomizerConfigPath))
                    {
                        // Delete the entire randomizer settings directory
                        Directory.Delete(randomizerConfigPath, true);
                        System.Diagnostics.Debug.WriteLine($"Cleared Sekiro Randomizer settings at: {randomizerConfigPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently ignore errors when clearing settings
                System.Diagnostics.Debug.WriteLine($"Error clearing Sekiro Randomizer settings: {ex.Message}");
            }
        }

        private void CleanupSekiroRandomizerConfig()
        {
            try
            {
                // Remove the exe.config file
                string exePath = GetExecutablePath();
                if (!string.IsNullOrEmpty(exePath))
                {
                    string configPath = exePath + ".config";
                    if (File.Exists(configPath))
                    {
                        File.Delete(configPath);
                        System.Diagnostics.Debug.WriteLine($"Deleted SekiroRandomizer.exe.config at: {configPath}");
                    }
                }
                
                // Also clear any AppData settings that might have been created
                ClearSekiroRandomizerSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up Sekiro Randomizer config: {ex.Message}");
            }
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

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "Sekiro", ModFile);
            return File.Exists(sourcePath);
        }
    }
}
