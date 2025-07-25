using System;
using System.Windows;
using System.Windows.Controls;

namespace SoulsModConfigurator.Controls
{
    /// <summary>
    /// Interaction logic for OverlayPanel.xaml
    /// </summary>
    public partial class OverlayPanel : UserControl
    {
        public OverlayPanel()
        {
            InitializeComponent();
            this.Visibility = Visibility.Hidden;
        }

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
            this.Visibility = Visibility.Visible;
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

            this.Visibility = Visibility.Hidden;
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
    }
}
