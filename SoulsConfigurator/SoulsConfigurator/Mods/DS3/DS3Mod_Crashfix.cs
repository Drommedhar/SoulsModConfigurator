using SoulsConfigurator.Interfaces;
using System;
using System.Collections.Generic;
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
