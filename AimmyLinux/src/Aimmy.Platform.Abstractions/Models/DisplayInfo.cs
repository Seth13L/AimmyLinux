namespace Aimmy.Platform.Abstractions.Models;

public sealed record DisplayInfo(
    string Id,
    string Name,
    bool IsPrimary,
    int OriginX,
    int OriginY,
    int Width,
    int Height,
    float DpiScaleX = 1f,
    float DpiScaleY = 1f);
