using Aimmy.Platform.Abstractions.Models;

namespace Aimmy.UI.Avalonia.Models;

public sealed record DisplayOptionModel(
    string Id,
    string Label,
    bool IsPrimary,
    int OriginX,
    int OriginY,
    int Width,
    int Height,
    float DpiScaleX,
    float DpiScaleY)
{
    public static DisplayOptionModel FromDisplayInfo(DisplayInfo info)
    {
        var prefix = info.IsPrimary ? "[Primary] " : string.Empty;
        var label = $"{prefix}{info.Name} ({info.Width}x{info.Height} @ {info.OriginX},{info.OriginY})";

        return new DisplayOptionModel(
            info.Id,
            label,
            info.IsPrimary,
            info.OriginX,
            info.OriginY,
            info.Width,
            info.Height,
            info.DpiScaleX,
            info.DpiScaleY);
    }
}
