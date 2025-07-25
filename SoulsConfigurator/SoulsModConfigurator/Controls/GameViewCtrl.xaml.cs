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
using SoulsConfigurator.Services;
using SoulsConfigurator.Interfaces;
using Microsoft.Win32;

namespace SoulsModConfigurator.Controls
{
    /// <summary>
    /// Interaction logic for GameViewCtrl.xaml
    /// </summary>
    public partial class GameViewCtrl : UserControl
    {
        private IGame? _game;
        private GameManagerService? _gameManager;
        private UserPresetService? _presetService;
        private ModDownloadService? _downloadService;
        private bool _isDirty = false;

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                // Update button enabled states
                UpdateButtonStates();
            }
        }

        public bool IsGameInitialized => _game != null;

        public GameViewCtrl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Initialize(IGame? game, GameManagerService gameManager, UserPresetService presetService, ModDownloadService downloadService)
        {
            _game = game;
            _gameManager = gameManager;
            _presetService = presetService;
            _downloadService = downloadService;

            RefreshForSelectedGame();
        }

        public void RefreshForSelectedGame()
        {
            if (_game == null) return;

            // Update the path textbox
            UpdatePathDisplay();
            
            // Refresh the mod list
            RefreshModList();
            
            // Update button states
            UpdateButtonStates();
        }

        public void RefreshModList()
        {
            if (_game == null) return;

            // This will be handled by the ModListCtrl
            // For now, we'll update the data context
            var modListCtrl = FindChild<ModListCtrl>(this);
            if (modListCtrl != null)
            {
                // Subscribe to selection change events if not already subscribed
                modListCtrl.SelectionChanged -= OnModSelectionChanged;
                modListCtrl.SelectionChanged += OnModSelectionChanged;
                
                modListCtrl.RefreshMods(_game.Mods);
            }
            
            // Update button states after refreshing mods
            UpdateButtonStates();
        }

        private void OnModSelectionChanged(object? sender, EventArgs e)
        {
            // Update button states when mod selection changes
            UpdateButtonStates();
        }

        private void UpdatePathDisplay()
        {
            var pathTextBox = FindChild<TextBox>(this);
            if (pathTextBox != null && _game != null)
            {
                pathTextBox.Text = _game.InstallPath ?? "Game path not set";
                pathTextBox.IsReadOnly = true;
            }
        }

        private void UpdateButtonStates()
        {
            // Find buttons and update their enabled state
            var buttons = FindChildren<Button>(this);
            var selectedMods = GetSelectedMods();
            var hasSelectedMods = selectedMods.Any();
            var hasValidPath = _game != null && !string.IsNullOrEmpty(_game.InstallPath);
            
            foreach (var button in buttons)
            {
                if (button.Content?.ToString() == "Install selected Mods")
                {
                    // Enable install button only if there are selected mods and path is valid
                    button.IsEnabled = hasSelectedMods && hasValidPath;
                }
                else if (button.Content?.ToString() == "Remove all Mods")
                {
                    // Remove all mods button should always be enabled if path is valid
                    button.IsEnabled = hasValidPath;
                }
            }
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            if (_game == null || _gameManager == null) return;

            var dialog = new OpenFolderDialog
            {
                Title = $"Select {_game.Name} installation folder",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = dialog.FolderName;
                if (_game.ValidateInstallPath(selectedPath))
                {
                    _game.InstallPath = selectedPath;
                    _gameManager.SetGameInstallPath(selectedPath);
                    UpdatePathDisplay();
                    RefreshModList();
                    IsDirty = true;
                }
                else
                {
                    MessageBox.Show($"Invalid {_game.Name} installation path. Please select the correct game folder.",
                        "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OnInstallModsClick(object sender, RoutedEventArgs e)
        {
            if (_game == null || _gameManager == null) return;

            var selectedMods = GetSelectedMods();
            if (selectedMods.Any())
            {
                var result = _gameManager.InstallSelectedMods(selectedMods);
                if (result)
                {
                    MessageBox.Show("Mods installed successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    IsDirty = false;
                }
                else
                {
                    MessageBox.Show("Failed to install mods. Please check the game path and mod files.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnRemoveAllModsClick(object sender, RoutedEventArgs e)
        {
            if (_game == null || _gameManager == null) return;

            var result = MessageBox.Show("Are you sure you want to remove all mods?", 
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var success = _gameManager.ClearAllMods();
                if (success)
                {
                    MessageBox.Show("All mods removed successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    IsDirty = false;
                    RefreshModList();
                }
                else
                {
                    MessageBox.Show("Failed to remove mods. Please check the game path.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private List<IMod> GetSelectedMods()
        {
            var selectedMods = new List<IMod>();
            var modListCtrl = FindChild<ModListCtrl>(this);
            if (modListCtrl != null)
            {
                selectedMods = modListCtrl.GetSelectedMods();
            }
            return selectedMods;
        }

        // Helper methods to find child controls
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

        private List<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var children = new List<T>();
            if (parent == null) return children;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    children.Add(result);

                children.AddRange(FindChildren<T>(child));
            }
            return children;
        }
    }
}
