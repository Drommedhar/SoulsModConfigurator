using SoulsConfigurator.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsConfigurator.Mods.DS3
{
    public class DS3Mod_Crashfix : IMod
    {
        public string Name => "Crashfix";

        public string ModFile => "DarkSoulsIII.exe";

        public bool TryInstallMod(string destPath)
        {
            File.Copy(Path.Combine("Data", "DS3", ModFile), Path.Combine(destPath, "DarkSoulsIII.exe"));
            return true;
        }

        /// <summary>
        /// Async version of TryInstallMod with status reporting capability
        /// </summary>
        public async Task<bool> TryInstallModAsync(string destPath, Action<string>? statusUpdater = null)
        {
            try
            {
                statusUpdater?.Invoke("Installing DS3 Crashfix...");
                await Task.Delay(100); // Small delay to show the message
                
                statusUpdater?.Invoke("Copying DarkSoulsIII.exe...");
                File.Copy(Path.Combine("Data", "DS3", ModFile), Path.Combine(destPath, "DarkSoulsIII.exe"), true);
                
                statusUpdater?.Invoke("Crashfix installed successfully!");
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
                if(File.Exists(Path.Combine(destPath, "DarkSoulsIII_org.exe")))
                {
                    File.Delete(Path.Combine(destPath, ModFile));
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "DS3", ModFile);
            return File.Exists(sourcePath);
        }
    }
}
