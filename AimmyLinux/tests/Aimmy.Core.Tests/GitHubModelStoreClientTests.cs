using Aimmy.Core.Config;
using Aimmy.Linux.App.Services.Network;
using Aimmy.Platform.Abstractions.Models;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class GitHubModelStoreClientTests
{
    [Fact]
    public async Task GetModelEntriesAsync_TransientFailure_RetriesAndReturnsEntries()
    {
        var settings = new StoreSettings
        {
            ModelsApiUrl = "https://api.github.com/repos/example/aimmy/contents/models",
            ConfigsApiUrl = "https://api.github.com/repos/example/aimmy/contents/configs"
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
                    [
                      { "name": "model-a.onnx", "download_url": "https://example.com/model-a.onnx" }
                    ]
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var client = new GitHubModelStoreClient(settings, new HttpClient(handler));
        var entries = await client.GetModelEntriesAsync(CancellationToken.None);

        Assert.Single(entries);
        Assert.Equal("model-a.onnx", entries[0].Name);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task DownloadAsync_WhenDestinationExists_AddsNumericSuffix()
    {
        var settings = new StoreSettings
        {
            ModelsApiUrl = "https://api.github.com/repos/example/aimmy/contents/models",
            ConfigsApiUrl = "https://api.github.com/repos/example/aimmy/contents/configs"
        };

        var tempDir = Path.Combine(Path.GetTempPath(), "aimmy-store-download-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var existingPath = Path.Combine(tempDir, "model-a.onnx");
            await File.WriteAllTextAsync(existingPath, "existing");

            var handler = new SequenceMessageHandler(
                _ => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("fresh"))
                });

            var client = new GitHubModelStoreClient(settings, new HttpClient(handler));
            var entry = new ModelStoreEntry("model-a.onnx", "https://example.com/model-a.onnx", "model");
            var downloadedPath = await client.DownloadAsync(entry, tempDir, CancellationToken.None);

            Assert.EndsWith("model-a (1).onnx", downloadedPath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(downloadedPath));
            Assert.Equal("fresh", await File.ReadAllTextAsync(downloadedPath));
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Ignore temp cleanup issues.
            }
        }
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
