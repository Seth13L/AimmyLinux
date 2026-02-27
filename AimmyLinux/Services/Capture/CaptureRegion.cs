namespace AimmyLinux.Services.Capture;

public readonly record struct CaptureRegion(int X, int Y, int Width, int Height)
{
    public static CaptureRegion Centered(int displayWidth, int displayHeight, int width, int height)
    {
        var x = Math.Max(0, (displayWidth - width) / 2);
        var y = Math.Max(0, (displayHeight - height) / 2);
        return new CaptureRegion(x, y, width, height);
    }
}
