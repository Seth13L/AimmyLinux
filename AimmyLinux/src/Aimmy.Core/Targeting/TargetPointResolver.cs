using Aimmy.Core.Config;
using Aimmy.Core.Enums;

namespace Aimmy.Core.Targeting;

public static class TargetPointResolver
{
    public static (float X, float Y) Resolve(int frameWidth, int frameHeight, AimmyConfig config, (float X, float Y)? cursorPosition = null)
    {
        if (config.Aim.DetectionAreaType == DetectionAreaType.ClosestToMouse && cursorPosition is { } cursor)
        {
            return cursor;
        }

        return (frameWidth / 2f, frameHeight / 2f);
    }
}
