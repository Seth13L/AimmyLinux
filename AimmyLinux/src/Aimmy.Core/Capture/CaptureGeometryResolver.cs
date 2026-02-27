using Aimmy.Core.Config;

namespace Aimmy.Core.Capture;

public readonly record struct CaptureGeometry(
    int DisplayOriginX,
    int DisplayOriginY,
    int DisplayWidth,
    int DisplayHeight,
    int CaptureX,
    int CaptureY,
    int CaptureWidth,
    int CaptureHeight,
    float DpiScaleX,
    float DpiScaleY)
{
    public float FovScale => MathF.Max(DpiScaleX, DpiScaleY);
}

public static class CaptureGeometryResolver
{
    public static CaptureGeometry Resolve(CaptureSettings settings)
    {
        var dpiScaleX = (float)Math.Clamp(settings.DpiScaleX, 0.25, 4.0);
        var dpiScaleY = (float)Math.Clamp(settings.DpiScaleY, 0.25, 4.0);

        var displayWidth = ScaleDimension(settings.DisplayWidth, dpiScaleX);
        var displayHeight = ScaleDimension(settings.DisplayHeight, dpiScaleY);
        var captureWidth = Math.Clamp(ScaleDimension(settings.Width, dpiScaleX), 1, displayWidth);
        var captureHeight = Math.Clamp(ScaleDimension(settings.Height, dpiScaleY), 1, displayHeight);

        var displayOriginX = settings.DisplayOffsetX;
        var displayOriginY = settings.DisplayOffsetY;
        var captureX = displayOriginX + Math.Max(0, (displayWidth - captureWidth) / 2);
        var captureY = displayOriginY + Math.Max(0, (displayHeight - captureHeight) / 2);

        return new CaptureGeometry(
            DisplayOriginX: displayOriginX,
            DisplayOriginY: displayOriginY,
            DisplayWidth: displayWidth,
            DisplayHeight: displayHeight,
            CaptureX: captureX,
            CaptureY: captureY,
            CaptureWidth: captureWidth,
            CaptureHeight: captureHeight,
            DpiScaleX: dpiScaleX,
            DpiScaleY: dpiScaleY);
    }

    private static int ScaleDimension(int value, float scale)
    {
        return Math.Max(1, (int)Math.Round(value * scale, MidpointRounding.AwayFromZero));
    }
}
