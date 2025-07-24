namespace SoulsConfigurator.Interfaces
{
    public interface IMod
    {
        public string Name { get; }
        public string ModFile { get; }

        public bool TryInstallMod(string destPath);
        public bool TryRemoveMod(string destPath);
        
        /// <summary>
        /// Removes the mod with additional context about what will happen next
        /// </summary>
        /// <param name="destPath">The destination path where the mod is installed</param>
        /// <param name="willReinstall">True if this mod will be reinstalled immediately after removal</param>
        /// <returns>True if removal was successful, false otherwise</returns>
        public bool TryRemoveMod(string destPath, bool willReinstall) => TryRemoveMod(destPath);
        
        /// <summary>
        /// Checks if the mod files are available for installation
        /// </summary>
        /// <returns>True if all required mod files exist, false otherwise</returns>
        public bool IsAvailable();
    }
}
