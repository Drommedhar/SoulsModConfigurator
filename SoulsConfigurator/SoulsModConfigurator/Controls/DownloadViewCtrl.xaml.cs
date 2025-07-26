using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SoulsConfigurator.Services;
using System.Net.Http;
using System.Text.Json;
using System.IO;

namespace SoulsModConfigurator.Controls
{
    public partial class DownloadViewCtrl : UserControl
    {
        private readonly ModDownloadService _downloadService;
        private readonly NexusModsService _nexusService;
        private bool _isPremium = false;
        private FileSystemWatcher? _fileWatcher;

        // UI Elements (manually defined to avoid XAML compilation issues)
        private TextBlock _statusIcon;
        private TextBlock _statusTitle;
        private TextBlock _statusDescription;
        private Button _authButton;
        private Button _btnAutoDownload;
        private TextBlock _autoDownloadDescription;
        private Border _premiumWarningPanel;
        private RadioButton _rbDS1;
        private RadioButton _rbDS2;
        private RadioButton _rbDS3;
        private RadioButton _rbSekiro;
        private Border _missingFilesPanel;
        private ItemsControl _missingFilesList;

        // Event to notify when files change - this will be used by MainWindow to refresh all game views
        public event EventHandler? FilesChanged;

        public DownloadViewCtrl()
        {
            InitializeComponent();
            _downloadService = new ModDownloadService();
            _nexusService = new NexusModsService();
            
            _downloadService.DownloadProgress += OnDownloadProgress;
            _downloadService.DownloadCompleted += OnDownloadCompleted;
            _downloadService.DownloadFailed += OnDownloadFailed;
            
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            FindUIElements();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Task.Run(async () => await UpdateAuthenticationStatus());
            UpdateGameSelection();
            SetupFileWatcher();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DisposeFileWatcher();
        }

        private void SetupFileWatcher()
        {
            if (_fileWatcher != null) return;
            
            try
            {
                // Create Data directory if it doesn't exist
                string dataPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Data");
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }

                _fileWatcher = new FileSystemWatcher(dataPath)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.Size,
                    Filter = "*.*"
                };

                // Subscribe to events
                _fileWatcher.Created += OnFileChanged;
                _fileWatcher.Deleted += OnFileChanged;
                _fileWatcher.Renamed += OnFileRenamed;

                // Enable monitoring
                _fileWatcher.EnableRaisingEvents = true;
                
                System.Diagnostics.Debug.WriteLine($"DownloadView file watcher setup for: {dataPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up file watcher in DownloadView: {ex.Message}");
            }
        }

        private void DisposeFileWatcher()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Created -= OnFileChanged;
                _fileWatcher.Deleted -= OnFileChanged;
                _fileWatcher.Renamed -= OnFileRenamed;
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (IsRelevantModFile(e.FullPath))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"DownloadView detected file change: {e.ChangeType} - {e.FullPath}");
                    UpdateMissingFilesDelayed();
                    // Notify MainWindow to refresh all game views
                    FilesChanged?.Invoke(this, EventArgs.Empty);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (IsRelevantModFile(e.FullPath) || IsRelevantModFile(e.OldFullPath))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"DownloadView detected file rename: {e.OldFullPath} -> {e.FullPath}");
                    UpdateMissingFilesDelayed();
                    // Notify MainWindow to refresh all game views
                    FilesChanged?.Invoke(this, EventArgs.Empty);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private bool IsRelevantModFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            
            try
            {
                string fileName = System.IO.Path.GetFileName(filePath).ToLowerInvariant();
                string directory = System.IO.Path.GetDirectoryName(filePath)?.ToLowerInvariant() ?? "";
                
                // Check if it's in a game-specific data folder
                if (!directory.Contains("\\data\\") && !directory.Contains("/data/")) return false;
                
                // Check for common mod file extensions
                if (fileName.EndsWith(".zip") || 
                    fileName.EndsWith(".exe") || 
                    fileName.EndsWith(".dll") ||
                    fileName.EndsWith(".ini"))
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateMissingFilesDelayed()
        {
            // Debounce rapid file changes
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
            }
            
            _refreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000) // Increased delay to 1000ms
            };
            
            _refreshTimer.Tick += (s, e) =>
            {
                _refreshTimer?.Stop();
                _refreshTimer = null;
                UpdateMissingFiles();
            };
            
            _refreshTimer.Start();
        }
        
        private System.Windows.Threading.DispatcherTimer? _refreshTimer;

        private void FindUIElements()
        {
            // Find UI elements by name to avoid XAML compilation issues
            _statusIcon = FindName("StatusIcon") as TextBlock;
            _statusTitle = FindName("StatusTitle") as TextBlock;
            _statusDescription = FindName("StatusDescription") as TextBlock;
            _authButton = FindName("AuthButton") as Button;
            _btnAutoDownload = FindName("btnAutoDownload") as Button;
            _autoDownloadDescription = FindName("AutoDownloadDescription") as TextBlock;
            _premiumWarningPanel = FindName("PremiumWarningPanel") as Border;
            _rbDS1 = FindName("rbDS1") as RadioButton;
            _rbDS2 = FindName("rbDS2") as RadioButton;
            _rbDS3 = FindName("rbDS3") as RadioButton;
            _rbSekiro = FindName("rbSekiro") as RadioButton;
            _missingFilesPanel = FindName("MissingFilesPanel") as Border;
            _missingFilesList = FindName("MissingFilesList") as ItemsControl;
        }

        private async Task UpdateAuthenticationStatus()
        {
            if (_downloadService.IsAuthenticated)
            {
                // Check for premium status
                _isPremium = await CheckPremiumStatus();
                
                if (_isPremium)
                {
                    if (_statusIcon != null) _statusIcon.Text = "✓";
                    if (_statusTitle != null) _statusTitle.Text = "Authenticated with Nexus Mods Premium";
                    if (_statusDescription != null) _statusDescription.Text = "You can download mods automatically";
                    if (_authButton != null) _authButton.Content = "Logout";
                    if (_btnAutoDownload != null) _btnAutoDownload.IsEnabled = true;
                    if (_autoDownloadDescription != null) _autoDownloadDescription.Text = "Download all required mods automatically";
                    if (_premiumWarningPanel != null) _premiumWarningPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (_statusIcon != null) _statusIcon.Text = "✓";
                    if (_statusTitle != null) _statusTitle.Text = "Authenticated with Nexus Mods (Free)";
                    if (_statusDescription != null) _statusDescription.Text = "Premium subscription required for automatic downloads";
                    if (_authButton != null) _authButton.Content = "Logout";
                    if (_btnAutoDownload != null) _btnAutoDownload.IsEnabled = false;
                    if (_autoDownloadDescription != null) _autoDownloadDescription.Text = "Download all required mods automatically (Premium required)";
                    if (_premiumWarningPanel != null) _premiumWarningPanel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (_statusIcon != null) _statusIcon.Text = "⚠";
                if (_statusTitle != null) _statusTitle.Text = "Not Authenticated with Nexus Mods";
                if (_statusDescription != null) _statusDescription.Text = "Authenticate to enable automatic downloads from Nexus Mods";
                if (_authButton != null) _authButton.Content = "Authenticate with Nexus";
                if (_btnAutoDownload != null) _btnAutoDownload.IsEnabled = false;
                if (_autoDownloadDescription != null) _autoDownloadDescription.Text = "Download all required mods automatically (Authentication required)";
                if (_premiumWarningPanel != null) _premiumWarningPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<bool> CheckPremiumStatus()
        {
            try
            {
                // Use the ModDownloadService method which has better debugging
                return await _downloadService.CheckPremiumStatusAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking premium status: {ex.Message}");
                return false;
            }
        }

        private async void AuthButton_Click(object sender, RoutedEventArgs e)
        {
            if (_downloadService.IsAuthenticated)
            {
                // Logout
                _downloadService.Logout();
                await UpdateAuthenticationStatus();
            }
            else
            {
                // Show loading using simple message box for now
                var success = await _downloadService.AuthenticateAsync();
                
                if (success)
                {
                    await UpdateAuthenticationStatus();
                    MessageBox.Show("Authentication successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Authentication failed. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetSelectedGame()
        {
            if (_rbDS1?.IsChecked == true) return "DS1";
            if (_rbDS2?.IsChecked == true) return "DS2";
            if (_rbDS3?.IsChecked == true) return "DS3";
            if (_rbSekiro?.IsChecked == true) return "Sekiro";
            return "DS3"; // Default
        }

        private async void BtnAutoDownload_Click(object sender, RoutedEventArgs e)
        {
            var game = GetSelectedGame();
            var mainWindow = Window.GetWindow(this) as MainWindow;
            var overlay = mainWindow?.FindName("pnlInfo") as OverlayPanel;
            
            if (overlay == null)
            {
                MessageBox.Show("Could not access overlay panel", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                // Disable button during download
                if (sender is Button btn)
                {
                    btn.IsEnabled = false;
                }
                
                // Get total file count for progress tracking with validation
                var totalFiles = _downloadService.GetTotalFileCount(game);
                if (totalFiles <= 0)
                {
                    totalFiles = 1; // Fallback to prevent division by zero
                }
                
                var currentFileIndex = 1;
                
                // Show the download overlay with initial status
                overlay.ShowDownloadProgress(currentFileIndex, totalFiles);
                
                // Subscribe to download progress events
                _downloadService.DownloadProgress += OnDownloadProgressUpdate;
                _downloadService.DownloadCompleted += OnDownloadCompletedUpdate;
                _downloadService.DownloadFailed += OnDownloadFailedUpdate;
                
                var success = await _downloadService.DownloadAllForGameAsync(game);
                
                // Unsubscribe from events
                _downloadService.DownloadProgress -= OnDownloadProgressUpdate;
                _downloadService.DownloadCompleted -= OnDownloadCompletedUpdate;
                _downloadService.DownloadFailed -= OnDownloadFailedUpdate;
                
                // Hide download progress overlay
                overlay.HideDownloadProgress();
                
                // Show completion notification
                if (success)
                {
                    overlay.ShowNotification("Downloads Complete",
                        $"All mods for {game} have been downloaded successfully!",
                        "https://static-cdn.jtvnw.net/emoticons/v2/emotesv2_3bddbb609de94c67a8ac7f6f898a844c/default/dark/1.0");
                }
                else
                {
                    overlay.ShowNotification("Download Issues",
                        "Some files could not be downloaded. Check the missing files list.",
                        "https://static-cdn.jtvnw.net/emoticons/v2/emotesv2_3bddbb609de94c67a8ac7f6f898a844c/default/dark/1.0");
                }
                
                UpdateMissingFiles();
            }
            catch (Exception ex)
            {
                // Unsubscribe from events in case of error
                _downloadService.DownloadProgress -= OnDownloadProgressUpdate;
                _downloadService.DownloadCompleted -= OnDownloadCompletedUpdate;
                _downloadService.DownloadFailed -= OnDownloadFailedUpdate;
                
                // Hide download progress overlay safely
                try
                {
                    overlay.HideDownloadProgress();
                }
                catch (Exception hideEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error hiding download progress: {hideEx.Message}");
                }
                
                overlay.ShowNotification("Download Error",
                    $"An error occurred during download: {ex.Message}",
                    "https://static-cdn.jtvnw.net/emoticons/v2/emotesv2_3bddbb609de94c67a8ac7f6f898a844c/default/dark/1.0");
            }
            finally
            {
                if (sender is Button btn)
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private void OnDownloadProgressUpdate(object? sender, DownloadProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                var overlay = mainWindow?.FindName("pnlInfo") as OverlayPanel;
                overlay?.UpdateDownloadProgress(e.FileName, e.ProgressPercentage, e.BytesDownloaded, e.TotalBytes);
            });
        }

        private void OnDownloadCompletedUpdate(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                // For completed downloads, we don't need to do anything special here
                // The download service will handle moving to the next file internally
                System.Diagnostics.Debug.WriteLine($"Download completed: {message}");
            });
        }

        private void OnDownloadFailedUpdate(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                // For failed downloads, we could show a temporary error message
                // but continue with the download process
                System.Diagnostics.Debug.WriteLine($"Download failed: {message}");
            });
        }

        private void BtnManualDownload_Click(object sender, RoutedEventArgs e)
        {
            var game = GetSelectedGame();
            _downloadService.OpenBrowserForManualDownload(game);
            MessageBox.Show("Download pages have been opened in your browser. Download the main files and save them to the appropriate Data folder.", "Browser Opened", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnCheckFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Check Files Again clicked - processing all games");
                
                // Process expected filenames for all games to handle files downloaded with different names
                var game = GetSelectedGame();  // Get the current game
                System.Diagnostics.Debug.WriteLine($"Creating expected filenames for {game}...");
                await _downloadService.CreateExpectedFilenamesForGame(game);
                System.Diagnostics.Debug.WriteLine("Expected filenames processing complete");
                
                // Small delay after filename processing
                await Task.Delay(500);
                
                // Update missing files for the current game
                UpdateMissingFiles();
                
                // Also notify that files should be re-checked across all game views
                FilesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in BtnCheckFiles_Click: {ex.Message}");
                
                // Still try to update the UI even if processing fails
                UpdateMissingFiles();
                FilesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateGameSelection()
        {
            // Set up game selection change events
            if (_rbDS1 != null) _rbDS1.Checked += (s, e) => UpdateMissingFiles();
            if (_rbDS2 != null) _rbDS2.Checked += (s, e) => UpdateMissingFiles();
            if (_rbDS3 != null) _rbDS3.Checked += (s, e) => UpdateMissingFiles();
            if (_rbSekiro != null) _rbSekiro.Checked += (s, e) => UpdateMissingFiles();
            
            UpdateMissingFiles();
        }

        private void UpdateMissingFiles()
        {
            var game = GetSelectedGame();
            var missingFiles = _downloadService.GetMissingFiles(game);
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"UpdateMissingFiles for {game}: Found {missingFiles.Count} missing files");
            foreach (var file in missingFiles)
            {
                System.Diagnostics.Debug.WriteLine($"  Missing: {file}");
            }
            
            if (missingFiles.Any())
            {
                if (_missingFilesPanel != null)
                {
                    _missingFilesPanel.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("Showing missing files panel");
                }
                if (_missingFilesList != null)
                {
                    _missingFilesList.ItemsSource = missingFiles;
                    System.Diagnostics.Debug.WriteLine($"Set ItemsSource with {missingFiles.Count} items");
                }
            }
            else
            {
                if (_missingFilesPanel != null)
                {
                    _missingFilesPanel.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine("Hiding missing files panel - no missing files");
                }
            }
        }

        private void OnDownloadProgress(object? sender, DownloadProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Download progress: {e.FileName} - {e.ProgressPercentage:F1}%");
            });
        }

        private void OnDownloadCompleted(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Download completed: {message}");
            });
        }

        private void OnDownloadFailed(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Download failed: {message}");
            });
        }
    }
}