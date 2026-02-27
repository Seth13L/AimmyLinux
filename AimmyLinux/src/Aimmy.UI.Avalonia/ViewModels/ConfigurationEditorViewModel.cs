using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.UI.Avalonia.Models;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class ConfigurationEditorViewModel
{
    public DisplaySelectionViewModel DisplaySelection { get; } = new();
    public DataCollectionSettingsViewModel DataCollection { get; } = new();

    public void Load(AimmyConfig config, IEnumerable<DisplayInfo> discoveredDisplays)
    {
        var options = discoveredDisplays.Select(DisplayOptionModel.FromDisplayInfo);
        DisplaySelection.Load(config, options);
        DataCollection.Load(config);
    }

    public void Apply(AimmyConfig config)
    {
        DisplaySelection.Apply(config);
        DataCollection.Apply(config);
        config.Normalize();
    }
}
