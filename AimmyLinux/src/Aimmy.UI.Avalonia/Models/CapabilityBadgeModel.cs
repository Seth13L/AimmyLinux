using Aimmy.Core.Enums;

namespace Aimmy.UI.Avalonia.Models;

public sealed record CapabilityBadgeModel(string Name, FeatureState State, bool IsDegraded, string Message);
