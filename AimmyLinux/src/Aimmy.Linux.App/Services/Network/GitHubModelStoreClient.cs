using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Aimmy.Linux.App.Services.Network;

public sealed class GitHubModelStoreClient : IModelStoreClient
{
    private const int MaxAttempts = 3;

    private readonly HttpClient _httpClient;
    private readonly StoreSettings _settings;

    public GitHubModelStoreClient(StoreSettings settings, HttpClient? httpClient = null)
    {
        _settings = settings;
        _httpClient = httpClient ?? CreateHttpClient();
    }

    public Task<IReadOnlyList<ModelStoreEntry>> GetModelEntriesAsync(CancellationToken cancellationToken)
    {
        return GetEntriesAsync(_settings.ModelsApiUrl, "model", cancellationToken);
    }

    public Task<IReadOnlyList<ModelStoreEntry>> GetConfigEntriesAsync(CancellationToken cancellationToken)
    {
        return GetEntriesAsync(_settings.ConfigsApiUrl, "config", cancellationToken);
    }

    public async Task<string> DownloadAsync(ModelStoreEntry entry, string destinationDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateAbsoluteHttpUrl(entry.DownloadUrl, nameof(entry.DownloadUrl));

        Directory.CreateDirectory(destinationDirectory);
        var destinationPath = ResolveUniqueDestinationPath(destinationDirectory, entry.Name);

        await ExecuteWithRetryAsync(
            $"download '{entry.Name}'",
            async token =>
            {
                using var response = await _httpClient.GetAsync(entry.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                EnsureSuccessStatusCode(response, $"download '{entry.Name}'");

                await using var sourceStream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                await using var destinationStream = File.Create(destinationPath);
                await sourceStream.CopyToAsync(destinationStream, token).ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);

        return destinationPath;
    }

    private async Task<IReadOnlyList<ModelStoreEntry>> GetEntriesAsync(string apiUrl, string type, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            return Array.Empty<ModelStoreEntry>();
        }

        ValidateAbsoluteHttpUrl(apiUrl, nameof(apiUrl));

        return await ExecuteWithRetryAsync(
            $"load {type} store entries",
            async token =>
            {
                using var response = await _httpClient.GetAsync(apiUrl, token).ConfigureAwait(false);
                EnsureSuccessStatusCode(response, $"load {type} store entries");

                await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token).ConfigureAwait(false);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return (IReadOnlyList<ModelStoreEntry>)Array.Empty<ModelStoreEntry>();
                }

                var entries = new List<ModelStoreEntry>();
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    if (!TryGetString(item, "name", out var name) || !TryGetString(item, "download_url", out var url))
                    {
                        continue;
                    }

                    entries.Add(new ModelStoreEntry(name, url, type));
                }

                return (IReadOnlyList<ModelStoreEntry>)entries;
            },
            cancellationToken).ConfigureAwait(false);
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
                    $"GitHub store operation '{operation}' failed on attempt {attempt}/{MaxAttempts}: {ex.Message}",
                    ex);
            }
        }

        throw new InvalidOperationException(
            $"GitHub store operation failed after {MaxAttempts} attempts: {lastException?.Message ?? "unknown error"}",
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

    private static TimeSpan GetRetryDelay(int attempt)
    {
        return attempt switch
        {
            1 => TimeSpan.FromMilliseconds(250),
            _ => TimeSpan.FromMilliseconds(750)
        };
    }

    private static void EnsureSuccessStatusCode(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new HttpRequestException(
            $"GitHub store operation '{operation}' failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
            null,
            response.StatusCode);
    }

    private static string ResolveUniqueDestinationPath(string destinationDirectory, string suggestedName)
    {
        var safeFileName = Path.GetFileName(string.IsNullOrWhiteSpace(suggestedName) ? "download.bin" : suggestedName);
        var baseName = Path.GetFileNameWithoutExtension(safeFileName);
        var extension = Path.GetExtension(safeFileName);
        var candidate = Path.Combine(destinationDirectory, safeFileName);
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var suffix = 1;
        while (true)
        {
            candidate = Path.Combine(destinationDirectory, $"{baseName} ({suffix}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    private static void ValidateAbsoluteHttpUrl(string url, string parameterName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Expected an absolute HTTP/HTTPS URL.", parameterName);
        }
    }

    private static bool TryGetString(JsonElement element, string key, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(key, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }
}
