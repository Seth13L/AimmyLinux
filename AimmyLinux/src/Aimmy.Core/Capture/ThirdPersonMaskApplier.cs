using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Aimmy.Core.Capture;

public static class ThirdPersonMaskApplier
{
    public static void Apply(Image<Rgba32> frame)
    {
        if (frame.Width <= 0 || frame.Height <= 0)
        {
            return;
        }

        var maskedWidth = frame.Width / 2;
        var maskedHeight = frame.Height / 2;
        var startY = frame.Height - maskedHeight;

        if (maskedWidth <= 0 || maskedHeight <= 0)
        {
            return;
        }

        for (var y = startY; y < frame.Height; y++)
        {
            for (var x = 0; x < maskedWidth; x++)
            {
                frame[x, y] = new Rgba32(0, 0, 0, 255);
            }
        }
    }
}
