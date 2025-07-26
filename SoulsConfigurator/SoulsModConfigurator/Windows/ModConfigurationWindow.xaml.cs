using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SoulsConfigurator.Models;
using SoulsConfigurator.Services;

namespace SoulsModConfigurator.Windows
{
    public partial class ModConfigurationWindow : Window
    {
        private readonly ModConfiguration _modConfiguration;
        private readonly Dictionary<string, FrameworkElement> _controls;
        private readonly Dictionary<string, object> _currentConfiguration;
        private readonly UserPresetService _presetService;
        private string? _currentlyLoadedPresetName;

        public Dictionary<string, object> Configuration => _currentConfiguration;
        public Dictionary<string, object> SavedConfiguration { get; private set; } = new();
        public bool DialogResultValue { get; private set; } = false;

        public ModConfigurationWindow(ModConfiguration modConfiguration, UserPresetService? presetService = null)
        {
            InitializeComponent();
            
            _modConfiguration = modConfiguration;
            _controls = new Dictionary<string, FrameworkElement>();
            _currentConfiguration = new Dictionary<string, object>();
            _presetService = presetService ?? new UserPresetService();
            
            Title = $"{_modConfiguration.ModName} Configuration";
            
            GenerateUI();
            InitializeDefaultValues();
            RefreshPresetComboBox();
            UpdateSaveButtonText();
        }

        private void GenerateUI()
        {
            // Clear existing controls
            MainTabControl.Items.Clear();
            SingleContentPanel.Children.Clear();
            _controls.Clear();
            _currentConfiguration.Clear();

            // Group options by TabName first, then by GroupName
            var groupedOptions = _modConfiguration.Options
                .GroupBy(o => o.TabName ?? "General")
                .ToDictionary(g => g.Key, g => g.GroupBy(o => o.GroupName).ToList());

            if (groupedOptions.Count > 1 && groupedOptions.Any(g => g.Key != "General"))
            {
                // Create tabs if there are multiple tabs
                MainTabControl.Visibility = Visibility.Visible;
                SingleContentScrollViewer.Visibility = Visibility.Collapsed;

                foreach (var tab in groupedOptions)
                {
                    var tabItem = new TabItem
                    {
                        Header = tab.Key
                    };

                    var scrollViewer = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Padding = new Thickness(15)
                    };

                    var wrapPanel = new WrapPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    GenerateTabContent(wrapPanel, tab.Value);

                    scrollViewer.Content = wrapPanel;
                    scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    tabItem.Content = scrollViewer;
                    MainTabControl.Items.Add(tabItem);
                }
            }
            else
            {
                // Single tab, generate directly in single content area
                MainTabControl.Visibility = Visibility.Collapsed;
                SingleContentScrollViewer.Visibility = Visibility.Visible;

                var options = groupedOptions.FirstOrDefault().Value ?? new List<IGrouping<string, ModConfigurationOption>>();
                GenerateTabContent(SingleContentPanel, options);
            }

            // Set up conditional control dependencies after all controls are created
            SetupConditionalControls();
        }

        private void GenerateTabContent(Panel parent, List<IGrouping<string, ModConfigurationOption>> groupedOptions)
        {
            foreach (var group in groupedOptions)
            {
                var groupBox = new GroupBox
                {
                    Header = group.Key,
                    Style = (Style)FindResource("GroupBoxStyle"),
                    Margin = new Thickness(5, 5, 5, 5),
                    MinWidth = 350,
                    MaxWidth = 600,
                    VerticalAlignment = VerticalAlignment.Top
                };

                var groupPanel = new StackPanel();
                var groupOptions = group.OrderBy(o => o.Order).ThenBy(o => o.Name).ToList();
                
                foreach (var option in groupOptions)
                {
                    // Check if this is a single control in the group and if the display name is similar to the group name
                    bool hideLabel = groupOptions.Count == 1 &&
                                   (string.Equals(option.DisplayName?.Trim(), group.Key?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(option.Name?.Trim(), group.Key?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                    (option.DisplayName?.Trim()?.Contains(group.Key?.Trim() ?? "", StringComparison.OrdinalIgnoreCase) == true) ||
                                    (group.Key?.Trim()?.Contains(option.DisplayName?.Trim() ?? "", StringComparison.OrdinalIgnoreCase) == true));
                    
                    var controlContainer = CreateControlForOption(option, hideLabel);
                    if (controlContainer != null)
                    {
                        groupPanel.Children.Add(controlContainer);
                        
                        // Add consistent spacing between controls
                        if (groupPanel.Children.Count > 1)
                        {
                            ((FrameworkElement)groupPanel.Children[groupPanel.Children.Count - 1]).Margin = new Thickness(0, 8, 0, 0);
                        }
                    }
                }

                groupBox.Content = groupPanel;
                parent.Children.Add(groupBox);
            }
        }

        private FrameworkElement? CreateControlForOption(ModConfigurationOption option, bool hideLabel = false)
        {
            switch (option.ControlType)
            {
                case ModControlType.CheckBox:
                    var checkBox = new CheckBox
                    {
                        Name = SanitizeName(option.Name),
                        Content = option.DisplayName,
                        IsChecked = Convert.ToBoolean(option.DefaultValue),
                        Margin = new Thickness(0, 4, 0, 4),
                        FontSize = 13
                    };
                    checkBox.Checked += (s, e) => UpdateConfiguration();
                    checkBox.Unchecked += (s, e) => UpdateConfiguration();
                    _controls[option.Name] = checkBox;
                    
                    AddTooltipIfNeeded(checkBox, option.Description);
                    return checkBox;

                case ModControlType.RadioButton:
                    var radioButton = new RadioButton
                    {
                        Name = SanitizeName(option.ControlName),
                        Content = option.DisplayName,
                        IsChecked = Convert.ToBoolean(option.DefaultValue),
                        Margin = new Thickness(0, 4, 0, 4),
                        FontSize = 13
                    };
                    
                    // Set group name if part of a radio button group
                    if (option.RadioButtonGroup.Any())
                    {
                        radioButton.GroupName = option.RadioButtonGroup.First();
                    }
                    
                    radioButton.Checked += (s, e) => UpdateConfiguration();
                    radioButton.Unchecked += (s, e) => UpdateConfiguration();
                    _controls[option.ControlName] = radioButton;
                    
                    AddTooltipIfNeeded(radioButton, option.Description);
                    return radioButton;

                case ModControlType.TextBox:
                    var textBoxPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    
                    if (!hideLabel)
                    {
                        var label = new TextBlock
                        {
                            Text = option.DisplayName + ":",
                            Style = (Style)FindResource("ControlLabelStyle")
                        };
                        textBoxPanel.Children.Add(label);
                    }
                    
                    var textBox = new TextBox
                    {
                        Name = SanitizeName(option.Name),
                        Text = option.DefaultValue?.ToString() ?? "",
                        Width = hideLabel ? 350 : 280,
                        Margin = new Thickness(0, 4, 0, 4),
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    textBox.TextChanged += (s, e) => UpdateConfiguration();
                    
                    textBoxPanel.Children.Add(textBox);
                    _controls[option.Name] = textBox;
                    
                    AddTooltipIfNeeded(textBoxPanel, option.Description);
                    return textBoxPanel;

                case ModControlType.TrackBar:
                    var trackBarPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    
                    if (!hideLabel)
                    {
                        var trackBarLabel = new TextBlock
                        {
                            Text = option.DisplayName + ":",
                            Style = (Style)FindResource("ControlLabelStyle")
                        };
                        trackBarPanel.Children.Add(trackBarLabel);
                    }
                    
                    var slider = new Slider
                    {
                        Name = SanitizeName(option.Name),
                        Width = hideLabel ? 350 : 280,
                        Minimum = option.Properties.ContainsKey("min") ? Convert.ToDouble(option.Properties["min"]) : 0,
                        Maximum = option.Properties.ContainsKey("max") ? Convert.ToDouble(option.Properties["max"]) : 100,
                        Value = Convert.ToDouble(option.DefaultValue),
                        IsSnapToTickEnabled = true,
                        TickFrequency = 1,
                        Margin = new Thickness(0, 4, 0, 4)
                    };
                    
                    var valueLabel = new TextBlock
                    {
                        Text = slider.Value.ToString("F0"),
                        Width = 50,
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    
                    slider.ValueChanged += (s, e) =>
                    {
                        valueLabel.Text = slider.Value.ToString("F0");
                        UpdateConfiguration();
                    };
                    
                    trackBarPanel.Children.Add(slider);
                    trackBarPanel.Children.Add(valueLabel);
                    _controls[option.Name] = slider;
                    
                    AddTooltipIfNeeded(trackBarPanel, option.Description);
                    return trackBarPanel;

                case ModControlType.ComboBox:
                    var comboBoxPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    
                    if (!hideLabel)
                    {
                        var comboBoxLabel = new TextBlock
                        {
                            Text = option.DisplayName + ":",
                            Style = (Style)FindResource("ControlLabelStyle")
                        };
                        comboBoxPanel.Children.Add(comboBoxLabel);
                    }
                    
                    var comboBox = new ComboBox
                    {
                        Name = SanitizeName(option.Name),
                        Width = hideLabel ? 350 : 280,
                        Margin = new Thickness(0, 4, 0, 4),
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    
                    // Add items from properties
                    if (option.Properties.ContainsKey("items") && option.Properties["items"] is IEnumerable<object> items)
                    {
                        foreach (var item in items)
                        {
                            comboBox.Items.Add(item);
                        }
                    }
                    
                    comboBox.SelectedItem = option.DefaultValue;
                    comboBox.SelectionChanged += (s, e) => UpdateConfiguration();
                    
                    comboBoxPanel.Children.Add(comboBox);
                    _controls[option.Name] = comboBox;
                    
                    AddTooltipIfNeeded(comboBoxPanel, option.Description);
                    return comboBoxPanel;

                default:
                    return null;
            }
        }

        private void AddTooltipIfNeeded(FrameworkElement control, string? description)
        {
            if (!string.IsNullOrEmpty(description))
            {
                var tooltip = new ToolTip
                {
                    Content = description,
                };
                control.ToolTip = tooltip;
            }
        }

        private string SanitizeName(string name)
        {
            return name.Replace(" ", "_").Replace("-", "_").Replace(".", "_");
        }

        private void SetupConditionalControls()
        {
            foreach (var option in _modConfiguration.Options)
            {
                if (!string.IsNullOrEmpty(option.EnabledWhen) && _controls.TryGetValue(option.Name, out FrameworkElement? dependentControl))
                {
                    if (_controls.TryGetValue(option.EnabledWhen, out FrameworkElement? parentControl))
                    {
                        SetupControlDependency(parentControl, dependentControl, option.EnabledWhenValue ?? true);
                        UpdateDependentControlState(parentControl, dependentControl, option.EnabledWhenValue ?? true);
                    }
                }
            }
        }

        private void SetupControlDependency(FrameworkElement parentControl, FrameworkElement dependentControl, object requiredValue)
        {
            switch (parentControl)
            {
                case CheckBox checkBox:
                    checkBox.Checked += (s, e) => UpdateDependentControlState(parentControl, dependentControl, requiredValue);
                    checkBox.Unchecked += (s, e) => UpdateDependentControlState(parentControl, dependentControl, requiredValue);
                    break;
                case RadioButton radioButton:
                    radioButton.Checked += (s, e) => UpdateDependentControlState(parentControl, dependentControl, requiredValue);
                    radioButton.Unchecked += (s, e) => UpdateDependentControlState(parentControl, dependentControl, requiredValue);
                    break;
                case TextBox textBox:
                    textBox.TextChanged += (s, e) => UpdateDependentControlState(parentControl, dependentControl, requiredValue);
                    break;
                case Slider slider:
                    slider.ValueChanged += (s, e) => UpdateDependentControlState(parentControl, dependentControl, requiredValue);
                    break;
                case ComboBox comboBox:
                    comboBox.SelectionChanged += (s, e) => UpdateDependentControlState(parentControl, dependentControl, requiredValue);
                    break;
            }
        }

        private void UpdateDependentControlState(FrameworkElement parentControl, FrameworkElement dependentControl, object requiredValue)
        {
            object? currentValue = parentControl switch
            {
                CheckBox checkBox => checkBox.IsChecked,
                RadioButton radioButton => radioButton.IsChecked,
                TextBox textBox => textBox.Text,
                Slider slider => slider.Value,
                ComboBox comboBox => comboBox.SelectedItem,
                _ => null
            };

            bool shouldBeEnabled = currentValue?.Equals(requiredValue) == true;
            dependentControl.IsEnabled = shouldBeEnabled;
            dependentControl.Opacity = shouldBeEnabled ? 1.0 : 0.5;
        }

        private void InitializeDefaultValues()
        {
            // Handle radio button groups first
            var radioButtonGroups = _modConfiguration.Options
                .Where(o => o.ControlType == ModControlType.RadioButton && o.RadioButtonGroup.Any())
                .GroupBy(o => o.GroupName)
                .ToList();

            foreach (var group in radioButtonGroups)
            {
                // Find the default selected option in this group
                var defaultOption = group.FirstOrDefault(o => Convert.ToBoolean(o.DefaultValue));
                if (defaultOption != null)
                {
                    _currentConfiguration[group.Key] = defaultOption.ControlName;
                }
            }

            // Handle other options
            foreach (var option in _modConfiguration.Options.Where(o => o.ControlType != ModControlType.RadioButton))
            {
                _currentConfiguration[option.Name] = option.DefaultValue;
            }
        }

        private void UpdateConfiguration()
        {
            // Handle radio button groups first
            var radioButtonGroups = _modConfiguration.Options
                .Where(o => o.ControlType == ModControlType.RadioButton && o.RadioButtonGroup.Any())
                .GroupBy(o => o.GroupName)
                .ToList();

            foreach (var group in radioButtonGroups)
            {
                var groupOptions = group.ToList();
                string? selectedOptionName = null;

                // Find which radio button in this group is selected
                foreach (var option in groupOptions)
                {
                    if (_controls.TryGetValue(option.ControlName, out FrameworkElement? control) && 
                        control is RadioButton radioButton && radioButton.IsChecked == true)
                    {
                        selectedOptionName = option.ControlName;
                        break;
                    }
                }

                // Store the selected option name for the group
                if (!string.IsNullOrEmpty(selectedOptionName))
                {
                    _currentConfiguration[group.Key] = selectedOptionName;
                }
            }

            // Handle other controls
            foreach (var kvp in _controls)
            {
                string optionName = kvp.Key;
                FrameworkElement control = kvp.Value;

                // Skip radio buttons as they're handled above
                var option = _modConfiguration.Options.FirstOrDefault(o => o.ControlName == optionName);
                if (option?.ControlType == ModControlType.RadioButton)
                    continue;

                // Skip disabled controls for conditional options
                if (!control.IsEnabled)
                {
                    var optionConfig = _modConfiguration.Options.FirstOrDefault(o => o.Name == optionName);
                    if (optionConfig != null && !string.IsNullOrEmpty(optionConfig.EnabledWhen))
                    {
                        _currentConfiguration[optionName] = optionConfig.DefaultValue;
                        continue;
                    }
                }

                object? value = control switch
                {
                    CheckBox checkBox => checkBox.IsChecked,
                    TextBox textBox => textBox.Text,
                    Slider slider => (int)slider.Value,
                    ComboBox comboBox => comboBox.SelectedItem,
                    _ => _currentConfiguration.ContainsKey(optionName) ? _currentConfiguration[optionName] : null
                };

                if (value != null)
                {
                    _currentConfiguration[optionName] = value;
                }
            }
        }

        private void RefreshPresetComboBox()
        {
            PresetComboBox.Items.Clear();
            var userPresets = _presetService.LoadPresets(_modConfiguration.ModName);

            if (userPresets.Count == 0)
            {
                PresetComboBox.Items.Add("(No presets saved)");
                PresetComboBox.IsEnabled = false;
            }
            else
            {
                PresetComboBox.IsEnabled = true;
                foreach (var preset in userPresets.OrderBy(p => p.Name))
                {
                    // Mark outdated presets visually
                    var displayName = preset.IsOutdated ? $"{preset.Name} (Outdated)" : preset.Name;
                    PresetComboBox.Items.Add(displayName);
                }
            }

            UpdateSaveButtonText();
        }

        private void UpdateSaveButtonText()
        {
            if (!string.IsNullOrEmpty(_currentlyLoadedPresetName))
            {
                SavePresetButton.Content = "Save";
                SavePresetButton.IsEnabled = true;
            }
            else
            {
                bool hasValidPresetSelected = PresetComboBox.SelectedItem != null &&
                                            PresetComboBox.SelectedItem.ToString() != "(No presets saved)";

                SavePresetButton.Content = "Save As...";
                SavePresetButton.IsEnabled = true; // Always enable Save As
            }
        }

        private void LoadUserPreset(UserPreset preset)
        {
            // Handle radio button groups first
            var radioButtonGroups = _modConfiguration.Options
                .Where(o => o.ControlType == ModControlType.RadioButton && o.RadioButtonGroup.Any())
                .GroupBy(o => o.GroupName)
                .ToList();

            foreach (var group in radioButtonGroups)
            {
                if (preset.OptionValues.TryGetValue(group.Key, out object? groupValue))
                {
                    string? selectedControlName = null;
                    
                    if (groupValue is JsonElement jsonElement)
                    {
                        try
                        {
                            selectedControlName = jsonElement.GetString();
                        }
                        catch(Exception)
                        {
                            // for legacy reasons we want this to not fail
                        }
                    }
                    else
                    {
                        selectedControlName = groupValue?.ToString();
                    }

                    if (!string.IsNullOrEmpty(selectedControlName))
                    {
                        // Uncheck all radio buttons in this group first
                        foreach (var option in group)
                        {
                            if (_controls.TryGetValue(option.ControlName, out FrameworkElement? control) && 
                                control is RadioButton radioButton)
                            {
                                radioButton.IsChecked = false;
                            }
                        }

                        // Check the selected radio button
                        if (_controls.TryGetValue(selectedControlName, out FrameworkElement? selectedControl) && 
                            selectedControl is RadioButton selectedRadioButton)
                        {
                            selectedRadioButton.IsChecked = true;
                        }
                    }
                }
            }

            // Handle other controls
            foreach (var kvp in preset.OptionValues)
            {
                // Skip radio button groups as they're handled above
                var isRadioButtonGroup = radioButtonGroups.Any(g => g.Key == kvp.Key);
                if (isRadioButtonGroup) continue;

                if (_controls.TryGetValue(kvp.Key, out FrameworkElement? control))
                {
                    switch (control)
                    {
                        case CheckBox checkBox:
                            if (kvp.Value is JsonElement jsonElement)
                            {
                                checkBox.IsChecked = jsonElement.ValueKind == JsonValueKind.True ||
                                                   (jsonElement.ValueKind == JsonValueKind.String &&
                                                    bool.TryParse(jsonElement.GetString(), out bool boolValue) && boolValue);
                            }
                            else
                            {
                                checkBox.IsChecked = Convert.ToBoolean(kvp.Value);
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
                        case Slider slider:
                            if (kvp.Value is JsonElement jsonElementSlider)
                            {
                                slider.Value = jsonElementSlider.GetDouble();
                            }
                            else
                            {
                                slider.Value = Convert.ToDouble(kvp.Value);
                            }
                            break;
                        case ComboBox comboBox:
                            if (kvp.Value is JsonElement jsonElementCombo)
                            {
                                var stringValue = jsonElementCombo.GetString();
                                comboBox.SelectedItem = stringValue;
                            }
                            else
                            {
                                comboBox.SelectedItem = kvp.Value;
                            }
                            break;
                    }
                }
            }

            UpdateConfiguration();
        }

        // Event Handlers
        public void LoadSavedConfiguration(Dictionary<string, object> savedConfiguration)
        {
            // Handle radio button groups first
            var radioButtonGroups = _modConfiguration.Options
                .Where(o => o.ControlType == ModControlType.RadioButton && o.RadioButtonGroup.Any())
                .GroupBy(o => o.Name)
                .ToList();

            foreach (var group in radioButtonGroups)
            {
                if (savedConfiguration.TryGetValue(group.Key, out object? groupValue))
                {
                    string? selectedControlName = groupValue?.ToString();

                    if (!string.IsNullOrEmpty(selectedControlName))
                    {
                        // Uncheck all radio buttons in this group first
                        foreach (var option in group)
                        {
                            if (_controls.TryGetValue(option.ControlName, out FrameworkElement? control) && 
                                control is RadioButton radioButton)
                            {
                                radioButton.IsChecked = false;
                            }
                        }

                        // Check the selected radio button
                        if (_controls.TryGetValue(selectedControlName, out FrameworkElement? selectedControl) && 
                            selectedControl is RadioButton selectedRadioButton)
                        {
                            selectedRadioButton.IsChecked = true;
                        }
                    }
                }
            }

            // Handle other controls
            foreach (var kvp in savedConfiguration)
            {
                // Skip radio button groups as they're handled above
                var isRadioButtonGroup = radioButtonGroups.Any(g => g.Key == kvp.Key);
                if (isRadioButtonGroup) continue;

                if (_controls.TryGetValue(kvp.Key, out FrameworkElement? control))
                {
                    switch (control)
                    {
                        case CheckBox checkBox:
                            checkBox.IsChecked = Convert.ToBoolean(kvp.Value);
                            break;
                        case TextBox textBox:
                            textBox.Text = kvp.Value?.ToString() ?? "";
                            break;
                        case Slider slider:
                            slider.Value = Convert.ToDouble(kvp.Value);
                            break;
                        case ComboBox comboBox:
                            comboBox.SelectedItem = kvp.Value;
                            break;
                    }
                }
            }

            UpdateConfiguration();
        }
        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PresetComboBox.SelectedItem?.ToString() != _currentlyLoadedPresetName)
            {
                _currentlyLoadedPresetName = null;
            }

            if (PresetComboBox.SelectedItem != null)
            {
                var presetName = PresetComboBox.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(presetName) && presetName != "(No presets saved)")
                {
                    var preset = _presetService.GetPreset(_modConfiguration.ModName, presetName!.Replace(" (Outdated)", ""));
                    if (preset != null)
                    {
                        LoadUserPreset(preset);
                        _currentlyLoadedPresetName = presetName!.Replace(" (Outdated)", "");
                        UpdateSaveButtonText();
                    }
                }
            }
        }

        private void SavePresetButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateConfiguration();

            string? presetName;

            if (string.IsNullOrEmpty(_currentlyLoadedPresetName))
            {
                presetName = ShowInputDialog("Enter a name for this preset:", "Save Preset");
                if (string.IsNullOrWhiteSpace(presetName))
                    return;
            }
            else
            {
                var selectedPreset = PresetComboBox.SelectedItem?.ToString()?.Replace(" (Outdated)", "");
                if (!string.IsNullOrEmpty(selectedPreset) && selectedPreset != "(No presets saved)")
                {
                    var result = MessageBox.Show(
                        $"Do you want to update the preset '{selectedPreset}' with the current settings?\n\n" +
                        "Click Yes to update the existing preset.\n" +
                        "Click No to create a new preset.",
                        "Update or Create Preset",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel)
                        return;

                    if (result == MessageBoxResult.Yes)
                    {
                        presetName = selectedPreset;
                    }
                    else
                    {
                        presetName = ShowInputDialog("Enter a name for this preset:", "Save Preset");
                        if (string.IsNullOrWhiteSpace(presetName))
                            return;
                    }
                }
                else
                {
                    MessageBox.Show("Please select a preset from the dropdown first, or load a preset to update it.",
                        "No Preset Selected", MessageBoxButton.OK, MessageBoxImage.Information);
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
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
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
            PresetComboBox.SelectedItem = presetName;
            _currentlyLoadedPresetName = presetName;
            UpdateSaveButtonText();

            Close();
        }

        private void DeletePresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (PresetComboBox.SelectedItem != null)
            {
                var presetName = PresetComboBox.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(presetName) && presetName != "(No presets saved)")
                {
                    var result = MessageBox.Show($"Are you sure you want to delete the preset '{presetName}'?",
                        "Delete Preset", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _presetService.DeletePreset(_modConfiguration.ModName, presetName);
                        RefreshPresetComboBox();

                        if (_currentlyLoadedPresetName == presetName)
                        {
                            _currentlyLoadedPresetName = null;
                            UpdateSaveButtonText();
                        }
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResultValue = false;
            DialogResult = false;
            Close();
        }

        private string? ShowInputDialog(string prompt, string title)
        {
            var inputDialog = new Window
            {
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Title = title,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var promptLabel = new TextBlock
            {
                Text = prompt,
                Margin = new Thickness(20, 20, 20, 10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(promptLabel, 0);

            var inputTextBox = new TextBox
            {
                Margin = new Thickness(20, 0, 20, 20),
                Height = 25
            };
            Grid.SetRow(inputTextBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 0)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30,
                IsCancel = true
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(promptLabel);
            grid.Children.Add(inputTextBox);
            grid.Children.Add(buttonPanel);

            inputDialog.Content = grid;

            string? result = null;
            okButton.Click += (s, e) =>
            {
                result = inputTextBox.Text;
                inputDialog.DialogResult = true;
                inputDialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                inputDialog.DialogResult = false;
                inputDialog.Close();
            };

            inputTextBox.Focus();
            inputDialog.ShowDialog();

            return inputDialog.DialogResult == true ? result : null;
        }
    }
}