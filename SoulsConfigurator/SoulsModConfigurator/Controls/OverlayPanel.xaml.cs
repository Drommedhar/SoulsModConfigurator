using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SoulsConfigurator.Services;

namespace SoulsModConfigurator.Controls
{
    /// <summary>
    /// Interaction logic for OverlayPanel.xaml
    /// </summary>
    public partial class OverlayPanel : UserControl
    {
        private readonly Queue<NotificationMessage> _notificationQueue = new();
        private bool _isShowingNotification = false;
        private NotificationMessage? _currentNotification = null;
        
        // Download progress tracking
        private DispatcherTimer? _speedTimer;
        private long _lastBytesDownloaded = 0;
        private DateTime _lastSpeedUpdate = DateTime.Now;
        private int _currentFileIndex = 0;
        private int _totalFiles = 0;

        public OverlayPanel()
        {
            InitializeComponent();
            this.Visibility = Visibility.Hidden;
            InitializeSpeedTimer();
        }

        #region Speed Calculation

        private void InitializeSpeedTimer()
        {
            _speedTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _speedTimer.Tick += UpdateDownloadSpeed;
        }

        private void UpdateDownloadSpeed(object? sender, EventArgs e)
        {
            if (DownloadPanel?.Visibility != Visibility.Visible)
                return;

            var now = DateTime.Now;
            var timeDiff = (now - _lastSpeedUpdate).TotalSeconds;
            
            if (timeDiff > 0 && _lastBytesDownloaded > 0)
            {
                var speedBps = _lastBytesDownloaded / timeDiff;
                var speedText = FormatSpeed(speedBps);
                
                if (txtDownloadSpeed != null)
                    txtDownloadSpeed.Text = speedText;
            }
            
            _lastSpeedUpdate = now;
        }

        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1024)
                return $"{bytesPerSecond:F0} B/s";
            else if (bytesPerSecond < 1024 * 1024)
                return $"{bytesPerSecond / 1024:F1} KB/s";
            else if (bytesPerSecond < 1024 * 1024 * 1024)
                return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
            else
                return $"{bytesPerSecond / (1024 * 1024 * 1024):F1} GB/s";
        }

        #endregion

        #region Download Progress Methods

        /// <summary>
        /// Shows the download progress overlay with file information
        /// </summary>
        public void ShowDownloadProgress(int currentFile, int totalFiles)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowDownloadProgress(currentFile, totalFiles));
                return;
            }

            if (currentFile < 1) currentFile = 1;
            if (totalFiles < 1) totalFiles = 1;
            if (currentFile > totalFiles) currentFile = totalFiles;

            _currentFileIndex = currentFile;
            _totalFiles = totalFiles;
            _lastBytesDownloaded = 0;
            _lastSpeedUpdate = DateTime.Now;

            if (txtDownloadTitle != null)
                txtDownloadTitle.Text = $"Downloading file {currentFile} of {totalFiles}";
            
            if (txtDownloadFileName != null)
                txtDownloadFileName.Text = "Preparing...";
            
            if (txtDownloadPercentage != null)
                txtDownloadPercentage.Text = "0.0%";
            
            if (txtDownloadSpeed != null)
                txtDownloadSpeed.Text = "0 B/s";

            if (FindName("txtDownloadSize") is TextBlock txtDownloadSize)
                txtDownloadSize.Text = "";

            if (FindName("downloadProgressBar") is ProgressBar progressBar)
            {
                progressBar.Value = 0;
                progressBar.IsIndeterminate = false;
            }

            ShowDownloadPanel();
            _speedTimer?.Start();
        }

        /// <summary>
        /// Updates the download progress with detailed information
        /// </summary>
        public void UpdateDownloadProgress(string fileName, double progressPercentage, long bytesDownloaded, long totalBytes)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => UpdateDownloadProgress(fileName, progressPercentage, bytesDownloaded, totalBytes), DispatcherPriority.Background);
                return;
            }

            try
            {
                if (txtDownloadFileName != null && !string.IsNullOrEmpty(fileName))
                    txtDownloadFileName.Text = fileName;

                if (FindName("txtDownloadSize") is TextBlock txtDownloadSize)
                {
                    if (totalBytes > 0)
                    {
                        var downloadedMB = bytesDownloaded / (1024.0 * 1024.0);
                        var totalMB = totalBytes / (1024.0 * 1024.0);
                        txtDownloadSize.Text = $"{downloadedMB:F1} MB / {totalMB:F1} MB";
                    }
                    else if (bytesDownloaded > 0)
                    {
                        var downloadedMB = bytesDownloaded / (1024.0 * 1024.0);
                        txtDownloadSize.Text = $"{downloadedMB:F1} MB downloaded";
                    }
                    else
                    {
                        txtDownloadSize.Text = "";
                    }
                }

                if (FindName("downloadProgressBar") is ProgressBar progressBar)
                {
                    if (progressPercentage < 0)
                    {
                        progressBar.IsIndeterminate = true;
                        if (txtDownloadPercentage != null)
                            txtDownloadPercentage.Text = "Downloading...";
                    }
                    else
                    {
                        progressBar.IsIndeterminate = false;
                        progressPercentage = Math.Max(0, Math.Min(100, progressPercentage));
                        progressBar.Value = progressPercentage;
                        
                        if (txtDownloadPercentage != null)
                            txtDownloadPercentage.Text = $"{progressPercentage:F1}%";
                    }
                }

                var now = DateTime.Now;
                var timeDiff = (now - _lastSpeedUpdate).TotalSeconds;
                if (timeDiff > 0 && bytesDownloaded >= 0)
                {
                    var bytesDelta = bytesDownloaded - _lastBytesDownloaded;
                    if (bytesDelta > 0)
                    {
                        var speedBps = bytesDelta / timeDiff;
                        var speedText = FormatSpeed(speedBps);
                        if (txtDownloadSpeed != null)
                            txtDownloadSpeed.Text = speedText;
                    }
                }
                
                _lastBytesDownloaded = Math.Max(0, bytesDownloaded);
                _lastSpeedUpdate = now;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateDownloadProgress: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the download progress and stops speed calculation
        /// </summary>
        public void HideDownloadProgress()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(HideDownloadProgress);
                return;
            }

            _speedTimer?.Stop();
            _lastBytesDownloaded = 0;
            _currentFileIndex = 0;
            _totalFiles = 0;
            
            HideWithAnimation();
        }

        #endregion

        #region Loading/Processing Methods

        public void ShowOverlay(string statusMessage = "Processing mod installation...")
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowOverlay(statusMessage));
                return;
            }

            UpdateStatus(statusMessage);
            ShowLoadingPanel();
        }

        public void HideOverlay()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(HideOverlay);
                return;
            }

            _speedTimer?.Stop();
            HideWithAnimation();
        }

        public void UpdateStatus(string statusMessage)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateStatus(statusMessage));
                return;
            }

            if (lblStatus != null)
            {
                lblStatus.Content = statusMessage;
            }
        }

        public void ShowForUIExecutable(string modName)
        {
            ShowOverlay($"Configuring {modName}...\nThis may take a moment while the tool opens and processes your settings.");
        }

        public void ShowForCommandLineExecutable(string modName)
        {
            ShowOverlay($"Installing {modName}...\nProcessing mod installation...");
        }

        #endregion

        #region Notification Methods

        public void ShowNotification(NotificationMessage notification)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowNotification(notification));
                return;
            }

            _notificationQueue.Enqueue(notification);
            ProcessNotificationQueue();
        }

        private async void ProcessNotificationQueue()
        {
            if (_isShowingNotification || _notificationQueue.Count == 0)
                return;

            _isShowingNotification = true;
            var notification = _notificationQueue.Dequeue();
            _currentNotification = notification;

            await ShowNotificationInternal(notification);
        }

        private async Task ShowNotificationInternal(NotificationMessage notification)
        {
            txtNotificationTitle.Text = notification.Title;
            txtNotificationContent.Text = notification.Content;
            btnCloseNotification.Visibility = notification.IsClosable ? Visibility.Visible : Visibility.Collapsed;

            SetupNotificationLinks(notification.Links);
            ShowNotificationPanel();

            if (notification.AutoCloseAfter.HasValue)
            {
                await Task.Delay(notification.AutoCloseAfter.Value);
                if (_currentNotification?.Id == notification.Id)
                {
                    CloseCurrentNotification();
                }
            }
        }

        private void SetupNotificationLinks(Dictionary<string, string>? links)
        {
            pnlNotificationLinks.Children.Clear();

            if (links?.Any() == true)
            {
                pnlNotificationLinks.Visibility = Visibility.Visible;

                foreach (var link in links)
                {
                    var linkButton = new Button
                    {
                        Content = link.Key,
                        Tag = link.Value,
                        Style = CreateLinkButtonStyle(),
                        Margin = new Thickness(0, 5, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    linkButton.Click += (s, e) =>
                    {
                        if (s is Button btn && btn.Tag is string url)
                        {
                            NotificationService.OpenUrl(url);
                        }
                    };

                    pnlNotificationLinks.Children.Add(linkButton);
                }
            }
            else
            {
                pnlNotificationLinks.Visibility = Visibility.Collapsed;
            }
        }

        private Style CreateLinkButtonStyle()
        {
            var style = new Style(typeof(Button));
            
            style.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
            style.Setters.Add(new Setter(ForegroundProperty, Brushes.LightBlue));
            style.Setters.Add(new Setter(FontSizeProperty, 14.0));
            style.Setters.Add(new Setter(CursorProperty, System.Windows.Input.Cursors.Hand));
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            style.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));

            var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
            style.Triggers.Add(trigger);

            return style;
        }

        private void CloseCurrentNotification()
        {
            _currentNotification = null;
            _isShowingNotification = false;

            if (_notificationQueue.Count > 0)
            {
                ProcessNotificationQueue();
            }
            else
            {
                HideWithAnimation();
            }
        }

        #endregion

        #region UI Display Methods

        private void ShowLoadingPanel()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            DownloadPanel.Visibility = Visibility.Collapsed;
            NotificationPanel.Visibility = Visibility.Collapsed;
            ShowWithAnimation();
        }

        private void ShowDownloadPanel()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            DownloadPanel.Visibility = Visibility.Visible;
            NotificationPanel.Visibility = Visibility.Collapsed;
            ShowWithAnimation();
        }

        private void ShowNotificationPanel()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            DownloadPanel.Visibility = Visibility.Collapsed;
            NotificationPanel.Visibility = Visibility.Visible;
            ShowWithAnimation();
        }

        private void ShowWithAnimation()
        {
            this.Visibility = Visibility.Visible;
            var storyboard = (Storyboard)FindResource("FadeInStoryboard");
            storyboard.Begin();
        }

        private void HideWithAnimation()
        {
            var storyboard = (Storyboard)FindResource("FadeOutStoryboard");
            storyboard.Completed += (s, e) => this.Visibility = Visibility.Hidden;
            storyboard.Begin();
        }

        #endregion

        #region Event Handlers

        private void BtnCloseNotification_Click(object sender, RoutedEventArgs e)
        {
            CloseCurrentNotification();
        }

        #endregion

        #region Additional Methods for Download Support

        public void ShowLoading(string message)
        {
            ShowOverlay(message);
        }

        public void Hide()
        {
            HideOverlay();
        }

        public void ShowNotification(string title, string content, string gifUrl)
        {
            var notification = new NotificationMessage
            {
                Title = title,
                Content = content,
                IsClosable = true
            };
            ShowNotification(notification);
        }

        #endregion
    }
}