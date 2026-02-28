using Aimmy.Core.Config;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class FovSettingsViewModel
{
    public bool Enabled { get; set; }
    public bool ShowFov { get; set; }
    public int Size { get; set; }
    public int DynamicSize { get; set; }
    public string Style { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public void Load(AimmyConfig config)
    {
        Enabled = config.Fov.Enabled;
        ShowFov = config.Fov.ShowFov;
        Size = config.Fov.Size;
        DynamicSize = config.Fov.DynamicSize;
        Style = config.Fov.Style;
        Color = config.Fov.Color;
    }

    public void Apply(AimmyConfig config)
    {
        config.Fov.Enabled = Enabled;
        config.Fov.ShowFov = ShowFov;
        config.Fov.Size = Size;
        config.Fov.DynamicSize = DynamicSize;
        config.Fov.Style = Style;
        config.Fov.Color = Color;
    }
}
