using Aimmy.Core.Enums;

namespace Aimmy.Platform.Abstractions.Models;

public sealed record InferenceRuntimeInfo(
    GpuExecutionMode SelectedMode,
    string Provider,
    string Message,
    bool IsFallback);
