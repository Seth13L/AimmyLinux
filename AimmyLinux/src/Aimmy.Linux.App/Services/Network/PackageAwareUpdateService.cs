using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Aimmy.Linux.App.Services.Network;

public sealed class PackageAwareUpdateService : IUpdateService
{
    private const int MaxAttempts = 3;

    private readonly HttpClient _httpClient;
    private readonly UpdateSettings _settings;

    public PackageAwareUpdateService(UpdateSettings settings, HttpClient? httpClient = null)
    {
        _settings = settings;
        _httpClient = httpClient ?? CreateHttpClient();
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.ReleasesApiUrl))
        {
            return new UpdateCheckResult(
                false,
                currentVersion,
                currentVersion,
                string.Empty,
                "Updates are disabled.");
        }

        try
        {
            var release = await ResolveTargetReleaseAsync(cancellationToken).ConfigureAwait(false);
            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
            {
                return new UpdateCheckResult(
                    false,
                    currentVersion,
                    currentVersion,
                    string.Empty,
                    "No matching release found for the configured channel.");
            }

            var latestVersion = release.TagName;
            var updateAvailable = CompareVersions(currentVersion, latestVersion) < 0;
            var selectedAsset = SelectPackageAsset(release.Assets, _settings.PackageType);
            var downloadUrl = selectedAsset?.DownloadUrl
                ?? release.HtmlUrl
                ?? string.Empty;

            var notes = BuildNotes(updateAvailable, selectedAsset, release);
            return new UpdateCheckResult(
                updateAvailable,
                currentVersion,
                latestVersion,
                downloadUrl,
                notes);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                false,
                currentVersion,
                currentVersion,
                string.Empty,
                $"Update check failed: {ex.Message}");
        }
    }

    private async Task<ReleaseInfo?> ResolveTargetReleaseAsync(CancellationToken cancellationToken)
    {
        var channel = NormalizeChannel(_settings.Channel);
        if (channel == "stable" && IsLatestEndpoint(_settings.ReleasesApiUrl))
        {
            return await ExecuteWithRetryAsync(
                "load latest stable release",
                async token =>
                {
                    using var response = await _httpClient.GetAsync(_settings.ReleasesApiUrl, token).ConfigureAwait(false);
                    EnsureSuccessStatusCode(response, "load latest stable release");

                    await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                    using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token).ConfigureAwait(false);
                    return ParseRelease(document.RootElement);
                },
                cancellationToken).ConfigureAwait(false);
        }

        var listEndpoint = ToReleasesListEndpoint(_settings.ReleasesApiUrl);
        return await ExecuteWithRetryAsync(
            "load releases list",
            async token =>
            {
                using var response = await _httpClient.GetAsync(listEndpoint, token).ConfigureAwait(false);
                EnsureSuccessStatusCode(response, "load releases list");

                await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token).ConfigureAwait(false);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                var releases = new List<ReleaseInfo>();
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    var parsed = ParseRelease(item);
                    if (parsed is null || parsed.Draft)
                    {
                        continue;
                    }

                    releases.Add(parsed);
                }

                if (releases.Count == 0)
                {
                    return null;
                }

                var ordered = releases
                    .OrderByDescending(r => r.PublishedAt)
                    .ThenByDescending(r => r.TagName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return SelectReleaseForChannel(ordered, channel);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static ReleaseInfo? SelectReleaseForChannel(IReadOnlyList<ReleaseInfo> releases, string channel)
    {
        if (releases.Count == 0)
        {
            return null;
        }

        if (channel == "stable")
        {
            return releases.FirstOrDefault(release => !release.PreRelease);
        }

        var prerelease = releases.FirstOrDefault(release => release.PreRelease);
        return prerelease ?? releases[0];
    }

    private static ReleaseAssetInfo? SelectPackageAsset(IReadOnlyList<ReleaseAssetInfo> assets, string packageType)
    {
        if (assets.Count == 0)
        {
            return null;
        }

        var extension = NormalizePackageType(packageType) switch
        {
            "deb" => ".deb",
            "rpm" => ".rpm",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(extension))
        {
            return assets[0];
        }

        var matches = assets
            .Where(asset =>
                asset.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
                asset.DownloadUrl.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matches.Count == 0)
        {
            return null;
        }

        return matches.FirstOrDefault(IsPreferredLinuxX64Asset) ?? matches[0];
    }

    private static bool IsPreferredLinuxX64Asset(ReleaseAssetInfo asset)
    {
        return asset.Name.Contains("x86_64", StringComparison.OrdinalIgnoreCase)
            || asset.Name.Contains("amd64", StringComparison.OrdinalIgnoreCase)
            || asset.Name.Contains("linux-x64", StringComparison.OrdinalIgnoreCase);
    }

    private string BuildNotes(bool updateAvailable, ReleaseAssetInfo? selectedAsset, ReleaseInfo release)
    {
        var channel = NormalizeChannel(_settings.Channel);
        var packageType = NormalizePackageType(_settings.PackageType);

        if (!updateAvailable)
        {
            return $"Current version is up to date for '{channel}' channel.";
        }

        if (selectedAsset is null)
        {
            return $"Release {release.TagName} is available on '{channel}' channel, but no '{packageType}' package asset was found.";
        }

        return packageType switch
        {
            "deb" => $"Update {release.TagName} available on '{channel}' channel. Install with: sudo apt install ./{selectedAsset.Name}",
            "rpm" => $"Update {release.TagName} available on '{channel}' channel. Install with: sudo dnf install ./{selectedAsset.Name}",
            _ => $"Update {release.TagName} available on '{channel}' channel. Download and install with your package manager."
        };
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await action(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransientFailure(ex))
            {
                lastException = ex;
                await Task.Delay(GetRetryDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Updater operation '{operation}' failed on attempt {attempt}/{MaxAttempts}: {ex.Message}",
                    ex);
            }
        }

        throw new InvalidOperationException(
            $"Updater operation '{operation}' failed after {MaxAttempts} attempts: {lastException?.Message ?? "unknown error"}",
            lastException);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AimmyLinux", "3.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private static bool IsTransientFailure(Exception ex)
    {
        if (ex is TaskCanceledException)
        {
            return true;
        }

        if (ex is not HttpRequestException requestException)
        {
            return false;
        }

        if (!requestException.StatusCode.HasValue)
        {
            return true;
        }

        var code = requestException.StatusCode.Value;
        return code == HttpStatusCode.RequestTimeout
               || code == HttpStatusCode.TooManyRequests
               || (int)code >= 500;
    }

    private static void EnsureSuccessStatusCode(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new HttpRequestException(
            $"Updater operation '{operation}' failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
            null,
            response.StatusCode);
    }

    private static TimeSpan GetRetryDelay(int attempt)
    {
        return attempt switch
        {
            1 => TimeSpan.FromMilliseconds(250),
            _ => TimeSpan.FromMilliseconds(750)
        };
    }

    private static bool IsLatestEndpoint(string url)
    {
        return url.EndsWith("/latest", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToReleasesListEndpoint(string endpoint)
    {
        if (IsLatestEndpoint(endpoint))
        {
            return endpoint[..^"/latest".Length];
        }

        return endpoint;
    }

    private static string NormalizeChannel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "stable";
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "stable" => "stable",
            "beta" => "beta",
            "preview" => "beta",
            "rc" => "beta",
            "nightly" => "nightly",
            _ => normalized
        };
    }

    private static string NormalizePackageType(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "deb" : value.Trim().ToLowerInvariant();
    }

    private static ReleaseInfo? ParseRelease(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var tagName = GetString(element, "tag_name");
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        var assets = new List<ReleaseAssetInfo>();
        if (element.TryGetProperty("assets", out var assetsElement) && assetsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assetsElement.EnumerateArray())
            {
                var name = GetString(asset, "name");
                var downloadUrl = GetString(asset, "browser_download_url");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(downloadUrl))
                {
                    continue;
                }

                assets.Add(new ReleaseAssetInfo(name, downloadUrl));
            }
        }

        var publishedAt = GetDateTimeOffset(element, "published_at");
        var htmlUrl = GetString(element, "html_url");
        var prerelease = GetBoolean(element, "prerelease");
        var draft = GetBoolean(element, "draft");

        return new ReleaseInfo(tagName, htmlUrl, prerelease, draft, publishedAt, assets);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return property.GetString() ?? string.Empty;
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False)
        {
            return false;
        }

        return property.GetBoolean();
    }

    private static DateTimeOffset GetDateTimeOffset(JsonElement element, string propertyName)
    {
        var text = GetString(element, propertyName);
        return DateTimeOffset.TryParse(text, out var parsed) ? parsed : DateTimeOffset.MinValue;
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

    private sealed record ReleaseInfo(
        string TagName,
        string HtmlUrl,
        bool PreRelease,
        bool Draft,
        DateTimeOffset PublishedAt,
        IReadOnlyList<ReleaseAssetInfo> Assets);

    private sealed record ReleaseAssetInfo(string Name, string DownloadUrl);
}
