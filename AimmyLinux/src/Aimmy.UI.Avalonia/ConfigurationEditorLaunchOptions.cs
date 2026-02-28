using Aimmy.Core.Config;
using Aimmy.Core.Capabilities;
using Aimmy.Platform.Abstractions.Models;

namespace Aimmy.UI.Avalonia;

public sealed class ConfigurationEditorLaunchOptions
{
    public required AimmyConfig Config { get; init; }
    public required IReadOnlyList<DisplayInfo> Displays { get; init; }
    public required RuntimeCapabilities RuntimeCapabilities { get; init; }
    public required string ConfigPath { get; init; }
    public required Action<AimmyConfig> SaveConfigCallback { get; init; }
}
