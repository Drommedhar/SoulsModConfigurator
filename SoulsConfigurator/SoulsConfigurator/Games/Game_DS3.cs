using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Mods.DS3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace SoulsConfigurator.Games
{
    /// <summary>
    /// Dark Souls 3 game implementation with conditional ModEngine installation logic.
    /// 
    /// ModEngine Installation Strategy:
    /// - If Item/Enemy randomizer is installed: Extract ModEngine files directly from the randomizer zip to game directory
    /// - If only FogMod is installed (without Item/Enemy randomizer): Use dedicated ModEngine.zip
    /// - If neither mod is installed: No ModEngine installation required
    /// 
    /// CRITICAL: ModEngine must be installed BEFORE mods that need to modify modengine.ini
    /// Both FogMod and Item/Enemy randomizer call ModAutomationHelper.ModifyModEngineIni() during installation,
    /// so the modengine.ini file must exist in the game directory before these mods are installed.
    /// 
    /// Installation Order:
    /// 1. Extract/Install ModEngine (from Item/Enemy randomizer zip or standalone)
    /// 2. Install crashfix
    /// 3. Install all requested mods (which can now modify modengine.ini)
    /// </summary>
    public class Game_DS3 : IGame
    {
        public string Name => "Dark Souls 3";

        private string? _installPath;
        public string? InstallPath { get => _installPath; set => _installPath = value; }

        public string ModFolder => @"Data\DS3";

        private List<IMod> _mods = [];
        public List<IMod> Mods { get => _mods; set => _mods = value; }

        // Prerequisites needed for DS3 mods
        private IMod _modEngine = new DS3Mod_ModEngine();
        private IMod _crashFix = new DS3Mod_Crashfix();

        public Game_DS3()
        {
            // Add both mods
            _mods.Add(new DS3Mod_Item_Enemy());
            _mods.Add(new DS3Mod_FogGate());
        }

        public bool InstallMods(List<IMod> mods)
        {
            if (string.IsNullOrEmpty(_installPath))
            {
                return false;
            }

            // Check if all required mods and prerequisites are available
            if (!_crashFix.IsAvailable())
            {
                return false;
            }

            // Determine if we need ModEngine and which one to use
            bool shouldUseStandaloneModEngine = ShouldUseStandaloneModEngine(mods);
            bool hasItemEnemyRandomizer = IsItemEnemyRandomizerInstalled(mods);

            if (shouldUseStandaloneModEngine && !_modEngine.IsAvailable())
            {
                return false;
            }

            // If we need to extract ModEngine from Item/Enemy randomizer, ensure its zip file exists
            if (hasItemEnemyRandomizer)
            {
                string itemEnemyZipPath = Path.Combine("Data", "DS3", "DS3_Item_Enemy_Randomizer.zip");
                if (!File.Exists(itemEnemyZipPath))
                {
                    return false;
                }
            }

            foreach (var mod in mods)
            {
                if (!mod.IsAvailable())
                {
                    return false;
                }
            }

            BackupFiles();

            // CRITICAL: Install ModEngine BEFORE installing mods that need to modify modengine.ini
            if (hasItemEnemyRandomizer)
            {
                // Extract only ModEngine files from Item/Enemy randomizer zip
                if (!ExtractModEngineFromItemEnemyRandomizer())
                {
                    return false;
                }
            }
            else if (shouldUseStandaloneModEngine)
            {
                // Install standalone ModEngine first
                if (!_modEngine.TryInstallMod(_installPath))
                {
                    return false;
                }
            }

            // Always install crashfix
            if (!_crashFix.TryInstallMod(_installPath))
            {
                return false;
            }

            // Install all the requested mods
            foreach (var mod in mods)
            {
                if (!mod.TryInstallMod(_installPath))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ShouldUseStandaloneModEngine(List<IMod> mods)
        {
            // Use standalone ModEngine.zip only if:
            // 1. FogMod is installed AND Item/Enemy randomizer is NOT installed
            bool hasFogMod = mods.Any(mod => mod.Name == "DS3 Fog Gate Randomizer");
            bool hasItemEnemyMod = mods.Any(mod => mod.Name == "DS3 Item & Enemy Randomizer");

            return hasFogMod && !hasItemEnemyMod;
        }

        private bool IsItemEnemyRandomizerInstalled(List<IMod> mods)
        {
            return mods.Any(mod => mod.Name == "DS3 Item & Enemy Randomizer");
        }

        private bool ExtractModEngineFromItemEnemyRandomizer()
        {
            if (string.IsNullOrEmpty(_installPath))
            {
                return false;
            }

            try
            {
                string sourcePath = Path.Combine("Data", "DS3", "DS3_Item_Enemy_Randomizer.zip");
                if (!File.Exists(sourcePath))
                {
                    return false;
                }

                // Selectively extract only ModEngine files from the Item/Enemy randomizer zip
                // This avoids installing the full mod while ensuring we get the correct ModEngine version
                using (var archive = ZipFile.OpenRead(sourcePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Skip empty entries and directories
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        // Check if this is a ModEngine file we want to extract
                        if (IsModEngineFileInRandomizer(entry.FullName))
                        {
                            // Extract ModEngine files from randomizer/ModEngine/ to game root
                            string destinationPath = GetModEngineDestinationPath(entry.FullName, _installPath);
                            
                            // Create directory if it doesn't exist
                            string? destinationDir = Path.GetDirectoryName(destinationPath);
                            if (!string.IsNullOrEmpty(destinationDir))
                            {
                                Directory.CreateDirectory(destinationDir);
                            }

                            // Extract the file
                            entry.ExtractToFile(destinationPath, true);
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsModEngineFileInRandomizer(string fullPath)
        {
            // Check if the path is within the randomizer/ModEngine directory
            if (!fullPath.Contains("randomizer/ModEngine/", StringComparison.OrdinalIgnoreCase) &&
                !fullPath.Contains("randomizer\\ModEngine\\", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string fileName = Path.GetFileName(fullPath);
            
            // Check for essential ModEngine files
            if (fileName.Equals("dinput8.dll", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("modengine.ini", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check for mod folder contents (if ModEngine has a mod subdirectory)
            if (fullPath.Contains("/mod/", StringComparison.OrdinalIgnoreCase) ||
                fullPath.Contains("\\mod\\", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private string GetModEngineDestinationPath(string entryFullName, string destPath)
        {
            string fileName = Path.GetFileName(entryFullName);
            
            // Extract the relative path after randomizer/ModEngine/
            string modEnginePrefix = "randomizer/ModEngine/";
            string modEnginePrefixAlt = "randomizer\\ModEngine\\";
            
            string relativePath = "";
            if (entryFullName.Contains(modEnginePrefix, StringComparison.OrdinalIgnoreCase))
            {
                int index = entryFullName.IndexOf(modEnginePrefix, StringComparison.OrdinalIgnoreCase);
                relativePath = entryFullName.Substring(index + modEnginePrefix.Length);
            }
            else if (entryFullName.Contains(modEnginePrefixAlt, StringComparison.OrdinalIgnoreCase))
            {
                int index = entryFullName.IndexOf(modEnginePrefixAlt, StringComparison.OrdinalIgnoreCase);
                relativePath = entryFullName.Substring(index + modEnginePrefixAlt.Length);
            }
            else
            {
                // Fallback to just the filename
                relativePath = fileName;
            }
            
            // For main ModEngine files, place them directly in game root
            if (fileName.Equals("dinput8.dll", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("modengine.ini", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(destPath, fileName);
            }
            
            // For other files, maintain the relative directory structure
            return Path.Combine(destPath, relativePath);
        }

        public bool ClearMods()
        {
            // TODO: More error handling here!

            if (string.IsNullOrEmpty(_installPath))
            {
                return false; 
            }

            foreach (var mod in _mods)
            {
                if(!mod.TryRemoveMod(_installPath))
                {
                    return false;
                }
            }

            if (!_modEngine.TryRemoveMod(_installPath))
            {
                return false;
            }

            if (!_crashFix.TryRemoveMod(_installPath))
            {
                return false;
            }

            RestoreFiles();

            return true;
        }

        public bool BackupFiles()
        {
            if (string.IsNullOrEmpty(_installPath))
                return false;
                
            File.Move(Path.Combine(_installPath, "DarkSoulsIII.exe"), Path.Combine(_installPath, "DarkSoulsIII_org.exe"));
            return true;
        }

        public bool RestoreFiles()
        {
            if (string.IsNullOrEmpty(_installPath))
                return false;
                
            if (File.Exists(Path.Combine(_installPath, "DarkSoulsIII_org.exe")))
            {
                File.Move(Path.Combine(_installPath, "DarkSoulsIII_org.exe"), Path.Combine(_installPath, "DarkSoulsIII.exe"));
            }
                
            return true;
        }

        public bool ValidateInstallPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Check if the path ends with the expected folder name
            if (!path.EndsWith("Game", StringComparison.OrdinalIgnoreCase))
                return false;

            // Check if the executable exists
            var executablePath = Path.Combine(path, GetExpectedExecutableName());
            return File.Exists(executablePath);
        }

        public string GetExpectedExecutableName()
        {
            return "DarkSoulsIII.exe";
        }
    }
}
