using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Models;
using SoulsConfigurator.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SoulsConfigurator.Mods.DS1
{
    public class DS1Mod_ItemRandomizer : IMod, IConfigurableMod
    {
        private ModConfiguration? _configuration;
        private Dictionary<string, object>? _savedConfiguration;
        private readonly UserPresetService _presetService;
        private string? _selectedPreset;

        public string Name => "Dark Souls 1 Item Randomizer";
        public string ModFile => ""; // This mod is deployed with our application

        public DS1Mod_ItemRandomizer()
        {
            _presetService = new UserPresetService();
            InitializeConfiguration();
        }

        public bool IsAvailable()
        {
            // Check if the randomizer executable exists in our Data/DS1 folder
            string randomizerPath = Path.Combine("Data", "DS1", "randomizer_gui.exe");
            return File.Exists(randomizerPath);
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                File.Copy(
                    Path.Combine("Data", "DS1", "randomizer_gui.exe"),
                    Path.Combine(destPath, "randomizer_gui.exe"),
                    true);

                // If we have saved configuration, run the randomizer with it
                if (_savedConfiguration != null)
                {
                    return RunWithConfiguration(_savedConfiguration, destPath);
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryRemoveMod(string destPath)
        {
            try
            {
                // First, revert the game data to vanilla
                string randomizerPath = Path.Combine(destPath, "randomizer_gui.exe");
                if (File.Exists(randomizerPath))
                {
                    try
                    {
                        ProcessStartInfo processInfo = new ProcessStartInfo
                        {
                            FileName = randomizerPath,
                            WorkingDirectory = destPath,
                            Arguments = "--revert",
                            UseShellExecute = true,
                            CreateNoWindow = false
                        };

                        using (var process = Process.Start(processInfo))
                        {
                            if (process != null)
                            {
                                process.WaitForExit();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Continue with cleanup even if revert fails
                    }
                }

                // Now remove the mod files
                string[] filesToRemove = {
                    "randomizer.ini",
                    "randomizer_gui.exe"
                };

                foreach (string file in filesToRemove)
                {
                    string fullPath = Path.Combine(destPath, file);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }

                var dirs = Directory.GetDirectories(destPath, "random-seed-*");
                foreach (var dir in dirs)
                {
                    Directory.Delete(dir, true);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void InitializeConfiguration()
        {
            _configuration = new ModConfiguration
            {
                ModName = Name,
                ExecutablePath = "randomizer_gui.exe",
                WindowTitle = "Dark Souls Item Randomizer",
                Options = new List<ModConfigurationOption>
                {
                    // Seed - Top of left column
                    new ModConfigurationOption
                    {
                        Name = "seed",
                        DisplayName = "Seed",
                        Description = "Enter a seed for reproducible randomization (leave blank for random)",
                        ControlType = ModControlType.TextBox,
                        ControlName = "seed",
                        DefaultValue = "",
                        GroupName = "Core Settings",
                        TabName = "General",
                        Order = 1
                    },

                    // Difficulty - Left column
                    new ModConfigurationOption
                    {
                        Name = "difficulty_fair",
                        DisplayName = "Fair",
                        Description = "Items have an equal chance to be placed anywhere.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "difficulty_fair",
                        DefaultValue = true,
                        GroupName = "Difficulty",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "difficulty_fair", "difficulty_unfair", "difficulty_very_unfair" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "difficulty_unfair",
                        DisplayName = "Unfair",
                        Description = "Very items are shuffled but can be placed only in certain locations.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "difficulty_unfair",
                        DefaultValue = false,
                        GroupName = "Difficulty",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "difficulty_fair", "difficulty_unfair", "difficulty_very_unfair" },
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "difficulty_very_unfair",
                        DisplayName = "Very Unfair",
                        Description = "Good / rare items are shuffled but can be placed only in hidden or challenging locations.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "difficulty_very_unfair",
                        DefaultValue = false,
                        GroupName = "Difficulty",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "difficulty_fair", "difficulty_unfair", "difficulty_very_unfair" },
                        Order = 4
                    },

                    // Key Placement - Left column
                    new ModConfigurationOption
                    {
                        Name = "key_placement_not_shuffled",
                        DisplayName = "Not Shuffled",
                        Description = "Key items are left in their original locations.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "key_placement_not_shuffled",
                        DefaultValue = false,
                        GroupName = "Key Placement",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "key_placement_not_shuffled", "key_placement_shuffled", "key_placement_race_mode", "key_placement_race_mode_plus" },
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "key_placement_shuffled",
                        DisplayName = "Shuffled",
                        Description = "Key items are shuffled to random locations.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "key_placement_shuffled",
                        DefaultValue = false,
                        GroupName = "Key Placement",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "key_placement_not_shuffled", "key_placement_shuffled", "key_placement_race_mode", "key_placement_race_mode_plus" },
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "key_placement_race_mode",
                        DisplayName = "Race Mode",
                        Description = "Key items are shuffled, but the Lordvessel is available from the start.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "key_placement_race_mode",
                        DefaultValue = true,
                        GroupName = "Key Placement",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "key_placement_not_shuffled", "key_placement_shuffled", "key_placement_race_mode", "key_placement_race_mode_plus" },
                        Order = 7
                    },
                    new ModConfigurationOption
                    {
                        Name = "key_placement_race_mode_plus",
                        DisplayName = "Race Mode +",
                        Description = "Race Mode, but all bonfires start kindled and the Lordvessel is at Firelink Shrine.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "key_placement_race_mode_plus",
                        DefaultValue = false,
                        GroupName = "Key Placement",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "key_placement_not_shuffled", "key_placement_shuffled", "key_placement_race_mode", "key_placement_race_mode_plus" },
                        Order = 8
                    },

                    // Soul Items - Left column
                    new ModConfigurationOption
                    {
                        Name = "soul_items_shuffled",
                        DisplayName = "Shuffled",
                        Description = "Boss souls are shuffled with other items.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "soul_items_shuffled",
                        DefaultValue = false,
                        GroupName = "Soul Items",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "soul_items_shuffled", "soul_items_replaced", "soul_items_transposed" },
                        Order = 9
                    },
                    new ModConfigurationOption
                    {
                        Name = "soul_items_replaced",
                        DisplayName = "Replaced",
                        Description = "Boss souls are replaced with consumable souls of equivalent value.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "soul_items_replaced",
                        DefaultValue = false,
                        GroupName = "Soul Items",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "soul_items_shuffled", "soul_items_replaced", "soul_items_transposed" },
                        Order = 10
                    },
                    new ModConfigurationOption
                    {
                        Name = "soul_items_transposed",
                        DisplayName = "Transposed",
                        Description = "Boss souls are replaced with their associated weapons/spells.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "soul_items_transposed",
                        DefaultValue = true,
                        GroupName = "Soul Items",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "soul_items_shuffled", "soul_items_replaced", "soul_items_transposed" },
                        Order = 11
                    },

                    // Lordvessel - Left column
                    new ModConfigurationOption
                    {
                        Name = "lordvessel_gwynevere",
                        DisplayName = "Gwynevere",
                        Description = "The Lordvessel is given by Gwynevere as normal.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "lordvessel_gwynevere",
                        DefaultValue = false,
                        GroupName = "Lordvessel",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "lordvessel_gwynevere", "lordvessel_randomized", "lordvessel_firelink" },
                        Order = 12
                    },
                    new ModConfigurationOption
                    {
                        Name = "lordvessel_randomized",
                        DisplayName = "Randomized",
                        Description = "The Lordvessel is placed at a random location.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "lordvessel_randomized",
                        DefaultValue = false,
                        GroupName = "Lordvessel",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "lordvessel_gwynevere", "lordvessel_randomized", "lordvessel_firelink" },
                        Order = 13
                    },
                    new ModConfigurationOption
                    {
                        Name = "lordvessel_firelink",
                        DisplayName = "Firelink",
                        Description = "The Lordvessel is placed at Firelink Shrine from the start.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "lordvessel_firelink",
                        DefaultValue = true,
                        GroupName = "Lordvessel",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "lordvessel_gwynevere", "lordvessel_randomized", "lordvessel_firelink" },
                        Order = 14
                    },

                    // Starting Items - Top of right column
                    new ModConfigurationOption
                    {
                        Name = "start_items_shield_1h",
                        DisplayName = "Shield & 1H Weapon",
                        Description = "Start with a shield and a one-handed weapon.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "start_items_shield_1h",
                        DefaultValue = false,
                        GroupName = "Starting Items",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "start_items_shield_1h", "start_items_shield_12h", "start_items_combined" },
                        Order = 15
                    },
                    new ModConfigurationOption
                    {
                        Name = "start_items_shield_12h",
                        DisplayName = "Shield & 1/2H Weapon",
                        Description = "Start with a shield and either a one-handed or two-handed weapon.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "start_items_shield_12h",
                        DefaultValue = true,
                        GroupName = "Starting Items",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "start_items_shield_1h", "start_items_shield_12h", "start_items_combined" },
                        Order = 16
                    },
                    new ModConfigurationOption
                    {
                        Name = "start_items_combined",
                        DisplayName = "Shield/Weapon & Weapon",
                        Description = "Start with either a shield or weapon, plus another weapon.",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "start_items_combined",
                        DefaultValue = false,
                        GroupName = "Starting Items",
                        TabName = "General",
                        RadioButtonGroup = new List<string> { "start_items_shield_1h", "start_items_shield_12h", "start_items_combined" },
                        Order = 17
                    },

                    // Other Settings checkboxes - Right column
                    new ModConfigurationOption
                    {
                        Name = "fashion_souls",
                        DisplayName = "Fashion Souls",
                        Description = "Armor sets are NOT kept together during shuffling. Players will need to mix-and-match armor pieces.",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "fashion_souls",
                        DefaultValue = true,
                        GroupName = "Other Settings",
                        TabName = "General",
                        Order = 18
                    },

                    new ModConfigurationOption
                    {
                        Name = "randomize_npc_armor",
                        DisplayName = "Laundromat Mixup",
                        Description = "NPCs wear randomly chosen armor instead of their normal sets",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "randomize_npc_armor",
                        DefaultValue = true,
                        GroupName = "Other Settings",
                        TabName = "General",
                        Order = 19
                    },

                    new ModConfigurationOption
                    {
                        Name = "use_lord_souls",
                        DisplayName = "Senile Primordial Serpents",
                        Description = "Include the 4 Lord Souls in randomized keys. Difficulty ranges from much easier to much harder.",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "use_lord_souls",
                        DefaultValue = false,
                        GroupName = "Other Settings",
                        TabName = "General",
                        Order = 20
                    },

                    new ModConfigurationOption
                    {
                        Name = "ascend_weapons",
                        DisplayName = "Eager Smiths",
                        Description = "Normal weapons have a 25% chance to be ascended with a random ember",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "ascend_weapons",
                        DefaultValue = true,
                        GroupName = "Other Settings",
                        TabName = "General",
                        Order = 21
                    },

                    new ModConfigurationOption
                    {
                        Name = "set_up_hints",
                        DisplayName = "Seek Guidance Hints",
                        Description = "The dev messages visible with Seek Guidance will have hints automatically added",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "set_up_hints",
                        DefaultValue = true,
                        GroupName = "Other Settings",
                        TabName = "General",
                        Order = 22
                    },

                    new ModConfigurationOption
                    {
                        Name = "keys_not_in_dlc",
                        DisplayName = "No DLC",
                        Description = "Key items will NOT be placed in DLC areas (Painted World, Artorias of the Abyss)",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "keys_not_in_dlc",
                        DefaultValue = true,
                        GroupName = "Other Settings",
                        TabName = "General",
                        Order = 23
                    },

                    new ModConfigurationOption
                    {
                        Name = "no_black_knight_weapons",
                        DisplayName = "No Black Knight Weapons",
                        Description = "Black Knight weapons are replaced by Titanite Chunks and Slabs",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "no_black_knight_weapons",
                        DefaultValue = true,
                        GroupName = "Other Settings",
                        TabName = "General",
                        Order = 24
                    },

                    // Hidden options that are always set to their defaults
                    new ModConfigurationOption
                    {
                        Name = "no_online_items",
                        DisplayName = "No Online Items",
                        Description = "Exclude online-only items from randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "no_online_items",
                        DefaultValue = true,
                        GroupName = "Advanced",
                        Order = 99
                    },

                    new ModConfigurationOption
                    {
                        Name = "better_start_spells",
                        DisplayName = "Better Starting Spells",
                        Description = "Improve starting spell selection for caster classes",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "better_start_spells",
                        DefaultValue = true,
                        GroupName = "Advanced",
                        Order = 100
                    }
                }
            };
        }

        public ModConfiguration GetUIConfiguration()
        {
            return _configuration ?? throw new InvalidOperationException("Configuration not initialized");
        }

        public bool RunWithConfiguration(Dictionary<string, object> configuration, string destPath)
        {
            try
            {
                if (_configuration == null)
                    return false;

                // Save configuration for potential use during installation
                _savedConfiguration = configuration;

                // Create the INI file with the configuration
                string iniPath = Path.Combine(destPath, "randomizer.ini");
                CreateIniFile(configuration, iniPath);

                // Run the randomizer in scramble mode using command line arguments
                string randomizerPath = Path.Combine(destPath, "randomizer_gui.exe");
                
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = randomizerPath,
                    WorkingDirectory = Path.Combine(destPath),
                    UseShellExecute = false,
                    CreateNoWindow = false, // Show console output
                    Arguments = "--scramble --config randomizer.ini"
                };

                using (Process process = Process.Start(startInfo)!)
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string? ConvertConfigValueToIniValue(string key, object value)
        {
            // Handle radio button groups
            if (key.StartsWith("difficulty_"))
            {
                if (Convert.ToBoolean(((JsonElement)value).ValueKind.ToString()))
                {
                    return key switch
                    {
                        "difficulty_fair" => "0",
                        "difficulty_unfair" => "1", 
                        "difficulty_very_unfair" => "2",
                        _ => "0"
                    };
                }
                return null; // Don't write if this radio button is not selected
            }

            if (key.StartsWith("key_placement_"))
            {
                if (Convert.ToBoolean(((JsonElement)value).ValueKind.ToString()))
                {
                    return key switch
                    {
                        "key_placement_not_shuffled" => "0",
                        "key_placement_shuffled" => "1",
                        "key_placement_race_mode" => "2",
                        "key_placement_race_mode_plus" => "3",
                        _ => "2"
                    };
                }
                return null;
            }

            if (key.StartsWith("soul_items_"))
            {
                if (Convert.ToBoolean(((JsonElement)value).ValueKind.ToString()))
                {
                    return key switch
                    {
                        "soul_items_shuffled" => "0",
                        "soul_items_replaced" => "1",
                        "soul_items_transposed" => "2",
                        _ => "2"
                    };
                }
                return null;
            }

            if (key.StartsWith("lordvessel_"))
            {
                if (Convert.ToBoolean(((JsonElement)value).ValueKind.ToString()))
                {
                    return key switch
                    {
                        "lordvessel_gwynevere" => "Gwynevere",
                        "lordvessel_randomized" => "Randomized",
                        "lordvessel_firelink" => "Firelink",
                        _ => "Firelink"
                    };
                }
                return null;
            }

            if (key.StartsWith("start_items_"))
            {
                if (Convert.ToBoolean(((JsonElement)value).ValueKind.ToString()))
                {
                    return key switch
                    {
                        "start_items_shield_1h" => "0",
                        "start_items_shield_12h" => "1",
                        "start_items_combined" => "2",
                        _ => "1"
                    };
                }
                return null;
            }

            // Handle non-radio button values
            switch (key)
            {
                case "seed":
                    return value.ToString() ?? "";

                default:
                    // Boolean values
                    if (value is JsonElement boolValue)
                    {
                        return Convert.ToBoolean(boolValue.ValueKind.ToString()) ? "True" : "False";
                    }
                    return value.ToString() ?? "";
            }
        }

        private void CreateIniFile(Dictionary<string, object> configuration, string iniPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[DEFAULT]");

            // Always use Remastered version
            sb.AppendLine("game_version = DARK SOULS: REMASTERED");

            // Map our configuration to the expected INI format
            foreach (var kvp in configuration)
            {
                string? value = ConvertConfigValueToIniValue(kvp.Key, kvp.Value);
                if (value != null)
                {
                    // Map radio button keys back to expected INI keys
                    string iniKey = kvp.Key switch
                    {
                        var k when k.StartsWith("difficulty_") => "difficulty",
                        var k when k.StartsWith("key_placement_") => "key_placement", 
                        var k when k.StartsWith("soul_items_") => "soul_items_diff",
                        var k when k.StartsWith("lordvessel_") => "use_lordvessel",
                        var k when k.StartsWith("start_items_") => "start_items_diff",
                        _ => kvp.Key
                    };
                    
                    sb.AppendLine($"{iniKey} = {value}");
                }
            }

            File.WriteAllText(iniPath, sb.ToString());
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
