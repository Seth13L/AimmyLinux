using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Inference.OnnxRuntime.Backend;

namespace Aimmy.Inference.OnnxRuntime;

public static class InferenceBackendFactory
{
    public static IInferenceBackend Create(AimmyConfig config)
    {
        return new OnnxRuntimeInferenceBackend(config);
    }
}
