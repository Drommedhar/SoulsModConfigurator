using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Models;
using SoulsConfigurator.Services;
using System.Collections.Generic;
using System.Linq;

namespace SoulsConfigurator.Services
{
    public class ModConfigurationService
    {
        private readonly List<IConfigurableMod> _configurableMods;

        public ModConfigurationService()
        {
            _configurableMods = new List<IConfigurableMod>();
        }

        public void RegisterMod(IConfigurableMod mod)
        {
            _configurableMods.Add(mod);
        }

        public List<IConfigurableMod> GetConfigurableMods()
        {
            return _configurableMods.ToList();
        }

        public IConfigurableMod? GetModByName(string modName)
        {
            return _configurableMods.FirstOrDefault(m => m.Name == modName);
        }

        public ModConfiguration? GetModConfiguration(string modName)
        {
            var mod = GetModByName(modName);
            return mod?.GetUIConfiguration();
        }

        public List<UserPreset> GetModUserPresets(string modName)
        {
            var mod = GetModByName(modName);
            return mod?.GetUserPresets() ?? new List<UserPreset>();
        }

        public bool RunModWithConfiguration(string modName, Dictionary<string, object> configuration, string destPath)
        {
            var mod = GetModByName(modName);
            return mod?.RunWithConfiguration(configuration, destPath) ?? false;
        }

        public bool ApplyModUserPreset(string modName, string presetName, string destPath)
        {
            var mod = GetModByName(modName);
            return mod?.ApplyUserPreset(presetName, destPath) ?? false;
        }

        public Dictionary<string, object> CreateConfigurationFromUserPreset(string modName, string presetName)
        {
            var preset = UserPresetService.Instance.GetPreset(modName, presetName);
            return preset?.OptionValues ?? new Dictionary<string, object>();
        }

        public bool ValidateConfiguration(string modName, Dictionary<string, object> configuration)
        {
            var modConfig = GetModConfiguration(modName);
            if (modConfig == null)
                return false;

            // Check that all required options are present and have valid values
            foreach (var option in modConfig.Options)
            {
                if (!configuration.ContainsKey(option.Name))
                {
                    // Use default value if not specified
                    configuration[option.Name] = option.DefaultValue;
                }

                // Validate radio button groups
                if (option.ControlType == ModControlType.RadioButton && option.RadioButtonGroup.Count > 0)
                {
                    // Ensure only one option in the group is selected
                    var groupOptions = modConfig.Options.Where(o => option.RadioButtonGroup.Contains(o.Name)).ToList();
                    int selectedCount = groupOptions.Count(o => configuration.ContainsKey(o.Name) && Convert.ToBoolean(configuration[o.Name]));
                    
                    if (selectedCount != 1)
                    {
                        // Reset the group to default state
                        foreach (var groupOption in groupOptions)
                        {
                            configuration[groupOption.Name] = groupOption.DefaultValue;
                        }
                    }
                }
            }

            return true;
        }

        public void SaveUserPreset(string modName, UserPreset preset)
        {
            UserPresetService.Instance.SavePreset(modName, preset);
        }

        public void DeleteUserPreset(string modName, string presetName)
        {
            UserPresetService.Instance.DeletePreset(modName, presetName);
        }
    }
}
