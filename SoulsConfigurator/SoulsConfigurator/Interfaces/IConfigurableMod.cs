using SoulsConfigurator.Models;
using SoulsConfigurator.Services;
using System.Collections.Generic;

namespace SoulsConfigurator.Interfaces
{
    public interface IConfigurableMod : IMod
    {
        /// <summary>
        /// Gets the UI configuration for this mod
        /// </summary>
        ModConfiguration GetUIConfiguration();

        /// <summary>
        /// Runs the mod with the specified configuration
        /// </summary>
        /// <param name="configuration">The configuration options to apply</param>
        /// <param name="destPath">The destination path where the mod should be applied</param>
        /// <returns>True if successful, false otherwise</returns>
        bool RunWithConfiguration(Dictionary<string, object> configuration, string destPath);

        /// <summary>
        /// Gets available user-saved presets for this mod
        /// </summary>
        List<UserPreset> GetUserPresets();

        /// <summary>
        /// Applies a user preset configuration
        /// </summary>
        /// <param name="presetName">Name of the user preset to apply</param>
        /// <param name="destPath">The destination path where the mod should be applied</param>
        /// <returns>True if successful, false otherwise</returns>
        bool ApplyUserPreset(string presetName, string destPath);

        /// <summary>
        /// Saves configuration for later use when installing the mod
        /// </summary>
        /// <param name="configuration">The configuration to save</param>
        void SaveConfiguration(Dictionary<string, object> configuration);

        /// <summary>
        /// Gets the saved configuration
        /// </summary>
        /// <returns>The saved configuration, or null if none is saved</returns>
        Dictionary<string, object>? GetSavedConfiguration();

        /// <summary>
        /// Gets the selected preset name for this mod (if any)
        /// </summary>
        string? GetSelectedPreset();

        /// <summary>
        /// Sets the selected preset name for this mod
        /// </summary>
        /// <param name="presetName">Name of the preset to select</param>
        void SetSelectedPreset(string? presetName);
    }
}
