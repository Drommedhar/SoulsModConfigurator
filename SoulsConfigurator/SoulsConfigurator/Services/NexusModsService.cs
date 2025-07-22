using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Diagnostics;
using System.Linq;

namespace SoulsConfigurator.Services
{
    // SSO Response models
    public class SSOResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public SSOData? Data { get; set; }
        public string? ConnectionId { get; set; }
    }
    
    public class SSOData
    {
        public string? ApiKey { get; set; }
        public string? Token { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("api_key")]
        public string? Api_Key { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("connection_token")]
        public string? Connection_Token { get; set; }
    }

    public class NexusModsService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SettingsService _settingsService;
        private ClientWebSocket? _webSocket;
        private string? _apiKey;
        private bool _disposed = false;
        
        // Using the existing 'vortex' application slug that's already registered. We should actually use our own, but ehhh
        private const string APPLICATION_SLUG = "Vortex";
        private const string SSO_WEBSOCKET_URL = "wss://sso.nexusmods.com";
        private const string NEXUS_API_BASE_URL = "https://api.nexusmods.com/v1"; // Corrected to match Swagger documentation
        
        public event EventHandler<string>? AuthenticationCompleted;
        public event EventHandler<string>? AuthenticationFailed;
        public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
        
        public bool IsAuthenticated => !string.IsNullOrEmpty(_apiKey);
        
        public NexusModsService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SoulsConfigurator/1.1.0");
            _settingsService = new SettingsService();
            
            // Try to load saved API key on startup (async operation)
            _ = LoadSavedApiKeyAsync();
        }
        
        /// <summary>
        /// Loads saved API key from settings and validates it
        /// </summary>
        private async Task LoadSavedApiKeyAsync()
        {
            try
            {
                var savedApiKey = _settingsService.GetNexusApiKey();
                if (!string.IsNullOrEmpty(savedApiKey))
                {
                    // Validate the API key by making a test request
                    _apiKey = savedApiKey;
                    _httpClient.DefaultRequestHeaders.Remove("apikey");
                    _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
                    
                    // Test the API key with a simple request
                    try
                    {
                        var validateEndpoint = $"{NEXUS_API_BASE_URL}/users/validate.json";
                        System.Diagnostics.Debug.WriteLine($"Validating API key at: {validateEndpoint}");
                        
                        var response = await _httpClient.GetAsync(validateEndpoint);
                        if (response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine("✓ Loaded saved API key successfully");
                            AuthenticationCompleted?.Invoke(this, "Loaded saved authentication");
                        }
                        else
                        {
                            // API key is invalid, clear it
                            _apiKey = null;
                            _httpClient.DefaultRequestHeaders.Remove("apikey");
                            _settingsService.ClearNexusApiKey();
                            System.Diagnostics.Debug.WriteLine($"✗ Saved API key is invalid (HTTP {response.StatusCode}), cleared from settings");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Network error or API unavailable, keep the key but don't validate
                        System.Diagnostics.Debug.WriteLine($"⚠ Could not validate saved API key: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine("  Keeping API key for later validation");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading saved API key: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clears the saved authentication
        /// </summary>
        public void Logout()
        {
            _apiKey = null;
            _httpClient.DefaultRequestHeaders.Remove("apikey");
            _settingsService.ClearNexusApiKey();
            System.Diagnostics.Debug.WriteLine("✓ Logged out and cleared saved API key");
        }
        
        /// <summary>
        /// Initiates the SSO authentication process with Nexus Mods using the Vortex application slug
        /// </summary>
        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                var uuid = Guid.NewGuid().ToString();
                
                _webSocket = new ClientWebSocket();
                
                // Add proper headers for the WebSocket connection
                _webSocket.Options.SetRequestHeader("User-Agent", "SoulsConfigurator/1.1.0");
                
                await _webSocket.ConnectAsync(new Uri(SSO_WEBSOCKET_URL), CancellationToken.None);
                
                // Send SSO request with proper format
                var ssoRequest = new
                {
                    id = uuid,
                    appid = APPLICATION_SLUG,
                    token = (string?)null,
                    protocol = 2
                };
                
                var requestJson = JsonSerializer.Serialize(ssoRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                
                System.Diagnostics.Debug.WriteLine($"Sending SSO request: {requestJson}");
                
                await _webSocket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                
                // Open browser for user authentication
                var authUrl = $"https://www.nexusmods.com/sso?id={uuid}&application=vortex";
                
                System.Diagnostics.Debug.WriteLine($"Opening browser to: {authUrl}");
                
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
                
                // Listen for response with the same UUID
                return await ListenForAuthResponse(uuid);
            }
            catch (Exception ex)
            {
                AuthenticationFailed?.Invoke(this, $"Authentication failed: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ListenForAuthResponse(string sessionId)
        {
            if (_webSocket == null) return false;
            
            var buffer = new byte[8192]; // Increased buffer size
            var timeout = TimeSpan.FromMinutes(5); // 5 minute timeout
            var cancellationToken = new CancellationTokenSource(timeout).Token;
            string? connectionToken = null;
            bool connectionTokenSent = false;
            
            try
            {
                while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        
                        // Log the raw response for debugging
                        System.Diagnostics.Debug.WriteLine($"SSO Response: {responseJson}");
                        
                        try
                        {
                            // Handle different response formats
                            using var jsonDoc = JsonDocument.Parse(responseJson);
                            var root = jsonDoc.RootElement;
                            
                            // Check if this is a success response
                            if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                            {
                                if (root.TryGetProperty("data", out var dataProp))
                                {
                                    // Check for API key in various formats
                                    var apiKey = TryGetApiKey(dataProp);
                                    if (!string.IsNullOrEmpty(apiKey))
                                    {
                                        _apiKey = apiKey;
                                        _httpClient.DefaultRequestHeaders.Remove("apikey");
                                        _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
                                        
                                        // Save the API key for future use
                                        _settingsService.SaveNexusApiKey(_apiKey);
                                        
                                        System.Diagnostics.Debug.WriteLine($"✓ Authentication successful! API Key received and saved: {_apiKey[..8]}...");
                                        AuthenticationCompleted?.Invoke(this, "Authentication successful!");
                                        return true;
                                    }
                                    
                                    // Check for connection token
                                    if (dataProp.TryGetProperty("connection_token", out var tokenProp))
                                    {
                                        connectionToken = tokenProp.GetString();
                                        if (!string.IsNullOrEmpty(connectionToken) && !connectionTokenSent)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Received connection token: {connectionToken}");
                                            
                                            // Send follow-up message with the SAME session ID
                                            var followUpRequest = new
                                            {
                                                id = sessionId, // Use the same ID!
                                                appid = APPLICATION_SLUG,
                                                token = connectionToken,
                                                protocol = 2
                                            };
                                            
                                            var followUpJson = JsonSerializer.Serialize(followUpRequest);
                                            var followUpBytes = Encoding.UTF8.GetBytes(followUpJson);
                                            await _webSocket.SendAsync(new ArraySegment<byte>(followUpBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                            
                                            connectionTokenSent = true;
                                            System.Diagnostics.Debug.WriteLine($"Sent follow-up request with connection token: {followUpJson}");
                                            // Continue listening for the final API key
                                        }
                                    }
                                }
                            }
                            else if (root.TryGetProperty("success", out var failSuccessProp) && !failSuccessProp.GetBoolean())
                            {
                                // Handle explicit failure
                                var error = root.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : "Unknown error";
                                System.Diagnostics.Debug.WriteLine($"Authentication failed: {error}");
                                AuthenticationFailed?.Invoke(this, error ?? "Unknown error");
                                return false;
                            }
                            
                            // Check if this is a direct API key response (some SSO flows return this directly)
                            if (root.TryGetProperty("api_key", out var directApiKeyProp))
                            {
                                var apiKey = directApiKeyProp.GetString();
                                if (!string.IsNullOrEmpty(apiKey))
                                {
                                    _apiKey = apiKey;
                                    _httpClient.DefaultRequestHeaders.Remove("apikey");
                                    _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
                                    
                                    // Save the API key for future use
                                    _settingsService.SaveNexusApiKey(_apiKey);
                                    
                                    System.Diagnostics.Debug.WriteLine($"✓ Authentication successful! Direct API Key received and saved: {_apiKey[..8]}...");
                                    AuthenticationCompleted?.Invoke(this, "Authentication successful!");
                                    return true;
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            // Log JSON parsing errors but continue listening
                            System.Diagnostics.Debug.WriteLine($"JSON parsing error: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"Raw response: {responseJson}");
                            
                            // Try to parse as a simple string in case it's just an API key
                            if (responseJson.Length > 10 && !responseJson.Contains("{") && !responseJson.Contains("error"))
                            {
                                _apiKey = responseJson.Trim();
                                _httpClient.DefaultRequestHeaders.Remove("apikey");
                                _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
                                
                                // Save the API key for future use
                                _settingsService.SaveNexusApiKey(_apiKey);
                                
                                System.Diagnostics.Debug.WriteLine($"✓ Received and saved API key as plain text: {_apiKey[..8]}...");
                                AuthenticationCompleted?.Invoke(this, "Authentication successful!");
                                return true;
                            }
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        System.Diagnostics.Debug.WriteLine("WebSocket connection closed by server");
                        if (!string.IsNullOrEmpty(_apiKey))
                        {
                            // If we already got an API key but connection closed, that's OK
                            return true;
                        }
                        AuthenticationFailed?.Invoke(this, "WebSocket connection closed by server");
                        return false;
                    }
                }
                
                // If we exit the loop due to timeout
                if (cancellationToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("Authentication timed out");
                    AuthenticationFailed?.Invoke(this, "Authentication timed out - please try again");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Authentication timed out");
                AuthenticationFailed?.Invoke(this, "Authentication timed out - please try again");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during authentication: {ex.Message}");
                AuthenticationFailed?.Invoke(this, $"Error during authentication: {ex.Message}");
            }
            
            return false;
        }
        
        private static string? TryGetApiKey(JsonElement dataElement)
        {
            // Try different possible field names for the API key
            var possibleKeys = new[] { "api_key", "apikey", "apiKey", "token", "Token", "key" };
            
            foreach (var keyName in possibleKeys)
            {
                if (dataElement.TryGetProperty(keyName, out var keyProp))
                {
                    var key = keyProp.GetString();
                    if (!string.IsNullOrEmpty(key) && key.Length > 10) // Valid API keys are longer than 10 chars
                    {
                        return key;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Downloads a mod file from Nexus Mods
        /// </summary>
        /// <param name="gameDomainName">Game domain name (e.g., "darksouls3", "sekiro")</param>
        /// <param name="modId">Nexus mod ID</param>
        /// <param name="fileName">Desired filename for the downloaded file (can be generic)</param>
        /// <param name="outputPath">Directory where the file should be saved</param>
        public async Task<bool> DownloadModAsync(string gameDomainName, int modId, string fileName, string outputPath)
        {
            if (!IsAuthenticated)
            {
                throw new InvalidOperationException("Not authenticated with Nexus Mods");
            }
            
            ModFile? mainFile = null;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to download mod {modId} from {gameDomainName}");
                
                // Get mod files - FIXED ENDPOINT
                var filesEndpoint = $"{NEXUS_API_BASE_URL}/games/{gameDomainName}/mods/{modId}/files.json";
                System.Diagnostics.Debug.WriteLine($"Fetching files from: {filesEndpoint}");
                
                var filesResponse = await _httpClient.GetStringAsync(filesEndpoint);
                System.Diagnostics.Debug.WriteLine($"Raw API response: {filesResponse}");
                
                var filesData = JsonSerializer.Deserialize<ModFilesResponse>(filesResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                System.Diagnostics.Debug.WriteLine($"Deserialized files count: {filesData?.Files?.Count ?? 0}");
                
                if (filesData?.Files != null)
                {
                    foreach (var file in filesData.Files)
                    {
                        System.Diagnostics.Debug.WriteLine($"File: {file.FileName}, Category: {file.CategoryId}, Timestamp: {file.UploadedTimestamp}");
                    }
                }
                
                if (filesData?.Files == null || filesData.Files.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No files found for mod {modId}");
                    return false;
                }
                
                // Find the most recent main file
                mainFile = filesData.Files
                    .Where(f => f.CategoryId == 1) // Main files
                    .OrderByDescending(f => f.UploadedTimestamp)
                    .FirstOrDefault();
                
                if (mainFile == null)
                {
                    System.Diagnostics.Debug.WriteLine($"No main files found for mod {modId}");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"Found file: {mainFile.FileName} (ID: {mainFile.FileId})");
                
                // Get download links - FIXED ENDPOINT
                var downloadEndpoint = $"{NEXUS_API_BASE_URL}/games/{gameDomainName}/mods/{modId}/files/{mainFile.FileId}/download_link.json";
                System.Diagnostics.Debug.WriteLine($"Fetching download links from: {downloadEndpoint}");
                
                var downloadLinksResponse = await _httpClient.GetStringAsync(downloadEndpoint);
                System.Diagnostics.Debug.WriteLine($"Raw download links response: {downloadLinksResponse}");
                
                var downloadLinks = JsonSerializer.Deserialize<List<DownloadLink>>(downloadLinksResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                System.Diagnostics.Debug.WriteLine($"Deserialized download links count: {downloadLinks?.Count ?? 0}");
                
                if (downloadLinks != null)
                {
                    foreach (var link in downloadLinks)
                    {
                        System.Diagnostics.Debug.WriteLine($"Download link: {link.Name} -> {link.URI}");
                    }
                }
                
                if (downloadLinks == null || downloadLinks.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No download links found for file {mainFile.FileId}");
                    return false;
                }
                
                // Use the actual filename from Nexus or fall back to the provided name
                var actualFileName = !string.IsNullOrEmpty(mainFile.FileName) ? mainFile.FileName : fileName;
                
                // Download the file
                var downloadUrl = downloadLinks[0].URI;
                var filePath = Path.Combine(outputPath, actualFileName);
                
                System.Diagnostics.Debug.WriteLine($"Downloading from: {downloadUrl}");
                System.Diagnostics.Debug.WriteLine($"Saving to: {filePath}");
                
                Directory.CreateDirectory(outputPath);
                
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                
                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                int bytesRead;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    
                    if (totalBytes > 0)
                    {
                        var progress = (double)totalBytesRead / totalBytes * 100;
                        DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(actualFileName, progress, totalBytesRead, totalBytes));
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"✓ Successfully downloaded: {actualFileName}");
                return true;
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"✗ HTTP Error downloading mod {modId}: {httpEx.Message}");
                
                // Handle 403 Forbidden specifically - this means premium membership required
                if (httpEx.Message.Contains("403") || httpEx.Message.Contains("Forbidden"))
                {
                    var outputFolderName = outputPath.Split(Path.DirectorySeparatorChar).Last();
                    throw new Exception($"Download requires Nexus Mods Premium membership. Please download manually:\n" +
                                      $"• Mod ID: {modId}\n" +
                                      $"• File: {mainFile?.FileName ?? "Latest main file"}\n" +
                                      $"• URL: https://www.nexusmods.com/{gameDomainName}/mods/{modId}\n" +
                                      $"• Save as: {fileName} in Data/{outputFolderName}");
                }
                else if (httpEx.Message.Contains("404"))
                {
                    throw new Exception($"Mod {modId} not found on Nexus Mods for game {gameDomainName}");
                }
                throw new Exception($"HTTP error downloading mod {modId}: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error downloading mod {modId}: {ex.Message}");
                throw new Exception($"Failed to download mod {modId}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Downloads a file from a direct URL (for non-Nexus files like crash fixes)
        /// </summary>
        public async Task<bool> DownloadFileAsync(string url, string fileName, string outputPath)
        {
            try
            {
                Directory.CreateDirectory(outputPath);
                var filePath = Path.Combine(outputPath, fileName);
                
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                
                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                int bytesRead;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    
                    if (totalBytes > 0)
                    {
                        var progress = (double)totalBytesRead / totalBytes * 100;
                        DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(fileName, progress, totalBytesRead, totalBytes));
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download file from {url}: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Downloads a specific file from a Nexus mod by file ID or name pattern
        /// </summary>
        /// <param name="gameDomainName">Game domain name (e.g., "darksouls3", "sekiro")</param>
        /// <param name="modId">Nexus mod ID</param>
        /// <param name="fileName">Desired filename for the downloaded file</param>
        /// <param name="outputPath">Directory where the file should be saved</param>
        /// <param name="fileNamePattern">Pattern to match specific file (e.g., "Combined SFX", "Divine Dragon")</param>
        /// <param name="categoryId">Optional category ID filter (1=MAIN, 3=OPTIONAL, etc.)</param>
        public async Task<bool> DownloadSpecificFileAsync(string gameDomainName, int modId, string fileName, string outputPath, string fileNamePattern, int? categoryId = null)
        {
            if (!IsAuthenticated)
            {
                throw new InvalidOperationException("Not authenticated with Nexus Mods");
            }
            
            ModFile? targetFile = null;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to download specific file from mod {modId}: pattern='{fileNamePattern}', category={categoryId}");
                
                // Get mod files
                var filesEndpoint = $"{NEXUS_API_BASE_URL}/games/{gameDomainName}/mods/{modId}/files.json";
                System.Diagnostics.Debug.WriteLine($"Fetching files from: {filesEndpoint}");
                
                var filesResponse = await _httpClient.GetStringAsync(filesEndpoint);
                var filesData = JsonSerializer.Deserialize<ModFilesResponse>(filesResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (filesData?.Files == null || filesData.Files.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No files found for mod {modId}");
                    return false;
                }
                
                // Find files matching the pattern and category
                var matchingFiles = filesData.Files.Where(f =>
                {
                    bool nameMatches = !string.IsNullOrEmpty(f.Name) && f.Name.Contains(fileNamePattern, StringComparison.OrdinalIgnoreCase);
                    bool categoryMatches = !categoryId.HasValue || f.CategoryId == categoryId.Value;
                    return nameMatches && categoryMatches;
                }).OrderByDescending(f => f.UploadedTimestamp).ToList();
                
                if (matchingFiles.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No files found matching pattern '{fileNamePattern}' with category {categoryId}");
                    return false;
                }
                
                targetFile = matchingFiles.First();
                System.Diagnostics.Debug.WriteLine($"Found matching file: {targetFile.FileName} (ID: {targetFile.FileId})");
                
                // Get download links
                var downloadEndpoint = $"{NEXUS_API_BASE_URL}/games/{gameDomainName}/mods/{modId}/files/{targetFile.FileId}/download_link.json";
                System.Diagnostics.Debug.WriteLine($"Fetching download links from: {downloadEndpoint}");
                
                var downloadLinksResponse = await _httpClient.GetStringAsync(downloadEndpoint);
                var downloadLinks = JsonSerializer.Deserialize<List<DownloadLink>>(downloadLinksResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (downloadLinks == null || downloadLinks.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No download links found for file {targetFile.FileId}");
                    return false;
                }
                
                // Use the actual filename from Nexus or fall back to the provided name
                var actualFileName = !string.IsNullOrEmpty(targetFile.FileName) ? targetFile.FileName : fileName;
                
                // Download the file
                var downloadUrl = downloadLinks[0].URI;
                var filePath = Path.Combine(outputPath, actualFileName);
                
                System.Diagnostics.Debug.WriteLine($"Downloading from: {downloadUrl}");
                System.Diagnostics.Debug.WriteLine($"Saving to: {filePath}");
                
                Directory.CreateDirectory(outputPath);
                
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                
                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                int bytesRead;
                
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    
                    if (totalBytes > 0)
                    {
                        var progress = (double)totalBytesRead / totalBytes * 100;
                        DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(actualFileName, progress, totalBytesRead, totalBytes));
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"✓ Successfully downloaded specific file: {actualFileName}");
                return true;
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"✗ HTTP Error downloading specific file from mod {modId}: {httpEx.Message}");
                
                // Handle 403 Forbidden specifically - this means premium membership required
                if (httpEx.Message.Contains("403") || httpEx.Message.Contains("Forbidden"))
                {
                    throw new Exception($"Download requires Nexus Mods Premium membership. Please download manually:\n" +
                                      $"• Mod ID: {modId}\n" +
                                      $"• File: {targetFile?.FileName ?? "Matching file"}\n" +
                                      $"• URL: https://www.nexusmods.com/{gameDomainName}/mods/{modId}\n" +
                                      $"• Save as: {fileName} in Data/{outputPath.Split(Path.DirectorySeparatorChar).Last()}");
                }
                throw new Exception($"HTTP error downloading specific file from mod {modId}: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error downloading specific file from mod {modId}: {ex.Message}");
                throw new Exception($"Failed to download specific file from mod {modId}: {ex.Message}", ex);
            }
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _webSocket?.Dispose();
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
    
    // Data models for Nexus Mods API responses
    public class ModFilesResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("files")]
        public List<ModFile>? Files { get; set; }
    }
    
    public class ModFile
    {
        [System.Text.Json.Serialization.JsonPropertyName("file_id")]
        public int FileId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("version")]
        public string? Version { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("category_id")]
        public int CategoryId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("category_name")]
        public string? CategoryName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("is_primary")]
        public bool IsPrimary { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("size")]
        public long Size { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("file_name")]
        public string? FileName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("uploaded_timestamp")]
        public long UploadedTimestamp { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("mod_version")]
        public string? ModVersion { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("external_virus_scan_url")]
        public string? ExternalVirusScanUrl { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("size_kb")]
        public int SizeKb { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("size_in_bytes")]
        public int? SizeInBytes { get; set; } // Made nullable to handle null values from API
        
        [System.Text.Json.Serialization.JsonPropertyName("changelog_html")]
        public string? ChangelogHtml { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("content_preview_link")]
        public string? ContentPreviewLink { get; set; }
    }
    
    public class DownloadLink
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("short_name")]
        public string ShortName { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("URI")]
        public string URI { get; set; } = string.Empty;
    }
    
    public class DownloadProgressEventArgs : EventArgs
    {
        public string FileName { get; }
        public double ProgressPercentage { get; }
        public long BytesDownloaded { get; }
        public long TotalBytes { get; }
        
        public DownloadProgressEventArgs(string fileName, double progressPercentage, long bytesDownloaded, long totalBytes)
        {
            FileName = fileName;
            ProgressPercentage = progressPercentage;
            BytesDownloaded = bytesDownloaded;
            TotalBytes = totalBytes;
        }
    }
}
