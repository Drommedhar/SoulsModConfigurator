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
        public bool BackupFiles();
        public bool RestoreFiles();
        public bool ValidateInstallPath(string path);
        public string GetExpectedExecutableName();
    }
}
