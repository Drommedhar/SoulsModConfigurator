using SoulsConfigurator.Interfaces;
using System;
using System.IO;
using System.IO.Compression;

namespace SoulsConfigurator.Mods.Sekiro
{
    public class SekiroMod_DivineDragonTextures : IMod
    {
        public string Name => "Sekiro Divine Dragon Textures";
        public string ModFile => "Divine_Dragon_Textures.zip";

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
                // Remove texture files - typically in texture/graphics folders
                string[] dirsToRemove = {
                    Path.Combine(destPath, "parts"),
                    Path.Combine(destPath, "chr"),
                    Path.Combine(destPath, "textures")
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