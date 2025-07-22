using SoulsConfigurator.Interfaces;
using System;
using System.IO;
using System.IO.Compression;

namespace SoulsConfigurator.Mods.Sekiro
{
    public class SekiroMod_CombinedSFX : IMod
    {
        public string Name => "Sekiro Combined SFX";
        public string ModFile => "Combined_SFX.zip";

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "Sekiro", ModFile);
            return File.Exists(sourcePath);
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                string sourcePath = Path.Combine("Data", "Sekiro", ModFile);
                if (File.Exists(sourcePath))
                {
                    ZipFile.ExtractToDirectory(sourcePath, destPath, true);
                    return true;
                }
                return false;
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
                // Remove SFX files - typically in sound folders
                string[] dirsToRemove = {
                    Path.Combine(destPath, "sound"),
                    Path.Combine(destPath, "sfx")
                };

                foreach (string dir in dirsToRemove)
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}