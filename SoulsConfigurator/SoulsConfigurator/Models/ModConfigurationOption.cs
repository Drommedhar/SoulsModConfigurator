using System.Collections.Generic;

namespace SoulsConfigurator.Models
{
    public enum ModControlType
    {
        CheckBox,
        RadioButton,
        TextBox,
        TrackBar,
        ComboBox,
        Button
    }

    public class ModConfigurationOption
    {
        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public required string Description { get; set; }
        public ModControlType ControlType { get; set; }
        public required string ControlName { get; set; }
        public required object DefaultValue { get; set; }
        public required string GroupName { get; set; }
        public string? TabName { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<string> RadioButtonGroup { get; set; } = new List<string>(); // For mutually exclusive options

        // Support for conditional controls
        public string? EnabledWhen { get; set; } // Name of the control that must be enabled for this control to be enabled
        public object? EnabledWhenValue { get; set; } // Value that the dependent control must have (default: true for checkboxes/radio buttons)

        // Support for control ordering
        public int Order { get; set; } = 0; // Used to determine display order within a group
    }

    public class ModConfiguration
    {
        public required string ModName { get; set; }
        public required string ExecutablePath { get; set; }
        public required string WindowTitle { get; set; }
        public List<ModConfigurationOption> Options { get; set; } = new List<ModConfigurationOption>();
    }

    public class ModPreset
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public Dictionary<string, object> OptionValues { get; set; } = new Dictionary<string, object>();
    }
}
