using Aimmy.Core.Models;
using Aimmy.Platform.Abstractions.Interfaces;

namespace Aimmy.Platform.Linux.X11.Overlay;

public sealed class NoopOverlayBackend : IOverlayBackend
{
    public string Name => "noop-overlay";

    public Task ShowFovAsync(int size, string style, string color, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task HideFovAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ShowDetectionsAsync(IReadOnlyList<Detection> detections, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ClearDetectionsAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
