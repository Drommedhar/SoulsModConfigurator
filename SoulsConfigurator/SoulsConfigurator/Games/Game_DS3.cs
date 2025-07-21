using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Mods.DS3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsConfigurator.Games
{
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
            if (!_modEngine.IsAvailable() || !_crashFix.IsAvailable())
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

            if (!_crashFix.TryInstallMod(_installPath))
            {
                return false;
            }

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
