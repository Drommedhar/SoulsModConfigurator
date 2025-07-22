using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoulsConfigurator.Services
{
    public class ModDownloadService
    {
        private readonly NexusModsService _nexusService;
        private readonly MediaFireDownloadService _mediaFireService;
        
        public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;
        public event EventHandler<string>? DownloadCompleted;
        public event EventHandler<string>? DownloadFailed;
        
        // Mod download configurations - Updated to use latest files dynamically
        private static readonly Dictionary<string, ModDownloadInfo> ModDownloads = new()
        {
            // Dark Souls 2 mods
            ["DS2_Randomizer"] = new ModDownloadInfo
            {
                GameDomain = "darksouls2",
                ModId = 1317,
                FileName = "DS2_Randomizer.zip", // Generic name, will use latest file
                OutputFolder = "DS2"
            },

            // Dark Souls 3 mods
            ["DS3_FogGate"] = new ModDownloadInfo
            {
                GameDomain = "darksouls3",
                ModId = 551,
                FileName = "DS3_FogGate_Randomizer.zip", // Generic name, will use latest file
                OutputFolder = "DS3"
            },
            ["DS3_ItemEnemy"] = new ModDownloadInfo
            {
                GameDomain = "darksouls3", 
                ModId = 361,
                FileName = "DS3_Item_Enemy_Randomizer.zip", // Generic name, will use latest file
                OutputFolder = "DS3"
            },
            ["DS3_ModEngine"] = new ModDownloadInfo
            {
                GameDomain = "darksouls3",
                ModId = 332,
                FileName = "ModEngine.zip",
                OutputFolder = "DS3"
            },
            
            // Sekiro mods
            ["Sekiro_Randomizer"] = new ModDownloadInfo
            {
                GameDomain = "sekiro",
                ModId = 543, // Sekiro Randomizer - Main file
                FileName = "Sekiro_Randomizer.zip",
                OutputFolder = "Sekiro"
            },
            ["Sekiro_ModEngine"] = new ModDownloadInfo
            {
                GameDomain = "sekiro",
                ModId = 6, // Sekiro ModEngine
                FileName = "ModEngine.zip",
                OutputFolder = "Sekiro"
            }
        };
        
        // Special downloads for specific files within mods (like Sekiro optional files)
        private static readonly Dictionary<string, SpecificFileDownloadInfo> SpecificFileDownloads = new()
        {
            ["Sekiro_CombinedSFX"] = new SpecificFileDownloadInfo
            {
                GameDomain = "sekiro",
                ModId = 543, // Same mod as Sekiro Randomizer
                FileNamePattern = "Combined SFX",
                CategoryId = 3, // OPTIONAL category
                FileName = "Combined_SFX.zip",
                OutputFolder = "Sekiro"
            },
            ["Sekiro_DivineDragon"] = new SpecificFileDownloadInfo
            {
                GameDomain = "sekiro",
                ModId = 543, // Same mod as Sekiro Randomizer
                FileNamePattern = "Divine Dragon",
                CategoryId = 3, // OPTIONAL category
                FileName = "Divine_Dragon_Textures.zip",
                OutputFolder = "Sekiro"
            }
        };
        
        // Direct download URLs for non-Nexus files
        private static readonly Dictionary<string, DirectDownloadInfo> DirectDownloads = new()
        {

        };
        
        // MediaFire downloads that require browser automation
        private static readonly Dictionary<string, MediaFireDownloadInfo> MediaFireDownloads = new()
        {
            ["DS3_CrashFix"] = new MediaFireDownloadInfo
            {
                Url = "https://www.mediafire.com/file/2popj38c55nbhx2/DarkSoulsIII.exe/file",
                FileName = "DarkSoulsIII.exe",
                OutputFolder = "DS3",
                Description = "DS3 Crash Fix"
            }
        };
        
        public ModDownloadService()
        {
            _nexusService = new NexusModsService();
            _nexusService.DownloadProgress += OnNexusDownloadProgress;
            _nexusService.AuthenticationCompleted += OnAuthenticationCompleted;
            _nexusService.AuthenticationFailed += OnAuthenticationFailed;
            
            _mediaFireService = new MediaFireDownloadService();
            _mediaFireService.DownloadProgress += OnMediaFireDownloadProgress;
            _mediaFireService.DownloadCompleted += OnMediaFireDownloadCompleted;
            _mediaFireService.DownloadFailed += OnMediaFireDownloadFailed;
        }
        
        public bool IsAuthenticated => _nexusService.IsAuthenticated;
        
        /// <summary>
        /// Initiates SSO authentication with Nexus Mods
        /// </summary>
        public async Task<bool> AuthenticateAsync()
        {
            return await _nexusService.AuthenticateAsync();
        }
        
        /// <summary>
        /// Logs out and clears saved authentication
        /// </summary>
        public void Logout()
        {
            _nexusService.Logout();
        }
        
        /// <summary>
        /// Downloads all required files for a specific game
        /// </summary>
        /// <param name="gameName">Game name ("DS3" or "Sekiro")</param>
        public async Task<bool> DownloadAllForGameAsync(string gameName)
        {
            if (!IsAuthenticated)
            {
                DownloadFailed?.Invoke(this, "Not authenticated with Nexus Mods. Please authenticate first.");
                return false;
            }
            
            try
            {
                var basePath = Path.Combine("Data");
                var successCount = 0;
                var totalCount = 0;
                var downloadedCount = 0; // Track actual downloads vs skipped files
                
                // Download Nexus mods (only if missing)
                foreach (var kvp in ModDownloads)
                {
                    var modInfo = kvp.Value;
                    if (modInfo.OutputFolder == gameName)
                    {
                        totalCount++;
                        var outputPath = Path.Combine(basePath, modInfo.OutputFolder);
                        
                        // Check if file already exists
                        if (IsFileAvailable(basePath, modInfo.OutputFolder, modInfo.FileName, kvp.Key))
                        {
                            System.Diagnostics.Debug.WriteLine($"? Skipping {modInfo.FileName} - already available");
                            DownloadCompleted?.Invoke(this, $"Skipped (already present): {modInfo.FileName}");
                            successCount++;
                            continue;
                        }
                        
                        try
                        {
                            var success = await _nexusService.DownloadModAsync(
                                modInfo.GameDomain,
                                modInfo.ModId,
                                modInfo.FileName,
                                outputPath);
                                
                            if (success)
                            {
                                // After successful download, create a symlink/copy with the expected filename
                                await CreateExpectedFilename(outputPath, modInfo.FileName, gameName, kvp.Key);
                                
                                successCount++;
                                downloadedCount++;
                                DownloadCompleted?.Invoke(this, $"Downloaded: {modInfo.FileName}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"? Nexus download failed for {modInfo.FileName} - continuing with other downloads");
                                DownloadFailed?.Invoke(this, $"Failed to download: {modInfo.FileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"? Nexus download error for {modInfo.FileName}: {ex.Message} - continuing with other downloads");
                            DownloadFailed?.Invoke(this, $"Error downloading {modInfo.FileName}: {ex.Message}");
                        }
                    }
                }
                
                // Download specific files (like Sekiro optional files) - only if missing
                foreach (var kvp in SpecificFileDownloads)
                {
                    var fileInfo = kvp.Value;
                    if (fileInfo.OutputFolder == gameName)
                    {
                        totalCount++;
                        var outputPath = Path.Combine(basePath, fileInfo.OutputFolder);
                        
                        // Check if file already exists
                        if (IsFileAvailable(basePath, fileInfo.OutputFolder, fileInfo.FileName, kvp.Key))
                        {
                            System.Diagnostics.Debug.WriteLine($"? Skipping {fileInfo.FileName} - already available");
                            DownloadCompleted?.Invoke(this, $"Skipped (already present): {fileInfo.FileName}");
                            successCount++;
                            continue;
                        }
                        
                        try
                        {
                            var success = await _nexusService.DownloadSpecificFileAsync(
                                fileInfo.GameDomain,
                                fileInfo.ModId,
                                fileInfo.FileName,
                                outputPath,
                                fileInfo.FileNamePattern,
                                fileInfo.CategoryId);
                                
                            if (success)
                            {
                                // After successful download, create a symlink/copy with the expected filename
                                await CreateExpectedFilename(outputPath, fileInfo.FileName, gameName, kvp.Key);
                                
                                successCount++;
                                downloadedCount++;
                                DownloadCompleted?.Invoke(this, $"Downloaded: {fileInfo.FileName}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"? Nexus specific file download failed for {fileInfo.FileName} - continuing with other downloads");
                                DownloadFailed?.Invoke(this, $"Failed to download: {fileInfo.FileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"? Nexus specific file download error for {fileInfo.FileName}: {ex.Message} - continuing with other downloads");
                            DownloadFailed?.Invoke(this, $"Error downloading {fileInfo.FileName}: {ex.Message}");
                        }
                    }
                }
                
                // Download direct files - only if missing
                foreach (var kvp in DirectDownloads)
                {
                    var downloadInfo = kvp.Value;
                    if (downloadInfo.OutputFolder == gameName)
                    {
                        totalCount++;
                        var outputPath = Path.Combine(basePath, downloadInfo.OutputFolder);
                        var filePath = Path.Combine(basePath, downloadInfo.OutputFolder, downloadInfo.FileName);
                        
                        // Check if file already exists
                        if (File.Exists(filePath))
                        {
                            System.Diagnostics.Debug.WriteLine($"? Skipping {downloadInfo.FileName} - already available");
                            DownloadCompleted?.Invoke(this, $"Skipped (already present): {downloadInfo.FileName}");
                            successCount++;
                            continue;
                        }
                        
                        try
                        {
                            var success = await _nexusService.DownloadFileAsync(
                                downloadInfo.Url,
                                downloadInfo.FileName,
                                outputPath);
                                
                            if (success)
                            {
                                // Direct downloads should already have the correct filename
                                successCount++;
                                downloadedCount++;
                                DownloadCompleted?.Invoke(this, $"Downloaded: {downloadInfo.FileName}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"? Direct download failed for {downloadInfo.FileName} - continuing with other downloads");
                                DownloadFailed?.Invoke(this, $"Failed to download: {downloadInfo.FileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"? Direct download error for {downloadInfo.FileName}: {ex.Message} - continuing with other downloads");
                            DownloadFailed?.Invoke(this, $"Error downloading {downloadInfo.FileName}: {ex.Message}");
                        }
                    }
                }
                
                // MediaFire downloads (requires browser automation) - only if missing
                foreach (var kvp in MediaFireDownloads)
                {
                    var mediaFireInfo = kvp.Value;
                    if (mediaFireInfo.OutputFolder == gameName)
                    {
                        totalCount++;
                        var outputPath = Path.Combine(basePath, mediaFireInfo.OutputFolder);
                        var filePath = Path.Combine(basePath, mediaFireInfo.OutputFolder, mediaFireInfo.FileName);
                        
                        // Check if file already exists
                        if (File.Exists(filePath))
                        {
                            System.Diagnostics.Debug.WriteLine($"? Skipping {mediaFireInfo.FileName} - already available");
                            DownloadCompleted?.Invoke(this, $"Skipped (already present): {mediaFireInfo.FileName}");
                            successCount++;
                            continue;
                        }
                        
                        try
                        {
                            var success = await _mediaFireService.DownloadFileAsync(
                                mediaFireInfo.Url,
                                mediaFireInfo.FileName,
                                outputPath);
                                
                            if (success)
                            {
                                successCount++;
                                downloadedCount++;
                                DownloadCompleted?.Invoke(this, $"Downloaded: {mediaFireInfo.FileName}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"? MediaFire download failed for {mediaFireInfo.FileName}");
                                DownloadFailed?.Invoke(this, $"Failed to download: {mediaFireInfo.FileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"? MediaFire download error for {mediaFireInfo.FileName}: {ex.Message}");
                            DownloadFailed?.Invoke(this, $"Error downloading {mediaFireInfo.FileName}: {ex.Message}");
                        }
                    }
                }
                
                var allSuccessful = successCount == totalCount;
                
                // Check if essential files (like MediaFire crash fixes) were downloaded successfully
                bool essentialFilesDownloaded = true;
                int mediaFireSuccessCount = 0;
                int mediaFireTotalCount = 0;
                
                foreach (var kvp in MediaFireDownloads)
                {
                    var mediaFireInfo = kvp.Value;
                    if (mediaFireInfo.OutputFolder == gameName)
                    {
                        mediaFireTotalCount++;
                        var filePath = Path.Combine(basePath, mediaFireInfo.OutputFolder, mediaFireInfo.FileName);
                        if (File.Exists(filePath))
                        {
                            mediaFireSuccessCount++;
                        }
                        else
                        {
                            essentialFilesDownloaded = false;
                        }
                    }
                }
                
                // Provide detailed completion message
                if (allSuccessful)
                {
                    if (downloadedCount == 0)
                    {
                        DownloadCompleted?.Invoke(this, $"All {totalCount} files for {gameName} are already present! No downloads needed.");
                    }
                    else if (downloadedCount == totalCount)
                    {
                        DownloadCompleted?.Invoke(this, $"Successfully downloaded all {totalCount} files for {gameName}!");
                    }
                    else
                    {
                        DownloadCompleted?.Invoke(this, $"All {totalCount} files for {gameName} are now available! ({downloadedCount} downloaded, {totalCount - downloadedCount} already present)");
                    }
                }
                else if (essentialFilesDownloaded && mediaFireTotalCount > 0)
                {
                    // If essential files (MediaFire) are available, consider it a partial success
                    System.Diagnostics.Debug.WriteLine($"? Essential files downloaded successfully (MediaFire: {mediaFireSuccessCount}/{mediaFireTotalCount})");
                    DownloadCompleted?.Invoke(this, $"Essential files for {gameName} downloaded successfully! Some Nexus downloads may require premium membership.");
                    allSuccessful = true; // Override the failure since essential files are available
                }
                else
                {
                    DownloadFailed?.Invoke(this, $"Downloaded {successCount}/{totalCount} files for {gameName}. Some downloads failed.");
                }
                
                return allSuccessful;
            }
            catch (Exception ex)
            {
                DownloadFailed?.Invoke(this, $"Error during bulk download: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Downloads a specific mod by key
        /// </summary>
        public async Task<bool> DownloadModAsync(string modKey)
        {
            if (!IsAuthenticated)
            {
                DownloadFailed?.Invoke(this, "Not authenticated with Nexus Mods. Please authenticate first.");
                return false;
            }
            
            try
            {
                var basePath = Path.Combine("Data");
                
                if (ModDownloads.TryGetValue(modKey, out var modInfo))
                {
                    var outputPath = Path.Combine(basePath, modInfo.OutputFolder);
                    return await _nexusService.DownloadModAsync(
                        modInfo.GameDomain,
                        modInfo.ModId,
                        modInfo.FileName,
                        outputPath);
                }
                
                if (SpecificFileDownloads.TryGetValue(modKey, out var fileInfo))
                {
                    var outputPath = Path.Combine(basePath, fileInfo.OutputFolder);
                    return await _nexusService.DownloadSpecificFileAsync(
                        fileInfo.GameDomain,
                        fileInfo.ModId,
                        fileInfo.FileName,
                        outputPath,
                        fileInfo.FileNamePattern,
                        fileInfo.CategoryId);
                }
                
                if (DirectDownloads.TryGetValue(modKey, out var downloadInfo))
                {
                    var outputPath = Path.Combine(basePath, downloadInfo.OutputFolder);
                    return await _nexusService.DownloadFileAsync(
                        downloadInfo.Url,
                        downloadInfo.FileName,
                        outputPath);
                }
                
                // MediaFire downloads (requires browser automation)
                if (MediaFireDownloads.TryGetValue(modKey, out var mediaFireInfo))
                {
                    var outputPath = Path.Combine(basePath, mediaFireInfo.OutputFolder);
                    return await _mediaFireService.DownloadFileAsync(
                        mediaFireInfo.Url,
                        mediaFireInfo.FileName,
                        outputPath);
                }
                
                DownloadFailed?.Invoke(this, $"Unknown mod key: {modKey}");
                return false;
            }
            catch (Exception ex)
            {
                DownloadFailed?.Invoke(this, $"Error downloading mod {modKey}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks which files are missing for a given game
        /// </summary>
        public List<string> GetMissingFiles(string gameName)
        {
            var missingFiles = new List<string>();
            var basePath = Path.Combine("Data");
            
            // Check Nexus mods
            foreach (var kvp in ModDownloads)
            {
                var modInfo = kvp.Value;
                if (modInfo.OutputFolder == gameName)
                {
                    if (!IsFileAvailable(basePath, modInfo.OutputFolder, modInfo.FileName, kvp.Key))
                    {
                        missingFiles.Add(modInfo.FileName);
                    }
                }
            }
            
            // Check specific file downloads
            foreach (var kvp in SpecificFileDownloads)
            {
                var fileInfo = kvp.Value;
                if (fileInfo.OutputFolder == gameName)
                {
                    if (!IsFileAvailable(basePath, fileInfo.OutputFolder, fileInfo.FileName, kvp.Key))
                    {
                        missingFiles.Add(fileInfo.FileName);
                    }
                }
            }
            
            // Check direct downloads
            foreach (var kvp in DirectDownloads)
            {
                var downloadInfo = kvp.Value;
                if (downloadInfo.OutputFolder == gameName)
                {
                    var filePath = Path.Combine(basePath, downloadInfo.OutputFolder, downloadInfo.FileName);
                    if (!File.Exists(filePath))
                    {
                        missingFiles.Add(downloadInfo.FileName);
                    }
                }
            }
            
            // Check MediaFire downloads
            foreach (var kvp in MediaFireDownloads)
            {
                var mediaFireInfo = kvp.Value;
                if (mediaFireInfo.OutputFolder == gameName)
                {
                    var filePath = Path.Combine(basePath, mediaFireInfo.OutputFolder, mediaFireInfo.FileName);
                    if (!File.Exists(filePath))
                    {
                        missingFiles.Add(mediaFireInfo.FileName);
                    }
                }
            }
            
            return missingFiles;
        }
        
        /// <summary>
        /// Checks if a file is available, using intelligent pattern matching for downloaded files
        /// </summary>
        private bool IsFileAvailable(string basePath, string outputFolder, string expectedFilename, string modKey)
        {
            var expectedFilePath = Path.Combine(basePath, outputFolder, expectedFilename);
            
            // First check if the expected file exists
            if (File.Exists(expectedFilePath))
            {
                return true;
            }
            
            // If not, check if there's a matching downloaded file we can use
            var directory = new DirectoryInfo(Path.Combine(basePath, outputFolder));
            if (!directory.Exists)
            {
                return false;
            }
            
            // Look for files that match the mod type using the same logic as CreateExpectedFilename
            FileInfo? matchingFile = null;
            
            if (modKey.Contains("ModEngine"))
            {
                matchingFile = directory.GetFiles("ModEngine*.zip", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
            }
            else if (modKey.Contains("FogGate"))
            {
                matchingFile = directory.GetFiles("*Fog*Gate*.zip", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
            }
            else if (modKey.Contains("ItemEnemy") || modKey.Contains("Item_Enemy"))
            {
                matchingFile = directory.GetFiles("*Item*Enemy*.zip", SearchOption.TopDirectoryOnly)
                    .Concat(directory.GetFiles("*Static*Item*.zip", SearchOption.TopDirectoryOnly))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
            }
            else if (modKey.Contains("Randomizer"))
            {
                matchingFile = directory.GetFiles("*Randomizer*.zip", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
            }
            else if (modKey.Contains("CombinedSFX"))
            {
                matchingFile = directory.GetFiles("*Combined*SFX*.zip", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
            }
            else if (modKey.Contains("DivineDragon"))
            {
                matchingFile = directory.GetFiles("*Divine*Dragon*.zip", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
            }
            else if (modKey.Contains("CrashFix"))
            {
                // Special handling for crash fix executable
                matchingFile = directory.GetFiles("DarkSoulsIII.exe", SearchOption.TopDirectoryOnly)
                    .Concat(directory.GetFiles("*DarkSouls*.exe", SearchOption.TopDirectoryOnly))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();
            }
            
            return matchingFile != null && matchingFile.Exists;
        }
        
        /// <summary>
        /// Gets the total number of files that should be downloaded for a game
        /// </summary>
        public int GetTotalFileCount(string gameName)
        {
            var count = 0;
            
            foreach (var modInfo in ModDownloads.Values)
            {
                if (modInfo.OutputFolder == gameName)
                    count++;
            }
            
            foreach (var fileInfo in SpecificFileDownloads.Values)
            {
                if (fileInfo.OutputFolder == gameName)
                    count++;
            }
            
            foreach (var downloadInfo in DirectDownloads.Values)
            {
                if (downloadInfo.OutputFolder == gameName)
                    count++;
            }
            
            foreach (var mediaFireInfo in MediaFireDownloads.Values)
            {
                if (mediaFireInfo.OutputFolder == gameName)
                    count++;
            }
            
            return count;
        }
        
        /// <summary>
        /// Generates a manual download guide for all missing files
        /// </summary>
        public string GenerateManualDownloadGuide(string gameName)
        {
            var guide = new StringBuilder();
            guide.AppendLine($"MANUAL DOWNLOAD GUIDE FOR {gameName.ToUpper()}");
            guide.AppendLine("".PadRight(50, '='));
            guide.AppendLine();
            guide.AppendLine("Due to Nexus Mods API limitations, automatic downloads require premium membership.");
            guide.AppendLine("Please download the following files manually:");
            guide.AppendLine();

            var fileCount = 1;

            // Regular mod downloads
            foreach (var kvp in ModDownloads)
            {
                var modInfo = kvp.Value;
                if (modInfo.OutputFolder == gameName)
                {
                    guide.AppendLine($"{fileCount}. {kvp.Key}:");
                    guide.AppendLine($"   • URL: https://www.nexusmods.com/{modInfo.GameDomain}/mods/{modInfo.ModId}");
                    guide.AppendLine($"   • Download the latest MAIN file");
                    guide.AppendLine($"   • Save as: {modInfo.FileName}");
                    guide.AppendLine($"   • Location: Data\\{modInfo.OutputFolder}\\");
                    guide.AppendLine();
                    fileCount++;
                }
            }

            // Specific file downloads (like Sekiro optional files)
            foreach (var kvp in SpecificFileDownloads)
            {
                var fileInfo = kvp.Value;
                if (fileInfo.OutputFolder == gameName)
                {
                    guide.AppendLine($"{fileCount}. {kvp.Key}:");
                    guide.AppendLine($"   • URL: https://www.nexusmods.com/{fileInfo.GameDomain}/mods/{fileInfo.ModId}");
                    guide.AppendLine($"   • Download the '{fileInfo.FileNamePattern}' file from OPTIONAL section");
                    guide.AppendLine($"   • Save as: {fileInfo.FileName}");
                    guide.AppendLine($"   • Location: Data\\{fileInfo.OutputFolder}\\");
                    guide.AppendLine();
                    fileCount++;
                }
            }

            // Direct downloads (currently empty but for completeness)
            foreach (var kvp in DirectDownloads)
            {
                var downloadInfo = kvp.Value;
                if (downloadInfo.OutputFolder == gameName)
                {
                    guide.AppendLine($"{fileCount}. {kvp.Key}:");
                    guide.AppendLine($"   • URL: {downloadInfo.Url}");
                    guide.AppendLine($"   • Save as: {downloadInfo.FileName}");
                    guide.AppendLine($"   • Location: Data\\{downloadInfo.OutputFolder}\\");
                    guide.AppendLine();
                    fileCount++;
                }
            }

            // MediaFire downloads
            foreach (var kvp in MediaFireDownloads)
            {
                var mediaFireInfo = kvp.Value;
                if (mediaFireInfo.OutputFolder == gameName)
                {
                    guide.AppendLine($"{fileCount}. {kvp.Key}:");
                    guide.AppendLine($"   • URL: {mediaFireInfo.Url}");
                    guide.AppendLine($"   • Save as: {mediaFireInfo.FileName}");
                    guide.AppendLine($"   • Location: Data\\{mediaFireInfo.OutputFolder}\\");
                    guide.AppendLine($"   • Description: {mediaFireInfo.Description}");
                    guide.AppendLine();
                    fileCount++;
                }
            }

            guide.AppendLine("IMPORTANT NOTES:");
            guide.AppendLine("• Create the Data folder structure if it doesn't exist");
            guide.AppendLine("• Use the exact filenames shown above");
            guide.AppendLine("• Download the latest/newest version of each file");
            guide.AppendLine("• After downloading all files, click 'Check Files' again to verify");

            return guide.ToString();
        }
        
        /// <summary>
        /// Opens browser to mod download pages for manual download
        /// </summary>
        public void OpenBrowserForManualDownload(string gameName)
        {
            var openedUrls = new HashSet<string>(); // Avoid opening the same URL multiple times
            
            // Open regular mod downloads
            foreach (var kvp in ModDownloads)
            {
                var modInfo = kvp.Value;
                if (modInfo.OutputFolder == gameName)
                {
                    var url = $"https://www.nexusmods.com/{modInfo.GameDomain}/mods/{modInfo.ModId}";
                    if (!openedUrls.Contains(url))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                        openedUrls.Add(url);
                        
                        // Small delay between opening tabs to avoid overwhelming the browser
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
            
            // Open specific file downloads (like Sekiro optional files)
            foreach (var kvp in SpecificFileDownloads)
            {
                var fileInfo = kvp.Value;
                if (fileInfo.OutputFolder == gameName)
                {
                    var url = $"https://www.nexusmods.com/{fileInfo.GameDomain}/mods/{fileInfo.ModId}";
                    if (!openedUrls.Contains(url))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                        openedUrls.Add(url);
                        
                        // Small delay between opening tabs
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
            
            // Open direct downloads
            foreach (var kvp in DirectDownloads)
            {
                var downloadInfo = kvp.Value;
                if (downloadInfo.OutputFolder == gameName)
                {
                    if (!openedUrls.Contains(downloadInfo.Url))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(downloadInfo.Url) { UseShellExecute = true });
                        openedUrls.Add(downloadInfo.Url);
                        
                        // Small delay between opening tabs
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
            
            // Open MediaFire downloads (requires browser automation)
            foreach (var kvp in MediaFireDownloads)
            {
                var mediaFireInfo = kvp.Value;
                if (mediaFireInfo.OutputFolder == gameName)
                {
                    if (!openedUrls.Contains(mediaFireInfo.Url))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(mediaFireInfo.Url) { UseShellExecute = true });
                        openedUrls.Add(mediaFireInfo.Url);
                        
                        // Small delay between opening tabs
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets detailed download information for browser-based downloads
        /// </summary>
        public List<BrowserDownloadInfo> GetBrowserDownloadInfo(string gameName)
        {
            var downloads = new List<BrowserDownloadInfo>();
            
            // Regular mod downloads
            foreach (var kvp in ModDownloads)
            {
                var modInfo = kvp.Value;
                if (modInfo.OutputFolder == gameName)
                {
                    downloads.Add(new BrowserDownloadInfo
                    {
                        ModKey = kvp.Key,
                        ModName = kvp.Key.Replace("_", " "),
                        Url = $"https://www.nexusmods.com/{modInfo.GameDomain}/mods/{modInfo.ModId}",
                        Instructions = "Download the latest MAIN file",
                        ExpectedFileName = modInfo.FileName,
                        OutputFolder = modInfo.OutputFolder
                    });
                }
            }
            
            // Specific file downloads (like Sekiro optional files)
            foreach (var kvp in SpecificFileDownloads)
            {
                var fileInfo = kvp.Value;
                if (fileInfo.OutputFolder == gameName)
                {
                    downloads.Add(new BrowserDownloadInfo
                    {
                        ModKey = kvp.Key,
                        ModName = kvp.Key.Replace("_", " "),
                        Url = $"https://www.nexusmods.com/{fileInfo.GameDomain}/mods/{fileInfo.ModId}",
                        Instructions = $"Download the '{fileInfo.FileNamePattern}' file from OPTIONAL section",
                        ExpectedFileName = fileInfo.FileName,
                        OutputFolder = fileInfo.OutputFolder
                    });
                }
            }
            
            // Direct downloads
            foreach (var kvp in DirectDownloads)
            {
                var downloadInfo = kvp.Value;
                if (downloadInfo.OutputFolder == gameName)
                {
                    downloads.Add(new BrowserDownloadInfo
                    {
                        ModKey = kvp.Key,
                        ModName = kvp.Key.Replace("_", " "),
                        Url = downloadInfo.Url,
                        Instructions = "Download directly",
                        ExpectedFileName = downloadInfo.FileName,
                        OutputFolder = downloadInfo.OutputFolder
                    });
                }
            }
            
            // MediaFire downloads
            foreach (var kvp in MediaFireDownloads)
            {
                var mediaFireInfo = kvp.Value;
                if (mediaFireInfo.OutputFolder == gameName)
                {
                    downloads.Add(new BrowserDownloadInfo
                    {
                        ModKey = kvp.Key,
                        ModName = kvp.Key.Replace("_", " "),
                        Url = mediaFireInfo.Url,
                        Instructions = "Download via browser automation",
                        ExpectedFileName = mediaFireInfo.FileName,
                        OutputFolder = mediaFireInfo.OutputFolder
                    });
                }
            }
            
            return downloads;
        }
        
        private void OnNexusDownloadProgress(object? sender, DownloadProgressEventArgs e)
        {
            DownloadProgress?.Invoke(this, e);
        }
        
        private void OnAuthenticationCompleted(object? sender, string message)
        {
            DownloadCompleted?.Invoke(this, $"Authentication successful: {message}");
        }
        
        private void OnAuthenticationFailed(object? sender, string message)
        {
            DownloadFailed?.Invoke(this, $"Authentication failed: {message}");
        }
        
        private void OnMediaFireDownloadProgress(object? sender, DownloadProgressEventArgs e)
        {
            DownloadProgress?.Invoke(this, e);
        }
        
        private void OnMediaFireDownloadCompleted(object? sender, string message)
        {
            DownloadCompleted?.Invoke(this, message);
        }
        
        private void OnMediaFireDownloadFailed(object? sender, string message)
        {
            DownloadFailed?.Invoke(this, message);
        }
        
        public void Dispose()
        {
            _nexusService?.Dispose();
            _mediaFireService?.Dispose();
        }
        
        /// <summary>
        /// Creates an archive with the expected filename that mod classes can find
        /// Renames simple files or normalizes complex archive structures as needed
        /// </summary>
        private async Task CreateExpectedFilename(string outputPath, string expectedFilename, string gameName, string modKey)
        {
            try
            {
                var expectedFilePath = Path.Combine(outputPath, expectedFilename);
                
                // If the expected file already exists, we're good
                if (File.Exists(expectedFilePath))
                {
                    return;
                }
                
                // Find the actual downloaded file in the directory
                var directory = new DirectoryInfo(outputPath);
                if (!directory.Exists)
                {
                    return;
                }
                
                // Look for files that match the mod type
                FileInfo? actualFile = null;
                
                // Try different matching strategies based on the mod key
                if (modKey.Contains("ModEngine"))
                {
                    actualFile = directory.GetFiles("ModEngine*.zip", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();
                }
                else if (modKey.Contains("FogGate"))
                {
                    actualFile = directory.GetFiles("*Fog*Gate*.zip", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();
                }
                else if (modKey.Contains("ItemEnemy") || modKey.Contains("Item_Enemy"))
                {
                    actualFile = directory.GetFiles("*Item*Enemy*.zip", SearchOption.TopDirectoryOnly)
                        .Concat(directory.GetFiles("*Static*Item*.zip", SearchOption.TopDirectoryOnly))
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();
                }
                else if (modKey.Contains("Randomizer"))
                {
                    actualFile = directory.GetFiles("*Randomizer*.zip", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();
                }
                else if (modKey.Contains("CombinedSFX"))
                {
                    actualFile = directory.GetFiles("*Combined*SFX*.zip", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();
                }
                else if (modKey.Contains("DivineDragon"))
                {
                    actualFile = directory.GetFiles("*Divine*Dragon*.zip", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();
                }
                else
                {
                    // Generic fallback - find the most recent zip file
                    actualFile = directory.GetFiles("*.zip", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();
                }
                
                if (actualFile != null && actualFile.Exists)
                {
                    // Check if the archive needs structure normalization
                    bool needsNormalization = ArchiveNeedsNormalization(actualFile.FullName);
                    
                    if (needsNormalization)
                    {
                        // Create a normalized archive with the expected filename
                        await NormalizeArchiveStructure(actualFile.FullName, expectedFilePath, modKey);
                        
                        // Remove the original file since we've created a normalized version
                        File.Delete(actualFile.FullName);
                        
                        System.Diagnostics.Debug.WriteLine($"? Created normalized archive: {actualFile.Name} ? {expectedFilename}");
                        DownloadCompleted?.Invoke(this, $"Created normalized archive: {expectedFilename}");
                    }
                    else
                    {
                        // Archive structure is already correct, just rename it
                        File.Move(actualFile.FullName, expectedFilePath);
                        
                        System.Diagnostics.Debug.WriteLine($"? Renamed archive: {actualFile.Name} ? {expectedFilePath}");
                        DownloadCompleted?.Invoke(this, $"Renamed archive: {expectedFilename}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"? Could not find downloaded file for {modKey} in {outputPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error creating expected filename for {modKey}: {ex.Message}");
                // Don't throw - this is a nice-to-have feature
            }
        }
        
        /// <summary>
        /// Checks if an archive needs structure normalization (has a single root folder)
        /// </summary>
        private bool ArchiveNeedsNormalization(string archivePath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(archivePath);
                var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
                var allInSingleFolder = AnalyzeArchiveStructure(entries);
                
                // Returns true if all files are in a single subfolder (needs normalization)
                return allInSingleFolder != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error analyzing archive structure: {ex.Message}");
                // If we can't analyze it, assume it needs normalization to be safe
                return true;
            }
        }
        
        /// <summary>
        /// Creates expected filenames for all downloaded files in a game folder
        /// This helps when users manually download files with different names
        /// </summary>
        public async Task CreateExpectedFilenamesForGame(string gameName)
        {
            try
            {
                var basePath = Path.Combine("Data");
                
                // Process regular mod downloads
                foreach (var kvp in ModDownloads)
                {
                    var modInfo = kvp.Value;
                    if (modInfo.OutputFolder == gameName)
                    {
                        var outputPath = Path.Combine(basePath, modInfo.OutputFolder);
                        await CreateExpectedFilename(outputPath, modInfo.FileName, gameName, kvp.Key);
                    }
                }
                
                // Process specific file downloads
                foreach (var kvp in SpecificFileDownloads)
                {
                    var fileInfo = kvp.Value;
                    if (fileInfo.OutputFolder == gameName)
                    {
                        var outputPath = Path.Combine(basePath, fileInfo.OutputFolder);
                        await CreateExpectedFilename(outputPath, fileInfo.FileName, gameName, kvp.Key);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"? Processed expected filenames for {gameName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error creating expected filenames for {gameName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Normalizes archive structure by handling both single-folder and multi-folder archives
        /// Creates a new archive with files at the root level for proper mod installation
        /// </summary>
        private async Task NormalizeArchiveStructure(string sourceArchivePath, string targetArchivePath, string modKey)
        {
            try
            {
                using var sourceArchive = ZipFile.OpenRead(sourceArchivePath);
                
                // Analyze the archive structure
                var entries = sourceArchive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();
                var allInSingleFolder = AnalyzeArchiveStructure(entries);

                // Create a new normalized archive
                using var targetStream = File.Create(targetArchivePath);
                using var targetArchive = new ZipArchive(targetStream, ZipArchiveMode.Create);
                
                foreach (var entry in entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // Skip directories
                    
                    string targetEntryName;
                    
                    if (allInSingleFolder != null && modKey.Contains("modengine", StringComparison.OrdinalIgnoreCase))
                    {
                        // Remove the common folder prefix
                        targetEntryName = entry.FullName.Substring(allInSingleFolder.Length + 1);
                    }
                    else
                    {
                        // Keep original structure (files are already at root or mixed structure)
                        targetEntryName = entry.FullName;
                    }
                    
                    // Create the entry in the target archive
                    var targetEntry = targetArchive.CreateEntry(targetEntryName);
                    targetEntry.LastWriteTime = entry.LastWriteTime;
                    
                    // Copy the file content
                    using var sourceStream = entry.Open();
                    using var targetEntryStream = targetEntry.Open();
                    await sourceStream.CopyToAsync(targetEntryStream);
                }
                
                System.Diagnostics.Debug.WriteLine($"? Normalized archive structure for {modKey}");
                
                // Log the transformation
                if (allInSingleFolder != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  Removed common folder: {allInSingleFolder}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  Archive structure was already normalized");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error normalizing archive structure: {ex.Message}");
                
                // Fallback: Just copy the original file
                try
                {
                    File.Copy(sourceArchivePath, targetArchivePath, overwrite: true);
                    System.Diagnostics.Debug.WriteLine($"  Fallback: Copied original archive");
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"  Fallback failed: {fallbackEx.Message}");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Analyzes archive structure to determine if all files are in a single root folder
        /// Returns the common folder name if found, null if files are at root or mixed structure
        /// </summary>
        private string? AnalyzeArchiveStructure(List<ZipArchiveEntry> entries)
        {
            if (!entries.Any()) return null;
            
            // Get all unique top-level folders
            var topLevelItems = new HashSet<string>();
            
            foreach (var entry in entries)
            {
                var path = entry.FullName;
                var firstSlash = path.IndexOf('/');
                var firstBackslash = path.IndexOf('\\');
                
                int separatorIndex = -1;
                if (firstSlash >= 0 && firstBackslash >= 0)
                {
                    separatorIndex = Math.Min(firstSlash, firstBackslash);
                }
                else if (firstSlash >= 0)
                {
                    separatorIndex = firstSlash;
                }
                else if (firstBackslash >= 0)
                {
                    separatorIndex = firstBackslash;
                }
                
                if (separatorIndex >= 0)
                {
                    // File is in a subfolder
                    var topLevelItem = path.Substring(0, separatorIndex);
                    topLevelItems.Add(topLevelItem);
                }
                else
                {
                    // File is at root level
                    topLevelItems.Add("");
                }
            }
            
            // If all files are in exactly one subfolder (and none at root), return that folder
            if (topLevelItems.Count == 1 && !topLevelItems.Contains(""))
            {
                return topLevelItems.First();
            }
            
            return null; // Mixed structure or files at root
        }
    }
    
    public class ModDownloadInfo
    {
        public string GameDomain { get; set; } = string.Empty;
        public int ModId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;
    }
    
    public class SpecificFileDownloadInfo
    {
        public string GameDomain { get; set; } = string.Empty;
        public int ModId { get; set; }
        public string FileNamePattern { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;
    }
    
    public class DirectDownloadInfo
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;
    }
    
    public class MediaFireDownloadInfo
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    
    public class BrowserDownloadInfo
    {
        public string ModKey { get; set; } = string.Empty;
        public string ModName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string ExpectedFileName { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;
    }
}
