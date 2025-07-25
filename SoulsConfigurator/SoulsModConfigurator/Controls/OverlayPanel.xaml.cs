using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
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

        public OverlayPanel()
        {
            InitializeComponent();
            this.Visibility = Visibility.Hidden;
        }

        #region Loading/Processing Methods

        /// <summary>
        /// Shows the overlay panel with the specified status message
        /// </summary>
        /// <param name="statusMessage">The status message to display</param>
        public void ShowOverlay(string statusMessage = "Processing mod installation...")
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowOverlay(statusMessage));
                return;
            }

            UpdateStatus(statusMessage);
            ShowLoadingPanel();
        }

        /// <summary>
        /// Hides the overlay panel
        /// </summary>
        public void HideOverlay()
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(HideOverlay);
                return;
            }

            HideWithAnimation();
        }

        /// <summary>
        /// Updates the status message displayed in the overlay
        /// </summary>
        /// <param name="statusMessage">The new status message</param>
        public void UpdateStatus(string statusMessage)
        {
            // Ensure we're on the UI thread
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

        /// <summary>
        /// Shows the overlay with a generic message for UI-based executables
        /// </summary>
        /// <param name="modName">The name of the mod being processed</param>
        public void ShowForUIExecutable(string modName)
        {
            ShowOverlay($"Configuring {modName}...\nThis may take a moment while the tool opens and processes your settings.");
        }

        /// <summary>
        /// Shows the overlay for command line executables
        /// </summary>
        /// <param name="modName">The name of the mod being processed</param>
        public void ShowForCommandLineExecutable(string modName)
        {
            ShowOverlay($"Installing {modName}...\nProcessing mod installation...");
        }

        #endregion

        #region Notification Methods

        /// <summary>
        /// Shows a notification message
        /// </summary>
        /// <param name="notification">The notification to display</param>
        public void ShowNotification(NotificationMessage notification)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowNotification(notification));
                return;
            }

            _notificationQueue.Enqueue(notification);
            ProcessNotificationQueue();
        }

        /// <summary>
        /// Processes the notification queue, showing notifications one by one
        /// </summary>
        private async void ProcessNotificationQueue()
        {
            if (_isShowingNotification || _notificationQueue.Count == 0)
                return;

            _isShowingNotification = true;
            var notification = _notificationQueue.Dequeue();
            _currentNotification = notification;

            await ShowNotificationInternal(notification);
        }

        /// <summary>
        /// Internal method to show a single notification
        /// </summary>
        private async Task ShowNotificationInternal(NotificationMessage notification)
        {
            // Configure notification content
            txtNotificationTitle.Text = notification.Title;
            txtNotificationContent.Text = notification.Content;

            // Configure close button visibility
            btnCloseNotification.Visibility = notification.IsClosable ? Visibility.Visible : Visibility.Collapsed;

            // Setup links
            SetupNotificationLinks(notification.Links);

            // Show notification panel and hide loading panel
            ShowNotificationPanel();

            // Handle auto-close
            if (notification.AutoCloseAfter.HasValue)
            {
                await Task.Delay(notification.AutoCloseAfter.Value);
                if (_currentNotification?.Id == notification.Id) // Check if still current
                {
                    CloseCurrentNotification();
                }
            }
        }

        /// <summary>
        /// Sets up clickable links in the notification
        /// </summary>
        private void SetupNotificationLinks(Dictionary<string, string> links)
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

        /// <summary>
        /// Creates a style for link buttons
        /// </summary>
        private Style CreateLinkButtonStyle()
        {
            var style = new Style(typeof(Button));
            
            style.Setters.Add(new Setter(BackgroundProperty, System.Windows.Media.Brushes.Transparent));
            style.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
            style.Setters.Add(new Setter(ForegroundProperty, System.Windows.Media.Brushes.LightBlue));
            style.Setters.Add(new Setter(FontSizeProperty, 14.0));
            style.Setters.Add(new Setter(CursorProperty, System.Windows.Input.Cursors.Hand));
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            style.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));

            // Hover effect
            var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(ForegroundProperty, System.Windows.Media.Brushes.White));
            style.Triggers.Add(trigger);

            return style;
        }

        /// <summary>
        /// Closes the current notification and shows the next one if available
        /// </summary>
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

        /// <summary>
        /// Shows the loading panel
        /// </summary>
        private void ShowLoadingPanel()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            NotificationPanel.Visibility = Visibility.Collapsed;
            ShowWithAnimation();
        }

        /// <summary>
        /// Shows the notification panel
        /// </summary>
        private void ShowNotificationPanel()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            NotificationPanel.Visibility = Visibility.Visible;
            ShowWithAnimation();
        }

        /// <summary>
        /// Shows the overlay with fade-in animation
        /// </summary>
        private void ShowWithAnimation()
        {
            this.Visibility = Visibility.Visible;
            var storyboard = (Storyboard)FindResource("FadeInStoryboard");
            storyboard.Begin();
        }

        /// <summary>
        /// Hides the overlay with fade-out animation
        /// </summary>
        private void HideWithAnimation()
        {
            var storyboard = (Storyboard)FindResource("FadeOutStoryboard");
            storyboard.Completed += (s, e) => this.Visibility = Visibility.Hidden;
            storyboard.Begin();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the close button click
        /// </summary>
        private void BtnCloseNotification_Click(object sender, RoutedEventArgs e)
        {
            CloseCurrentNotification();
        }

        #endregion
    }
}
