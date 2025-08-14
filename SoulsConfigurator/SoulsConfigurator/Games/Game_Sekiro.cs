using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Mods.Sekiro;
using System;
using System.Collections.Generic;
using System.IO;

namespace SoulsConfigurator.Games
{
    public class Game_Sekiro : IGame
    {
        public string Name => "Sekiro: Shadows Die Twice";

        private string? _installPath;
        public string? InstallPath { get => _installPath; set => _installPath = value; }

        public string ModFolder => @"Data\Sekiro";

        private List<IMod> _mods = [];
        public List<IMod> Mods { get => _mods; set => _mods = value; }

        // Prerequisites needed for Sekiro mods
        private IMod _modEngine = new SekiroMod_ModEngine();
        private IMod _combinedSFX = new SekiroMod_CombinedSFX();
        private IMod _divineDragonTextures = new SekiroMod_DivineDragonTextures();

        public Game_Sekiro()
        {
            // Add the main randomizer mod
            _mods.Add(new SekiroMod_Randomizer());
        }

        public bool InstallMods(List<IMod> mods)
        {
            if (string.IsNullOrEmpty(_installPath))
            {
                return false;
            }

            // Check if all required mods and prerequisites are available
            if (!_modEngine.IsAvailable() || !_combinedSFX.IsAvailable() || !_divineDragonTextures.IsAvailable())
            {
                return false;
            }

            foreach (var mod in mods)
            {
                if (!mod.IsAvailable())
                {
                    return false;
                }
            }

            BackupFiles();

            // Always install prerequisites first
            if (!_modEngine.TryInstallMod(_installPath))
            {
                return false;
            }

            // Then install selected mods
            foreach (var mod in mods)
            {
                if (!mod.TryInstallMod(_installPath))
                {
                    return false;
                }
            }

            if (!_combinedSFX.TryInstallMod(Path.Combine(_installPath, "randomizer")))
            {
                return false;
            }

            if (!_divineDragonTextures.TryInstallMod(Path.Combine(_installPath, "randomizer")))
            {
                return false;
            }

            return true;
        }

        public async Task<bool> InstallModsAsync(List<IMod> mods, Action<string>? statusUpdater = null)
        {
            if (string.IsNullOrEmpty(_installPath))
            {
                return false;
            }

            statusUpdater?.Invoke("Checking mod availability...");
            await Task.Delay(200);

            // Check if all required mods and prerequisites are available
            if (!_modEngine.IsAvailable() || !_combinedSFX.IsAvailable() || !_divineDragonTextures.IsAvailable())
            {
                statusUpdater?.Invoke("Error: Prerequisites not available");
                return false;
            }

            foreach (var mod in mods)
            {
                if (!mod.IsAvailable())
                {
                    statusUpdater?.Invoke($"Error: {mod.Name} not available");
                    return false;
                }
            }

            statusUpdater?.Invoke("Backing up game files...");
            await Task.Delay(300);
            BackupFiles();

            // Always install prerequisites first
            statusUpdater?.Invoke("Installing prerequisite: Sekiro Mod Engine");
            if (_modEngine is SekiroMod_ModEngine sekiroModEngine && sekiroModEngine.TryInstallModAsync != null)
            {
                if (!await sekiroModEngine.TryInstallModAsync(_installPath, statusUpdater))
                {
                    return false;
                }
            }
            else if (!_modEngine.TryInstallMod(_installPath))
            {
                return false;
            }

            statusUpdater?.Invoke("Installing prerequisite: Sekiro Combined SFX");
            if (_combinedSFX is SekiroMod_CombinedSFX combinedSFX && combinedSFX.TryInstallModAsync != null)
            {
                if (!await combinedSFX.TryInstallModAsync(Path.Combine(_installPath, "randomizer"), statusUpdater))
                {
                    return false;
                }
            }
            else if (!_combinedSFX.TryInstallMod(Path.Combine(_installPath, "randomizer")))
            {
                return false;
            }

            statusUpdater?.Invoke("Installing prerequisite: Sekiro Divine Dragon Textures");
            if (_divineDragonTextures is SekiroMod_DivineDragonTextures textures && textures.TryInstallModAsync != null)
            {
                if (!await textures.TryInstallModAsync(Path.Combine(_installPath, "randomizer"), statusUpdater))
                {
                    return false;
                }
            }
            else if (!_divineDragonTextures.TryInstallMod(Path.Combine(_installPath, "randomizer")))
            {
                return false;
            }

            // Then install selected mods
            int currentMod = 0;
            int totalMods = mods.Count;
            foreach (var mod in mods)
            {
                currentMod++;
                statusUpdater?.Invoke($"Installing mod {currentMod} of {totalMods}: {mod.Name}");
                
                if (mod is SekiroMod_Randomizer randomizerMod && randomizerMod.TryInstallModAsync != null)
                {
                    if (!await randomizerMod.TryInstallModAsync(_installPath, statusUpdater))
                    {
                        return false;
                    }
                }
                else if (!mod.TryInstallMod(_installPath))
                {
                    return false;
                }
                
                await Task.Delay(200);
            }

            return true;
        }

        public async Task<bool> ClearModsAsync(Action<string>? statusUpdater = null)
        {
            if (string.IsNullOrEmpty(_installPath))
            {
                return false;
            }

            statusUpdater?.Invoke("Starting mod removal...");
            await Task.Delay(200);

            // Remove user-selected mods
            int currentMod = 0;
            int totalMods = _mods.Count + 3; // +3 for prerequisites
            foreach (var mod in _mods)
            {
                currentMod++;
                statusUpdater?.Invoke($"Removing mod {currentMod} of {totalMods}: {mod.Name}");
                
                if (!mod.TryRemoveMod(_installPath))
                {
                    return false;
                }
                await Task.Delay(200);
            }

            // Remove prerequisites in reverse order
            currentMod++;
            statusUpdater?.Invoke($"Removing prerequisite {currentMod} of {totalMods}: Divine Dragon Textures");
            if (!_divineDragonTextures.TryRemoveMod(_installPath))
            {
                return false;
            }
            await Task.Delay(200);

            currentMod++;
            statusUpdater?.Invoke($"Removing prerequisite {currentMod} of {totalMods}: Combined SFX");
            if (!_combinedSFX.TryRemoveMod(_installPath))
            {
                return false;
            }
            await Task.Delay(200);

            currentMod++;
            statusUpdater?.Invoke($"Removing prerequisite {currentMod} of {totalMods}: Mod Engine");
            if (!_modEngine.TryRemoveMod(_installPath))
            {
                return false;
            }

            statusUpdater?.Invoke("Restoring original game files...");
            await Task.Delay(300);
            RestoreFiles();
            return true;
        }

        public bool ClearMods()
        {
            if (string.IsNullOrEmpty(_installPath))
            {
                return false;
            }

            // Remove user-selected mods
            foreach (var mod in _mods)
            {
                if (!mod.TryRemoveMod(_installPath))
                {
                    return false;
                }
            }

            // Remove prerequisites
            if (!_divineDragonTextures.TryRemoveMod(_installPath))
            {
                return false;
            }

            if (!_combinedSFX.TryRemoveMod(_installPath))
            {
                return false;
            }

            if (!_modEngine.TryRemoveMod(_installPath))
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

            // For Sekiro, we might need to backup specific files
            // For now, return true as basic implementation
            return true;
        }

        public bool RestoreFiles()
        {
            if (string.IsNullOrEmpty(_installPath))
                return false;

            // For Sekiro, restore any backed up files
            // For now, return true as basic implementation
            return true;
        }

        public bool ValidateInstallPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // For Sekiro, we expect the main game folder containing the executable
            var executablePath = Path.Combine(path, GetExpectedExecutableName());
            return File.Exists(executablePath);
        }

        public string GetExpectedExecutableName()
        {
            return "sekiro.exe";
        }
    }
}
