using Aimmy.Core.Models;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IOverlayBackend : IAsyncDisposable
{
    string Name { get; }
    Task ShowFovAsync(int size, string style, string color, CancellationToken cancellationToken);
    Task HideFovAsync(CancellationToken cancellationToken);
    Task ShowDetectionsAsync(IReadOnlyList<Detection> detections, CancellationToken cancellationToken);
    Task ClearDetectionsAsync(CancellationToken cancellationToken);
}
