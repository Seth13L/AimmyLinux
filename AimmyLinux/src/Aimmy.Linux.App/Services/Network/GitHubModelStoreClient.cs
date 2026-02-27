using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Aimmy.Linux.App.Services.Network;

public sealed class GitHubModelStoreClient : IModelStoreClient
{
    private readonly HttpClient _httpClient;
    private readonly StoreSettings _settings;

    public GitHubModelStoreClient(StoreSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AimmyLinux", "3.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
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
        Directory.CreateDirectory(destinationDirectory);

        var destinationPath = Path.Combine(destinationDirectory, entry.Name);
        using var response = await _httpClient.GetAsync(entry.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destinationStream = File.Create(destinationPath);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);

        return destinationPath;
    }

    private async Task<IReadOnlyList<ModelStoreEntry>> GetEntriesAsync(string apiUrl, string type, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            return Array.Empty<ModelStoreEntry>();
        }

        using var response = await _httpClient.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ModelStoreEntry>();
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

        return entries;
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
