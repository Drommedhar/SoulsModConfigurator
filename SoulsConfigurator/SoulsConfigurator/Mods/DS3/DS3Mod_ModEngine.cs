using SoulsConfigurator.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Async version of TryInstallMod with status reporting capability
        /// </summary>
        public async Task<bool> TryInstallModAsync(string destPath, Action<string>? statusUpdater = null)
        {
            try
            {
                statusUpdater?.Invoke("Installing DS3 Mod Engine...");
                await Task.Delay(100); // Small delay to show the message
                
                statusUpdater?.Invoke("Extracting Mod Engine files...");
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS3", ModFile), destPath);
                
                statusUpdater?.Invoke("Mod Engine installed successfully!");
                return true;
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
                File.Delete(Path.Combine(destPath, "dinput8.dll"));
                File.Delete(Path.Combine(destPath, "modengine.ini"));
                File.Delete(Path.Combine(destPath, "readme.txt"));
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
