using SoulsConfigurator.Interfaces;
using System;
using System.IO.Compression;
using System.IO;

namespace SoulsConfigurator.Mods.DS2
{
    public class DS2Mod_Randomizer : IMod
    {
        public string Name => "DS2 Randomizer";
        public string ModFile => "DS2SRandomizer-1317-3-3-1-1747310715.zip";

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "DS2", ModFile);
            return File.Exists(sourcePath);
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS2", ModFile), destPath);
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
                // TODO: Implement specific removal logic for DS2 Randomizer
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
