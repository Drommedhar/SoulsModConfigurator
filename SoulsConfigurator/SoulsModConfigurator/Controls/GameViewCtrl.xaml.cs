using System;
using System.Collections.Generic;
using System.IO;
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
using SoulsModConfigurator;
using SoulsModConfigurator.Helpers;
using SoulsConfigurator.Mods.DS1;
using SoulsConfigurator.Mods.DS2;
using SoulsConfigurator.Mods.DS3;
using SoulsConfigurator.Mods.Sekiro;

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

        private async void OnInstallModsClick(object sender, RoutedEventArgs e)
        {
            if (_game == null || _gameManager == null) return;

            var selectedMods = GetSelectedMods();
            if (selectedMods.Any())
            {
                // Get reference to main window
                var mainWindow = Application.Current.MainWindow as MainWindow;
                
                try
                {
                    // Show overlay
                    mainWindow?.ShowOverlay("Preparing mod installation...");
                    
                    // Disable the button to prevent multiple clicks
                    if (sender is Button button)
                    {
                        button.IsEnabled = false;
                    }

                    var result = await InstallModsAsync(selectedMods, mainWindow);
                    
                    // Hide overlay
                    mainWindow?.HideOverlay();
                    
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
                catch (Exception ex)
                {
                    mainWindow?.HideOverlay();
                    MessageBox.Show($"Error during mod installation: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // Re-enable the button
                    if (sender is Button button)
                    {
                        button.IsEnabled = true;
                    }
                }
            }
        }

        private async Task<bool> InstallModsAsync(List<IMod> selectedMods, MainWindow? mainWindow)
        {
            if (_game == null || _gameManager == null) return false;

            return await _gameManager.InstallSelectedModsAsync(selectedMods, message =>
            {
                mainWindow?.UpdateOverlayStatus(message);
            });
        }


        private async Task<bool> InstallModWithOutputCaptureAsync(IMod mod, MainWindow? mainWindow)
        {
            if (_game == null) return false;

            try
            {
                // Check if this mod has an async installation method with status reporting
                if (mod is DS1Mod_EnemyRandomizer enemyRandomizer)
                {
                    // Use the async method with status updates for console output
                    return await enemyRandomizer.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is DS1Mod_ItemRandomizer itemRandomizer)
                {
                    // Use the async method with status updates for console output
                    return await itemRandomizer.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is DS2Mod_Randomizer ds2Randomizer)
                {
                    // Use the async method with status updates for UI-based installation
                    return await ds2Randomizer.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is DS1Mod_FogGate fogGate)
                {
                    // Use the async method with status updates for UI-based installation
                    return await fogGate.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is DS3Mod_Item_Enemy ds3ItemEnemy)
                {
                    // Use the async method with status updates for UI-based installation
                    return await ds3ItemEnemy.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is DS3Mod_Crashfix ds3Crashfix)
                {
                    // Use the async method with status updates for simple file copy
                    return await ds3Crashfix.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is DS3Mod_FogGate ds3FogGate)
                {
                    // Use the async method with status updates for UI-based installation
                    return await ds3FogGate.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is DS3Mod_ModEngine ds3ModEngine)
                {
                    // Use the async method with status updates for simple file extraction
                    return await ds3ModEngine.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is SekiroMod_ModEngine sekiroModEngine)
                {
                    // Use the async method with status updates for complex file extraction
                    return await sekiroModEngine.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is SekiroMod_CombinedSFX sekiroCombinedSFX)
                {
                    // Use the async method with status updates for simple file extraction
                    return await sekiroCombinedSFX.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is SekiroMod_DivineDragonTextures sekiroTextures)
                {
                    // Use the async method with status updates for simple file extraction
                    return await sekiroTextures.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else if (mod is SekiroMod_Randomizer sekiroRandomizer)
                {
                    // Use the async method with status updates for UI-based installation
                    return await sekiroRandomizer.TryInstallModAsync(_game.InstallPath!, message =>
                    {
                        mainWindow?.UpdateOverlayStatus(message);
                    });
                }
                else
                {
                    // For other mods, provide basic status reporting
                    return await Task.Run(async () => {
                        try
                        {
                            mainWindow?.UpdateOverlayStatus($"Preparing {mod.Name} installation...");
                            await Task.Delay(500);

                            mainWindow?.UpdateOverlayStatus($"Copying files for {mod.Name}...");
                            await Task.Delay(1000);

                            // Perform the actual mod installation
                            bool result = mod.TryInstallMod(_game.InstallPath!);

                            if (result)
                            {
                                mainWindow?.UpdateOverlayStatus($"Successfully installed {mod.Name}");
                            }
                            else
                            {
                                mainWindow?.UpdateOverlayStatus($"Failed to install {mod.Name}");
                            }

                            await Task.Delay(500);
                            return result;
                        }
                        catch (Exception ex)
                        {
                            mainWindow?.UpdateOverlayStatus($"Error installing {mod.Name}: {ex.Message}");
                            return false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in InstallModWithOutputCaptureAsync: {ex.Message}");
                mainWindow?.UpdateOverlayStatus($"Error: {ex.Message}");
                return false;
            }
        }

        private async void OnRemoveAllModsClick(object sender, RoutedEventArgs e)
        {
            if (_game == null || _gameManager == null) return;

            var result = MessageBox.Show("Are you sure you want to remove all mods?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Get reference to main window
                var mainWindow = Application.Current.MainWindow as MainWindow;
                
                try
                {
                    // Show overlay
                    mainWindow?.ShowOverlay("Preparing to remove mods...");
                    
                    // Disable the button to prevent multiple clicks
                    if (sender is Button button)
                    {
                        button.IsEnabled = false;
                    }

                    var success = await RemoveAllModsAsync(mainWindow);
                    
                    // Hide overlay
                    mainWindow?.HideOverlay();
                    
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
                catch (Exception ex)
                {
                    mainWindow?.HideOverlay();
                    MessageBox.Show($"Error during mod removal: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // Re-enable the button
                    if (sender is Button button)
                    {
                        button.IsEnabled = true;
                    }
                }
            }
        }

        private async Task<bool> RemoveAllModsAsync(MainWindow? mainWindow)
        {
            if (_game == null || _gameManager == null) return false;

            return await _gameManager.ClearAllModsAsync(message =>
            {
                mainWindow?.UpdateOverlayStatus(message);
            });
        }

        private async Task<bool> RemoveModAsync(IMod mod, MainWindow? mainWindow)
        {
            if (_game == null) return false;

            try
            {
                // For mods that might need process execution (like DS1 Item Randomizer revert)
                if (mod is DS1Mod_ItemRandomizer itemRandomizer)
                {
                    // Use async removal with status updates
                    return await RemoveDS1ItemRandomizerAsync(itemRandomizer, mainWindow);
                }
                else
                {
                    // For other mods, just run the regular removal in a task
                    return await Task.Run(() =>
                    {
                        try
                        {
                            mainWindow?.UpdateOverlayStatus($"Cleaning up {mod.Name} files...");
                            return mod.TryRemoveMod(_game.InstallPath!);
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    });
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> RemoveDS1ItemRandomizerAsync(DS1Mod_ItemRandomizer itemRandomizer, MainWindow? mainWindow)
        {
            if (_game == null) return false;

            try
            {
                mainWindow?.UpdateOverlayStatus("Reverting Dark Souls 1 Item Randomizer changes...");
                
                // First, revert the game data to vanilla
                string randomizerPath = System.IO.Path.Combine(_game.InstallPath!, "randomizer_gui.exe");
                if (File.Exists(randomizerPath))
                {
                    try
                    {
                        mainWindow?.UpdateOverlayStatus("Running randomizer revert process...");
                        
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = randomizerPath,
                            WorkingDirectory = _game.InstallPath!,
                            Arguments = "--revert",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using (var process = System.Diagnostics.Process.Start(processInfo))
                        {
                            if (process != null)
                            {
                                await process.WaitForExitAsync();
                                
                                // Read output for debugging but don't spam UI
                                string output = await process.StandardOutput.ReadToEndAsync();
                                string error = await process.StandardError.ReadToEndAsync();
                                
                                if (!string.IsNullOrEmpty(output))
                                {
                                    System.Diagnostics.Debug.WriteLine($"Item Randomizer Revert Output: {output}");
                                }
                                if (!string.IsNullOrEmpty(error))
                                {
                                    System.Diagnostics.Debug.WriteLine($"Item Randomizer Revert Error: {error}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error running revert process: {ex.Message}");
                        // Continue with cleanup even if revert fails
                    }
                }

                mainWindow?.UpdateOverlayStatus("Removing Item Randomizer files...");
                
                // Now remove the mod files using the regular method
                return await Task.Run(() => itemRandomizer.TryRemoveMod(_game.InstallPath!));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RemoveDS1ItemRandomizerAsync: {ex.Message}");
                return false;
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
