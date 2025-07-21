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
                var settings = new AppSettings { GamePaths = gamePaths };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
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
    }

    public class AppSettings
    {
        public Dictionary<string, string> GamePaths { get; set; } = new Dictionary<string, string>();
    }
}
