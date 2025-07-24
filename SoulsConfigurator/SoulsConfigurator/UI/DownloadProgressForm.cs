using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SoulsConfigurator.Services;

namespace SoulsConfigurator.UI
{
    public partial class DownloadProgressForm : Form
    {
        private readonly ModDownloadService _downloadService;
        private readonly string _gameName;
        private Label _statusLabel = null!;
        private ProgressBar _progressBar = null!;
        private ListBox _logListBox = null!;
        private Button _closeButton = null!;
        private Button _authenticateButton = null!;
        
        // Enhanced progress tracking
        private int _totalFilesToDownload = 0;
        private int _completedDownloads = 0;
        private readonly Dictionary<string, double> _fileProgress = new();

        public DownloadProgressForm(ModDownloadService downloadService, string gameName)
        {
            _downloadService = downloadService;
            _gameName = gameName;
            InitializeComponent();
            SetupEventHandlers();
        }
        
        private void InitializeComponent()
        {
            Text = $"Downloading {_gameName} Mod Files";
            Size = new Size(750, 500); // Increased width from 650 to 750 to accommodate manual download button
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModConfigurationForm));
            Icon = (Icon)resources.GetObject("$this.Icon");

            // Status label
            _statusLabel = new Label
            {
                Text = "Click 'Authenticate with Nexus Mods' to begin downloading...",
                Location = new Point(12, 12),
                Size = new Size(710, 20), // Increased width to match new form width
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_statusLabel);
            
            // Authenticate button
            _authenticateButton = new Button
            {
                Text = "Authenticate with Nexus Mods",
                Location = new Point(12, 40),
                Size = new Size(200, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                UseVisualStyleBackColor = true
            };
            _authenticateButton.Click += AuthenticateButton_Click;
            Controls.Add(_authenticateButton);
            
            // Help label
            var helpLabel = new Label
            {
                Text = "This will open your browser to authenticate with Nexus Mods using SSO",
                Location = new Point(12, 78),
                Size = new Size(450, 20), // Increased width
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                ForeColor = Color.Gray
            };
            Controls.Add(helpLabel);
            
            // Progress bar
            _progressBar = new ProgressBar
            {
                Location = new Point(12, 105),
                Size = new Size(710, 23), // Increased width to match new form width
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Style = ProgressBarStyle.Continuous
            };
            Controls.Add(_progressBar);
            
            // Log list box
            _logListBox = new ListBox
            {
                Location = new Point(12, 135),
                Size = new Size(710, 320), // Increased width to match new form width
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ScrollAlwaysVisible = true
            };
            Controls.Add(_logListBox);
            
            // Close button
            _closeButton = new Button
            {
                Text = "Close",
                Location = new Point(647, 465), // Adjusted position for new form width
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            _closeButton.Click += (s, e) => Close();
            Controls.Add(_closeButton);
            
            // Update initial state
            UpdateAuthenticationState();
        }
        
        private void SetupEventHandlers()
        {
            _downloadService.DownloadProgress += OnDownloadProgress;
            _downloadService.DownloadCompleted += OnDownloadCompleted;
            _downloadService.DownloadFailed += OnDownloadFailed;
        }
        
        private void UpdateAuthenticationState()
        {
            try
            {
                var isAuth = _downloadService.IsAuthenticated;
                AddLogMessage($"DEBUG: UpdateAuthenticationState called - IsAuthenticated: {isAuth}");
                
                if (isAuth)
                {
                    _authenticateButton.Text = "✓ Authenticated";
                    _authenticateButton.Enabled = false;
                    _statusLabel.Text = "Ready to download. Click Start Download to begin.";
                    
                    // Add start download button if not already present
                    if (!Controls.ContainsKey("startDownloadButton"))
                    {
                        AddLogMessage("DEBUG: Adding Start Download button...");
                        var startButton = new Button
                        {
                            Name = "startDownloadButton",
                            Text = "Start Download",
                            Location = new Point(220, 40), // Position to the right of authenticate button
                            Size = new Size(120, 30),
                            Anchor = AnchorStyles.Top | AnchorStyles.Left,
                            UseVisualStyleBackColor = true
                        };
                        startButton.Click += StartDownloadButton_Click;
                        Controls.Add(startButton);
                        AddLogMessage("DEBUG: Start Download button added to controls");
                    }
                    else
                    {
                        AddLogMessage("DEBUG: Start Download button already exists");
                    }
                    
                    // Add logout button for clearing saved authentication
                    if (!Controls.ContainsKey("logoutButton"))
                    {
                        AddLogMessage("DEBUG: Adding Logout button...");
                        var logoutButton = new Button
                        {
                            Name = "logoutButton",
                            Text = "Logout",
                            Location = new Point(350, 40), // Position to the right of start download button
                            Size = new Size(75, 30),
                            Anchor = AnchorStyles.Top | AnchorStyles.Left,
                            UseVisualStyleBackColor = true
                        };
                        logoutButton.Click += LogoutButton_Click;
                        Controls.Add(logoutButton);
                        AddLogMessage("DEBUG: Logout button added to controls");
                    }
                    else
                    {
                        AddLogMessage("DEBUG: Logout button already exists");
                    }
                    
                    // Add manual download button
                    if (!Controls.ContainsKey("manualDownloadButton"))
                    {
                        AddLogMessage("DEBUG: Adding Manual Download button...");
                        var manualButton = new Button
                        {
                            Name = "manualDownloadButton",
                            Text = "Manual Download",
                            Location = new Point(435, 40), // Position to the right of logout button
                            Size = new Size(120, 30),
                            Anchor = AnchorStyles.Top | AnchorStyles.Left,
                            UseVisualStyleBackColor = true,
                            BackColor = Color.LightBlue
                        };
                        manualButton.Click += ManualDownloadButton_Click;
                        Controls.Add(manualButton);
                        AddLogMessage("DEBUG: Manual Download button added to controls");
                    }
                    else
                    {
                        AddLogMessage("DEBUG: Manual Download button already exists");
                    }
                    
                    // Force a refresh to make sure buttons are visible
                    Refresh();
                }
                else
                {
                    _authenticateButton.Text = "Authenticate with Nexus Mods";
                    _authenticateButton.Enabled = true;
                    _statusLabel.Text = "Click 'Authenticate with Nexus Mods' to begin downloading.";
                    
                    // Remove start download button if present
                    var startButton = Controls["startDownloadButton"];
                    if (startButton != null)
                    {
                        AddLogMessage("DEBUG: Removing Start Download button...");
                        Controls.Remove(startButton);
                        startButton.Dispose();
                    }
                    
                    // Remove logout button if present
                    var logoutButton = Controls["logoutButton"];
                    if (logoutButton != null)
                    {
                        AddLogMessage("DEBUG: Removing Logout button...");
                        Controls.Remove(logoutButton);
                        logoutButton.Dispose();
                    }
                    
                    // Remove manual download button if present
                    var manualButton = Controls["manualDownloadButton"];
                    if (manualButton != null)
                    {
                        AddLogMessage("DEBUG: Removing Manual Download button...");
                        Controls.Remove(manualButton);
                        manualButton.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"ERROR in UpdateAuthenticationState: {ex.Message}");
            }
        }
        
        private async void AuthenticateButton_Click(object? sender, EventArgs e)
        {
            _authenticateButton.Enabled = false;
            _authenticateButton.Text = "Authenticating...";
            _statusLabel.Text = "Opening browser for authentication...";
            
            AddLogMessage("Starting SSO authentication with Nexus Mods...");
            AddLogMessage("A browser window will open for you to sign in.");
            AddLogMessage("Please complete the authentication in your browser and wait for the process to finish.");
            
            var success = await _downloadService.AuthenticateAsync();
            
            if (success)
            {
                UpdateAuthenticationState();
                AddLogMessage("✓ Authentication successful!");
                AddLogMessage("You can now start downloading mod files.");
            }
            else
            {
                _authenticateButton.Enabled = true;
                _authenticateButton.Text = "Retry Authentication";
                _statusLabel.Text = "Authentication failed. Please try again.";
                AddLogMessage("✗ Authentication failed. This can happen if:");
                AddLogMessage("  • The browser authentication was not completed");
                AddLogMessage("  • The authentication process timed out");
                AddLogMessage("  • There was a network connectivity issue");
                AddLogMessage("  • Nexus Mods SSO service is temporarily unavailable");
                AddLogMessage("");
                AddLogMessage("Please try the following:");
                AddLogMessage("  1. Make sure you're logged into Nexus Mods in your browser");
                AddLogMessage("  2. Click 'Retry Authentication' to try again");
                AddLogMessage("  3. Complete the authentication process in the browser window");
                AddLogMessage("  4. Wait for the 'Authentication successful' message");
            }
        }
        
        private async void StartDownloadButton_Click(object? sender, EventArgs e)
        {
            var startButton = Controls["startDownloadButton"] as Button;
            if (startButton != null)
            {
                startButton.Enabled = false;
                startButton.Text = "Downloading...";
            }
            
            _statusLabel.Text = $"Checking {_gameName} mod files...";
            
            var missingFiles = _downloadService.GetMissingFiles(_gameName);
            var totalFiles = _downloadService.GetTotalFileCount(_gameName);
            
            // Initialize progress tracking - use total files instead of just missing files
            // because we'll get completion events for all files (including skipped ones)
            _totalFilesToDownload = totalFiles;
            _completedDownloads = totalFiles - missingFiles.Count; // Start with already-present files
            _fileProgress.Clear();
            
            AddLogMessage($"Found {missingFiles.Count} missing files out of {totalFiles} total files.");
            AddLogMessage($"DEBUG: Initial setup - completed: {_completedDownloads}, total: {_totalFilesToDownload}");
            
            if (missingFiles.Count == 0)
            {
                AddLogMessage("✓ All files are already present!");
                _statusLabel.Text = "All files already downloaded.";
                _progressBar.Style = ProgressBarStyle.Continuous;
                _progressBar.Value = 100;
                _closeButton.Enabled = true;
                return;
            }
            
            // Initialize progress bar for real progress tracking
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 100;
            
            // Set initial progress based on already-present files
            if (_totalFilesToDownload > 0)
            {
                double initialProgress = ((double)_completedDownloads / _totalFilesToDownload) * 100;
                _progressBar.Value = Math.Min(100, (int)initialProgress);
                AddLogMessage($"DEBUG: Initial progress set to {initialProgress:F1}%");
            }
            else
            {
                _progressBar.Value = 0;
            }
            
            foreach (var file in missingFiles)
            {
                AddLogMessage($"Missing: {file}");
            }
            
            AddLogMessage("Starting downloads...");
            _statusLabel.Text = $"Downloading {missingFiles.Count} missing files...";
            
            try
            {
                var success = await _downloadService.DownloadAllForGameAsync(_gameName);
                
                if (success)
                {
                    _progressBar.Value = 100;
                    _statusLabel.Text = $"✓ Successfully downloaded all {_gameName} mod files!";
                    AddLogMessage("✓ All downloads completed successfully!");
                }
                else
                {
                    _statusLabel.Text = "Downloads failed - Opening browser for manual download";
                    AddLogMessage("✗ Automatic downloads failed due to premium membership requirements.");
                    AddLogMessage("");
                    AddLogMessage("🌐 OPENING BROWSER TABS FOR MANUAL DOWNLOAD");
                    AddLogMessage("Opening mod pages in your browser for easy manual download...");
                    
                    // Open browser tabs for each required mod
                    _downloadService.OpenBrowserForManualDownload(_gameName);
                    
                    AddLogMessage("");
                    AddLogMessage("📋 DOWNLOAD INSTRUCTIONS:");
                    
                    // Show specific instructions for each file
                    var downloadInfo = _downloadService.GetBrowserDownloadInfo(_gameName);
                    foreach (var info in downloadInfo)
                    {
                        AddLogMessage($"• {info.ModName}:");
                        AddLogMessage($"  - {info.Instructions}");
                        AddLogMessage($"  - Save as: {info.ExpectedFileName}");
                        AddLogMessage($"  - Location: Data\\{info.OutputFolder}\\");

                        AddLogMessage("");
                    }
                    
                    AddLogMessage("💡 TIPS:");
                    AddLogMessage("• Create the Data folder structure if it doesn't exist");
                    AddLogMessage("• Use the exact filenames shown above");
                    AddLogMessage("• Download the latest/newest version of each file");
                    AddLogMessage("• After downloading all files, click 'Check Files' in the main window");
                    AddLogMessage("");
                    AddLogMessage("🔄 The browser tabs should now be open for easy downloading!");
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Error occurred - Opening browser for manual download";
                AddLogMessage($"✗ Error during download: {ex.Message}");
                AddLogMessage("");
                AddLogMessage("🌐 OPENING BROWSER TABS FOR MANUAL DOWNLOAD");
                AddLogMessage("Opening mod pages in your browser as fallback...");
                
                // Open browser tabs as fallback
                _downloadService.OpenBrowserForManualDownload(_gameName);
                
                AddLogMessage("");
                AddLogMessage("📋 Please download the files manually from the opened browser tabs.");
                AddLogMessage("After downloading, click 'Check Files' in the main window to verify.");
            }
            
            _closeButton.Enabled = true;
            
            if (startButton != null)
            {
                startButton.Text = "Download Complete";
            }
        }
        
        private void OnDownloadProgress(object? sender, DownloadProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDownloadProgress(sender, e));
                return;
            }
            
            // Update individual file progress
            _fileProgress[e.FileName] = e.ProgressPercentage;
            
            // Calculate overall progress across all files
            double overallProgress = 0;
            if (_totalFilesToDownload > 0)
            {
                // Calculate progress: completed files + partial progress of current downloads
                double completedFilesProgress = _completedDownloads * 100.0;
                double activeDownloadsProgress = _fileProgress.Values.Sum();
                
                // Total progress is completed files + current downloads
                overallProgress = (completedFilesProgress + activeDownloadsProgress) / _totalFilesToDownload;
            }
            else
            {
                // Fallback to individual file progress if total is unknown
                overallProgress = e.ProgressPercentage;
            }
            
            // Update progress bar with overall progress
            _progressBar.Value = Math.Min(100, Math.Max(0, (int)overallProgress));
            
            // Show current file being downloaded
            _statusLabel.Text = $"Downloading {e.FileName}: {e.ProgressPercentage:F1}% (Overall: {overallProgress:F1}%)";
            
            // Update log with progress for large files (reduced frequency)
            if (e.TotalBytes > 10_000_000) // Only show progress for files > 10MB
            {
                var mbDownloaded = e.BytesDownloaded / 1_048_576.0;
                var mbTotal = e.TotalBytes / 1_048_576.0;
                
                // Only log progress every 10% to avoid spam
                if (e.ProgressPercentage % 10 < 1) // Approximate 10% intervals
                {
                    AddLogMessage($"  Progress: {mbDownloaded:F1} MB / {mbTotal:F1} MB ({e.ProgressPercentage:F1}%)");
                }
            }
        }
        
        private void OnDownloadCompleted(object? sender, string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDownloadCompleted(sender, message));
                return;
            }
            
            AddLogMessage($"✓ {message}");
            
            // Check if this was an authentication completion message
            if (message.Contains("Authentication successful") || message.Contains("Loaded saved authentication"))
            {
                // Update the UI state to show download button
                UpdateAuthenticationState();
                return;
            }
            
            // Ignore file processing messages (these are just notifications, not actual downloads)
            if (message.Contains("Created normalized archive:") || 
                message.Contains("Renamed archive:"))
            {
                return; // Don't count these as completed downloads
            }
        }
        
        /// <summary>
        /// Extracts filename from download completion messages
        /// </summary>
        private string ExtractFileNameFromMessage(string message)
        {
            // Extract filename from messages like "Downloaded: filename.zip" or "Skipped (already present): filename.zip"
            if (message.Contains("Downloaded:"))
            {
                return message.Substring(message.IndexOf("Downloaded:") + "Downloaded:".Length).Trim();
            }
            else if (message.Contains("Skipped (already present):"))
            {
                return message.Substring(message.IndexOf("Skipped (already present):") + "Skipped (already present):".Length).Trim();
            }
            return string.Empty;
        }
        
        private void OnDownloadFailed(object? sender, string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnDownloadFailed(sender, message));
                return;
            }
            
            AddLogMessage($"✗ {message}");
            
            // Check if this was an authentication failure
            if (message.Contains("Authentication failed"))
            {
                // Update the UI state
                UpdateAuthenticationState();
            }
        }
        
        private void AddLogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => AddLogMessage(message));
                return;
            }
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logListBox.Items.Add($"[{timestamp}] {message}");
            _logListBox.TopIndex = _logListBox.Items.Count - 1; // Auto-scroll to bottom
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Cleanup event handlers
            _downloadService.DownloadProgress -= OnDownloadProgress;
            _downloadService.DownloadCompleted -= OnDownloadCompleted;
            _downloadService.DownloadFailed -= OnDownloadFailed;
            
            base.OnFormClosing(e);
        }
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            AddLogMessage("Download manager ready.");
            
            // Force immediate UI update using a timer to ensure it happens after form is fully loaded
            var immediateUpdateTimer = new System.Windows.Forms.Timer();
            immediateUpdateTimer.Interval = 100; // Very short delay
            immediateUpdateTimer.Tick += (s, args) =>
            {
                immediateUpdateTimer.Stop();
                immediateUpdateTimer.Dispose();
                
                // Check authentication status and update UI accordingly
                if (_downloadService.IsAuthenticated)
                {
                    AddLogMessage("Already authenticated with Nexus Mods.");
                    AddLogMessage($"Authentication status: {_downloadService.IsAuthenticated}");
                    
                    // Force update authentication state
                    UpdateAuthenticationState();
                    
                    // Debug: Check if button was added
                    if (Controls.ContainsKey("startDownloadButton"))
                    {
                        AddLogMessage("✓ Start Download button is now visible.");
                    }
                    else
                    {
                        AddLogMessage("⚠ Start Download button not found - forcing manual addition...");
                        ForceAddDownloadButton();
                    }
                }
                else
                {
                    AddLogMessage("Click 'Authenticate with Nexus Mods' to begin downloading.");
                    AddLogMessage("This will use SSO authentication through your browser.");
                }
            };
            immediateUpdateTimer.Start();
            
            // Set up a backup timer to periodically check authentication status
            // This handles the case where authentication completes asynchronously
            var authCheckTimer = new System.Windows.Forms.Timer();
            authCheckTimer.Interval = 2000; // Check every 2 seconds
            authCheckTimer.Tick += (s, args) =>
            {
                if (_downloadService.IsAuthenticated && !Controls.ContainsKey("startDownloadButton"))
                {
                    AddLogMessage("⚠ Detected authentication but no download button - fixing...");
                    UpdateAuthenticationState();
                    
                    if (!Controls.ContainsKey("startDownloadButton"))
                    {
                        ForceAddDownloadButton();
                    }
                }
                
                // Stop after finding the button or 30 seconds
                if (Controls.ContainsKey("startDownloadButton"))
                {
                    authCheckTimer.Stop();
                    authCheckTimer.Dispose();
                }
            };
            authCheckTimer.Start();
            
            // Stop the timer after 30 seconds to avoid it running forever
            var stopTimer = new System.Windows.Forms.Timer();
            stopTimer.Interval = 30000; // 30 seconds
            stopTimer.Tick += (s, args) =>
            {
                authCheckTimer.Stop();
                authCheckTimer.Dispose();
                stopTimer.Stop();
                stopTimer.Dispose();
            };
            stopTimer.Start();
        }
        
        /// <summary>
        /// Force add the download button if it's missing despite being authenticated
        /// </summary>
        private void ForceAddDownloadButton()
        {
            try
            {
                if (!_downloadService.IsAuthenticated)
                    return;
                    
                // Remove any existing button first
                var existingButton = Controls["startDownloadButton"];
                if (existingButton != null)
                {
                    Controls.Remove(existingButton);
                    existingButton.Dispose();
                }
                
                // Create and add the start download button
                var startButton = new Button
                {
                    Name = "startDownloadButton",
                    Text = "Start Download",
                    Location = new Point(220, 40), // Position to the right of authenticate button
                    Size = new Size(120, 30),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    UseVisualStyleBackColor = true
                };
                startButton.Click += StartDownloadButton_Click;
                Controls.Add(startButton);
                
                // Add logout button as well
                var existingLogoutButton = Controls["logoutButton"];
                if (existingLogoutButton == null)
                {
                    var logoutButton = new Button
                    {
                        Name = "logoutButton",
                        Text = "Logout",
                        Location = new Point(350, 40), // Position to the right of start download button
                        Size = new Size(75, 30),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left,
                        UseVisualStyleBackColor = true
                    };
                    logoutButton.Click += LogoutButton_Click;
                    Controls.Add(logoutButton);
                }
                
                // Add manual download button
                var existingManualButton = Controls["manualDownloadButton"];
                if (existingManualButton == null)
                {
                    var manualButton = new Button
                    {
                        Name = "manualDownloadButton",
                        Text = "Manual Download",
                        Location = new Point(435, 40), // Position to the right of logout button
                        Size = new Size(120, 30),
                        Anchor = AnchorStyles.Top | AnchorStyles.Left,
                        UseVisualStyleBackColor = true,
                        BackColor = Color.LightBlue
                    };
                    manualButton.Click += ManualDownloadButton_Click;
                    Controls.Add(manualButton);
                }
                
                // Update other UI elements
                _authenticateButton.Text = "✓ Authenticated";
                _authenticateButton.Enabled = false;
                _statusLabel.Text = "Ready to download. Click Start Download or Manual Download.";
                
                // Force refresh the form
                Refresh();
                
                AddLogMessage("✓ Download buttons manually added and visible.");
            }
            catch (Exception ex)
            {
                AddLogMessage($"✗ Error adding download buttons: {ex.Message}");
            }
        }
        
        private void LogoutButton_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout? This will clear your saved authentication and you'll need to re-authenticate next time.",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                // Call logout on the download service (which should clear the API key)
                _downloadService.Logout();
                UpdateAuthenticationState();
                AddLogMessage("✓ Logged out successfully. You'll need to re-authenticate for future downloads.");
            }
        }
        
        private void ManualDownloadButton_Click(object? sender, EventArgs e)
        {
            AddLogMessage("🌐 Opening browser tabs for manual download...");
            _statusLabel.Text = "Opening browser for manual download";
            
            try
            {
                // Open browser tabs for each required mod
                _downloadService.OpenBrowserForManualDownload(_gameName);
                
                AddLogMessage("");
                AddLogMessage("📋 MANUAL DOWNLOAD INSTRUCTIONS:");
                
                // Show specific instructions for each file
                var downloadInfo = _downloadService.GetBrowserDownloadInfo(_gameName);
                foreach (var info in downloadInfo)
                {
                    AddLogMessage($"• {info.ModName}:");
                    AddLogMessage($"  - {info.Instructions}");
                    AddLogMessage($"  - Save as: {info.ExpectedFileName}");
                    AddLogMessage($"  - Location: Data\\{info.OutputFolder}\\");

                    AddLogMessage("");
                }
                
                AddLogMessage("💡 HELPFUL TIPS:");
                AddLogMessage("• Create the Data folder structure if it doesn't exist");
                AddLogMessage("• You can use any filename - the app will detect matching files");
                AddLogMessage("• Download the latest/newest version of each file");
                AddLogMessage("• After downloading all files, click 'Check Files' in the main window");
                AddLogMessage("");
                AddLogMessage("🔄 AUTOMATIC FILENAME PROCESSING:");
                AddLogMessage("The app will automatically rename downloaded files to expected names.");
                AddLogMessage("");
                AddLogMessage("✅ Browser tabs are now open for easy downloading!");
                
                _statusLabel.Text = "Browser opened - Download files manually";
            }
            catch (Exception ex)
            {
                AddLogMessage($"✗ Error opening browser: {ex.Message}");
                _statusLabel.Text = "Error opening browser";
            }
        }
    }
}
