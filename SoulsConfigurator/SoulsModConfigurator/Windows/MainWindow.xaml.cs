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
using SoulsModConfigurator.Controls;

namespace SoulsModConfigurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GameManagerService _gameManager;
        private readonly UserPresetService _presetService;
        private readonly ModDownloadService _downloadService;
        private readonly VersionCheckService _versionCheckService;
        private readonly Dictionary<string, IGame> _gameTabMapping;
        private OverlayPanel? _overlayPanel;

        public MainWindow()
        {
            InitializeComponent();
            
            _gameManager = new GameManagerService();
            _presetService = new UserPresetService();
            _downloadService = new ModDownloadService();
            _versionCheckService = new VersionCheckService();
            _gameTabMapping = new Dictionary<string, IGame>();
            
            // Get reference to overlay panel
            _overlayPanel = FindName("pnlInfo") as OverlayPanel;
            
            // Subscribe to preset change events
            _presetService.PresetChanged += OnPresetChanged;
            
            // Subscribe to notification service events
            NotificationService.Instance.NotificationRequested += OnNotificationRequested;
            
            InitializeForm();
            
            // Ensure data is loaded after the window is fully loaded
            Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Handles preset change events and refreshes the mod list
        /// </summary>
        private void OnPresetChanged(object? sender, PresetChangedEventArgs e)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnPresetChanged(sender, e));
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"OnPresetChanged: ModName='{e.ModName}', PresetName='{e.PresetName}', ChangeType={e.ChangeType}");
                
                // Get the currently selected game view and refresh it
                var tabControl = FindName("tabControlGames") as TabControl;
                var selectedTab = tabControl?.SelectedItem as TabItem;
                if (selectedTab?.Content is GameViewCtrl gameView)
                {
                    gameView.RefreshModList();
                }
                
                System.Diagnostics.Debug.WriteLine("OnPresetChanged: Mod list refresh complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnPresetChanged: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Handles notification requests from the NotificationService
        /// </summary>
        private void OnNotificationRequested(object? sender, NotificationMessage notification)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnNotificationRequested(sender, notification));
                return;
            }

            // Show the notification using the OverlayPanel
            _overlayPanel?.ShowNotification(notification);
        }

        /// <summary>
        /// Handles file changes detected in the DownloadView and refreshes all game views
        /// </summary>
        private void OnFilesChanged(object? sender, EventArgs e)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => OnFilesChanged(sender, e));
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("Files changed detected - processing file names and refreshing all game views");
                
                // Add a small delay to ensure file operations are complete
                var delayTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1500) // 1.5 second delay
                };
                
                delayTimer.Tick += async (s, args) =>
                {
                    delayTimer.Stop();
                    
                    try
                    {
                        // Process expected filenames for all games to handle files downloaded with different names
                        System.Diagnostics.Debug.WriteLine("Creating expected filenames for all games...");
                        await _downloadService.CreateExpectedFilenamesForGame("DS1");
                        await _downloadService.CreateExpectedFilenamesForGame("DS2");
                        await _downloadService.CreateExpectedFilenamesForGame("DS3");
                        await _downloadService.CreateExpectedFilenamesForGame("Sekiro");
                        System.Diagnostics.Debug.WriteLine("Expected filenames processing complete");
                        
                        // Small additional delay after filename processing
                        await Task.Delay(500);
                        
                        // Refresh all game view controls to update mod availability
                        var gameViewDS1 = FindName("GameViewDS1") as GameViewCtrl;
                        var gameViewDS2 = FindName("GameViewDS2") as GameViewCtrl;
                        var gameViewDS3 = FindName("GameViewDS3") as GameViewCtrl;
                        var gameViewSekiro = FindName("GameViewSekiro") as GameViewCtrl;
                        
                        gameViewDS1?.RefreshModList();
                        gameViewDS2?.RefreshModList();
                        gameViewDS3?.RefreshModList();
                        gameViewSekiro?.RefreshModList();
                        
                        System.Diagnostics.Debug.WriteLine("All game views refreshed due to file changes (after filename processing)");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error during filename processing: {ex.Message}");
                        
                        // Still refresh the views even if filename processing fails
                        var gameViewDS1 = FindName("GameViewDS1") as GameViewCtrl;
                        var gameViewDS2 = FindName("GameViewDS2") as GameViewCtrl;
                        var gameViewDS3 = FindName("GameViewDS3") as GameViewCtrl;
                        var gameViewSekiro = FindName("GameViewSekiro") as GameViewCtrl;
                        
                        gameViewDS1?.RefreshModList();
                        gameViewDS2?.RefreshModList();
                        gameViewDS3?.RefreshModList();
                        gameViewSekiro?.RefreshModList();
                    }
                };
                
                delayTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnFilesChanged: {ex.Message}");
            }
        }

        private async void InitializeForm()
        {
            // Set up game-tab mapping
            var games = _gameManager.GetAvailableGames();
            var gamesByName = games.ToDictionary(g => g.Name, g => g);

            _gameTabMapping["Dark Souls 1"] = gamesByName.GetValueOrDefault("Dark Souls 1")!;
            _gameTabMapping["Dark Souls 2"] = gamesByName.GetValueOrDefault("Dark Souls 2")!;
            _gameTabMapping["Dark Souls 3"] = gamesByName.GetValueOrDefault("Dark Souls 3")!;
            _gameTabMapping["Sekiro"] = gamesByName.GetValueOrDefault("Sekiro: Shadows Die Twice")!;

            // Initialize game views
            var gameViewDS1 = FindName("GameViewDS1") as GameViewCtrl;
            var gameViewDS2 = FindName("GameViewDS2") as GameViewCtrl;
            var gameViewDS3 = FindName("GameViewDS3") as GameViewCtrl;
            var gameViewSekiro = FindName("GameViewSekiro") as GameViewCtrl;
            
            gameViewDS1?.Initialize(_gameTabMapping["Dark Souls 1"], _gameManager, _presetService, _downloadService);
            gameViewDS2?.Initialize(_gameTabMapping["Dark Souls 2"], _gameManager, _presetService, _downloadService);
            gameViewDS3?.Initialize(_gameTabMapping["Dark Souls 3"], _gameManager, _presetService, _downloadService);
            gameViewSekiro?.Initialize(_gameTabMapping["Sekiro"], _gameManager, _presetService, _downloadService);

            // Initialize the download view and subscribe to its FilesChanged event
            var downloadView = FindName("DownloadView") as DownloadViewCtrl;
            if (downloadView != null)
            {
                downloadView.FilesChanged += OnFilesChanged;
            }

            // Check if a game was auto-selected during initialization
            var selectedGame = _gameManager.GetSelectedGame();
            if (selectedGame != null)
            {
                // Select the appropriate tab based on the selected game
                SelectTabForGame(selectedGame);
            }
            else
            {
                // If no game was auto-selected, select the first game by default
                var firstGame = _gameTabMapping["Dark Souls 1"];
                if (firstGame != null)
                {
                    _gameManager.SelectGame(firstGame);
                    var tabControl = FindName("tabControlGames") as TabControl;
                    if (tabControl != null)
                        tabControl.SelectedIndex = 0;
                }
            }
            
            // Check for updates asynchronously
            await CheckForUpdatesAsync();
            
            // Check for outdated presets asynchronously
            _ = CheckForOutdatedPresetsAsync();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure all controls are fully loaded before refreshing data
            Dispatcher.BeginInvoke(new Action(() => 
            {
                LoadInitialTabData();
                
                // Also trigger a refresh of the currently selected tab to ensure data is visible
                var tabControl = FindName("tabControlGames") as TabControl;
                if (tabControl != null && tabControl.SelectedItem is TabItem selectedTab)
                {
                    if (selectedTab.Content is GameViewCtrl gameView)
                    {
                        gameView.RefreshForSelectedGame();
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void LoadInitialTabData()
        {
            // Load data for the currently selected tab (should be tab 0 by default)
            var currentGame = _gameManager.GetSelectedGame();
            if (currentGame != null)
            {
                switch (currentGame.Name)
                {
                    case "Dark Souls 1":
                        (FindName("GameViewDS1") as GameViewCtrl)?.RefreshForSelectedGame();
                        break;
                    case "Dark Souls 2":
                        (FindName("GameViewDS2") as GameViewCtrl)?.RefreshForSelectedGame();
                        break;
                    case "Dark Souls 3":
                        (FindName("GameViewDS3") as GameViewCtrl)?.RefreshForSelectedGame();
                        break;
                    case "Sekiro: Shadows Die Twice":
                        (FindName("GameViewSekiro") as GameViewCtrl)?.RefreshForSelectedGame();
                        break;
                }
            }
        }

        private void SelectTabForGame(IGame game)
        {
            var tabControl = FindName("tabControlGames") as TabControl;
            if (tabControl != null)
            {
                switch (game.Name)
                {
                    case "Dark Souls 1":
                        tabControl.SelectedIndex = 0;
                        break;
                    case "Dark Souls 2":
                        tabControl.SelectedIndex = 1;
                        break;
                    case "Dark Souls 3":
                        tabControl.SelectedIndex = 2;
                        break;
                    case "Sekiro: Shadows Die Twice":
                        tabControl.SelectedIndex = 3;
                        break;
                }
            }
        }

        private void TabControlGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabControl = FindName("tabControlGames") as TabControl;
            if (e.Source != tabControl || tabControl == null) return;

            var selectedTab = tabControl.SelectedItem as TabItem;
            if (selectedTab?.Header is StackPanel headerPanel)
            {
                var label = headerPanel.Children.OfType<Label>().FirstOrDefault();
                if (label?.Content is string gameName && _gameTabMapping.TryGetValue(gameName, out var game))
                {
                    _gameManager.SelectGame(game);
                    
                    // Ensure the game view is properly initialized before refreshing
                    if (selectedTab.Content is GameViewCtrl gameView)
                    {
                        // Always re-initialize to ensure proper setup
                        gameView.Initialize(game, _gameManager, _presetService, _downloadService);
                        
                        // Use dispatcher to ensure UI is ready
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            gameView.RefreshForSelectedGame();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
            }
        }


        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var result = await _versionCheckService.CheckForUpdatesAsync();
                if (result.IsUpdateAvailable)
                {
                    var content = $"There is a new version available.";

                    NotificationService.Instance.ShowNotification(new NotificationMessage
                    {
                        Title = "Update available",
                        Content = content,
                        Type = NotificationType.Warning,
                        Links = new Dictionary<string, string>
                        {
                            { "Click here to get it.", "https://github.com/Drommedhar/SoulsModConfigurator/releases/latest" }
                        },
                        IsClosable = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for updates: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks for outdated presets and shows notifications if any are found
        /// </summary>
        private async Task CheckForOutdatedPresetsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Check for outdated presets
                    _presetService.CheckForOutdatedPresets();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for outdated presets: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the overlay panel with the specified status message
        /// </summary>
        /// <param name="statusMessage">The status message to display</param>
        public void ShowOverlay(string statusMessage = "Processing mod installation...")
        {
            _overlayPanel?.ShowOverlay(statusMessage);
        }

        /// <summary>
        /// Hides the overlay panel
        /// </summary>
        public void HideOverlay()
        {
            _overlayPanel?.HideOverlay();
        }

        /// <summary>
        /// Updates the status message in the overlay
        /// </summary>
        /// <param name="statusMessage">The new status message</param>
        public void UpdateOverlayStatus(string statusMessage)
        {
            _overlayPanel?.UpdateStatus(statusMessage);
        }

        /// <summary>
        /// Shows the overlay for UI-based executables
        /// </summary>
        /// <param name="modName">The name of the mod being processed</param>
        public void ShowOverlayForUIExecutable(string modName)
        {
            _overlayPanel?.ShowForUIExecutable(modName);
        }

        /// <summary>
        /// Shows the overlay for command line executables
        /// </summary>
        /// <param name="modName">The name of the mod being processed</param>
        public void ShowOverlayForCommandLineExecutable(string modName)
        {
            _overlayPanel?.ShowForCommandLineExecutable(modName);
        }
    }
}