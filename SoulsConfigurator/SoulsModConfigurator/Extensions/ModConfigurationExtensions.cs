using System.Collections.Generic;
using System.Windows;
using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Models;
using SoulsConfigurator.Services;
using SoulsModConfigurator.Windows;

namespace SoulsModConfigurator.Extensions
{
    public static class ModConfigurationExtensions
    {
        /// <summary>
        /// Shows the WPF mod configuration window for a configurable mod
        /// </summary>
        /// <param name="configurableMod">The configurable mod to configure</param>
        /// <param name="owner">The owner window for modal display</param>
        /// <returns>The configuration if OK was clicked, null if cancelled</returns>
        public static Dictionary<string, object>? ShowConfigurationWindow(
            this IConfigurableMod configurableMod, 
            Window? owner = null)
        {
            var modConfiguration = configurableMod.GetUIConfiguration();
            var configWindow = new ModConfigurationWindow(modConfiguration);
            
            if (owner != null)
            {
                configWindow.Owner = owner;
            }
            
            // Load saved configuration if available
            var savedConfig = configurableMod.GetSavedConfiguration();
            if (savedConfig != null)
            {
                configWindow.LoadSavedConfiguration(savedConfig);
            }
            
            var result = configWindow.ShowDialog();
            
            if (result == true && configWindow.DialogResultValue)
            {
                var configuration = configWindow.SavedConfiguration;
                configurableMod.SaveConfiguration(configuration);
                return configuration;
            }
            
            return null;
        }
        
        /// <summary>
        /// Shows the WPF mod configuration window with direct ModConfiguration
        /// </summary>
        /// <param name="modConfiguration">The mod configuration to display</param>
        /// <param name="owner">The owner window for modal display</param>
        /// <returns>The configuration if OK was clicked, null if cancelled</returns>
        public static Dictionary<string, object>? ShowConfigurationWindow(
            ModConfiguration modConfiguration,
            Window? owner = null)
        {
            var configWindow = new ModConfigurationWindow(modConfiguration);
            
            if (owner != null)
            {
                configWindow.Owner = owner;
            }
            
            var result = configWindow.ShowDialog();
            
            if (result == true && configWindow.DialogResultValue)
            {
                return configWindow.SavedConfiguration;
            }
            
            return null;
        }
    }
}