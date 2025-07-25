using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SoulsConfigurator.Interfaces;
using SoulsConfigurator.UI;

namespace SoulsModConfigurator.Controls
{
    /// <summary>
    /// Interaction logic for ModEntryCtrl.xaml
    /// </summary>
    public partial class ModEntryCtrl : UserControl
    {
        private IMod? _mod;

        public bool IsModSelected => CheckBoxEnabled?.IsChecked == true;

        // Event to notify parent when selection changes
        public event EventHandler? SelectionChanged;

        public ModEntryCtrl()
        {
            InitializeComponent();
            
            // Handle checkbox change event
            CheckBoxEnabled.Checked += OnCheckBoxChanged;
            CheckBoxEnabled.Unchecked += OnCheckBoxChanged;
        }

        private void OnCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            // Notify parent that selection has changed
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Initialize(IMod mod)
        {
            _mod = mod;
            
            // Set up the UI based on the mod
            CheckBoxEnabled.Content = mod.Name;
            CheckBoxEnabled.IsEnabled = mod.IsAvailable();
            CheckBoxEnabled.IsChecked = false; // Default to unchecked
            
            // Set background color based on availability
            if (!mod.IsAvailable())
            {
                Background = new SolidColorBrush(Colors.MistyRose);
                CheckBoxEnabled.Content += " ⚠ Missing Files";
            }
            else
            {
                Background = new SolidColorBrush(Color.FromRgb(34, 35, 39)); // Match the design color
            }

            // Handle configurable mods
            if (mod is IConfigurableMod configurableMod && mod.IsAvailable())
            {
                SetupConfigurableMod(configurableMod);
            }
            else
            {
                // Hide preset controls for non-configurable mods
                Preset.Visibility = Visibility.Collapsed;
                btnConfigure.Visibility = Visibility.Collapsed;
                var presetLabel = FindChild<Label>(this);
                if (presetLabel != null)
                {
                    presetLabel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SetupConfigurableMod(IConfigurableMod configurableMod)
        {
            // Populate presets from user presets
            var userPresets = configurableMod.GetUserPresets();
            Preset.Items.Clear();
            
            if (userPresets.Any())
            {
                Preset.Items.Add("(No preset)");
                foreach (var preset in userPresets)
                {
                    Preset.Items.Add(preset.Name);
                }
                
                // Set selected preset
                var selectedPreset = configurableMod.GetSelectedPreset();
                if (!string.IsNullOrEmpty(selectedPreset) && Preset.Items.Contains(selectedPreset))
                {
                    Preset.SelectedItem = selectedPreset;
                }
                else
                {
                    Preset.SelectedIndex = 0; // "(No preset)"
                }
            }
            else
            {
                Preset.Items.Add("(No presets available)");
                Preset.SelectedIndex = 0;
                Preset.IsEnabled = false;
            }

            // Handle preset selection changes
            Preset.SelectionChanged += (s, e) =>
            {
                if (Preset.SelectedItem?.ToString() == "(No preset)")
                {
                    configurableMod.SetSelectedPreset(null);
                }
                else if (Preset.SelectedItem != null && 
                         Preset.SelectedItem.ToString() != "(No presets available)")
                {
                    configurableMod.SetSelectedPreset(Preset.SelectedItem.ToString());
                }
            };

            // Configure button click handler
            btnConfigure.Click += (s, e) => OpenModConfiguration(configurableMod);
        }

        private void OpenModConfiguration(IConfigurableMod configurableMod)
        {
            try
            {
                var modConfiguration = configurableMod.GetUIConfiguration();
                var configForm = new ModConfigurationForm(modConfiguration);
                configForm.ShowDialog();
                
                // Refresh presets after configuration
                SetupConfigurableMod(configurableMod);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening mod configuration: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public IMod? GetMod()
        {
            return _mod;
        }

        // Helper method to find child controls
        private T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childOfChild = FindChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
