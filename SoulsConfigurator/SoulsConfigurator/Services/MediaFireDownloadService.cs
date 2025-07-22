using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace SoulsConfigurator.Services
{
    /// <summary>
    /// Service for downloading files from MediaFire using automated WebView2 browser control
    /// </summary>
    public class MediaFireDownloadService : IDisposable
    {
        private WebView2? _webView;
        private Form? _hiddenForm;
        private TaskCompletionSource<bool>? _downloadCompletion;
        private string? _expectedFileName;
        private string? _outputPath;
        private bool _disposed = false;
        
        public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
        public event EventHandler<string>? DownloadCompleted;
        public event EventHandler<string>? DownloadFailed;
        
        /// <summary>
        /// Downloads a file from MediaFire using browser automation
        /// </summary>
        /// <param name="mediaFireUrl">MediaFire URL (e.g., https://www.mediafire.com/file/xxxxx/filename.exe/file)</param>
        /// <param name="fileName">Expected filename for the downloaded file</param>
        /// <param name="outputPath">Directory where the file should be saved</param>
        /// <returns>True if download was successful, false otherwise</returns>
        public async Task<bool> DownloadFileAsync(string mediaFireUrl, string fileName, string outputPath)
        {
            if (_disposed)
                return false;
                
            try
            {
                _expectedFileName = fileName;
                _outputPath = outputPath;
                _downloadCompletion = new TaskCompletionSource<bool>();
                
                // Ensure output directory exists
                Directory.CreateDirectory(outputPath);
                
                // Create hidden form with WebView2
                await InitializeWebView();
                
                if (_webView == null)
                {
                    DownloadFailed?.Invoke(this, "Failed to initialize WebView2 browser");
                    return false;
                }
                
                // Navigate to MediaFire URL
                _webView.Source = new Uri(mediaFireUrl);
                
                // Wait for download completion or timeout (extended for MediaFire)
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10)); // 10 minute timeout for MediaFire
                var completedTask = await Task.WhenAny(_downloadCompletion.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    // Before failing due to timeout, check if file was downloaded anyway
                    string expectedFilePath = Path.Combine(outputPath, fileName);
                    if (File.Exists(expectedFilePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"? File found despite timeout: {expectedFilePath}");
                        DownloadCompleted?.Invoke(this, $"Downloaded: {fileName}");
                        return true;
                    }
                    
                    DownloadFailed?.Invoke(this, "MediaFire download timed out after 10 minutes");
                    return false;
                }
                
                return await _downloadCompletion.Task;
            }
            catch (Exception ex)
            {
                DownloadFailed?.Invoke(this, $"Error downloading from MediaFire: {ex.Message}");
                return false;
            }
            finally
            {
                CleanupWebView();
            }
        }
        
        private async Task InitializeWebView()
        {
            try
            {
                // Create hidden form
                _hiddenForm = new Form
                {
                    WindowState = FormWindowState.Minimized,
                    ShowInTaskbar = false,
                    Visible = false,
                    Size = new System.Drawing.Size(1024, 768)
                };
                
                // Create WebView2 control
                _webView = new WebView2
                {
                    Dock = DockStyle.Fill
                };
                
                _hiddenForm.Controls.Add(_webView);
                
                // Initialize WebView2 environment
                await _webView.EnsureCoreWebView2Async();
                
                // Set up download handling
                _webView.CoreWebView2.DownloadStarting += OnDownloadStarting;
                _webView.CoreWebView2.DOMContentLoaded += OnDOMContentLoaded;
                _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                _webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
                _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
                
                // Configure browser settings
                var settings = _webView.CoreWebView2.Settings;
                settings.IsGeneralAutofillEnabled = false;
                settings.IsPasswordAutosaveEnabled = false;
                settings.AreDevToolsEnabled = false;
                settings.AreDefaultContextMenusEnabled = false;
                
                // Set user agent to appear as a regular browser
                settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                
                System.Diagnostics.Debug.WriteLine("? WebView2 initialized for MediaFire download");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error initializing WebView2: {ex.Message}");
                throw;
            }
        }
        
        private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled && !e.IsSuccess)
            {
                //DownloadFailed?.Invoke(this, "Failed to navigate to MediaFire page");
                //_downloadCompletion?.SetResult(false);
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"? Navigated to MediaFire page: {_webView?.Source}");
        }

        private bool isDownloading = false;
        private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"?? Navigation starting to: {e.Uri}");
            
            // Check if this is a direct download URL (MediaFire download servers)
            if (e.Uri.Contains("download") && e.Uri.Contains("mediafire.com") && 
                (e.Uri.Contains(_expectedFileName ?? "") || e.Uri.EndsWith(".exe") || e.Uri.EndsWith(".zip")))
            {
                if(isDownloading)
                {
                    return;
                }

                isDownloading = true;
                System.Diagnostics.Debug.WriteLine($"?? Detected direct download URL: {e.Uri}");
                
                // Capture the URL on the UI thread before canceling navigation
                string downloadUrl = e.Uri;
                string fileName = _expectedFileName!;
                string outputPath = _outputPath!;
                
                // Cancel the navigation and handle download manually
                e.Cancel = true;
                
                // Start manual download from the direct URL using captured values
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await DownloadFileFromUrl(downloadUrl, fileName, outputPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"? Manual download failed: {ex.Message}");
                        DownloadFailed?.Invoke(this, $"Download failed: {ex.Message}");
                        _downloadCompletion?.SetResult(false);
                    }
                });
            }
        }
        
        private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Prevent pop-ups but allow the navigation in the same window
            e.Handled = true;
            if (_webView != null)
            {
                _webView.Source = new Uri(e.Uri);
                System.Diagnostics.Debug.WriteLine($"?? Redirected pop-up to main window: {e.Uri}");
            }
        }
        
        private async void OnDOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            try
            {
                if (_webView == null) return;
                
                // Wait a moment for page to fully load
                await Task.Delay(2000);
                
                // Check if this is a redirect page or the actual download page
                string currentUrl = _webView.Source?.ToString() ?? "";
                System.Diagnostics.Debug.WriteLine($"?? DOM loaded for URL: {currentUrl}");
                
                // Try to find and click the download button
                await ClickDownloadButton();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error in DOM content loaded: {ex.Message}");
                DownloadFailed?.Invoke(this, $"Error processing MediaFire page: {ex.Message}");
                _downloadCompletion?.SetResult(false);
            }
        }
        
        private async Task ClickDownloadButton()
        {
            if (_webView?.CoreWebView2 == null) return;
            
            try
            {
                // First, try to detect if we're on a waiting/countdown page
                string checkWaitingScript = @"
                    (function() {
                        // Check for countdown elements
                        const countdown = document.querySelector('.countdown, #countdown, [id*=""countdown""], [class*=""countdown""]');
                        const waiting = document.querySelector('.waiting, #waiting, [id*=""waiting""], [class*=""waiting""]');
                        const timer = document.querySelector('.timer, #timer, [id*=""timer""], [class*=""timer""]');
                        
                        if (countdown || waiting || timer) {
                            return 'WAITING_PAGE';
                        }
                        return 'DOWNLOAD_PAGE';
                    })();";
                
                var pageTypeResult = await _webView.CoreWebView2.ExecuteScriptAsync(checkWaitingScript);
                System.Diagnostics.Debug.WriteLine($"?? Page type detected: {pageTypeResult}");
                
                if (pageTypeResult.Contains("WAITING_PAGE"))
                {
                    System.Diagnostics.Debug.WriteLine("? Detected waiting/countdown page, will wait for redirect...");
                    await Task.Delay(10000); // Wait 10 seconds for countdown
                    
                    // Check if we've been redirected to the actual download page
                    var newPageTypeResult = await _webView.CoreWebView2.ExecuteScriptAsync(checkWaitingScript);
                    if (newPageTypeResult.Contains("WAITING_PAGE"))
                    {
                        System.Diagnostics.Debug.WriteLine("? Still on waiting page after 10 seconds");
                        // Continue anyway and try to find download button
                    }
                }
                
                // Try multiple selectors for MediaFire download button
                string[] downloadButtonSelectors = {
                    "#downloadButton",
                    "a.input[aria-label='Download file']",
                    "a.input[href*='download']",
                    ".download_link a.input",
                    "a.input[download]",
                    "a[class*='download']",
                    ".popsok",
                    "a.retry",
                    "#download_link",
                    ".download-button",
                    "[data-testid='download-button']"
                };
                
                foreach (string selector in downloadButtonSelectors)
                {
                    try
                    {
                        // Check if button exists and click it
                        string script = $@"
                            (function() {{
                                const button = document.querySelector('{selector}');
                                if (button && button.offsetParent !== null) {{
                                    // Check if button is visible
                                    const rect = button.getBoundingClientRect();
                                    if (rect.width > 0 && rect.height > 0) {{
                                        button.scrollIntoView();
                                        button.click();
                                        return 'CLICKED';
                                    }}
                                }}
                                return 'NOT_FOUND';
                            }})();";
                        
                        var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                        
                        if (result.Contains("CLICKED"))
                        {
                            System.Diagnostics.Debug.WriteLine($"? Clicked download button using selector: {selector}");
                            
                            // Wait a moment to see if download starts
                            await Task.Delay(3000);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"? Selector '{selector}' failed: {ex.Message}");
                    }
                }
                
                // If no button found, wait a bit and try a more aggressive approach
                await Task.Delay(3000);
                System.Diagnostics.Debug.WriteLine("?? Trying fallback download detection...");
                
                // Final attempt with a more aggressive approach
                string fallbackScript = @"
                    (function() {
                        // Look for any link that might be a download
                        const links = document.querySelectorAll('a, button, input[type=""button""], input[type=""submit""]');
                        for (let element of links) {
                            const text = element.textContent?.toLowerCase() || '';
                            const href = element.href?.toLowerCase() || '';
                            const className = element.className?.toLowerCase() || '';
                            const id = element.id?.toLowerCase() || '';
                            
                            if (text.includes('download') || href.includes('download') || 
                                className.includes('download') || id.includes('download') ||
                                element.hasAttribute('download')) {
                                element.scrollIntoView();
                                element.click();
                                return 'CLICKED: ' + (text || href || className || id);
                            }
                        }
                        return 'NOT_FOUND';
                    })();";
                
                var fallbackResult = await _webView.CoreWebView2.ExecuteScriptAsync(fallbackScript);
                
                if (fallbackResult.Contains("CLICKED"))
                {
                    System.Diagnostics.Debug.WriteLine($"? Clicked download using fallback method: {fallbackResult}");
                    await Task.Delay(3000);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? Could not find any download button on MediaFire page");
                    
                    // Log page content for debugging
                    string debugScript = @"
                        (function() {
                            return {
                                title: document.title,
                                url: window.location.href,
                                hasDownloadLinks: document.querySelectorAll('a[href*=""download""]').length,
                                hasDownloadButtons: document.querySelectorAll('button, input[type=""button""]').length,
                                visibleText: document.body.textContent.substring(0, 200)
                            };
                        })();";
                    
                    var debugInfo = await _webView.CoreWebView2.ExecuteScriptAsync(debugScript);
                    System.Diagnostics.Debug.WriteLine($"?? Page debug info: {debugInfo}");
                    
                    DownloadFailed?.Invoke(this, "Could not find download button on MediaFire page");
                    _downloadCompletion?.SetResult(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error clicking download button: {ex.Message}");
                DownloadFailed?.Invoke(this, $"Error clicking download button: {ex.Message}");
                _downloadCompletion?.SetResult(false);
            }
        }
        
        private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            try
            {
                if (_outputPath == null || _expectedFileName == null)
                {
                    e.Cancel = true;
                    DownloadFailed?.Invoke(this, "Download configuration error");
                    _downloadCompletion?.SetResult(false);
                    return;
                }
                
                // Set the download path
                string downloadPath = Path.Combine(_outputPath, _expectedFileName);
                e.ResultFilePath = downloadPath;
                
                System.Diagnostics.Debug.WriteLine($"? MediaFire download started: {e.ResultFilePath}");
                System.Diagnostics.Debug.WriteLine($"?? Download URI: {e.DownloadOperation.Uri}");
                
                // Try to get suggested filename from the download operation
                var suggestedName = e.DownloadOperation.ResultFilePath;
                if (!string.IsNullOrEmpty(suggestedName))
                {
                    System.Diagnostics.Debug.WriteLine($"??? Suggested filename: {Path.GetFileName(suggestedName)}");
                }
                
                // Handle download progress and completion
                e.DownloadOperation.StateChanged += (s, args) =>
                {
                    var download = s as CoreWebView2DownloadOperation;
                    if (download == null) return;
                    
                    System.Diagnostics.Debug.WriteLine($"?? Download state changed: {download.State}");
                    
                    switch (download.State)
                    {
                        case CoreWebView2DownloadState.InProgress:
                            if (download.TotalBytesToReceive.HasValue && download.TotalBytesToReceive > 0)
                            {
                                double progress = (double)download.BytesReceived / download.TotalBytesToReceive.Value * 100;
                                System.Diagnostics.Debug.WriteLine($"?? Download progress: {progress:F1}% ({download.BytesReceived}/{download.TotalBytesToReceive.Value} bytes)");
                                DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(
                                    _expectedFileName, progress, (long)download.BytesReceived, (long)download.TotalBytesToReceive.Value));
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"?? Download progress: {download.BytesReceived} bytes (total unknown)");
                            }
                            break;
                            
                        case CoreWebView2DownloadState.Completed:
                            System.Diagnostics.Debug.WriteLine($"? MediaFire download completed: {_expectedFileName}");
                            DownloadCompleted?.Invoke(this, $"Downloaded: {_expectedFileName}");
                            _downloadCompletion?.SetResult(true);
                            break;
                            
                        case CoreWebView2DownloadState.Interrupted:
                            //System.Diagnostics.Debug.WriteLine($"? MediaFire download interrupted: {download.InterruptReason}");
                            //DownloadFailed?.Invoke(this, $"Download interrupted: {download.InterruptReason}");
                            //_downloadCompletion?.SetResult(false);
                            break;
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error handling download start: {ex.Message}");
                e.Cancel = true;
                DownloadFailed?.Invoke(this, $"Error starting download: {ex.Message}");
                _downloadCompletion?.SetResult(false);
            }
        }
        
        private Task CleanupWebView()
        {
            try
            {
                if (_webView != null)
                {
                    if (_webView.CoreWebView2 != null)
                    {
                        _webView.CoreWebView2.DownloadStarting -= OnDownloadStarting;
                        _webView.CoreWebView2.DOMContentLoaded -= OnDOMContentLoaded;
                        _webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                        _webView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
                        _webView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
                    }
                    
                    _webView.Dispose();
                    _webView = null;
                }
                
                if (_hiddenForm != null)
                {
                    _hiddenForm.Dispose();
                    _hiddenForm = null;
                }
                
                _downloadCompletion = null;
                _expectedFileName = null;
                _outputPath = null;
                
                System.Diagnostics.Debug.WriteLine("? WebView2 cleaned up");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error during WebView2 cleanup: {ex.Message}");
                return Task.CompletedTask;
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            // Cleanup will be called sync now
            CleanupWebView();
        }

        private async Task DownloadFileFromUrl(string downloadUrl, string fileName, string outputPath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"?? Starting manual download from: {downloadUrl}");
                
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(10);
                
                // Set headers to mimic a browser request
                httpClient.DefaultRequestHeaders.Add("User-Agent", 
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("Accept", 
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                
                string filePath = Path.Combine(outputPath, fileName);
                System.Diagnostics.Debug.WriteLine($"?? Downloading to: {filePath}");
                
                using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                System.Diagnostics.Debug.WriteLine($"?? File size: {totalBytes} bytes");
                
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                
                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                var lastProgressUpdate = DateTime.MinValue;
                int bytesRead;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    
                    // Throttle progress updates to avoid overwhelming the UI
                    var now = DateTime.Now;
                    if (totalBytes > 0 && (now - lastProgressUpdate).TotalMilliseconds >= 250) // Update every 250ms
                    {
                        var progress = (double)totalBytesRead / totalBytes * 100;
                        System.Diagnostics.Debug.WriteLine($"?? Download progress: {progress:F1}% ({totalBytesRead}/{totalBytes} bytes)");
                        DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(
                            fileName, progress, totalBytesRead, totalBytes));
                        lastProgressUpdate = now;
                    }
                    else if (totalBytes == 0 && (now - lastProgressUpdate).TotalSeconds >= 2) // For unknown size, update every 2 seconds
                    {
                        System.Diagnostics.Debug.WriteLine($"?? Downloaded: {totalBytesRead} bytes");
                        // For unknown file size, show progress as indeterminate
                        DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(
                            fileName, 0, totalBytesRead, 0));
                        lastProgressUpdate = now;
                    }
                }
                
                // Final progress update
                if (totalBytes > 0)
                {
                    DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(
                        fileName, 100, totalBytes, totalBytes));
                }
                
                System.Diagnostics.Debug.WriteLine($"? MediaFire download completed successfully: {fileName}");
                DownloadCompleted?.Invoke(this, $"Downloaded: {fileName}");
                _downloadCompletion?.SetResult(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Manual download error: {ex.Message}");
                DownloadFailed?.Invoke(this, $"Download error: {ex.Message}");
                _downloadCompletion?.SetResult(false);
                throw;
            }
        }
    }
}