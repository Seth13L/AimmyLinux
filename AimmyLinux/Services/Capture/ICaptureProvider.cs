using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AimmyLinux.Services.Capture;

public interface ICaptureProvider
{
    Task<Image<Rgba32>> CaptureAsync(CaptureRegion region, CancellationToken cancellationToken);
}
