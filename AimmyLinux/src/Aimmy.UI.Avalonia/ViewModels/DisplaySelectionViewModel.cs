using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.UI.Avalonia.Models;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class DisplaySelectionViewModel
{
    public IReadOnlyList<string> CaptureMethodOptions { get; } = Enum.GetNames<CaptureMethod>();
    public List<DisplayOptionModel> DisplayOptions { get; } = new();
    public string? SelectedDisplayId { get; set; }
    public bool UseDiscoveredDisplay { get; set; }
    public string CaptureMethod { get; set; } = Aimmy.Core.Enums.CaptureMethod.X11Fallback.ToString();
    public string ExternalBackendPreference { get; set; } = "auto";

    public int DisplayWidth { get; set; }
    public int DisplayHeight { get; set; }
    public int DisplayOffsetX { get; set; }
    public int DisplayOffsetY { get; set; }
    public double DpiScaleX { get; set; } = 1.0;
    public double DpiScaleY { get; set; } = 1.0;

    public void Load(AimmyConfig config, IEnumerable<DisplayOptionModel> discoveredDisplays)
    {
        DisplayOptions.Clear();
        DisplayOptions.AddRange(discoveredDisplays);

        DisplayWidth = config.Capture.DisplayWidth;
        DisplayHeight = config.Capture.DisplayHeight;
        DisplayOffsetX = config.Capture.DisplayOffsetX;
        DisplayOffsetY = config.Capture.DisplayOffsetY;
        DpiScaleX = config.Capture.DpiScaleX;
        DpiScaleY = config.Capture.DpiScaleY;
        CaptureMethod = config.Capture.Method.ToString();
        ExternalBackendPreference = config.Capture.ExternalBackendPreference;

        var matched = DisplayOptions.FirstOrDefault(option =>
            option.Width == DisplayWidth &&
            option.Height == DisplayHeight &&
            option.OriginX == DisplayOffsetX &&
            option.OriginY == DisplayOffsetY);

        if (matched is not null)
        {
            SelectedDisplayId = matched.Id;
            UseDiscoveredDisplay = true;
            return;
        }

        var primary = DisplayOptions.FirstOrDefault(option => option.IsPrimary);
        SelectedDisplayId = primary?.Id;
        UseDiscoveredDisplay = false;
    }

    public void Apply(AimmyConfig config)
    {
        if (Enum.TryParse<CaptureMethod>(CaptureMethod, ignoreCase: true, out var captureMethod))
        {
            config.Capture.Method = captureMethod;
        }

        config.Capture.ExternalBackendPreference = ExternalBackendPreference;

        if (UseDiscoveredDisplay)
        {
            var selected = DisplayOptions.FirstOrDefault(option =>
                string.Equals(option.Id, SelectedDisplayId, StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
            {
                config.Capture.DisplayWidth = selected.Width;
                config.Capture.DisplayHeight = selected.Height;
                config.Capture.DisplayOffsetX = selected.OriginX;
                config.Capture.DisplayOffsetY = selected.OriginY;
                config.Capture.DpiScaleX = selected.DpiScaleX;
                config.Capture.DpiScaleY = selected.DpiScaleY;
                return;
            }
        }

        config.Capture.DisplayWidth = DisplayWidth;
        config.Capture.DisplayHeight = DisplayHeight;
        config.Capture.DisplayOffsetX = DisplayOffsetX;
        config.Capture.DisplayOffsetY = DisplayOffsetY;
        config.Capture.DpiScaleX = DpiScaleX;
        config.Capture.DpiScaleY = DpiScaleY;
    }
}
