using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Mods.DS2;
using System;
using System.Collections.Generic;
using System.IO;

namespace SoulsConfigurator.Games
{
    public class Game_DS2 : IGame
    {
        public string Name => "Dark Souls 2";

        private string? _installPath;
        public string? InstallPath { get => _installPath; set => _installPath = value; }

        public string ModFolder => @"Data\DS2";

        private List<IMod> _mods = [];
        public List<IMod> Mods { get => _mods; set => _mods = value; }

        public Game_DS2()
        {
            _mods.Add(new DS2Mod_Randomizer());
        }

        public bool InstallMods(List<IMod> mods)
        {
            if (string.IsNullOrEmpty(_installPath))
            {
                return false;
            }

            BackupFiles();

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

            foreach (var mod in _mods)
            {
                if (!mod.TryRemoveMod(_installPath))
                {
                    return false;
                }
            }

            RestoreFiles();
            return true;
        }

        public bool BackupFiles()
        {
            // TODO: Implement DS2 specific backup logic
            return true;
        }

        public bool RestoreFiles()
        {
            // TODO: Implement DS2 specific restore logic
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
            return "DarkSoulsII.exe";
        }
    }
}
