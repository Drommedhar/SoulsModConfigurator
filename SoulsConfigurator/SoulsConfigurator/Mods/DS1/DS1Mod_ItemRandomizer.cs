using SoulsConfigurator.Interfaces;
using System;
using System.IO.Compression;
using System.IO;

namespace SoulsConfigurator.Mods.DS1
{
    public class DS1Mod_ItemRandomizer : IMod
    {
        public string Name => "Item Randomizer";
        public string ModFile => "Dark Souls Item Randomizer v0.3-86-v0-3.zip";

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
                // TODO: Implement specific removal logic for DS1 Item Randomizer
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
