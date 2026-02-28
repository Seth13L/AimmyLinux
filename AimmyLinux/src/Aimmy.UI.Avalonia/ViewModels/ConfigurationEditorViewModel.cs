using Aimmy.Core.Config;
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

    public void Load(AimmyConfig config, IEnumerable<DisplayInfo> discoveredDisplays)
    {
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
}
