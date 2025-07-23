using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Diagnostics;

namespace SoulsConfigurator.Services
{
    public class VersionCheckService
    {
        private readonly HttpClient _httpClient;
        private readonly string _githubApiUrl = "https://api.github.com/repos/Drommedhar/SoulsModConfigurator/releases/latest";

        public VersionCheckService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SoulsModConfigurator");
        }

        public async Task<VersionCheckResult> CheckForUpdatesAsync()
        {
            try
            {
                var currentVersion = GetCurrentVersion();
                var latestRelease = await GetLatestReleaseAsync();
                
                if (latestRelease == null)
                {
                    return new VersionCheckResult
                    {
                        IsUpdateAvailable = false,
                        ErrorMessage = "Could not retrieve latest release information"
                    };
                }

                var latestVersion = ExtractVersionFromReleaseName(latestRelease.TagName);
                
                if (latestVersion == null)
                {
                    return new VersionCheckResult
                    {
                        IsUpdateAvailable = false,
                        ErrorMessage = "Could not parse version from release"
                    };
                }

                var isUpdateAvailable = IsNewerVersion(latestVersion, currentVersion);

                return new VersionCheckResult
                {
                    IsUpdateAvailable = isUpdateAvailable,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    ReleaseUrl = latestRelease.HtmlUrl,
                    ReleaseName = latestRelease.Name
                };
            }
            catch (Exception ex)
            {
                return new VersionCheckResult
                {
                    IsUpdateAvailable = false,
                    ErrorMessage = $"Error checking for updates: {ex.Message}"
                };
            }
        }

        private Version GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version ?? new Version(0, 0, 0, 0);
        }

        private async Task<GitHubRelease?> GetLatestReleaseAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_githubApiUrl);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<GitHubRelease>(response, options);
            }
            catch
            {
                return null;
            }
        }

        private Version? ExtractVersionFromReleaseName(string tagName)
        {
            try
            {
                // Remove 'v' prefix if present and try to parse
                var versionString = tagName.StartsWith("v") ? tagName.Substring(1) : tagName;
                
                // Handle cases where the version might be in the format "1.2.0.0" or "1.2.0"
                if (Version.TryParse(versionString, out var version))
                {
                    return version;
                }

                // Try to extract version pattern from release name like "Release v1.2.0.0"
                var match = System.Text.RegularExpressions.Regex.Match(tagName, @"v?(\d+\.\d+\.\d+(?:\.\d+)?)");
                if (match.Success)
                {
                    if (Version.TryParse(match.Groups[1].Value, out version))
                    {
                        return version;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool IsNewerVersion(Version latestVersion, Version currentVersion)
        {
            return latestVersion > currentVersion;
        }

        public void ShowUpdateDialog(VersionCheckResult result)
        {
            if (!result.IsUpdateAvailable || string.IsNullOrEmpty(result.ReleaseUrl))
                return;

            var message = $"A new version is available!\n\n" +
                         $"Current version: {result.CurrentVersion}\n" +
                         $"Latest version: {result.LatestVersion}\n" +
                         $"Release: {result.ReleaseName}\n\n" +
                         $"Would you like to open the download page?";

            var dialogResult = MessageBox.Show(
                message,
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information
            );

            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = result.ReleaseUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not open the download page: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class VersionCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public Version? CurrentVersion { get; set; }
        public Version? LatestVersion { get; set; }
        public string? ReleaseUrl { get; set; }
        public string? ReleaseName { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }
    }
}
