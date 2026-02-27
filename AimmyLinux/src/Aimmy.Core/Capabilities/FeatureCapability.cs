using Aimmy.Core.Enums;

namespace Aimmy.Core.Capabilities;

public sealed record FeatureCapability(
    string Name,
    FeatureState State,
    bool IsDegraded = false,
    string Message = "");
