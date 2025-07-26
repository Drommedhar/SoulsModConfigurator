using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Models;
using SoulsConfigurator.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SoulsConfigurator.Mods.DS1
{
    public class DS1Mod_EnemyRandomizer : IMod, IConfigurableMod
    {
        private ModConfiguration? _configuration;
        private Dictionary<string, object>? _savedConfiguration;
        private string? _selectedPreset;
        private EnemyConfigManager? _enemyConfigManager;

        public string Name => "Dark Souls 1 Enemy Randomizer";
        public string ModFile => ""; // This mod is deployed with our application

        public DS1Mod_EnemyRandomizer()
        {
            InitializeConfiguration();
        }

        public bool IsAvailable()
        {
            // Check if the randomizer executable exists in our Data/DS1 folder
            string randomizerPath = Path.Combine("Data", "DS1", "enemy_randomizer.exe");
            return File.Exists(randomizerPath);
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                // Copy the randomizer executable
                File.Copy(
                    Path.Combine("Data", "DS1", "enemy_randomizer.exe"),
                    Path.Combine(destPath, "enemy_randomizer.exe"),
                    true);

                // Copy the entire enemyRandomizerData folder
                string sourceDataFolder = Path.Combine("Data", "DS1", "enemyRandomizerData");
                string destDataFolder = Path.Combine(destPath, "enemyRandomizerData");
                
                if (Directory.Exists(sourceDataFolder))
                {
                    CopyDirectory(sourceDataFolder, destDataFolder, true);
                }

                // Handle the valid_new.txt file specifically for enemy enable/disable
                string sourceValidNew = Path.Combine("Data", "DS1", "valid_new.txt");
                string destValidNew = Path.Combine(destPath, "enemyRandomizerData", "customConfigs", "valid_new.txt");
                
                if (File.Exists(sourceValidNew))
                {
                    // Ensure the replacement_ref directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(destValidNew)!);
                    
                    if (_savedConfiguration != null)
                    {
                        // Apply user configuration to valid_new.txt
                        ApplyEnemyConfiguration(sourceValidNew, destValidNew);
                    }
                    else
                    {
                        // Just copy the default file
                        File.Copy(sourceValidNew, destValidNew, true);
                    }
                }

                // Generate and copy the INI file for other randomizer settings
                string destIniPath = Path.Combine(destPath, "enemy_randomizer.ini");
                GenerateIniFile(destIniPath);

                // Execute the enemy randomizer to apply the randomization
                return ExecuteEnemyRandomizer(destPath, destIniPath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ExecuteEnemyRandomizer(string workingDir, string iniPath)
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Path.Combine(workingDir, "enemy_randomizer.exe"),
                    Arguments = $"--randomize --config \"{iniPath}\"",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    if (process == null)
                        return false;

                    // Read output for better user feedback
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    // Consider it successful if the process exits with code 0
                    bool success = process.ExitCode == 0;

                    // Log output for debugging
                    if (!string.IsNullOrEmpty(output))
                    {
                        System.Diagnostics.Debug.WriteLine($"Enemy Randomizer Output: {output}");
                    }
                    if (!string.IsNullOrEmpty(error))
                    {
                        System.Diagnostics.Debug.WriteLine($"Enemy Randomizer Error: {error}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing enemy randomizer: {ex.Message}");
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
                statusUpdater?.Invoke("Preparing Enemy Randomizer installation...");
                
                // Copy the randomizer executable
                statusUpdater?.Invoke("Copying enemy randomizer executable...");
                File.Copy(
                    Path.Combine("Data", "DS1", "enemy_randomizer.exe"),
                    Path.Combine(destPath, "enemy_randomizer.exe"),
                    true);

                // Copy the entire enemyRandomizerData folder
                statusUpdater?.Invoke("Copying enemy randomizer data files...");
                string sourceDataFolder = Path.Combine("Data", "DS1", "enemyRandomizerData");
                string destDataFolder = Path.Combine(destPath, "enemyRandomizerData");
                
                if (Directory.Exists(sourceDataFolder))
                {
                    CopyDirectory(sourceDataFolder, destDataFolder, true);
                }

                // Handle the valid_new.txt file specifically for enemy enable/disable
                statusUpdater?.Invoke("Applying enemy configuration...");
                string sourceValidNew = Path.Combine("Data", "DS1", "valid_new.txt");
                string destValidNew = Path.Combine(destPath, "enemyRandomizerData", "customConfigs", "valid_new.txt");
                
                if (File.Exists(sourceValidNew))
                {
                    // Ensure the replacement_ref directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(destValidNew)!);
                    
                    if (_savedConfiguration != null)
                    {
                        // Apply user configuration to valid_new.txt
                        ApplyEnemyConfiguration(sourceValidNew, destValidNew);
                    }
                    else
                    {
                        // Just copy the default file
                        File.Copy(sourceValidNew, destValidNew, true);
                    }
                }

                // Generate and copy the INI file for other randomizer settings
                statusUpdater?.Invoke("Generating configuration file...");
                string destIniPath = Path.Combine(destPath, "enemy_randomizer.ini");
                GenerateIniFile(destIniPath);

                // Execute the enemy randomizer to apply the randomization
                statusUpdater?.Invoke("Running enemy randomizer...");
                bool result = await ExecuteEnemyRandomizerAsync(destPath, destIniPath, statusUpdater);
                
                if (result)
                {
                    statusUpdater?.Invoke("Enemy Randomizer installed successfully!");
                }
                else
                {
                    statusUpdater?.Invoke("Enemy Randomizer installation failed.");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                statusUpdater?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ExecuteEnemyRandomizerAsync(string workingDir, string iniPath, Action<string>? statusUpdater = null)
        {
            try
            {
                statusUpdater?.Invoke("Starting enemy randomizer process...");
                
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Path.Combine(workingDir, "enemy_randomizer.exe"),
                    Arguments = $"--randomize --config \"{iniPath}\"",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        statusUpdater?.Invoke("Failed to start enemy randomizer process");
                        return false;
                    }

                    // Wait for the process to complete without overwhelming UI updates
                    statusUpdater?.Invoke("Processing enemy randomization - please wait...");
                    await process.WaitForExitAsync();

                    // Consider it successful if the process exits with code 0
                    bool success = process.ExitCode == 0;

                    return success;
                }
            }
            catch (Exception ex)
            {
                statusUpdater?.Invoke($"Error executing enemy randomizer: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error executing enemy randomizer: {ex.Message}");
                return false;
            }
        }

        public bool TryRemoveMod(string destPath)
        {
            return TryRemoveMod(destPath, false);
        }

        public bool TryRemoveMod(string destPath, bool willReinstall)
        {
            try
            {
                // First, revert the game data to vanilla
                string randomizerPath = Path.Combine(destPath, "enemy_randomizer.exe");
                if (File.Exists(randomizerPath))
                {
                    try
                    {
                        ProcessStartInfo processInfo = new ProcessStartInfo
                        {
                            FileName = randomizerPath,
                            WorkingDirectory = destPath,
                            Arguments = willReinstall ? "--revert --no-revert-effects" : "--revert",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using (var process = System.Diagnostics.Process.Start(processInfo))
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
                    "enemy_randomizer.exe",
                    "enemy_randomizer.ini",
                    "valid_new.txt"
                };

                foreach (string file in filesToRemove)
                {
                    string fullPath = Path.Combine(destPath, file);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }

                // Also remove the enemyRandomizerData folder
                string dataFolder = Path.Combine(destPath, "enemyRandomizerData");
                if (Directory.Exists(dataFolder))
                {
                    Directory.Delete(dataFolder, true);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ApplyEnemyConfiguration(string sourceValidNew, string destValidNew)
        {
            if (_savedConfiguration == null) return;

            var configManager = new EnemyConfigManager(sourceValidNew, destValidNew);
            configManager.LoadEnemies();

            // Apply saved enemy enable/disable configuration
            foreach (var kvp in _savedConfiguration)
            {
                if (kvp.Key.StartsWith("enemy_") && kvp.Value is bool enabled)
                {
                    string enemyId = kvp.Key;
                    configManager.SetEnemyEnabled(enemyId, enabled);
                }
            }

            configManager.SaveConfiguration();
        }

        private void GenerateIniFile(string iniFilePath)
        {
            var iniLines = new List<string>
            {
                "[DEFAULT]",
                "# Main Replacement Options",
                "boss_replace_mode = 1",
                "enemy_replace_mode = 2", 
                "npc_mode = 0",
                "mimic_mode = 1",
                "fit_mode = 0",
                "difficulty_mode = 3",
                "replace_chance_slider = 100",
                "boss_chance_slider = 10",
                "boss_chance_slider_bosses = 90",
                "gargoyle_mode = 1",
                "diff_strictness = 1",
                "tpose_city = 0",
                "boss_souls_slider = 50",
                "pinwheel_chaos = 1",
                "type_replacement = 1",
                "gwyn_nerf = 1",
                "prevent_same = 1",
                "unique_bosses = 0",
                "respawning_bosses = 0",
                "hostile_npcs = 0",
                "mosquito_replacement = 1",
                "seed_value = ",
                "",
                "# Enemy Configuration - enemies are controlled via valid_new.txt",
                "enemy_config_name = Custom_SoulsConfigurator",
                "",
                "# Additional Settings",
                "game_path = ",
                "backup_saves = True",
                "create_logs = True",
                "auto_backup = True"
            };

            // Apply user configuration values if available
            if (_savedConfiguration != null)
            {
                for (int i = 0; i < iniLines.Count; i++)
                {
                    string line = iniLines[i];
                    if (line.Contains(" = ") && !line.StartsWith("#"))
                    {
                        string key = line.Split(" = ")[0];
                        if (_savedConfiguration.ContainsKey(key))
                        {
                            var value = _savedConfiguration[key];
                            iniLines[i] = $"{key} = {value}";
                        }
                    }
                }
            }

            File.WriteAllLines(iniFilePath, iniLines);
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        private void InitializeConfiguration()
        {
            _configuration = new ModConfiguration
            {
                ModName = Name,
                ExecutablePath = "enemy_randomizer.exe",
                WindowTitle = "Dark Souls Enemy Randomizer"
            };

            int order = 0;

            // Main Replacement Options Tab
            _configuration.Options.AddRange(new[]
            {
                // Replace Bosses group
                new ModConfigurationOption { Name = "boss_replace_mode", DisplayName = "Don't replace", Description = "Bosses are not replaced", ControlType = ModControlType.RadioButton, ControlName = "boss_dont_replace", DefaultValue = false, GroupName = "Replace Bosses", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "boss_dont_replace", "boss_only_bosses", "boss_only_normal", "boss_bosses_or_normal" } },
                new ModConfigurationOption { Name = "boss_replace_mode", DisplayName = "Only with bosses", Description = "Bosses are replaced only with other bosses", ControlType = ModControlType.RadioButton, ControlName = "boss_only_bosses", DefaultValue = true, GroupName = "Replace Bosses", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "boss_dont_replace", "boss_only_bosses", "boss_only_normal", "boss_bosses_or_normal" } },
                new ModConfigurationOption { Name = "boss_replace_mode", DisplayName = "Only with normal enemies", Description = "Bosses are replaced only with normal enemies", ControlType = ModControlType.RadioButton, ControlName = "boss_only_normal", DefaultValue = false, GroupName = "Replace Bosses", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "boss_dont_replace", "boss_only_bosses", "boss_only_normal", "boss_bosses_or_normal" } },
                new ModConfigurationOption { Name = "boss_replace_mode", DisplayName = "With bosses or normal enemies", Description = "Bosses can be replaced with bosses or normal enemies", ControlType = ModControlType.RadioButton, ControlName = "boss_bosses_or_normal", DefaultValue = false, GroupName = "Replace Bosses", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "boss_dont_replace", "boss_only_bosses", "boss_only_normal", "boss_bosses_or_normal" } },

                // Replace Normal Enemies group
                new ModConfigurationOption { Name = "enemy_replace_mode", DisplayName = "Don't replace", Description = "Normal enemies are not replaced", ControlType = ModControlType.RadioButton, ControlName = "enemy_dont_replace", DefaultValue = false, GroupName = "Replace Normal Enemies", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "enemy_dont_replace", "enemy_only_bosses", "enemy_only_normal", "enemy_bosses_or_normal" } },
                new ModConfigurationOption { Name = "enemy_replace_mode", DisplayName = "Only with bosses", Description = "Normal enemies are replaced only with bosses", ControlType = ModControlType.RadioButton, ControlName = "enemy_only_bosses", DefaultValue = false, GroupName = "Replace Normal Enemies", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "enemy_dont_replace", "enemy_only_bosses", "enemy_only_normal", "enemy_bosses_or_normal" } },
                new ModConfigurationOption { Name = "enemy_replace_mode", DisplayName = "Only with normal enemies", Description = "Normal enemies are replaced only with other normal enemies", ControlType = ModControlType.RadioButton, ControlName = "enemy_only_normal", DefaultValue = true, GroupName = "Replace Normal Enemies", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "enemy_dont_replace", "enemy_only_bosses", "enemy_only_normal", "enemy_bosses_or_normal" } },
                new ModConfigurationOption { Name = "enemy_replace_mode", DisplayName = "With bosses or normal enemies", Description = "Normal enemies can be replaced with bosses or other normal enemies", ControlType = ModControlType.RadioButton, ControlName = "enemy_bosses_or_normal", DefaultValue = false, GroupName = "Replace Normal Enemies", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "enemy_dont_replace", "enemy_only_bosses", "enemy_only_normal", "enemy_bosses_or_normal" } },

                // Replace NPCs group
                new ModConfigurationOption { Name = "npc_mode", DisplayName = "Do not replace", Description = "NPCs are not replaced", ControlType = ModControlType.RadioButton, ControlName = "npc_dont_replace", DefaultValue = true, GroupName = "Replace NPCs", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "npc_dont_replace", "npc_only_bosses", "npc_only_normal", "npc_bosses_or_normal" } },
                new ModConfigurationOption { Name = "npc_mode", DisplayName = "Only with bosses", Description = "NPCs are replaced only with bosses", ControlType = ModControlType.RadioButton, ControlName = "npc_only_bosses", DefaultValue = false, GroupName = "Replace NPCs", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "npc_dont_replace", "npc_only_bosses", "npc_only_normal", "npc_bosses_or_normal" } },
                new ModConfigurationOption { Name = "npc_mode", DisplayName = "Only with normal enemies", Description = "NPCs are replaced only with normal enemies", ControlType = ModControlType.RadioButton, ControlName = "npc_only_normal", DefaultValue = false, GroupName = "Replace NPCs", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "npc_dont_replace", "npc_only_bosses", "npc_only_normal", "npc_bosses_or_normal" } },
                new ModConfigurationOption { Name = "npc_mode", DisplayName = "With bosses or normal enemies", Description = "NPCs can be replaced with bosses or normal enemies", ControlType = ModControlType.RadioButton, ControlName = "npc_bosses_or_normal", DefaultValue = false, GroupName = "Replace NPCs", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "npc_dont_replace", "npc_only_bosses", "npc_only_normal", "npc_bosses_or_normal" } },

                // Mode group
                new ModConfigurationOption { Name = "difficulty_mode", DisplayName = "Difficulty curve + easy asylum", Description = "Enemies get progressively harder with easy asylum", ControlType = ModControlType.RadioButton, ControlName = "mode_difficulty_easy", DefaultValue = false, GroupName = "Mode", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "mode_difficulty_easy", "mode_random_curve", "mode_fully_random_easy", "mode_fully_random" } },
                new ModConfigurationOption { Name = "difficulty_mode", DisplayName = "Random with difficulty curve", Description = "Random placement following difficulty curve", ControlType = ModControlType.RadioButton, ControlName = "mode_random_curve", DefaultValue = false, GroupName = "Mode", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "mode_difficulty_easy", "mode_random_curve", "mode_fully_random_easy", "mode_fully_random" } },
                new ModConfigurationOption { Name = "difficulty_mode", DisplayName = "Fully random + easy asylum", Description = "Completely random with easy asylum", ControlType = ModControlType.RadioButton, ControlName = "mode_fully_random_easy", DefaultValue = true, GroupName = "Mode", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "mode_difficulty_easy", "mode_random_curve", "mode_fully_random_easy", "mode_fully_random" } },
                new ModConfigurationOption { Name = "difficulty_mode", DisplayName = "Fully random", Description = "Completely random placement", ControlType = ModControlType.RadioButton, ControlName = "mode_fully_random", DefaultValue = false, GroupName = "Mode", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "mode_difficulty_easy", "mode_random_curve", "mode_fully_random_easy", "mode_fully_random" } },

                // Difficulty strictness group
                new ModConfigurationOption { Name = "diff_strictness", DisplayName = "Strict", Description = "Strict difficulty matching", ControlType = ModControlType.RadioButton, ControlName = "strict_strict", DefaultValue = false, GroupName = "Difficulty strictness", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "strict_strict", "strict_loose", "strict_very_loose" } },
                new ModConfigurationOption { Name = "diff_strictness", DisplayName = "A bit loose", Description = "Somewhat loose difficulty matching", ControlType = ModControlType.RadioButton, ControlName = "strict_loose", DefaultValue = true, GroupName = "Difficulty strictness", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "strict_strict", "strict_loose", "strict_very_loose" } },
                new ModConfigurationOption { Name = "diff_strictness", DisplayName = "Very loose", Description = "Very loose difficulty matching", ControlType = ModControlType.RadioButton, ControlName = "strict_very_loose", DefaultValue = false, GroupName = "Difficulty strictness", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "strict_strict", "strict_loose", "strict_very_loose" } },

                // Enemy placement group
                new ModConfigurationOption { Name = "fit_mode", DisplayName = "Only where they fit", Description = "Enemies are only placed where they fit properly", ControlType = ModControlType.RadioButton, ControlName = "placement_fit", DefaultValue = true, GroupName = "Enemy placement", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "placement_fit", "placement_anywhere", "placement_anywhere_except_npc" } },
                new ModConfigurationOption { Name = "fit_mode", DisplayName = "Anywhere", Description = "Enemies can be placed anywhere", ControlType = ModControlType.RadioButton, ControlName = "placement_anywhere", DefaultValue = false, GroupName = "Enemy placement", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "placement_fit", "placement_anywhere", "placement_anywhere_except_npc" } },
                new ModConfigurationOption { Name = "fit_mode", DisplayName = "Anywhere, except when replacing NPCs", Description = "Enemies can be placed anywhere except NPC locations", ControlType = ModControlType.RadioButton, ControlName = "placement_anywhere_except_npc", DefaultValue = false, GroupName = "Enemy placement", TabName = "Main Replacement Options", Order = order++, RadioButtonGroup = new List<string> { "placement_fit", "placement_anywhere", "placement_anywhere_except_npc" } },

                // Sliders
                new ModConfigurationOption { Name = "replace_chance_slider", DisplayName = "Replacement chance", Description = "Percentage chance for replacement", ControlType = ModControlType.TrackBar, ControlName = "replacement_chance", DefaultValue = 100, GroupName = "Replacement chance (%)", TabName = "Main Replacement Options", Order = order++, Properties = { ["Minimum"] = 0, ["Maximum"] = 100 } },
                new ModConfigurationOption { Name = "boss_chance_slider", DisplayName = "Boss chance [Normal Enemies]", Description = "Chance that a normal enemy or NPC will be replaced with a boss instead of an normal enemy", ControlType = ModControlType.TrackBar, ControlName = "boss_chance_normal", DefaultValue = 10, GroupName = "Boss chance [Normal Enemies] (%)", TabName = "Main Replacement Options", Order = order++, Properties = { ["Minimum"] = 0, ["Maximum"] = 100 } },
                new ModConfigurationOption { Name = "boss_chance_slider_bosses", DisplayName = "Boss chance [Bosses]", Description = "Chance that a boss will be replaced with a boss instead of an normal enemy", ControlType = ModControlType.TrackBar, ControlName = "boss_chance_bosses", DefaultValue = 90, GroupName = "Boss chance [Bosses] (%)", TabName = "Main Replacement Options", Order = order++, Properties = { ["Minimum"] = 0, ["Maximum"] = 100 } }
            });

            // Other Options Tab
            order = 0;
            _configuration.Options.AddRange(new[]
            {
                // Mimics group
                new ModConfigurationOption { Name = "mimic_mode", DisplayName = "Do not replace", Description = "Mimics are not replaced", ControlType = ModControlType.RadioButton, ControlName = "mimic_dont_replace", DefaultValue = false, GroupName = "Mimics", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "mimic_dont_replace", "mimic_replace" } },
                new ModConfigurationOption { Name = "mimic_mode", DisplayName = "Replace", Description = "Mimics are replaced", ControlType = ModControlType.RadioButton, ControlName = "mimic_replace", DefaultValue = true, GroupName = "Mimics", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "mimic_dont_replace", "mimic_replace" } },

                // Gargoyle #2 group
                new ModConfigurationOption { Name = "gargoyle_mode", DisplayName = "Do not replace", Description = "Second gargoyle is not replaced", ControlType = ModControlType.RadioButton, ControlName = "gargoyle_dont_replace", DefaultValue = false, GroupName = "Gargoyle #2", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "gargoyle_dont_replace", "gargoyle_replace" } },
                new ModConfigurationOption { Name = "gargoyle_mode", DisplayName = "Replace", Description = "Second gargoyle is replaced", ControlType = ModControlType.RadioButton, ControlName = "gargoyle_replace", DefaultValue = true, GroupName = "Gargoyle #2", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "gargoyle_dont_replace", "gargoyle_replace" } },

                // T-Posing enemies group
                new ModConfigurationOption { Name = "tpose_city", DisplayName = "Enabled", Description = "Allow T-posing enemies", ControlType = ModControlType.RadioButton, ControlName = "tpose_enabled", DefaultValue = true, GroupName = "T-Posing enemies", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "tpose_enabled", "tpose_disabled" } },
                new ModConfigurationOption { Name = "tpose_city", DisplayName = "Disabled", Description = "Prevent T-posing enemies", ControlType = ModControlType.RadioButton, ControlName = "tpose_disabled", DefaultValue = false, GroupName = "T-Posing enemies", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "tpose_enabled", "tpose_disabled" } },

                // Prevent replacement with same enemy group
                new ModConfigurationOption { Name = "prevent_same", DisplayName = "Enabled", Description = "Prevent replacing enemy with the same enemy", ControlType = ModControlType.RadioButton, ControlName = "prevent_same_enabled", DefaultValue = false, GroupName = "Prevent replacement with same enemy", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "prevent_same_enabled", "prevent_same_disabled" } },
                new ModConfigurationOption { Name = "prevent_same", DisplayName = "Disabled", Description = "Allow replacing enemy with the same enemy", ControlType = ModControlType.RadioButton, ControlName = "prevent_same_disabled", DefaultValue = true, GroupName = "Prevent replacement with same enemy", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "prevent_same_enabled", "prevent_same_disabled" } },

                // Roaming boss soul drops slider
                new ModConfigurationOption { Name = "boss_souls_slider", DisplayName = "Roaming boss soul drops", Description = "Controls the amount of souls bosses that replace normal enemies drop", ControlType = ModControlType.TrackBar, ControlName = "roaming_boss_souls", DefaultValue = 50, GroupName = "Roaming boss soul drops (%)", TabName = "Other Options", Order = order++, Properties = { ["Minimum"] = 0, ["Maximum"] = 100 } },

                // Type Replacement group
                new ModConfigurationOption { Name = "type_replacement", DisplayName = "Enabled", Description = "Enable type replacement", ControlType = ModControlType.RadioButton, ControlName = "type_enabled", DefaultValue = false, GroupName = "Type Replacement", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "type_enabled", "type_enabled_except_roaming", "type_disabled" } },
                new ModConfigurationOption { Name = "type_replacement", DisplayName = "Enabled, except roaming bosses", Description = "Enable type replacement except for roaming bosses", ControlType = ModControlType.RadioButton, ControlName = "type_enabled_except_roaming", DefaultValue = false, GroupName = "Type Replacement", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "type_enabled", "type_enabled_except_roaming", "type_disabled" } },
                new ModConfigurationOption { Name = "type_replacement", DisplayName = "Disabled", Description = "Disable type replacement", ControlType = ModControlType.RadioButton, ControlName = "type_disabled", DefaultValue = true, GroupName = "Type Replacement", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "type_enabled", "type_enabled_except_roaming", "type_disabled" } },

                // Pinwheel Chaos group
                new ModConfigurationOption { Name = "pinwheel_chaos", DisplayName = "Enabled", Description = "Enable Pinwheel chaos mode", ControlType = ModControlType.RadioButton, ControlName = "pinwheel_enabled", DefaultValue = false, GroupName = "Pinwheel Chaos", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "pinwheel_enabled", "pinwheel_disabled" } },
                new ModConfigurationOption { Name = "pinwheel_chaos", DisplayName = "Disabled", Description = "Disable Pinwheel chaos mode", ControlType = ModControlType.RadioButton, ControlName = "pinwheel_disabled", DefaultValue = true, GroupName = "Pinwheel Chaos", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "pinwheel_enabled", "pinwheel_disabled" } },

                // Gwyn Spawn-Rate Nerf group
                new ModConfigurationOption { Name = "gwyn_nerf", DisplayName = "Strong", Description = "Strong Gwyn nerf", ControlType = ModControlType.RadioButton, ControlName = "gwyn_strong", DefaultValue = false, GroupName = "Gwyn Spawn-Rate Nerf", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "gwyn_strong", "gwyn_medium", "gwyn_none" } },
                new ModConfigurationOption { Name = "gwyn_nerf", DisplayName = "Medium", Description = "Medium Gwyn nerf", ControlType = ModControlType.RadioButton, ControlName = "gwyn_medium", DefaultValue = true, GroupName = "Gwyn Spawn-Rate Nerf", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "gwyn_strong", "gwyn_medium", "gwyn_none" } },
                new ModConfigurationOption { Name = "gwyn_nerf", DisplayName = "None", Description = "No Gwyn nerf", ControlType = ModControlType.RadioButton, ControlName = "gwyn_none", DefaultValue = false, GroupName = "Gwyn Spawn-Rate Nerf", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "gwyn_strong", "gwyn_medium", "gwyn_none" } },

                // Try for unique bosses group
                new ModConfigurationOption { Name = "unique_bosses", DisplayName = "Enabled", Description = "Try to keep bosses unique", ControlType = ModControlType.RadioButton, ControlName = "unique_enabled", DefaultValue = false, GroupName = "Try for unique bosses", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "unique_enabled", "unique_disabled" } },
                new ModConfigurationOption { Name = "unique_bosses", DisplayName = "Disabled", Description = "Allow duplicate bosses", ControlType = ModControlType.RadioButton, ControlName = "unique_disabled", DefaultValue = true, GroupName = "Try for unique bosses", TabName = "Other Options", Order = order++, RadioButtonGroup = new List<string> { "unique_enabled", "unique_disabled" } }
            });

            // Even More Options Tab
            order = 0;
            _configuration.Options.AddRange(new[]
            {
                // Bosses replacing normals respawning group
                new ModConfigurationOption { Name = "respawning_bosses", DisplayName = "Enabled", Description = "Bosses replacing normal enemies can respawn", ControlType = ModControlType.RadioButton, ControlName = "respawning_enabled", DefaultValue = true, GroupName = "Bosses replacing normals respawning", TabName = "Even More Options", Order = order++, RadioButtonGroup = new List<string> { "respawning_enabled", "respawning_disabled" } },
                new ModConfigurationOption { Name = "respawning_bosses", DisplayName = "Disabled", Description = "Bosses replacing normal enemies don't respawn", ControlType = ModControlType.RadioButton, ControlName = "respawning_disabled", DefaultValue = false, GroupName = "Bosses replacing normals respawning", TabName = "Even More Options", Order = order++, RadioButtonGroup = new List<string> { "respawning_enabled", "respawning_disabled" } },

                // Hostile NPCs group
                new ModConfigurationOption { Name = "hostile_npcs", DisplayName = "Enabled", Description = "Make NPCs hostile", ControlType = ModControlType.RadioButton, ControlName = "hostile_enabled", DefaultValue = false, GroupName = "Hostile NPCs", TabName = "Even More Options", Order = order++, RadioButtonGroup = new List<string> { "hostile_enabled", "hostile_disabled" } },
                new ModConfigurationOption { Name = "hostile_npcs", DisplayName = "Disabled", Description = "Keep NPCs non-hostile", ControlType = ModControlType.RadioButton, ControlName = "hostile_disabled", DefaultValue = true, GroupName = "Hostile NPCs", TabName = "Even More Options", Order = order++, RadioButtonGroup = new List<string> { "hostile_enabled", "hostile_disabled" } },

                // Respawning mosquito replacement group
                new ModConfigurationOption { Name = "mosquito_replacement", DisplayName = "Enabled", Description = "Enable mosquito replacement", ControlType = ModControlType.RadioButton, ControlName = "mosquito_enabled", DefaultValue = true, GroupName = "Respawning mosquito replacement", TabName = "Even More Options", Order = order++, RadioButtonGroup = new List<string> { "mosquito_enabled", "mosquito_disabled" } },
                new ModConfigurationOption { Name = "mosquito_replacement", DisplayName = "Disabled", Description = "Disable mosquito replacement", ControlType = ModControlType.RadioButton, ControlName = "mosquito_disabled", DefaultValue = false, GroupName = "Respawning mosquito replacement", TabName = "Even More Options", Order = order++, RadioButtonGroup = new List<string> { "mosquito_enabled", "mosquito_disabled" } },

                // Seed value
                new ModConfigurationOption { Name = "seed_value", DisplayName = "Seed Value", Description = "Random seed (leave empty for random)", ControlType = ModControlType.TextBox, ControlName = "seed_value", DefaultValue = "", GroupName = "Seed", TabName = "Even More Options", Order = order++ }
            });

            // Enemy Configuration Tab
            string validNewPath = Path.Combine("Data", "DS1", "valid_new.txt");
            if (File.Exists(validNewPath))
            {
                _enemyConfigManager = new EnemyConfigManager(validNewPath);
                _enemyConfigManager.LoadEnemies();

                var enemiesByType = _enemyConfigManager.GetEnemiesByType();
                order = 0;

                foreach (var typeGroup in enemiesByType)
                {
                    string typeName = typeGroup.Key;
                    var enemies = typeGroup.Value;

                    // Add individual enemy options
                    foreach (var enemy in enemies)
                    {
                        _configuration.Options.Add(new ModConfigurationOption
                        {
                            Name = enemy.ID,
                            DisplayName = enemy.Name,
                            Description = enemy.DisplayName,
                            ControlType = ModControlType.CheckBox,
                            ControlName = $"chk_{enemy.ID}",
                            DefaultValue = enemy.IsEnabledByDefault,
                            GroupName = $"{typeName} Selection",
                            TabName = "Enemy Configuration",
                            Order = order++
                        });
                    }
                }
            }
        }

        public ModConfiguration GetUIConfiguration()
        {
            return _configuration ?? new ModConfiguration 
            { 
                ModName = Name, 
                ExecutablePath = "enemy_randomizer.exe",
                WindowTitle = "Dark Souls Enemy Randomizer"
            };
        }

        public bool RunWithConfiguration(Dictionary<string, object> configuration, string destPath)
        {
            SaveConfiguration(configuration);
            return TryInstallMod(destPath);
        }

        public List<UserPreset> GetUserPresets()
        {
            return UserPresetService.Instance.LoadPresets(Name);
        }

        public bool ApplyUserPreset(string presetName, string destPath)
        {
            var preset = UserPresetService.Instance.GetPreset(Name, presetName);
            if (preset != null)
            {
                _selectedPreset = presetName;
                return RunWithConfiguration(preset.OptionValues, destPath);
            }
            return false;
        }

        public void SaveConfiguration(Dictionary<string, object> configuration)
        {
            _savedConfiguration = configuration;
        }

        public Dictionary<string, object>? GetSavedConfiguration()
        {
            return _savedConfiguration;
        }

        public void SetConfiguration(Dictionary<string, object> configuration)
        {
            _savedConfiguration = configuration;
        }

        public string[] GetPresetNames()
        {
            var presets = UserPresetService.Instance.LoadPresets(Name);
            return presets.Select(p => p.Name).ToArray();
        }

        public void SavePreset(string presetName, Dictionary<string, object> configuration)
        {
            var preset = new UserPreset
            {
                Name = presetName,
                Description = $"Enemy configuration preset: {presetName}",
                OptionValues = configuration
            };
            UserPresetService.Instance.SavePreset(Name, preset);
        }

        public Dictionary<string, object>? LoadPreset(string presetName)
        {
            var preset = UserPresetService.Instance.GetPreset(Name, presetName);
            return preset?.OptionValues;
        }

        public void DeletePreset(string presetName)
        {
            UserPresetService.Instance.DeletePreset(Name, presetName);
        }

        public void SetSelectedPreset(string? presetName)
        {
            _selectedPreset = presetName;
        }

        public string? GetSelectedPreset()
        {
            return _selectedPreset;
        }
    }
}
