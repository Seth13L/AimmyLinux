using Aimmy.Core.Models;
using Aimmy.Platform.Abstractions.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IInferenceBackend : IAsyncDisposable
{
    string Name { get; }
    InferenceRuntimeInfo RuntimeInfo { get; }
    IReadOnlyList<Detection> Detect(Image<Rgba32> frame, float minimumConfidence);
}
