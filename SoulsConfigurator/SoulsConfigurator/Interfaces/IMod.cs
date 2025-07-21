namespace SoulsConfigurator.Interfaces
{
    public interface IMod
    {
        public string Name { get; }
        public string ModFile { get; }

        public bool TryInstallMod(string destPath);
        public bool TryRemoveMod(string destPath);
        
        /// <summary>
        /// Checks if the mod files are available for installation
        /// </summary>
        /// <returns>True if all required mod files exist, false otherwise</returns>
        public bool IsAvailable();
    }
}
