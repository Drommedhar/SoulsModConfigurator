using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SoulsConfigurator.Services
{
    public class SettingsService
    {
        private const string SettingsFileName = "souls_configurator_settings.json";
        private readonly string _settingsPath;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "SoulsConfigurator");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, SettingsFileName);
        }

        public Dictionary<string, string> LoadGamePaths()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings?.GamePaths ?? new Dictionary<string, string>();
                }
            }
            catch (Exception)
            {
                // If there's an error loading settings, return empty dictionary
            }

            return new Dictionary<string, string>();
        }

        public void SaveGamePaths(Dictionary<string, string> gamePaths)
        {
            try
            {
                var settings = LoadSettings();
                settings.GamePaths = gamePaths;
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // Silently fail if we can't save settings
            }
        }

        public void SaveGamePath(string gameName, string path)
        {
            var gamePaths = LoadGamePaths();
            gamePaths[gameName] = path;
            SaveGamePaths(gamePaths);
        }

        public string? GetGamePath(string gameName)
        {
            var gamePaths = LoadGamePaths();
            return gamePaths.TryGetValue(gameName, out var path) ? path : null;
        }

        /// <summary>
        /// Saves the Nexus Mods API key for persistent authentication
        /// </summary>
        public void SaveNexusApiKey(string apiKey)
        {
            try
            {
                var settings = LoadSettings();
                settings.NexusApiKey = apiKey;
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // Silently fail if we can't save settings
            }
        }

        /// <summary>
        /// Loads the saved Nexus Mods API key
        /// </summary>
        public string? GetNexusApiKey()
        {
            try
            {
                var settings = LoadSettings();
                return settings.NexusApiKey;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Clears the saved Nexus Mods API key (for logout functionality)
        /// </summary>
        public void ClearNexusApiKey()
        {
            try
            {
                var settings = LoadSettings();
                settings.NexusApiKey = null;
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // Silently fail if we can't save settings
            }
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception)
            {
                // If there's an error loading settings, return default
            }

            return new AppSettings();
        }

        private void SaveSettings(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception)
            {
                // Silently fail if we can't save settings
            }
        }
    }

    public class AppSettings
    {
        public Dictionary<string, string> GamePaths { get; set; } = new Dictionary<string, string>();
        public string? NexusApiKey { get; set; }
    }
}
