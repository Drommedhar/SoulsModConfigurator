using SoulsConfigurator.Models;
using SoulsConfigurator.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace SoulsConfigurator.UI
{
    public partial class ModConfigurationForm : Form
    {
        private ModConfiguration _modConfiguration;
        private Dictionary<string, Control> _controls;
        private Dictionary<string, object> _currentConfiguration;
        private readonly UserPresetService _presetService;
        private ComboBox? _presetComboBox;
        private string? _currentlyLoadedPresetName; // Track which preset is currently loaded

        public Dictionary<string, object> Configuration => _currentConfiguration;
        public Dictionary<string, object> SavedConfiguration { get; private set; } = new();

        public ModConfigurationForm(ModConfiguration modConfiguration) : this(modConfiguration, null)
        {
        }

        public ModConfigurationForm(ModConfiguration modConfiguration, UserPresetService? presetService)
        {
            _modConfiguration = modConfiguration;
            _controls = new Dictionary<string, Control>();
            _currentConfiguration = new Dictionary<string, object>();
            _presetService = presetService ?? new UserPresetService();
            
            InitializeComponent();
            GenerateUI();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            
            // Form properties - Make the form larger to accommodate more content
            AutoScaleDimensions = new SizeF(8F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 10); // Increased width from 800 to 1200, height from 700 to 800
            MinimumSize = new Size(1000, 600); // Increased minimum size
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterParent;
            Text = $"{_modConfiguration.ModName} Configuration";

            ResumeLayout(false);
        }

        private void GenerateUI()
        {
            // Clear existing controls
            Controls.Clear();
            _controls.Clear();
            _currentConfiguration.Clear();

            // Group options by TabName first, then by GroupName
            var groupedOptions = _modConfiguration.Options
                .GroupBy(o => o.TabName ?? "General")
                .ToDictionary(g => g.Key, g => g.GroupBy(o => o.GroupName).ToList());

            if (groupedOptions.Count > 1 && groupedOptions.Any(g => g.Key != "General"))
            {
                // Create tab control if there are multiple tabs
                TabControl tabControl = new TabControl
                {
                    Location = new Point(10, 10),
                    Size = new Size(ClientSize.Width - 20, ClientSize.Height - 120),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
                };

                foreach (var tab in groupedOptions)
                {
                    TabPage tabPage = new TabPage(tab.Key)
                    {
                        AutoScroll = true
                    };

                    GenerateTabContent(tabPage, tab.Value);
                    tabControl.TabPages.Add(tabPage);
                }

                Controls.Add(tabControl);
            }
            else
            {
                // Single tab, generate directly on form
                var options = groupedOptions.FirstOrDefault().Value ?? new List<IGrouping<string, ModConfigurationOption>>();
                Panel contentPanel = new Panel
                {
                    Location = new Point(10, 10),
                    Size = new Size(ClientSize.Width - 20, ClientSize.Height - 120),
                    AutoScroll = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
                };

                GenerateTabContent(contentPanel, options);
                Controls.Add(contentPanel);
            }

            void loadPreset()
            {
                if (_presetComboBox.SelectedItem != null)
                {
                    var presetName = _presetComboBox.SelectedItem.ToString();
                    if (!string.IsNullOrEmpty(presetName) && presetName != "(No presets saved)")
                    {
                        var preset = _presetService.GetPreset(_modConfiguration.ModName, presetName!);
                        if (preset != null)
                        {
                            LoadUserPreset(preset);
                            _currentlyLoadedPresetName = presetName; // Track the loaded preset
                            UpdateSaveButtonText(); // Update button text
                        }
                    }
                }
            };

            // Add preset controls
            Label presetLabel = new Label
            {
                Text = "User Presets:",
                Location = new Point(10, ClientSize.Height - 90),
                Size = new Size(90, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            _presetComboBox = new ComboBox
            {
                Location = new Point(105, ClientSize.Height - 92),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Track when preset selection changes to clear currently loaded preset if different
            _presetComboBox.SelectedIndexChanged += (s, e) =>
            {
                if (_presetComboBox.SelectedItem?.ToString() != _currentlyLoadedPresetName)
                {
                    _currentlyLoadedPresetName = null; // Clear if a different preset is selected
                }
                loadPreset();
            };

            // Populate user presets
            RefreshPresetComboBox();

            Button savePresetButton = new Button
            {
                Text = "Save As...",
                Location = new Point(275, ClientSize.Height - 92),
                Size = new Size(80, 25),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            Button deletePresetButton = new Button
            {
                Text = "Delete",
                Location = new Point(360, ClientSize.Height - 92),
                Size = new Size(60, 25),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            savePresetButton.Click += (s, e) => SaveCurrentAsPreset();

            deletePresetButton.Click += (s, e) =>
            {
                if (_presetComboBox.SelectedItem != null)
                {
                    var presetName = _presetComboBox.SelectedItem.ToString();
                    if (!string.IsNullOrEmpty(presetName) && presetName != "(No presets saved)")
                    {
                        var result = MessageBox.Show($"Are you sure you want to delete the preset '{presetName}'?",
                            "Delete Preset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        
                        if (result == DialogResult.Yes)
                        {
                            _presetService.DeletePreset(_modConfiguration.ModName, presetName);
                            RefreshPresetComboBox();
                            
                            // Clear currently loaded preset if it was deleted
                            if (_currentlyLoadedPresetName == presetName)
                            {
                                _currentlyLoadedPresetName = null;
                                UpdateSaveButtonText();
                            }
                        }
                    }
                }
            };

            Controls.Add(presetLabel);
            Controls.Add(_presetComboBox);
            Controls.Add(savePresetButton);
            Controls.Add(deletePresetButton);

            // Add buttons at fixed positions at bottom of form
            Button cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(425, ClientSize.Height - 92),
                Size = new Size(60, 25),
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            Controls.Add(cancelButton);

            AcceptButton = savePresetButton;
            CancelButton = cancelButton;

            // Initialize with default values
            InitializeDefaultValues();
            
            // Initialize save button state
            UpdateSaveButtonText();
        }

        private void UpdateSaveButtonText()
        {
            var saveButton = Controls.OfType<Button>().FirstOrDefault(b => b.Name.Equals("trueSaveButton"));
            if (saveButton != null)
            {
                if (!string.IsNullOrEmpty(_currentlyLoadedPresetName))
                {
                    saveButton.Text = "Save";
                    saveButton.Enabled = true;
                }
                else
                {
                    // Check if a preset is selected in the combobox
                    bool hasValidPresetSelected = _presetComboBox?.SelectedItem != null && 
                                                _presetComboBox.SelectedItem.ToString() != "(No presets saved)";
                    
                    if (hasValidPresetSelected)
                    {
                        saveButton.Text = "Save As...";
                        saveButton.Enabled = true;
                    }
                    else
                    {
                        saveButton.Text = "Save As...";
                        saveButton.Enabled = false; // Disable save when no preset is selected
                    }
                }
            }
        }

        private void RefreshPresetComboBox()
        {
            if (_presetComboBox == null) return;

            _presetComboBox.Items.Clear();
            var userPresets = _presetService.LoadPresets(_modConfiguration.ModName);
            
            if (userPresets.Count == 0)
            {
                _presetComboBox.Items.Add("(No presets saved)");
                _presetComboBox.Enabled = false;
            }
            else
            {
                _presetComboBox.Enabled = true;
                foreach (var preset in userPresets.OrderBy(p => p.Name))
                {
                    _presetComboBox.Items.Add(preset.Name);
                }
            }
            
            // Update save button state when preset list changes
            UpdateSaveButtonText();
        }

        private void GenerateTabContent(Control parent, List<IGrouping<string, ModConfigurationOption>> groupedOptions)
        {
            int yOffset = 10;
            int availableWidth = parent.Width - 40; // Leave some margin

            foreach (var group in groupedOptions)
            {
                GroupBox groupBox = new GroupBox
                {
                    Text = group.Key,
                    Location = new Point(10, yOffset),
                    Size = new Size(availableWidth, 120), // Will resize based on content
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                // First pass: Calculate the maximum control width needed in this group
                int maxControlWidth = CalculateMaxControlWidthForGroup(group);
                
                int groupYOffset = 25;
                int maxGroupHeight = 25;

                foreach (var option in group.OrderBy(o => o.Order).ThenBy(o => o.Name))
                {
                    Control? control = CreateControlForOption(option, maxControlWidth);
                    if (control != null)
                    {
                        control.Location = new Point(10, groupYOffset);
                        control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        
                        groupBox.Controls.Add(control);
                        _controls[option.Name] = control;

                        int rowHeight = control.Height; // Track the height needed for this row

                        // Add description label if present
                        if (!string.IsNullOrEmpty(option.Description))
                        {
                            // Calculate description label position and width
                            int descStartX = control.Right + 15; // Add some space between control and description
                            int remainingWidth = availableWidth - descStartX - 20; // Space left after control
                            int descWidth = Math.Max(300, remainingWidth); // Ensure minimum 300px width for better readability
                            
                            // If the remaining width would be too small, try to use more space by reducing control area
                            if (remainingWidth < 300 && control.Width > 400)
                            {
                                // Shrink the control a bit if it's very wide and description would be too cramped
                                int newControlWidth = Math.Max(400, control.Width - 150);
                                control.Width = newControlWidth;
                                descStartX = control.Right + 15;
                                descWidth = Math.Max(300, availableWidth - descStartX - 20);
                            }
                            
                            Label descLabel = new Label
                            {
                                Text = option.Description,
                                Location = new Point(descStartX, groupYOffset + 2), // Slight vertical offset for better alignment
                                Size = new Size(descWidth, 20), // Start with reasonable height
                                Font = new Font(Font.FontFamily, 8.25f, FontStyle.Regular),
                                ForeColor = Color.Gray,
                                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                                AutoSize = false,
                                UseMnemonic = false
                            };
                            
                            // Calculate height needed for text wrapping with proper graphics context
                            int neededHeight = 20; // Default height
                            try
                            {
                                using (Graphics g = groupBox.CreateGraphics())
                                {
                                    SizeF textSize = g.MeasureString(descLabel.Text, descLabel.Font, descWidth);
                                    neededHeight = Math.Max(20, (int)Math.Ceiling(textSize.Height) + 4);
                                }
                            }
                            catch
                            {
                                // Fallback: estimate based on text length with better estimation
                                int estimatedLines = Math.Max(1, (int)Math.Ceiling(descLabel.Text.Length / 35.0)); // More conservative estimate
                                neededHeight = estimatedLines * 16 + 4; // Account for line spacing
                            }
                            
                            descLabel.Height = neededHeight;
                            groupBox.Controls.Add(descLabel);
                            
                            // Update row height to accommodate the tallest element (control or description)
                            rowHeight = Math.Max(rowHeight, neededHeight);
                        }
                        
                        // Use the actual row height for spacing
                        groupYOffset += rowHeight + 4; // Increased spacing slightly for better readability
                        maxGroupHeight = Math.Max(maxGroupHeight, groupYOffset);
                    }
                }

                groupBox.Size = new Size(availableWidth, maxGroupHeight + 15); // More padding at bottom
                parent.Controls.Add(groupBox);
                yOffset += groupBox.Height + 10; // Maintain spacing between groups
            }
            
            // Set up conditional control dependencies after all controls are created
            SetupConditionalControls();
        }
        
        private int CalculateMaxControlWidthForGroup(IGrouping<string, ModConfigurationOption> group)
        {
            int maxWidth = 200; // Minimum width
            
            foreach (var option in group)
            {
                int requiredWidth = option.ControlType switch
                {
                    ModControlType.CheckBox or ModControlType.RadioButton => 
                        Math.Max(200, MeasureTextWidth(option.DisplayName) + 25), // +25 for checkbox/radio icon
                    ModControlType.TextBox => 450, // Fixed width for text input panels
                    ModControlType.TrackBar => 500, // Fixed width for trackbar panels
                    _ => 200
                };
                
                maxWidth = Math.Max(maxWidth, requiredWidth);
            }
            
            // Limit control width to ensure space for descriptions - use a more balanced approach
            int formWidth = ClientSize.Width > 0 ? ClientSize.Width : 1200; // Use actual form width or fallback
            int availableWidth = formWidth - 80; // Account for margins and scrollbar
            int maxAllowedControlWidth = Math.Max(450, (int)(availableWidth * 0.6)); // At least 450px, use 60% for controls, 40% for descriptions
            
            return Math.Min(maxWidth, maxAllowedControlWidth);
        }
        
        private void SetupConditionalControls()
        {
            foreach (var option in _modConfiguration.Options)
            {
                if (!string.IsNullOrEmpty(option.EnabledWhen) && _controls.TryGetValue(option.Name, out Control? dependentControl))
                {
                    if (_controls.TryGetValue(option.EnabledWhen, out Control? parentControl))
                    {
                        // Set up the dependency relationship
                        SetupControlDependency(parentControl, dependentControl, option.EnabledWhenValue ?? true);
                        
                        // Set initial state
                        UpdateDependentControlState(parentControl, dependentControl, option.EnabledWhenValue ?? true);
                    }
                }
            }
        }
        
        private void SetupControlDependency(Control parentControl, Control dependentControl, object requiredValue)
        {
            EventHandler updateHandler = (s, e) =>
            {
                UpdateDependentControlState(parentControl, dependentControl, requiredValue);
            };

            // Wire up events based on control type
            switch (parentControl)
            {
                case CheckBox checkBox:
                    checkBox.CheckedChanged += updateHandler;
                    break;
                case RadioButton radioButton:
                    radioButton.CheckedChanged += updateHandler;
                    break;
                case Panel panel when panel.Controls.OfType<TextBox>().Any():
                    panel.Controls.OfType<TextBox>().First().TextChanged += updateHandler;
                    break;
                case Panel panel when panel.Controls.OfType<TrackBar>().Any():
                    panel.Controls.OfType<TrackBar>().First().ValueChanged += updateHandler;
                    break;
            }
        }
        
        private void UpdateDependentControlState(Control parentControl, Control dependentControl, object requiredValue)
        {
            object? currentValue = parentControl switch
            {
                CheckBox checkBox => checkBox.Checked,
                RadioButton radioButton => radioButton.Checked,
                Panel panel when panel.Controls.OfType<TextBox>().Any() => 
                    panel.Controls.OfType<TextBox>().First().Text,
                Panel panel when panel.Controls.OfType<TrackBar>().Any() => 
                    panel.Controls.OfType<TrackBar>().First().Value,
                _ => null
            };

            bool shouldBeEnabled = currentValue?.Equals(requiredValue) == true;
            dependentControl.Enabled = shouldBeEnabled;
            
            // Also update the color to visually indicate disabled state
            if (dependentControl is CheckBox || dependentControl is RadioButton)
            {
                dependentControl.ForeColor = shouldBeEnabled ? SystemColors.ControlText : SystemColors.GrayText;
            }
        }

        private Control? CreateControlForOption(ModConfigurationOption option, int groupMaxWidth)
        {
            switch (option.ControlType)
            {
                case ModControlType.CheckBox:
                    var checkBox = new CheckBox
                    {
                        Name = option.Name,
                        Text = option.DisplayName,
                        Size = new Size(groupMaxWidth, 18), // Use group max width, reduced height from 20 to 18
                        Checked = Convert.ToBoolean(option.DefaultValue),
                        AutoSize = false
                    };
                    checkBox.CheckedChanged += (s, e) => UpdateConfiguration();
                    return checkBox;

                case ModControlType.RadioButton:
                    var radioButton = new RadioButton
                    {
                        Name = option.Name,
                        Text = option.DisplayName,
                        Size = new Size(groupMaxWidth, 18), // Use group max width, reduced height from 20 to 18
                        Checked = Convert.ToBoolean(option.DefaultValue),
                        AutoSize = false
                    };
                    radioButton.CheckedChanged += (s, e) => UpdateConfiguration();
                    return radioButton;

                case ModControlType.TextBox:
                    var textBox = new TextBox
                    {
                        Name = option.Name,
                        Size = new Size(200, 20),
                        Text = option.DefaultValue?.ToString() ?? ""
                    };
                    textBox.TextChanged += (s, e) => UpdateConfiguration();

                    // Add label for textbox
                    var panel = new Panel { Size = new Size(groupMaxWidth, 22) }; // Use group max width, reduced height from 25 to 22
                    var label = new Label
                    {
                        Text = option.DisplayName + ":",
                        Location = new Point(0, 2), // Reduced from 3 to 2
                        Size = new Size(Math.Max(120, MeasureTextWidth(option.DisplayName + ":") + 10), 18), // Reduced height
                        AutoSize = false
                    };
                    textBox.Location = new Point(label.Width + 5, 1); // Adjusted Y position
                    
                    panel.Controls.Add(label);
                    panel.Controls.Add(textBox);
                    return panel;

                case ModControlType.TrackBar:
                    var trackBarPanel = new Panel { Size = new Size(groupMaxWidth, 50) }; // Use group max width, reduced height
                    var trackBarLabel = new Label
                    {
                        Text = option.DisplayName + ":",
                        Location = new Point(0, 2), // Reduced from 3 to 2
                        Size = new Size(Math.Max(120, MeasureTextWidth(option.DisplayName + ":") + 10), 18), // Reduced height
                        AutoSize = false
                    };
                    
                    var trackBar = new TrackBar
                    {
                        Name = option.Name,
                        Location = new Point(trackBarLabel.Width + 5, 0),
                        Size = new Size(200, 40), // Reduced height from 45 to 40
                        Minimum = option.Properties.ContainsKey("min") ? Convert.ToInt32(option.Properties["min"]) : 0,
                        Maximum = option.Properties.ContainsKey("max") ? Convert.ToInt32(option.Properties["max"]) : 100,
                        Value = Convert.ToInt32(option.DefaultValue),
                        TickStyle = TickStyle.BottomRight
                    };

                    var valueLabel = new Label
                    {
                        Name = option.Name + "_value",
                        Text = trackBar.Value.ToString(),
                        Location = new Point(trackBar.Right + 10, 2), // Adjusted Y position
                        Size = new Size(50, 18) // Reduced height
                    };
                    
                    // Add description below the trackbar if it's long
                    if (!string.IsNullOrEmpty(option.Description) && option.Description.Length > 50)
                    {
                        var descriptionLabel = new Label
                        {
                            Text = option.Description,
                            Location = new Point(0, 42), // Adjusted position
                            Size = new Size(groupMaxWidth - 10, 12), // Use group width, reduced height
                            Font = new Font(Font.FontFamily, 7.5f, FontStyle.Regular),
                            ForeColor = Color.Gray,
                            AutoSize = false
                        };
                        trackBarPanel.Controls.Add(descriptionLabel);
                        trackBarPanel.Height = 55; // Adjust panel height
                    }

                    trackBar.ValueChanged += (s, e) =>
                    {
                        valueLabel.Text = trackBar.Value.ToString();
                        UpdateConfiguration();
                    };

                    trackBarPanel.Controls.Add(trackBarLabel);
                    trackBarPanel.Controls.Add(trackBar);
                    trackBarPanel.Controls.Add(valueLabel);
                    return trackBarPanel;

                default:
                    return null;
            }
        }
        
        private int MeasureTextWidth(string text)
        {
            using (Graphics g = CreateGraphics())
            {
                SizeF textSize = g.MeasureString(text, Font);
                return (int)Math.Ceiling(textSize.Width);
            }
        }

        private void InitializeDefaultValues()
        {
            foreach (var option in _modConfiguration.Options)
            {
                _currentConfiguration[option.Name] = option.DefaultValue;
            }
        }

        private void UpdateConfiguration()
        {
            foreach (var kvp in _controls)
            {
                string optionName = kvp.Key;
                Control control = kvp.Value;

                // Skip disabled controls for conditional options
                if (!control.Enabled)
                {
                    var option = _modConfiguration.Options.FirstOrDefault(o => o.Name == optionName);
                    if (option != null && !string.IsNullOrEmpty(option.EnabledWhen))
                    {
                        // For conditional controls that are disabled, use their default value
                        _currentConfiguration[optionName] = option.DefaultValue;
                        continue;
                    }
                }

                object? value = control switch
                {
                    CheckBox checkBox => checkBox.Checked,
                    RadioButton radioButton => radioButton.Checked,
                    TextBox textBox => textBox.Text,
                    Panel panel when panel.Controls.OfType<TextBox>().Any() => 
                        panel.Controls.OfType<TextBox>().First().Text,
                    Panel panel when panel.Controls.OfType<TrackBar>().Any() => 
                        panel.Controls.OfType<TrackBar>().First().Value,
                    _ => _currentConfiguration.ContainsKey(optionName) ? _currentConfiguration[optionName] : null
                };

                if (value != null)
                {
                    _currentConfiguration[optionName] = value;
                }
            }
        }

        public void LoadPreset(ModPreset preset)
        {
            foreach (var kvp in preset.OptionValues)
            {
                if (_controls.TryGetValue(kvp.Key, out Control? control))
                {
                    switch (control)
                    {
                        case CheckBox checkBox:
                            checkBox.Checked = Convert.ToBoolean(kvp.Value);
                            break;
                        case RadioButton radioButton:
                            radioButton.Checked = Convert.ToBoolean(kvp.Value);
                            break;
                        case TextBox textBox:
                            textBox.Text = kvp.Value?.ToString() ?? "";
                            break;
                        case Panel panel:
                            var textBoxInPanel = panel.Controls.OfType<TextBox>().FirstOrDefault();
                            if (textBoxInPanel != null)
                                textBoxInPanel.Text = kvp.Value?.ToString() ?? "";
                            
                            var trackBarInPanel = panel.Controls.OfType<TrackBar>().FirstOrDefault();
                            if (trackBarInPanel != null)
                                trackBarInPanel.Value = Convert.ToInt32(kvp.Value);
                            break;
                    }
                }
            }

            UpdateConfiguration();
        }

        private void LoadUserPreset(UserPreset preset)
        {
            foreach (var kvp in preset.OptionValues)
            {
                if (_controls.TryGetValue(kvp.Key, out Control? control))
                {
                    switch (control)
                    {
                        case CheckBox checkBox:
                            if (kvp.Value is JsonElement jsonElement)
                            {
                                checkBox.Checked = jsonElement.ValueKind == JsonValueKind.True || 
                                                 (jsonElement.ValueKind == JsonValueKind.String && 
                                                  bool.TryParse(jsonElement.GetString(), out bool boolValue) && boolValue);
                            }
                            else
                            {
                                checkBox.Checked = Convert.ToBoolean(kvp.Value);
                            }
                            break;
                        case RadioButton radioButton:
                            if (kvp.Value is JsonElement jsonElementRadio)
                            {
                                radioButton.Checked = jsonElementRadio.ValueKind == JsonValueKind.True || 
                                                    (jsonElementRadio.ValueKind == JsonValueKind.String && 
                                                     bool.TryParse(jsonElementRadio.GetString(), out bool boolValue) && boolValue);
                            }
                            else
                            {
                                radioButton.Checked = Convert.ToBoolean(kvp.Value);
                            }
                            break;
                        case TextBox textBox:
                            if (kvp.Value is JsonElement jsonElementText)
                            {
                                textBox.Text = jsonElementText.GetString() ?? "";
                            }
                            else
                            {
                                textBox.Text = kvp.Value?.ToString() ?? "";
                            }
                            break;
                        case Panel panel:
                            var textBoxInPanel = panel.Controls.OfType<TextBox>().FirstOrDefault();
                            if (textBoxInPanel != null)
                            {
                                if (kvp.Value is JsonElement jsonElementPanelText)
                                {
                                    textBoxInPanel.Text = jsonElementPanelText.GetString() ?? "";
                                }
                                else
                                {
                                    textBoxInPanel.Text = kvp.Value?.ToString() ?? "";
                                }
                            }
                        
                            var trackBarInPanel = panel.Controls.OfType<TrackBar>().FirstOrDefault();
                            if (trackBarInPanel != null)
                            {
                                if (kvp.Value is JsonElement jsonElementPanelTrack)
                                {
                                    trackBarInPanel.Value = jsonElementPanelTrack.GetInt32();
                                }
                                else
                                {
                                    trackBarInPanel.Value = Convert.ToInt32(kvp.Value);
                                }
                            }
                            break;
                    }
                }
            }

            UpdateConfiguration();
        }

        private void SaveCurrentAsPreset()
        {
            // Update current configuration first
            UpdateConfiguration();

            string? presetName;
            
            if (string.IsNullOrEmpty(_currentlyLoadedPresetName))
            {
                // Simple input dialog using InputBox from a form
                presetName = ShowInputDialog("Enter a name for this preset:", "Save Preset");
            }
            else
            {
                // Check if we can save to currently selected preset
                var selectedPreset = _presetComboBox?.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedPreset) && selectedPreset != "(No presets saved)")
                {
                    // Ask if user wants to update the selected preset or create a new one
                    var result = MessageBox.Show(
                        $"Do you want to update the preset '{selectedPreset}' with the current settings?\n\n" +
                        "Click Yes to update the existing preset.\n" +
                        "Click No to create a new preset.",
                        "Update or Create Preset",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Cancel)
                        return;
                    
                    if (result == DialogResult.Yes)
                    {
                        presetName = selectedPreset;
                    }
                    else
                    {
                        // Create new preset
                        presetName = ShowInputDialog("Enter a name for this preset:", "Save Preset");
                        if (string.IsNullOrWhiteSpace(presetName))
                            return;
                    }
                }
                else
                {
                    // No preset selected, can't save
                    MessageBox.Show("Please select a preset from the dropdown first, or load a preset to update it.",
                        "No Preset Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            // Check if preset already exists (for new presets)
            if (presetName != _currentlyLoadedPresetName)
            {
                var existingPreset = _presetService.GetPreset(_modConfiguration.ModName, presetName);
                if (existingPreset != null)
                {
                    var result = MessageBox.Show(
                        $"A preset named '{presetName}' already exists. Do you want to overwrite it?",
                        "Preset Exists",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                        return;
                }
            }

            // Create/update preset
            var newPreset = new UserPreset
            {
                Name = presetName,
                Description = _currentlyLoadedPresetName == presetName 
                    ? $"Custom preset updated on {DateTime.Now:yyyy-MM-dd HH:mm}"
                    : $"Custom preset created on {DateTime.Now:yyyy-MM-dd HH:mm}",
                CreatedDate = DateTime.Now,
                OptionValues = new Dictionary<string, object>(_currentConfiguration)
            };
            
            _presetService.SavePreset(_modConfiguration.ModName, newPreset);
            RefreshPresetComboBox();

            // Select the saved preset and mark it as currently loaded
            if (_presetComboBox != null)
            {
                _presetComboBox.SelectedItem = presetName;
            }
            _currentlyLoadedPresetName = presetName;
            UpdateSaveButtonText();
        }

        private string? ShowInputDialog(string prompt, string title)
        {
            Form inputForm = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Width = 350, Text = prompt };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 250 };
            Button confirmButton = new Button() { Text = "OK", Left = 280, Width = 80, Top = 48, DialogResult = DialogResult.OK };
            Button cancelButton = new Button() { Text = "Cancel", Left = 280, Width = 80, Top = 78, DialogResult = DialogResult.Cancel };

            confirmButton.Click += (sender, e) => { inputForm.Close(); };
            cancelButton.Click += (sender, e) => { inputForm.Close(); };

            inputForm.Controls.Add(textLabel);
            inputForm.Controls.Add(textBox);
            inputForm.Controls.Add(confirmButton);
            inputForm.Controls.Add(cancelButton);
            inputForm.AcceptButton = confirmButton;
            inputForm.CancelButton = cancelButton;

            return inputForm.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }
    }
}
