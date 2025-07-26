using SoulsConfigurator.Interfaces;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

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

        /// <summary>
        /// Async version of TryInstallMod with status reporting capability
        /// </summary>
        public async Task<bool> TryInstallModAsync(string destPath, Action<string>? statusUpdater = null)
        {
            try
            {
                statusUpdater?.Invoke("Installing Sekiro Combined SFX...");
                
                string sourcePath = Path.Combine("Data", "Sekiro", ModFile);
                if (File.Exists(sourcePath))
                {
                    statusUpdater?.Invoke("Extracting SFX files...");
                    await Task.Delay(100); // Small delay to show the message
                    
                    ZipFile.ExtractToDirectory(sourcePath, destPath, true);
                    
                    statusUpdater?.Invoke("Combined SFX installed successfully!");
                    return true;
                }
                else
                {
                    statusUpdater?.Invoke("Error: Combined_SFX.zip not found");
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