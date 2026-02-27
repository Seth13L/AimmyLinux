using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Aimmy.Linux.App.Services.Network;

public sealed class PackageAwareUpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly UpdateSettings _settings;

    public PackageAwareUpdateService(UpdateSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AimmyLinux", "3.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.ReleasesApiUrl))
        {
            return new UpdateCheckResult(false, currentVersion, currentVersion, string.Empty, "Updates are disabled.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(_settings.ReleasesApiUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var latestVersion = document.RootElement.TryGetProperty("tag_name", out var tag) ? tag.GetString() ?? currentVersion : currentVersion;
            var downloadUrl = string.Empty;

            if (document.RootElement.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("browser_download_url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                    {
                        var value = urlProp.GetString();
                        if (value is null)
                        {
                            continue;
                        }

                        if (_settings.PackageType == "rpm" && value.EndsWith(".rpm", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = value;
                            break;
                        }

                        if (_settings.PackageType == "deb" && value.EndsWith(".deb", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = value;
                            break;
                        }

                        downloadUrl = value;
                    }
                }
            }

            var updateAvailable = CompareVersions(currentVersion, latestVersion) < 0;
            var notes = updateAvailable
                ? $"Update available via {_settings.PackageType} channel."
                : "Current version is up to date.";

            return new UpdateCheckResult(updateAvailable, currentVersion, latestVersion, downloadUrl, notes);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(false, currentVersion, currentVersion, string.Empty, $"Update check failed: {ex.Message}");
        }
    }

    private static int CompareVersions(string currentVersion, string latestVersion)
    {
        static string Normalize(string value) => value.Trim().TrimStart('v', 'V');

        if (Version.TryParse(Normalize(currentVersion), out var current) && Version.TryParse(Normalize(latestVersion), out var latest))
        {
            return current.CompareTo(latest);
        }

        return string.Compare(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase);
    }
}
