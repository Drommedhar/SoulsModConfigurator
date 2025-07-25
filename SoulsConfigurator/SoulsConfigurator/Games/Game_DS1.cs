using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Mods.DS1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoulsConfigurator.Games
{
    public class Game_DS1 : IGame
    {
        public string Name => "Dark Souls 1";

        private string? _installPath;
        public string? InstallPath { get => _installPath; set => _installPath = value; }

        public string ModFolder => @"Data\DS1";

        private List<IMod> _mods = [];
        public List<IMod> Mods { get => _mods; set => _mods = value; }

        public Game_DS1()
        {
            _mods.Add(new DS1Mod_EnemyRandomizer());
            _mods.Add(new DS1Mod_ItemRandomizer());
            _mods.Add(new DS1Mod_FogGate());
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

        public async Task<bool> InstallModsAsync(List<IMod> mods, Action<string>? statusUpdater = null)
        {
            if (string.IsNullOrEmpty(_installPath))
            {
                return false;
            }

            statusUpdater?.Invoke("Checking mod availability...");
            await Task.Delay(200);

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

            int currentMod = 0;
            int totalMods = mods.Count;
            foreach (var mod in mods)
            {
                currentMod++;
                statusUpdater?.Invoke($"Installing mod {currentMod} of {totalMods}: {mod.Name}");
                
                if (mod is DS1Mod_EnemyRandomizer enemyRandomizer && enemyRandomizer.TryInstallModAsync != null)
                {
                    if (!await enemyRandomizer.TryInstallModAsync(_installPath, statusUpdater))
                    {
                        return false;
                    }
                }
                else if (mod is DS1Mod_ItemRandomizer itemRandomizer && itemRandomizer.TryInstallModAsync != null)
                {
                    if (!await itemRandomizer.TryInstallModAsync(_installPath, statusUpdater))
                    {
                        return false;
                    }
                }
                else if (mod is DS1Mod_FogGate fogGate && fogGate.TryInstallModAsync != null)
                {
                    if (!await fogGate.TryInstallModAsync(_installPath, statusUpdater))
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

            int currentMod = 0;
            int totalMods = _mods.Count;
            foreach (var mod in _mods)
            {
                currentMod++;
                statusUpdater?.Invoke($"Removing mod {currentMod} of {totalMods}: {mod.Name}");
                
                // Check if this is the enemy randomizer and if it will be reinstalled
                bool willReinstall = false;
                if (mod is DS1Mod_EnemyRandomizer)
                {
                    willReinstall = false; // We're just removing, not reinstalling
                }

                if (!mod.TryRemoveMod(_installPath, willReinstall))
                {
                    return false;
                }
                
                await Task.Delay(200);
            }

            statusUpdater?.Invoke("Restoring original game files...");
            await Task.Delay(300);
            RestoreFiles();
            return true;
        }

        public bool ClearMods()
        {
            return ClearMods(null);
        }

        public bool ClearMods(List<IMod>? modsToInstallNext)
        {
            if (string.IsNullOrEmpty(_installPath))
            {
                return false;
            }

            foreach (var mod in _mods)
            {
                // Check if this is the enemy randomizer and if it will be reinstalled
                bool willReinstall = false;
                if (mod is DS1Mod_EnemyRandomizer && modsToInstallNext != null)
                {
                    willReinstall = modsToInstallNext.Any(m => m is DS1Mod_EnemyRandomizer);
                }

                if (!mod.TryRemoveMod(_installPath, willReinstall))
                {
                    return false;
                }
            }

            RestoreFiles();
            return true;
        }

        public bool BackupFiles()
        {
            // TODO: Implement DS1 specific backup logic
            return true;
        }

        public bool RestoreFiles()
        {
            // TODO: Implement DS1 specific restore logic
            return true;
        }

        public bool ValidateInstallPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Check if the DS1 Remastered executable exists
            var executablePath = Path.Combine(path, GetExpectedExecutableName());
            return File.Exists(executablePath);
        }

        public string GetExpectedExecutableName()
        {
            return "DarkSoulsRemastered.exe";
        }
    }
}
