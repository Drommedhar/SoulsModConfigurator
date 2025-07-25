using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SoulsConfigurator.Services
{
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }

    public class NotificationMessage
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public NotificationType Type { get; set; } = NotificationType.Info;
        public Dictionary<string, string> Links { get; set; } = new(); // Text -> URL mapping
        public bool IsClosable { get; set; } = true;
        public TimeSpan? AutoCloseAfter { get; set; } = null;
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    public class NotificationService
    {
        public static NotificationService Instance { get; } = new NotificationService();
        
        // Event fired when a notification should be shown
        public event EventHandler<NotificationMessage>? NotificationRequested;
        
        private NotificationService() { }

        /// <summary>
        /// Shows a notification with the specified message
        /// </summary>
        public void ShowNotification(NotificationMessage notification)
        {
            NotificationRequested?.Invoke(this, notification);
        }

        /// <summary>
        /// Shows a simple text notification
        /// </summary>
        public void ShowNotification(string title, string content, NotificationType type = NotificationType.Info)
        {
            ShowNotification(new NotificationMessage
            {
                Title = title,
                Content = content,
                Type = type
            });
        }

        /// <summary>
        /// Shows a notification with clickable links
        /// </summary>
        public void ShowNotificationWithLinks(string title, string content, Dictionary<string, string> links, NotificationType type = NotificationType.Info)
        {
            ShowNotification(new NotificationMessage
            {
                Title = title,
                Content = content,
                Type = type,
                Links = links
            });
        }

        /// <summary>
        /// Shows a notification for outdated presets
        /// </summary>
        public void ShowOutdatedPresetsNotification(List<string> outdatedPresets)
        {
            var presetList = string.Join("\n• ", outdatedPresets);
            var content = $"The following presets were created with an older version and may not work correctly with recent changes:\n\n• {presetList}\n\n" +
                         "Please recreate these presets to ensure they work properly with the latest features.";

            ShowNotification(new NotificationMessage
            {
                Title = "Outdated Presets Detected",
                Content = content,
                Type = NotificationType.Warning,
                Links = new Dictionary<string, string>
                {
                    
                },
                IsClosable = true
            });
        }

        /// <summary>
        /// Shows a notification for application updates
        /// </summary>
        public void ShowUpdateNotification(string currentVersion, string newVersion, string downloadUrl)
        {
            var content = $"A new version of Souls Mod Configurator is available!\n\n" +
                         $"Current version: {currentVersion}\n" +
                         $"New version: {newVersion}\n\n" +
                         $"Click the link below to download the latest version.";

            ShowNotification(new NotificationMessage
            {
                Title = "Update Available",
                Content = content,
                Type = NotificationType.Info,
                Links = new Dictionary<string, string>
                {
                    { "Download Update", downloadUrl },
                    { "View Release Notes", $"{downloadUrl}/releases" }
                },
                IsClosable = true
            });
        }

        /// <summary>
        /// Opens a URL in the default browser
        /// </summary>
        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // Ignore errors when opening links
            }
        }
    }
}
