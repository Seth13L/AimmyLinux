using Aimmy.Core.Capabilities;
using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.UI.Avalonia.Models;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class ConfigurationEditorViewModel
{
    public DisplaySelectionViewModel DisplaySelection { get; } = new();
    public InputSettingsViewModel InputSettings { get; } = new();
    public RuntimeSettingsViewModel RuntimeSettings { get; } = new();
    public AimSettingsViewModel AimSettings { get; } = new();
    public PredictionSettingsViewModel PredictionSettings { get; } = new();
    public TriggerSettingsViewModel TriggerSettings { get; } = new();
    public FovSettingsViewModel FovSettings { get; } = new();
    public OverlaySettingsViewModel OverlaySettings { get; } = new();
    public DataCollectionSettingsViewModel DataCollection { get; } = new();
    public RuntimeStatusViewModel RuntimeStatus { get; } = new();

    public bool DisplaySectionEnabled { get; private set; } = true;
    public bool InputSectionEnabled { get; private set; } = true;
    public bool OverlaySectionEnabled { get; private set; } = true;
    public string DisplaySectionNotice { get; private set; } = string.Empty;
    public string InputSectionNotice { get; private set; } = string.Empty;
    public string OverlaySectionNotice { get; private set; } = string.Empty;

    public void Load(
        AimmyConfig config,
        IEnumerable<DisplayInfo> discoveredDisplays,
        RuntimeCapabilities? runtimeCapabilities = null)
    {
        var capabilities = runtimeCapabilities ?? RuntimeCapabilities.CreateDefault();
        RuntimeStatus.UpdateCapabilities(capabilities);

        var x11SessionCapability = capabilities.Get("X11Session");
        var captureCapability = capabilities.Get("CaptureBackend");
        var inputCapability = capabilities.Get("InputBackend");
        var overlayCapability = capabilities.Get("Overlay");

        DisplaySectionEnabled = x11SessionCapability.State == FeatureState.Enabled &&
                                captureCapability.State != FeatureState.Unavailable;
        InputSectionEnabled = inputCapability.State != FeatureState.Unavailable;
        OverlaySectionEnabled = overlayCapability.State != FeatureState.Unavailable;

        DisplaySectionNotice = BuildSectionNotice(
            "Display",
            DisplaySectionEnabled,
            captureCapability,
            "Display controls are disabled because capture/display capabilities are unavailable.");
        InputSectionNotice = BuildSectionNotice(
            "Input",
            InputSectionEnabled,
            inputCapability,
            "Input controls are disabled because no compatible input backend was detected.");
        OverlaySectionNotice = BuildSectionNotice(
            "Overlay",
            OverlaySectionEnabled,
            overlayCapability,
            "Overlay controls are disabled because no compatible overlay backend was detected.");

        var options = discoveredDisplays.Select(DisplayOptionModel.FromDisplayInfo);
        DisplaySelection.Load(config, options);
        InputSettings.Load(config);
        RuntimeSettings.Load(config);
        AimSettings.Load(config);
        PredictionSettings.Load(config);
        TriggerSettings.Load(config);
        FovSettings.Load(config);
        OverlaySettings.Load(config);
        DataCollection.Load(config);
    }

    public void Apply(AimmyConfig config)
    {
        DisplaySelection.Apply(config);
        InputSettings.Apply(config);
        RuntimeSettings.Apply(config);
        AimSettings.Apply(config);
        PredictionSettings.Apply(config);
        TriggerSettings.Apply(config);
        FovSettings.Apply(config);
        OverlaySettings.Apply(config);
        DataCollection.Apply(config);
        config.Normalize();
    }

    private static string BuildSectionNotice(
        string sectionName,
        bool enabled,
        FeatureCapability capability,
        string unavailableFallback)
    {
        if (!enabled)
        {
            return string.IsNullOrWhiteSpace(capability.Message)
                ? unavailableFallback
                : $"{sectionName}: {capability.Message}";
        }

        if (capability.IsDegraded)
        {
            return string.IsNullOrWhiteSpace(capability.Message)
                ? $"{sectionName} is running in degraded mode."
                : $"{sectionName}: {capability.Message}";
        }

        return string.IsNullOrWhiteSpace(capability.Message)
            ? $"{sectionName} capability is available."
            : $"{sectionName}: {capability.Message}";
    }
}
