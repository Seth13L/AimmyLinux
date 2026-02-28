using Aimmy.Core.Config;
using Aimmy.Linux.App.Services.Network;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class PackageAwareUpdateServiceTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_DebPackage_ProvidesAptGuidance()
    {
        var settings = new UpdateSettings
        {
            Enabled = true,
            Channel = "stable",
            PackageType = "deb",
            ReleasesApiUrl = "https://api.github.com/repos/example/aimmy/releases/latest"
        };

        var json = """
            {
              "tag_name": "v3.1.0",
              "html_url": "https://github.com/example/aimmy/releases/tag/v3.1.0",
              "assets": [
                { "name": "aimmy-3.1.0-x86_64.rpm", "browser_download_url": "https://example.com/aimmy-3.1.0-x86_64.rpm" },
                { "name": "aimmy_3.1.0_amd64.deb", "browser_download_url": "https://example.com/aimmy_3.1.0_amd64.deb" }
              ]
            }
            """;

        var service = new PackageAwareUpdateService(settings, CreateHttpClient(HttpStatusCode.OK, json));
        var result = await service.CheckForUpdatesAsync("3.0.0", CancellationToken.None);

        Assert.True(result.UpdateAvailable);
        Assert.Equal("v3.1.0", result.LatestVersion);
        Assert.EndsWith(".deb", result.DownloadUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sudo apt install", result.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_BetaChannel_UsesPrereleaseFromListEndpoint()
    {
        var settings = new UpdateSettings
        {
            Enabled = true,
            Channel = "beta",
            PackageType = "rpm",
            ReleasesApiUrl = "https://api.github.com/repos/example/aimmy/releases/latest"
        };

        var requestedUrls = new List<string>();
        var json = """
            [
              {
                "tag_name": "v3.1.0",
                "draft": false,
                "prerelease": false,
                "published_at": "2026-02-20T10:00:00Z",
                "html_url": "https://github.com/example/aimmy/releases/tag/v3.1.0",
                "assets": [
                  { "name": "aimmy-3.1.0-x86_64.rpm", "browser_download_url": "https://example.com/aimmy-3.1.0-x86_64.rpm" }
                ]
              },
              {
                "tag_name": "v3.2.0-beta.1",
                "draft": false,
                "prerelease": true,
                "published_at": "2026-02-25T10:00:00Z",
                "html_url": "https://github.com/example/aimmy/releases/tag/v3.2.0-beta.1",
                "assets": [
                  { "name": "aimmy-3.2.0-beta.1-x86_64.rpm", "browser_download_url": "https://example.com/aimmy-3.2.0-beta.1-x86_64.rpm" }
                ]
              }
            ]
            """;

        var client = CreateHttpClient(HttpStatusCode.OK, json, requestedUrls);
        var service = new PackageAwareUpdateService(settings, client);
        var result = await service.CheckForUpdatesAsync("3.1.0", CancellationToken.None);

        Assert.True(result.UpdateAvailable);
        Assert.Equal("v3.2.0-beta.1", result.LatestVersion);
        Assert.Contains(".rpm", result.DownloadUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/releases", requestedUrls[0], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/latest", requestedUrls[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sudo dnf install", result.Notes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_TransientFailure_RetriesAndSucceeds()
    {
        var settings = new UpdateSettings
        {
            Enabled = true,
            Channel = "stable",
            PackageType = "deb",
            ReleasesApiUrl = "https://api.github.com/repos/example/aimmy/releases/latest"
        };

        var handler = new SequenceMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("busy", Encoding.UTF8, "text/plain")
            },
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "tag_name": "v3.1.0",
                      "html_url": "https://github.com/example/aimmy/releases/tag/v3.1.0",
                      "assets": [
                        { "name": "aimmy_3.1.0_amd64.deb", "browser_download_url": "https://example.com/aimmy_3.1.0_amd64.deb" }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var service = new PackageAwareUpdateService(settings, new HttpClient(handler));
        var result = await service.CheckForUpdatesAsync("3.0.0", CancellationToken.None);

        Assert.True(result.UpdateAvailable);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_MissingPackageAsset_ProvidesClearGuidance()
    {
        var settings = new UpdateSettings
        {
            Enabled = true,
            Channel = "stable",
            PackageType = "deb",
            ReleasesApiUrl = "https://api.github.com/repos/example/aimmy/releases/latest"
        };

        var json = """
            {
              "tag_name": "v3.1.0",
              "html_url": "https://github.com/example/aimmy/releases/tag/v3.1.0",
              "assets": [
                { "name": "aimmy-3.1.0-x86_64.rpm", "browser_download_url": "https://example.com/aimmy-3.1.0-x86_64.rpm" }
              ]
            }
            """;

        var service = new PackageAwareUpdateService(settings, CreateHttpClient(HttpStatusCode.OK, json));
        var result = await service.CheckForUpdatesAsync("3.0.0", CancellationToken.None);

        Assert.True(result.UpdateAvailable);
        Assert.Equal("https://github.com/example/aimmy/releases/tag/v3.1.0", result.DownloadUrl);
        Assert.Contains("no 'deb' package asset", result.Notes, StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string content, List<string>? requestedUrls = null)
    {
        var handler = new SequenceMessageHandler(request =>
        {
            requestedUrls?.Add(request.RequestUri?.ToString() ?? string.Empty);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
        });

        return new HttpClient(handler);
    }

    private sealed class SequenceMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _lastResponse;

        public SequenceMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
        {
            if (responses.Length == 0)
            {
                throw new ArgumentException("At least one response factory is required.", nameof(responses));
            }

            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
            _lastResponse = responses[^1];
        }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var responseFactory = _responses.Count > 0
                ? _responses.Dequeue()
                : _lastResponse;
            return Task.FromResult(responseFactory(request));
        }
    }
}
