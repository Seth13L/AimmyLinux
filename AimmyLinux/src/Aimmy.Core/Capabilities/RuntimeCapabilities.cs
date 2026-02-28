using Aimmy.Core.Enums;

namespace Aimmy.Core.Capabilities;

public sealed class RuntimeCapabilities
{
    private readonly Dictionary<string, FeatureCapability> _features = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, FeatureCapability> Features => _features;

    public FeatureCapability Get(string name)
    {
        return _features.TryGetValue(name, out var capability)
            ? capability
            : new FeatureCapability(name, FeatureState.Unavailable, false, "Capability is not registered.");
    }

    public bool IsEnabled(string name)
    {
        return Get(name).State == FeatureState.Enabled;
    }

    public void Set(string name, FeatureState state, bool isDegraded = false, string message = "")
    {
        _features[name] = new FeatureCapability(name, state, isDegraded, message);
    }

    public static RuntimeCapabilities CreateDefault()
    {
        var capabilities = new RuntimeCapabilities();
        capabilities.Set("X11Session", FeatureState.Disabled, false, "Environment not probed yet.");
        capabilities.Set("WaylandAimAssist", FeatureState.Unavailable, false, "Wayland is intentionally unsupported for v1 aim pipeline.");
        capabilities.Set("CaptureBackend", FeatureState.Disabled, false, "Capture backend not selected yet.");
        capabilities.Set("InputBackend", FeatureState.Disabled, false, "Input backend not selected yet.");
        capabilities.Set("Hotkeys", FeatureState.Disabled, true, "Global hotkeys are running in fallback mode.");
        capabilities.Set("Overlay", FeatureState.Disabled, true, "X11 overlay backend not configured yet.");
        capabilities.Set("RuntimeUi", FeatureState.Enabled, false, "Runtime UI host is available.");
        capabilities.Set("StreamGuard", FeatureState.Unavailable, false, "StreamGuard equivalent is unsupported on Linux v1.");
        capabilities.Set("ModelStore", FeatureState.Enabled, false, "Model/config store client available.");
        capabilities.Set("Updater", FeatureState.Enabled, false, "Package-aware update client available.");
        return capabilities;
    }
}
