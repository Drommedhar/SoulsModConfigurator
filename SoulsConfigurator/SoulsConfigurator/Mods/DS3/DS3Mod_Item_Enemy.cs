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
    public class DS3Mod_Item_Enemy : IMod, IConfigurableMod
    {
        public string Name => "DS3 Item & Enemy Randomizer";
        public string ModFile => "DS3 Static Item and Enemy Randomizer-361-v0-3-1644921428.zip";

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "DS3", ModFile);
            bool modFileExists = File.Exists(sourcePath);
            
            // Also check for the preset file if we need it
            string presetPath = Path.Combine("Data", "DS3", "Carthus Worm Banned.txt");
            bool presetFileExists = File.Exists(presetPath);
            
            // For now, main mod file is required, preset is optional
            return modFileExists;
        }

        private ModConfiguration? _configuration;
        private Dictionary<string, object>? _savedConfiguration;
        private string? _selectedPreset;
        private string? _executablePath; // Store the executable path
        private readonly UserPresetService _presetService;

        public DS3Mod_Item_Enemy()
        {
            _presetService = new UserPresetService();
            InitializeConfiguration();
        }

        private void InitializeConfiguration()
        {
            _configuration = new ModConfiguration
            {
                ModName = Name,
                ExecutablePath = "DS3Randomizer.exe", 
                WindowTitle = "DS3 Static Item and Enemy Randomizer",
                Options = new List<ModConfigurationOption>
                {
                    // Main tab toggles (prominent at top of each tab)
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

                    // === ITEM RANDOMIZER TAB ===

                    // Bias group (top of item tab)
                    new ModConfigurationOption
                    {
                        Name = "difficulty",
                        DisplayName = "Bias",
                        Description = "All possible locations for items are equally likely.\nKey items will usually be easy to find and not require much side content.",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "difficulty",
                        DefaultValue = 0,
                        GroupName = "Bias",
                        TabName = "Item",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 100 } }
                    },

                    // DLC group (left side, second from top)
                    new ModConfigurationOption
                    {
                        Name = "dlc2fromdlc1",
                        DisplayName = "Use DLC1→DLC2 routing in logic",
                        Description = "Allow Cinders to appear in DLC2",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "dlc2fromdlc1",
                        DefaultValue = true,
                        GroupName = "DLC",
                        TabName = "Item",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "earlydlc",
                        DisplayName = "DLC may be required before Irithyll",
                        Description = "May require Friede at medium soul level, most estus, and +7 weapon",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "earlydlc",
                        DefaultValue = false,
                        GroupName = "DLC",
                        TabName = "Item",
                        Order = 2
                    },

                    // Key item placement group (center top)
                    new ModConfigurationOption
                    {
                        Name = "norandom",
                        DisplayName = "Not randomized",
                        Description = "Keep key items in their original locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "norandom",
                        DefaultValue = false,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "norandom", "defaultKey", "racemode" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "defaultKey",
                        DisplayName = "Randomize to anywhere (over 1000 checks)",
                        Description = "Standard randomization with full location pool",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "defaultKey",
                        DefaultValue = true,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "norandom", "defaultKey", "racemode" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "racemode",
                        DisplayName = "Randomize to the following locations only:",
                        Description = "Limited location pool for race mode",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "racemode",
                        DefaultValue = false,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "norandom", "defaultKey", "racemode" },
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "defaultC",
                        DisplayName = "Boss drops",
                        Description = "Always included in this mode! Approx 25 checks",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "defaultC",
                        DefaultValue = true,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        EnabledWhen = "racemode",
                        EnabledWhenValue = true,
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "defaultD",
                        DisplayName = "Vanilla locations of key items, coals, and healing upgrades",
                        Description = "Original locations for essential progression items",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "defaultD",
                        DefaultValue = true,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        EnabledWhen = "racemode",
                        EnabledWhenValue = true,
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "raceloc_chest",
                        DisplayName = "Chests",
                        Description = "Approx 30 checks. Does not include mimics",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "raceloc_chest",
                        DefaultValue = true,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        EnabledWhen = "racemode",
                        EnabledWhenValue = true,
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "raceloc_ashes",
                        DisplayName = "NPC shops and non-NPC ashes",
                        Description = "Approx 30 checks. Includes items spawning in handmaid's inventory",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "raceloc_ashes",
                        DefaultValue = true,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        EnabledWhen = "racemode",
                        EnabledWhenValue = true,
                        Order = 7
                    },
                    new ModConfigurationOption
                    {
                        Name = "raceloc_miniboss",
                        DisplayName = "Powerful non-boss enemies",
                        Description = "Approx 45 checks. Includes most hostile non-respawning enemies and also the High Wall Darkwraith, or their replacements in enemy rando",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "raceloc_miniboss",
                        DefaultValue = true,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        EnabledWhen = "racemode",
                        EnabledWhenValue = true,
                        Order = 8
                    },
                    new ModConfigurationOption
                    {
                        Name = "raceloc_lizard",
                        DisplayName = "Small crystal lizards",
                        Description = "Approx 45 checks. Scurrying lizards, or their replacements in enemy rando",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "raceloc_lizard",
                        DefaultValue = false,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        EnabledWhen = "racemode",
                        EnabledWhenValue = true,
                        Order = 9
                    },
                    new ModConfigurationOption
                    {
                        Name = "raceloc_ring",
                        DisplayName = "Original locations of rings",
                        Description = "Approx 50 checks. Excludes rings covered by other categories and rings which only appear in later game cycles",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "raceloc_ring",
                        DefaultValue = false,
                        GroupName = "Key item placement",
                        TabName = "Item",
                        EnabledWhen = "racemode",
                        EnabledWhenValue = true,
                        Order = 10
                    },

                    // Healing upgrade item placement group (right top)
                    new ModConfigurationOption
                    {
                        Name = "norandom_health",
                        DisplayName = "Not randomized",
                        Description = "Keep healing upgrades in original locations",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "norandom_health",
                        DefaultValue = false,
                        GroupName = "Healing upgrade item placement",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "norandom_health", "defaultHealth", "racemode_health" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "defaultHealth",
                        DisplayName = "Randomize to anywhere",
                        Description = "Standard randomization for healing items",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "defaultHealth",
                        DefaultValue = false,
                        GroupName = "Healing upgrade item placement",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "norandom_health", "defaultHealth", "racemode_health" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "racemode_health",
                        DisplayName = "Randomized in key item pool",
                        Description = "Include healing upgrades in key item randomization",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "racemode_health",
                        DefaultValue = true,
                        GroupName = "Healing upgrade item placement",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "norandom_health", "defaultHealth", "racemode_health" },
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "estusprogression",
                        DisplayName = "Estus upgrade availability similar to base game",
                        Description = "Comparable difficulty to base game",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "estusprogression",
                        DefaultValue = true,
                        GroupName = "Healing upgrade item placement",
                        TabName = "Item",
                        Order = 4
                    },

                    // Other item progression group (right middle)
                    new ModConfigurationOption
                    {
                        Name = "weaponprogression",
                        DisplayName = "Weapon upgrade availability similar to base game",
                        Description = "Comparable difficulty to base game",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "weaponprogression",
                        DefaultValue = true,
                        GroupName = "Other item progression",
                        TabName = "Item",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "soulsprogression",
                        DisplayName = "Soul item availability similar to base game",
                        Description = "Comparable difficulty to base game",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "soulsprogression",
                        DefaultValue = true,
                        GroupName = "Other item progression",
                        TabName = "Item",
                        Order = 2
                    },

                    // Misc group (right bottom)
                    new ModConfigurationOption
                    {
                        Name = "ngplusrings",
                        DisplayName = "Add NG+ rings and ring locations",
                        Description = "Include New Game Plus ring content",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "ngplusrings",
                        DefaultValue = false,
                        GroupName = "Misc",
                        TabName = "Item",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "onehand",
                        DisplayName = "Disallow starting weapons requiring two-handing",
                        Description = "Ensure starting weapons can be used one-handed",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "onehand",
                        DefaultValue = false,
                        GroupName = "Misc",
                        TabName = "Item",
                        Order = 2
                    },

                    // Lothric Castle group (center bottom)
                    new ModConfigurationOption
                    {
                        Name = "regdancer",
                        DisplayName = "Lothric Castle not required early",
                        Description = "Standard progression through the game",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "regdancer",
                        DefaultValue = true,
                        GroupName = "Lothric Castle",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "regdancer", "middancer", "earlylothric" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "middancer",
                        DisplayName = "Lothric Castle may be required before Irithyll",
                        Description = "May require Dancer at medium soul level with +7 weapon",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "middancer",
                        DefaultValue = false,
                        GroupName = "Lothric Castle",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "regdancer", "middancer", "earlylothric" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "earlylothric",
                        DisplayName = "Lothric Castle may be required before Settlement",
                        Description = "May require Dancer at low soul level with +3 to +5 weapon",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "earlylothric",
                        DefaultValue = false,
                        GroupName = "Lothric Castle",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "regdancer", "middancer", "earlylothric" },
                        Order = 3
                    },

                    // Skips group (left bottom)
                    new ModConfigurationOption
                    {
                        Name = "treeskip",
                        DisplayName = "Tree skip in Firelink Shrine",
                        Description = "Fairly easy. Access lower Firelink Roof by jumping from a tree",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "treeskip",
                        DefaultValue = false,
                        GroupName = "Skips",
                        TabName = "Item",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "vilhelmskip",
                        DisplayName = "Doll skip and Vilhelm skip",
                        Description = "Extremely difficult. Spook quitout with long deathcam run",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "vilhelmskip",
                        DefaultValue = false,
                        GroupName = "Skips",
                        TabName = "Item",
                        Order = 2
                    },

                    // General checkboxes (applies to both item and enemy randomization)
                    new ModConfigurationOption
                    {
                        Name = "dlc1",
                        DisplayName = "Randomize DLC1",
                        Description = "Include DLC1 areas in randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "dlc1",
                        DefaultValue = true,
                        GroupName = "General Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "dlc2",
                        DisplayName = "Randomize DLC2",
                        Description = "Include DLC2 areas in randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "dlc2",
                        DefaultValue = true,
                        GroupName = "General Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "mergemods",
                        DisplayName = "Merge mods from normal 'mod' directory",
                        Description = "Attempt to merge with other installed mods",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "mergemods",
                        DefaultValue = false,
                        GroupName = "General Options"
                    },
                    new ModConfigurationOption
                    {
                        Name = "edittext",
                        DisplayName = "Edit in-game text",
                        Description = "Modify item descriptions for randomized items",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "edittext",
                        DefaultValue = true,
                        GroupName = "General Options"
                    },

                    // === ENEMY RANDOMIZER TAB ===

                    // Randomness group (top left)
                    new ModConfigurationOption
                    {
                        Name = "mimics",
                        DisplayName = "Randomize mimics",
                        Description = "Randomize mimic enemies",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "mimics",
                        DefaultValue = true,
                        GroupName = "Randomness",
                        TabName = "Enemy"
                    },
                    new ModConfigurationOption
                    {
                        Name = "lizards",
                        DisplayName = "Randomize small crystal lizards",
                        Description = "Randomize crystal lizard enemies",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "lizards",
                        DefaultValue = true,
                        GroupName = "Randomness",
                        TabName = "Enemy"
                    },
                    new ModConfigurationOption
                    {
                        Name = "reducepassive",
                        DisplayName = "Reduce frequency of \"harmless\" enemies",
                        Description = "Reduce the chance of harmless enemies appearing",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "reducepassive",
                        DefaultValue = false,
                        GroupName = "Randomness",
                        TabName = "Enemy"
                    },

                    // Progression group (middle left)
                    new ModConfigurationOption
                    {
                        Name = "earlyreq",
                        DisplayName = "Simple Gundyr and Vordt (no super late bosses)",
                        Description = "Ensure early bosses remain relatively simple",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "earlyreq",
                        DefaultValue = true,
                        GroupName = "Progression",
                        TabName = "Enemy"
                    },
                    new ModConfigurationOption
                    {
                        Name = "scale",
                        DisplayName = "Scale up/down enemy health/damage",
                        Description = "Scale enemy stats based on progression",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "scale",
                        DefaultValue = true,
                        GroupName = "Progression",
                        TabName = "Enemy"
                    },

                    // Enemy presets group (top right)
                    new ModConfigurationOption
                    {
                        Name = "carthus_worm_banned",
                        DisplayName = "Use Carthus Worm Banned preset",
                        Description = "Apply the Carthus Worm Banned enemy placement preset",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "carthus_worm_banned",
                        DefaultValue = false,
                        GroupName = "Enemy Presets",
                        TabName = "Enemy",
                        Order = 1
                    },

                    // Misc group (bottom left)
                    new ModConfigurationOption
                    {
                        Name = "yhormruler",
                        DisplayName = "Grant Storm Ruler upon encountering Yhorm",
                        Description = "Automatically grant Storm Ruler when facing Yhorm",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "yhormruler",
                        DefaultValue = false,
                        GroupName = "Misc",
                        TabName = "Enemy"
                    },
                    new ModConfigurationOption
                    {
                        Name = "chests",
                        DisplayName = "Turn all chests into mimics (randomized)",
                        Description = "Convert all chest locations to mimics",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "chests",
                        DefaultValue = false,
                        GroupName = "Misc",
                        TabName = "Enemy"
                    },
                    new ModConfigurationOption
                    {
                        Name = "supermimic",
                        DisplayName = "Impatient mimics",
                        Description = "Mimics become aggressive more quickly",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "supermimic",
                        DefaultValue = false,
                        GroupName = "Misc",
                        TabName = "Enemy"
                    },

                    // Seeds
                    new ModConfigurationOption
                    {
                        Name = "fixedseed",
                        DisplayName = "Overall seed",
                        Description = "Fixed seed for reproducible results",
                        ControlType = ModControlType.TextBox,
                        ControlName = "fixedseed",
                        DefaultValue = "",
                        GroupName = "Seeds"
                    },
                    new ModConfigurationOption
                    {
                        Name = "enemyseed",
                        DisplayName = "Separate enemy seed",
                        Description = "Separate seed for enemy randomization (same as overall seed)",
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
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS3", ModFile), destPath);
                
                // Copy Carthus Worm Banned preset if enabled
                if (_savedConfiguration != null && 
                    _savedConfiguration.TryGetValue("carthus_worm_banned", out object? presetValue) &&
                    Convert.ToBoolean(((JsonElement)presetValue).ValueKind.ToString()))
                {
                    CopyCarthusWormPreset(destPath);
                }
                
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

        private void CopyCarthusWormPreset(string destPath)
        {
            try
            {
                string sourcePresetPath = Path.Combine("Data", "DS3", "Carthus Worm Banned.txt");
                string randomizerPath = Path.Combine(destPath, "randomizer");
                string presetsPath = Path.Combine(randomizerPath, "presets");
                
                // Create presets directory if it doesn't exist
                Directory.CreateDirectory(presetsPath);
                
                string destPresetPath = Path.Combine(presetsPath, "Carthus Worm Banned.txt");
                
                // Copy the preset file
                if (File.Exists(sourcePresetPath))
                {
                    File.Copy(sourcePresetPath, destPresetPath, true);
                }
            }
            catch (Exception)
            {
                // If preset copying fails, continue with mod installation
                // This is not critical to basic functionality
            }
        }

        public bool TryRemoveMod(string destPath)
        {
            try
            {
                Directory.Delete(Path.Combine(destPath, "randomizer"), true);
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
            return _presetService.LoadPresets(Name);
        }

        public bool ApplyUserPreset(string presetName, string destPath)
        {
            var preset = _presetService.GetPreset(Name, presetName);
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

                // Copy Carthus Worm Banned preset if enabled
                if (configuration.TryGetValue("carthus_worm_banned", out object? presetValue) &&
                    Convert.ToBoolean(((JsonElement)presetValue).ValueKind.ToString()))
                {
                    CopyCarthusWormPreset(destPath);
                }

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

                // Modify the DS3 Randomizer settings before launching
                if (!SetDS3RandomizerSettings(configuration))
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

        private void CleanupDS3RandomizerConfig()
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
                        System.Diagnostics.Debug.WriteLine($"Deleted DS3Randomizer.exe.config at: {configPath}");
                    }
                }
                
                // Also clear any AppData settings that might have been created
                ClearDS3RandomizerSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up DS3 Randomizer config: {ex.Message}");
            }
        }

        private bool SetDS3RandomizerSettings(Dictionary<string, object> configuration)
        {
            try
            {
                // Generate the options string that DS3 Randomizer expects
                string optionsString = GenerateDS3OptionsString(configuration);
                                
                // Debug logging to see what we're generating
                System.Diagnostics.Debug.WriteLine($"DS3 Randomizer Options String: '{optionsString}'");
                
                // Clear any existing AppData configs first
                ClearDS3RandomizerSettings();
                
                // Create the exe.config file with our settings
                string exePath = GetExecutablePath();
                if (string.IsNullOrEmpty(exePath))
                {
                    return false;
                }
                
                return CreateDS3RandomizerExeConfig(exePath, optionsString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetDS3RandomizerSettings error: {ex.Message}");
                return false;
            }
        }

        private string GenerateDS3OptionsString(Dictionary<string, object> configuration)
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
                            // Skip preset option as it's handled separately
                            if (option.Name != "carthus_worm_banned")
                            {
                                options.Add(option.Name);
                            }
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

            // Add seed
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

            // Add preset command line argument if Carthus Worm Banned is enabled
            if (configuration.TryGetValue("carthus_worm_banned", out object? presetValue) &&
                Convert.ToBoolean(((JsonElement)presetValue).ValueKind.ToString()))
            {
                options.Add("--preset");
                options.Add("Carthus Worm Banned");
            }

            return string.Join(" ", options);
        }

        private bool CreateDS3RandomizerExeConfig(string exePath, string optionsString)
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
                System.Diagnostics.Debug.WriteLine($"Created DS3Randomizer.exe.config at: {configPath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateDS3RandomizerExeConfig error: {ex.Message}");
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
                    CleanupDS3RandomizerConfig();
                    
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

        private void ClearDS3RandomizerSettings()
        {
            try
            {
                // Clear all DS3 Randomizer settings in AppData
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                // Try multiple possible directory names for DS3 Randomizer
                string[] possiblePaths = {
                    Path.Combine(appDataPath, "DS3Randomizer"),
                    Path.Combine(appDataPath, "DS3_Static_Item_and_Enemy_Randomizer"),
                    Path.Combine(appDataPath, "RandomizerCommon")
                };

                foreach (string randomizerConfigPath in possiblePaths)
                {
                    if (Directory.Exists(randomizerConfigPath))
                    {
                        // Delete the entire randomizer settings directory
                        Directory.Delete(randomizerConfigPath, true);
                        System.Diagnostics.Debug.WriteLine($"Cleared DS3 Randomizer settings at: {randomizerConfigPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently ignore errors when clearing settings
                System.Diagnostics.Debug.WriteLine($"Error clearing DS3 Randomizer settings: {ex.Message}");
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
                var preset = _presetService.GetPreset(Name, presetName);
                if (preset != null)
                {
                    SaveConfiguration(preset.OptionValues);
                }
            }
        }
    }
}
