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

            if (!_combinedSFX.TryInstallMod(_installPath))
            {
                return false;
            }

            if (!_divineDragonTextures.TryInstallMod(_installPath))
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
