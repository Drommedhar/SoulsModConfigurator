using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Services;
using SoulsConfigurator.UI;
using System.Drawing;
using System.Reflection;

namespace SoulsConfigurator
{
    public partial class Form1 : Form
    {
        private readonly GameManagerService _gameManager;
        private readonly UserPresetService _presetService;
        private readonly ModDownloadService _downloadService;
        private readonly VersionCheckService _versionCheckService;
        private FolderBrowserDialog? _folderBrowserDialog;
        private readonly Dictionary<string, ComboBox> _modPresetComboBoxes;

        public Form1()
        {
            InitializeComponent();
            _gameManager = new GameManagerService();
            _presetService = new UserPresetService();
            _downloadService = new ModDownloadService();
            _versionCheckService = new VersionCheckService();
            _modPresetComboBoxes = new Dictionary<string, ComboBox>();
            
            // Subscribe to preset change events
            _presetService.PresetChanged += OnPresetChanged;
            
            InitializeForm();
        }

        /// <summary>
        /// Handles preset change events and refreshes the mod list
        /// </summary>
        private void OnPresetChanged(object? sender, PresetChangedEventArgs e)
        {
            // Ensure we're on the UI thread
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnPresetChanged(sender, e)));
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"OnPresetChanged: ModName='{e.ModName}', PresetName='{e.PresetName}', ChangeType={e.ChangeType}");
                
                // Store the current mod checkbox states and selected presets to preserve user selections
                var modStates = new Dictionary<string, bool>();
                var selectedPresets = new Dictionary<string, string?>();
                
                // Capture current state before rebuilding
                foreach (var mod in _gameManager.GetAvailableModsForSelectedGame())
                {
                    // Find the checkbox for this mod in the current UI
                    foreach (Control control in panelModsContainer.Controls)
                    {
                        if (control is Panel modPanel)
                        {
                            var checkBox = modPanel.Controls.OfType<CheckBox>().FirstOrDefault();
                            if (checkBox?.Tag is IMod taggedMod && taggedMod.Name == mod.Name)
                            {
                                modStates[mod.Name] = checkBox.Checked;
                                break;
                            }
                        }
                    }
                    
                    // Store current preset selections for configurable mods
                    if (mod is IConfigurableMod configurableMod)
                    {
                        selectedPresets[mod.Name] = configurableMod.GetSelectedPreset();
                        
                        // For the mod that just had a preset added, update its selection
                        if (mod.Name == e.ModName && e.ChangeType == PresetChangeType.Added)
                        {
                            selectedPresets[mod.Name] = e.PresetName;
                            configurableMod.SetSelectedPreset(e.PresetName);
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("OnPresetChanged: Rebuilding mod list to refresh all preset combo boxes");
                
                // Rebuild the entire mod list
                LoadModsForSelectedGame();
                
                // Restore the previous states
                foreach (Control control in panelModsContainer.Controls)
                {
                    if (control is Panel modPanel)
                    {
                        var checkBox = modPanel.Controls.OfType<CheckBox>().FirstOrDefault();
                        if (checkBox?.Tag is IMod mod && modStates.TryGetValue(mod.Name, out bool wasChecked))
                        {
                            checkBox.Checked = wasChecked;
                        }
                        
                        // Restore preset selections for configurable mods
                        if (checkBox?.Tag is IConfigurableMod configurableMod && 
                            selectedPresets.TryGetValue(configurableMod.Name, out string? presetName))
                        {
                            configurableMod.SetSelectedPreset(presetName);
                            
                            // Update the combo box selection
                            var comboBox = modPanel.Controls.OfType<ComboBox>().FirstOrDefault();
                            if (comboBox != null && !string.IsNullOrEmpty(presetName) && comboBox.Items.Contains(presetName))
                            {
                                comboBox.SelectedItem = presetName;
                            }
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("OnPresetChanged: Mod list rebuild complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnPresetChanged: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void InitializeForm()
        {
            // Populate games combobox
            cmbGames.DisplayMember = "Name";
            cmbGames.ValueMember = "Name";
            cmbGames.DataSource = _gameManager.GetAvailableGames();

            // Initialize folder browser dialog
            _folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select the game installation folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            // Check if a game was auto-selected during initialization
            var selectedGame = _gameManager.GetSelectedGame();
            if (selectedGame != null)
            {
                // Update the combobox selection to match the auto-selected game
                cmbGames.SelectedItem = selectedGame;
                RefreshUIForSelectedGame();
            }
            else
            {
                // Initially disable mod controls
                EnableModControls(false);
            }
            
            // Set initial status message
            UpdateStatusMessage();
            
            // Check for updates asynchronously
            _ = CheckForUpdatesAsync();
        }

        private void cmbGames_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbGames.SelectedItem is IGame selectedGame)
            {
                _gameManager.SelectGame(selectedGame);
                RefreshUIForSelectedGame();
                
                // Update status message for the selected game
                UpdateStatusMessage();
            }
        }

        private void LoadModsForSelectedGame()
        {          
            // Clear the existing custom mod list panel
            var existingPanel = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "panelModsContainer");
            if (existingPanel != null)
            {
                existingPanel.Controls.Clear();
            }

            _modPresetComboBoxes.Clear();

            var mods = _gameManager.GetAvailableModsForSelectedGame();
            
            // Create a panel for the mod list with preset selection
            panelModsContainer.AutoScroll = true;
            panelModsContainer.BorderStyle = BorderStyle.FixedSingle;
            
            // Ensure the container panel is wide enough for the mod panels
            if (panelModsContainer.Width < 705) // Updated to match designer width
            {
                panelModsContainer.Width = 705;
            }

            int yOffset = 5;
            foreach (var mod in mods)
            {
                Panel modPanel = CreateModPanel(mod, yOffset);
                panelModsContainer.Controls.Add(modPanel);
                yOffset += modPanel.Height + 5;
            }
        }

        private Panel CreateModPanel(IMod mod, int yOffset)
        {
            Panel modPanel = new Panel
            {
                Location = new Point(5, yOffset),
                Size = new Size(680, 35), // Increased width from 620 to 680 to accommodate all controls properly
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Check if mod is available
            bool isAvailable = mod.IsAvailable();

            CheckBox modCheckBox = new CheckBox
            {
                Text = mod.Name,
                Location = new Point(10, 8), // Centered vertically
                Size = new Size(280, 20), // Keep checkbox width consistent
                Tag = mod,
                Enabled = isAvailable // Disable checkbox if mod is not available
            };

            // Add availability indicator
            if (!isAvailable)
            {
                Label unavailableLabel = new Label
                {
                    Text = "⚠ Missing Files",
                    Location = new Point(300, 10),
                    Size = new Size(85, 16),
                    ForeColor = Color.Red,
                    Font = new Font(Font.FontFamily, 8f, FontStyle.Bold)
                };
                modPanel.Controls.Add(unavailableLabel);
                modPanel.BackColor = Color.MistyRose; // Light red background for unavailable mods
            }

            modPanel.Controls.Add(modCheckBox);

            // Only add preset selection for configurable mods when files are available
            if (mod is IConfigurableMod configurableMod && isAvailable)
            {
                // Standard positioning when files are available
                int baseX = 300; 
                int comboBoxWidth = 140;
                int buttonX = 550;

                Label presetLabel = new Label
                {
                    Text = "Preset:",
                    Location = new Point(baseX, 10),
                    Size = new Size(50, 16),
                    Font = new Font(Font.FontFamily, 8f)
                };

                ComboBox presetComboBox = new ComboBox
                {
                    Location = new Point(baseX + 55, 8),
                    Size = new Size(comboBoxWidth, 20),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font(Font.FontFamily, 8f)
                };

                // Populate user presets
                RefreshModPresetComboBox(configurableMod, presetComboBox);

                // Store reference for later updates
                _modPresetComboBoxes[mod.Name] = presetComboBox;

                // Handle preset selection changes
                presetComboBox.SelectedIndexChanged += (s, e) =>
                {
                    if (presetComboBox.SelectedItem?.ToString() == "(No preset)")
                    {
                        configurableMod.SetSelectedPreset(null);
                    }
                    else if (presetComboBox.SelectedItem != null && 
                             presetComboBox.SelectedItem.ToString() != "(No presets available)")
                    {
                        configurableMod.SetSelectedPreset(presetComboBox.SelectedItem.ToString());
                    }
                };

                Button configureButton = new Button
                {
                    Text = "Configure",
                    Location = new Point(buttonX, 6),
                    Size = new Size(70, 24),
                    Font = new Font(Font.FontFamily, 8f)
                };

                configureButton.Click += (s, e) => OpenModConfiguration(configurableMod);

                modPanel.Controls.Add(presetLabel);
                modPanel.Controls.Add(presetComboBox);
                modPanel.Controls.Add(configureButton);
            }

            return modPanel;
        }

        private void RefreshModPresetComboBox(IConfigurableMod mod, ComboBox comboBox)
        {
            comboBox.Items.Clear();
            
            // Use the shared preset service instead of the mod's service to ensure consistency
            var userPresets = _presetService.LoadPresets(mod.Name);
            
            comboBox.Items.Add("(No preset)");
            
            if (userPresets.Count == 0)
            {
                comboBox.Items.Add("(No presets available)");
                comboBox.SelectedIndex = 1;
            }
            else
            {
                foreach (var preset in userPresets.OrderBy(p => p.Name))
                {
                    comboBox.Items.Add(preset.Name);
                }
                
                // Select the currently selected preset or default to "No preset"
                var selectedPreset = mod.GetSelectedPreset();
                if (!string.IsNullOrEmpty(selectedPreset) && comboBox.Items.Contains(selectedPreset))
                {
                    comboBox.SelectedItem = selectedPreset;
                }
                else
                {
                    comboBox.SelectedIndex = 0; // "(No preset)"
                }
            }
        }

        private void OpenModConfiguration(IConfigurableMod mod)
        {
            try
            {
                var modConfiguration = mod.GetUIConfiguration();
                using (var configForm = new ModConfigurationForm(modConfiguration, _presetService))
                {
                    if (configForm.ShowDialog() == DialogResult.OK)
                    {
                        var configuration = configForm.SavedConfiguration;
                        
                        // Save the configuration for later use when installing the mod
                        mod.SaveConfiguration(configuration);
                        
                        MessageBox.Show($"Configuration for {mod.Name} saved successfully!\n\n" +
                                      "The mod will use these settings when you install it.",
                            "Configuration Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    // Note: Preset combo boxes are automatically refreshed via the PresetChanged event
                    // No need to manually refresh here anymore
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during mod configuration: {ex.Message}", 
                    "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<IMod> GetSelectedMods()
        {
            var selectedMods = new List<IMod>();
            var modListPanel = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "panelModsContainer");
            
            if (modListPanel != null)
            {
                foreach (Panel modPanel in modListPanel.Controls.OfType<Panel>())
                {
                    var checkBox = modPanel.Controls.OfType<CheckBox>().FirstOrDefault();
                    if (checkBox?.Checked == true && checkBox.Tag is IMod mod)
                    {
                        selectedMods.Add(mod);
                    }
                }
            }

            return selectedMods;
        }

        private void ShowNoPresetsMessage()
        {
            MessageBox.Show("No user presets have been created yet.\n\n" +
                          "To create a preset:\n" +
                          "1. Select a configurable mod (DS3 Fog Gate Randomizer or DS3 Item & Enemy Randomizer)\n" +
                          "2. Click 'Configure' to open the configuration window\n" +
                          "3. Set your desired options and click 'Save As...' to create a preset\n" +
                          "4. Your preset will then be available for selection in the mod list",
                "No Presets Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowMissingFilesInfo()
        {
            var selectedGame = _gameManager.GetSelectedGame();
            if (selectedGame == null)
            {
                MessageBox.Show("Please select a game first.", "No Game Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // Map game names to download service format
            string downloadGameName = selectedGame.Name switch
            {
                "Dark Souls 3" => "DS3",
                "Sekiro: Shadows Die Twice" => "Sekiro",
                _ => selectedGame.Name
            };
            
            // Process any manually downloaded files to create expected filenames
            _ = Task.Run(async () =>
            {
                try
                {
                    await _downloadService.CreateExpectedFilenamesForGame(downloadGameName);
                    
                    // Refresh the UI on the main thread after processing
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => ShowMissingFilesInfoAfterProcessing(selectedGame, downloadGameName)));
                    }
                    else
                    {
                        ShowMissingFilesInfoAfterProcessing(selectedGame, downloadGameName);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing filenames: {ex.Message}");
                    // Fallback to original logic
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => ShowMissingFilesInfoAfterProcessing(selectedGame, downloadGameName)));
                    }
                    else
                    {
                        ShowMissingFilesInfoAfterProcessing(selectedGame, downloadGameName);
                    }
                }
            });
        }
        
        private void ShowMissingFilesInfoAfterProcessing(IGame selectedGame, string downloadGameName)
        {
            var allMods = selectedGame.Mods;
            var missingMods = allMods.Where(mod => !mod.IsAvailable()).ToList();

            // Check prerequisites based on game type
            var missingPrerequisites = new List<string>();
            
            if (selectedGame.Name == "Sekiro: Shadows Die Twice")
            {
                var sekiroGame = selectedGame as Games.Game_Sekiro;
                if (sekiroGame != null)
                {
                    // Check Sekiro prerequisites using reflection to access private fields
                    var modEngineField = typeof(Games.Game_Sekiro).GetField("_modEngine", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var combinedSFXField = typeof(Games.Game_Sekiro).GetField("_combinedSFX", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var divineDragonTexturesField = typeof(Games.Game_Sekiro).GetField("_divineDragonTextures", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (modEngineField?.GetValue(sekiroGame) is IMod modEngine && !modEngine.IsAvailable())
                        missingPrerequisites.Add($"• {modEngine.Name} - Missing: {modEngine.ModFile}");
                    if (combinedSFXField?.GetValue(sekiroGame) is IMod combinedSFX && !combinedSFX.IsAvailable())
                        missingPrerequisites.Add($"• {combinedSFX.Name} - Missing: {combinedSFX.ModFile}");
                    if (divineDragonTexturesField?.GetValue(sekiroGame) is IMod divineDragonTextures && !divineDragonTextures.IsAvailable())
                        missingPrerequisites.Add($"• {divineDragonTextures.Name} - Missing: {divineDragonTextures.ModFile}");
                }
            }
            else if (selectedGame.Name == "Dark Souls 3")
            {
                var ds3Game = selectedGame as Games.Game_DS3;
                if (ds3Game != null)
                {
                    // Check DS3 prerequisites using reflection to access private fields
                    var modEngineField = typeof(Games.Game_DS3).GetField("_modEngine", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var crashFixField = typeof(Games.Game_DS3).GetField("_crashFix", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (modEngineField?.GetValue(ds3Game) is IMod modEngine && !modEngine.IsAvailable())
                        missingPrerequisites.Add($"• {modEngine.Name} - Missing: {modEngine.ModFile}");
                    if (crashFixField?.GetValue(ds3Game) is IMod crashFix && !crashFix.IsAvailable())
                        missingPrerequisites.Add($"• {crashFix.Name} - Missing: {crashFix.ModFile}");
                }
            }

            if (missingMods.Any() || missingPrerequisites.Any())
            {
                var message = new System.Text.StringBuilder();
                message.AppendLine($"File Status for {selectedGame.Name}:");
                message.AppendLine();

                if (missingPrerequisites.Any())
                {
                    message.AppendLine("MISSING PREREQUISITES (required for all mods):");
                    foreach (var prerequisite in missingPrerequisites)
                    {
                        message.AppendLine(prerequisite);
                    }
                    message.AppendLine();
                }

                if (missingMods.Any())
                {
                    message.AppendLine("MISSING MOD FILES:");
                    var missingFilesList = missingMods.Select(mod => 
                        $"• {mod.Name} - Missing: {mod.ModFile}");
                    foreach (var missingFile in missingFilesList)
                    {
                        message.AppendLine(missingFile);
                    }
                    message.AppendLine();
                }

                message.AppendLine($"These files should be placed in: {selectedGame.ModFolder}");
                message.AppendLine();
                message.AppendLine("NOTE: The app automatically detects files with different names from Nexus Mods.");
                message.AppendLine("Would you like to automatically download the missing files using Nexus Mods integration?");
                
                var result = MessageBox.Show(message.ToString(), "Missing Mod Files", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    
                if (result == DialogResult.Yes)
                {
                    OpenDownloadManager(selectedGame.Name);
                }
            }
            else
            {
                MessageBox.Show($"✓ All mod files and prerequisites are available for {selectedGame.Name}!\n\nAll required files are present in: {selectedGame.ModFolder}", 
                    "All Files Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void EnableModControls(bool enabled)
        {
            var modListPanel = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "ModListPanel");
            if (modListPanel != null)
            {
                modListPanel.Enabled = enabled;
            }
            
            btnInstallMods.Enabled = enabled;
            btnClearMods.Enabled = enabled;
            btnConfigureMod.Enabled = enabled; // Always enable to check file status
            btnConfigureMod.Text = "Check Files"; // Update button text
            btnDownloadFiles.Enabled = enabled; // Enable/disable download button
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (_folderBrowserDialog?.ShowDialog() == DialogResult.OK)
            {
                var selectedPath = _folderBrowserDialog.SelectedPath;
                
                if (_gameManager.ValidateInstallPath(selectedPath))
                {
                    txtInstallPath.Text = selectedPath;
                    _gameManager.SetGameInstallPath(selectedPath);
                    EnableModControls(true);
                }
                else
                {
                    MessageBox.Show($"Invalid installation path!\n\n{_gameManager.GetValidationMessage()}", 
                        "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    EnableModControls(false);
                }
                
                // Update status message after path selection
                UpdateStatusMessage();
            }
        }

        private void txtInstallPath_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtInstallPath.Text))
            {
                if (_gameManager.ValidateInstallPath(txtInstallPath.Text))
                {
                    _gameManager.SetGameInstallPath(txtInstallPath.Text);
                    EnableModControls(true);
                    txtInstallPath.BackColor = Color.White;
                }
                else
                {
                    EnableModControls(false);
                    txtInstallPath.BackColor = Color.LightPink;
                }
            }
            else
            {
                EnableModControls(false);
                txtInstallPath.BackColor = Color.White;
            }
            
            // Update status message when path changes
            UpdateStatusMessage();
        }

        private void btnInstallMods_Click(object sender, EventArgs e)
        {
            var selectedMods = GetSelectedMods();

            if (selectedMods.Count == 0)
            {
                // Check if there are any configurable mods without presets selected
                var modListPanel = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "ModListPanel");
                bool hasConfigurableModsWithoutPresets = false;
                
                if (modListPanel != null)
                {
                    foreach (Panel modPanel in modListPanel.Controls.OfType<Panel>())
                    {
                        var checkBox = modPanel.Controls.OfType<CheckBox>().FirstOrDefault();
                        if (checkBox?.Checked == true && checkBox.Tag is IConfigurableMod)
                        {
                            hasConfigurableModsWithoutPresets = true;
                            break;
                        }
                    }
                }

                if (hasConfigurableModsWithoutPresets)
                {
                    ShowNoPresetsMessage();
                }
                else
                {
                    MessageBox.Show("Please select at least one mod to install.", "No Mods Selected", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            // Check for unavailable mods in selection
            var unavailableMods = selectedMods.Where(mod => !mod.IsAvailable()).ToList();
            if (unavailableMods.Any())
            {
                var modNames = string.Join("\n", unavailableMods.Select(m => $"• {m.Name}"));
                MessageBox.Show(
                    $"The following selected mods have missing files and cannot be installed:\n\n{modNames}\n\n" +
                    $"Please ensure all required mod files are placed in the appropriate Data folders before installation.",
                    "Missing Mod Files", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                return;
            }

            // Check if any configurable mods are selected without presets
            var configurableModsWithoutPresets = selectedMods
                .OfType<IConfigurableMod>()
                .Where(mod => string.IsNullOrEmpty(mod.GetSelectedPreset()))
                .ToList();

            if (configurableModsWithoutPresets.Any())
            {
                var modNames = string.Join(", ", configurableModsWithoutPresets.Select(m => m.Name));
                var result = MessageBox.Show(
                    $"The following configurable mods don't have presets selected:\n{modNames}\n\n" +
                    "These mods will use their default settings. Do you want to continue?",
                    "No Presets Selected", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;
            }

            try
            {
                bool success = _gameManager.InstallSelectedMods(selectedMods);
                
                if (success)
                {
                    MessageBox.Show($"Successfully installed {selectedMods.Count} mod(s)!", "Installation Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to install one or more mods. This could be due to:\n\n" +
                                  "• Missing mod files or prerequisites\n" +
                                  "• Invalid installation path\n" +
                                  "• File permission issues\n\n" +
                                  "Please check that all required files are available and try again.", 
                        "Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during installation: {ex.Message}", "Installation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearMods_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to remove all mods? This will restore the game to its original state.", 
                "Confirm Mod Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    bool success = _gameManager.ClearAllMods();
                    
                    if (success)
                    {
                        MessageBox.Show("All mods have been successfully removed!", "Removal Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Uncheck all mod checkboxes
                        var modListPanel = Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "ModListPanel");
                        if (modListPanel != null)
                        {
                            foreach (Panel modPanel in modListPanel.Controls.OfType<Panel>())
                            {
                                var checkBox = modPanel.Controls.OfType<CheckBox>().FirstOrDefault();
                                if (checkBox != null)
                                {
                                    checkBox.Checked = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to remove one or more mods. Please check the installation path and try again.", 
                            "Removal Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred during mod removal: {ex.Message}", "Removal Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnConfigureMod_Click(object sender, EventArgs e)
        {
            // Repurpose this button to show file availability information
            ShowMissingFilesInfo();
        }

        private void btnDownloadFiles_Click(object sender, EventArgs e)
        {
            var selectedGame = _gameManager.GetSelectedGame();
            if (selectedGame == null)
            {
                MessageBox.Show("Please select a game first.", "No Game Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OpenDownloadManager(selectedGame.Name);
        }

        private void OpenDownloadManager(string gameName)
        {
            // Map game names to the folder structure expected by the download service
            string downloadGameName = gameName switch
            {
                "Dark Souls 1" => "DS1",
                "Dark Souls 2" => "DS2",
                "Dark Souls 3" => "DS3",
                "Sekiro: Shadows Die Twice" => "Sekiro",
                _ => gameName
            };

            using var downloadForm = new DownloadProgressForm(_downloadService, downloadGameName);
            downloadForm.ShowDialog(this);
            
            // Refresh the mod list after download to update availability status
            LoadModsForSelectedGame();
        }

        private void UpdateStatusMessage()
        {
            var selectedGame = _gameManager.GetSelectedGame();
            if (selectedGame == null)
            {
                lblStatus.Text = "Please select a game.";
                lblStatus.ForeColor = Color.Gray;
            }
            else if (string.IsNullOrEmpty(selectedGame.InstallPath))
            {
                lblStatus.Text = _gameManager.GetValidationMessage();
                lblStatus.ForeColor = Color.Orange;
            }
            else if (_gameManager.ValidateInstallPath(selectedGame.InstallPath))
            {
                lblStatus.Text = $"✓ {selectedGame.Name} - Ready to install mods";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                lblStatus.Text = $"✗ Invalid path: {_gameManager.GetValidationMessage()}";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void RefreshUIForSelectedGame()
        {
            var selectedGame = _gameManager.GetSelectedGame();
            if (selectedGame != null)
            {
                LoadModsForSelectedGame();
                txtInstallPath.Text = selectedGame.InstallPath ?? string.Empty;
                EnableModControls(!string.IsNullOrEmpty(selectedGame.InstallPath));
                
                // Update the folder browser dialog description
                if (_folderBrowserDialog != null)
                {
                    _folderBrowserDialog.Description = _gameManager.GetValidationMessage();
                }
            }
        }

        /// <summary>
        /// Checks for application updates asynchronously
        /// </summary>
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var result = await _versionCheckService.CheckForUpdatesAsync();
                
                if (result.IsUpdateAvailable)
                {
                    // Show the update dialog on the UI thread
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => _versionCheckService.ShowUpdateDialog(result)));
                    }
                    else
                    {
                        _versionCheckService.ShowUpdateDialog(result);
                    }
                }
                else if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    // Optionally log the error, but don't show it to the user as it's not critical
                    System.Diagnostics.Debug.WriteLine($"Version check failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't show it to the user
                System.Diagnostics.Debug.WriteLine($"Version check exception: {ex.Message}");
            }
        }
    }
}
