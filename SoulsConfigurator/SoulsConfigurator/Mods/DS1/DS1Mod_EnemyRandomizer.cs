using SoulsConfigurator.Interfaces;
using System;
using System.IO.Compression;
using System.IO;

namespace SoulsConfigurator.Mods.DS1
{
    public class DS1Mod_EnemyRandomizer : IMod
    {
        public string Name => "Enemy Randomizer";
        public string ModFile => "Dark Souls Enemy Randomizer v0.4.1.2-1407-v0-4-1-2-1550837469.zip";

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "DS1", ModFile);
            return File.Exists(sourcePath);
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS1", ModFile), destPath);
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
                // TODO: Implement specific removal logic for DS1 Enemy Randomizer
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
