using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsConfigurator.Interfaces
{
    public interface IGame
    {
        public string Name { get; }
        public string? InstallPath { get; set; }
        public string ModFolder { get; }
        public List<IMod> Mods { get; }

        public bool InstallMods(List<IMod> mods);
        public bool ClearMods();
        
        /// <summary>
        /// Clears all mods with context about what will be installed next
        /// </summary>
        /// <param name="modsToInstallNext">The mods that will be installed after clearing, or null if none</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ClearMods(List<IMod>? modsToInstallNext) => ClearMods();
        
        /// <summary>
        /// Async version of InstallMods with status reporting capability
        /// </summary>
        public Task<bool> InstallModsAsync(List<IMod> mods, Action<string>? statusUpdater = null);
        
        /// <summary>
        /// Async version of ClearMods with status reporting capability
        /// </summary>
        public Task<bool> ClearModsAsync(Action<string>? statusUpdater = null);
        
        /// <summary>
        /// Async version of ClearMods with context about what will be installed next
        /// </summary>
        public Task<bool> ClearModsAsync(List<IMod>? modsToInstallNext, Action<string>? statusUpdater = null) => ClearModsAsync(statusUpdater);
        
        public bool BackupFiles();
        public bool RestoreFiles();
        public bool ValidateInstallPath(string path);
        public string GetExpectedExecutableName();
    }
}
