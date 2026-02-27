using Aimmy.Platform.Abstractions.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface ICaptureBackend
{
    string Name { get; }
    Task<Image<Rgba32>> CaptureAsync(CaptureRegion region, CancellationToken cancellationToken);
}
