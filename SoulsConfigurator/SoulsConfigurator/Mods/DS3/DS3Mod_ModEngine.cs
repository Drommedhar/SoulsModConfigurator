using SoulsConfigurator.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsConfigurator.Mods.DS3
{
    public class DS3Mod_ModEngine : IMod
    {
        public string Name => "Mod Engine";

        public string ModFile => "ModEngine.zip";

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "DS3", ModFile);
            return File.Exists(sourcePath);
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS3", ModFile), destPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryRemoveMod(string destPath)
        {
            try
            {
                File.Delete(Path.Combine(destPath, "dinput8.dll"));
                File.Delete(Path.Combine(destPath, "modengine.ini"));
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
