using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SoulsConfigurator.Models;

namespace SoulsConfigurator.Services
{
    public class UserPresetService
    {
        private const string PresetsFileName = "user_presets.json";
        private readonly string _presetsPath;

        // Event to notify when presets are changed
        public event EventHandler<PresetChangedEventArgs>? PresetChanged;

        public UserPresetService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "SoulsConfigurator");
            Directory.CreateDirectory(appFolder);
            _presetsPath = Path.Combine(appFolder, PresetsFileName);
        }

        public List<UserPreset> LoadPresets(string modName)
        {
            try
            {
                if (File.Exists(_presetsPath))
                {
                    var json = File.ReadAllText(_presetsPath);
                    var allPresets = JsonSerializer.Deserialize<Dictionary<string, List<UserPreset>>>(json);
                    
                    if (allPresets != null && allPresets.TryGetValue(modName, out var modPresets))
                    {
                        return modPresets;
                    }
                }
            }
            catch (Exception)
            {
                // If there's an error loading presets, return empty list
            }

            return new List<UserPreset>();
        }

        public void SavePreset(string modName, UserPreset preset)
        {
            var allPresets = LoadAllPresets();
            
            if (!allPresets.ContainsKey(modName))
            {
                allPresets[modName] = new List<UserPreset>();
            }

            // Check if this is an update or new preset
            bool isUpdate = allPresets[modName].Any(p => p.Name == preset.Name);

            // Remove existing preset with same name
            allPresets[modName].RemoveAll(p => p.Name == preset.Name);
            
            // Add new/updated preset
            allPresets[modName].Add(preset);

            SaveAllPresets(allPresets);

            // Notify listeners that a preset was saved
            var changeType = isUpdate ? PresetChangeType.Updated : PresetChangeType.Added;
            
            PresetChanged?.Invoke(this, new PresetChangedEventArgs(
                modName, 
                preset.Name, 
                changeType
            ));
        }

        public void DeletePreset(string modName, string presetName)
        {
            var allPresets = LoadAllPresets();
            
            if (allPresets.ContainsKey(modName))
            {
                var removed = allPresets[modName].RemoveAll(p => p.Name == presetName) > 0;
                
                if (allPresets[modName].Count == 0)
                {
                    allPresets.Remove(modName);
                }

                SaveAllPresets(allPresets);

                // Notify listeners that a preset was deleted (only if something was actually removed)
                if (removed)
                {
                    PresetChanged?.Invoke(this, new PresetChangedEventArgs(
                        modName, 
                        presetName, 
                        PresetChangeType.Deleted
                    ));
                }
            }
        }

        public UserPreset? GetPreset(string modName, string presetName)
        {
            var presets = LoadPresets(modName);
            return presets.Find(p => p.Name == presetName);
        }

        private Dictionary<string, List<UserPreset>> LoadAllPresets()
        {
            try
            {
                if (File.Exists(_presetsPath))
                {
                    var json = File.ReadAllText(_presetsPath);
                    return JsonSerializer.Deserialize<Dictionary<string, List<UserPreset>>>(json) ?? new Dictionary<string, List<UserPreset>>();
                }
            }
            catch (Exception)
            {
                // If there's an error loading presets, return empty dictionary
            }

            return new Dictionary<string, List<UserPreset>>();
        }

        private void SaveAllPresets(Dictionary<string, List<UserPreset>> allPresets)
        {
            try
            {
                var json = JsonSerializer.Serialize(allPresets, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_presetsPath, json);
            }
            catch (Exception)
            {
                // Silently fail if we can't save presets
            }
        }
    }

    /// <summary>
    /// Event arguments for preset change notifications
    /// </summary>
    public class PresetChangedEventArgs : EventArgs
    {
        public string ModName { get; }
        public string PresetName { get; }
        public PresetChangeType ChangeType { get; }

        public PresetChangedEventArgs(string modName, string presetName, PresetChangeType changeType)
        {
            ModName = modName;
            PresetName = presetName;
            ChangeType = changeType;
        }
    }

    /// <summary>
    /// Types of preset changes
    /// </summary>
    public enum PresetChangeType
    {
        Added,
        Updated,
        Deleted
    }

    public class UserPreset
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public Dictionary<string, object> OptionValues { get; set; } = new Dictionary<string, object>();
    }
}