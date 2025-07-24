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
        
        public bool BackupFiles();
        public bool RestoreFiles();
        public bool ValidateInstallPath(string path);
        public string GetExpectedExecutableName();
    }
}
