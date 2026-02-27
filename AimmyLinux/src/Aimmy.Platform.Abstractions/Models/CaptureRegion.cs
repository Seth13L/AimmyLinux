namespace Aimmy.Platform.Abstractions.Models;

public readonly record struct CaptureRegion(int X, int Y, int Width, int Height)
{
    public static CaptureRegion Centered(
        int displayWidth,
        int displayHeight,
        int width,
        int height,
        int displayOffsetX = 0,
        int displayOffsetY = 0)
    {
        var x = displayOffsetX + Math.Max(0, (displayWidth - width) / 2);
        var y = displayOffsetY + Math.Max(0, (displayHeight - height) / 2);
        return new CaptureRegion(x, y, width, height);
    }
}
