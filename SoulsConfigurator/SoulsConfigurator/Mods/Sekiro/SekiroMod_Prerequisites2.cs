using SoulsConfigurator.Interfaces;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

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

        /// <summary>
        /// Async version of TryInstallMod with status reporting capability
        /// </summary>
        public async Task<bool> TryInstallModAsync(string destPath, Action<string>? statusUpdater = null)
        {
            try
            {
                statusUpdater?.Invoke("Installing Sekiro Divine Dragon Textures...");
                
                string sourcePath = Path.Combine("Data", "Sekiro", ModFile);
                if (File.Exists(sourcePath))
                {
                    statusUpdater?.Invoke("Extracting texture files...");
                    await Task.Delay(100); // Small delay to show the message
                    
                    ZipFile.ExtractToDirectory(sourcePath, destPath, true);
                    
                    statusUpdater?.Invoke("Divine Dragon Textures installed successfully!");
                    return true;
                }
                else
                {
                    statusUpdater?.Invoke("Error: Divine_Dragon_Textures.zip not found");
                    return false;
                }
            }
            catch (Exception ex)
            {
                statusUpdater?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }

        public bool TryRemoveMod(string destPath)
        {
            try
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}