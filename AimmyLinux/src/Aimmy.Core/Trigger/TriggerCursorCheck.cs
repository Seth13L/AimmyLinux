using Aimmy.Core.Models;

namespace Aimmy.Core.Trigger;

public static class TriggerCursorCheck
{
    public static bool IsCursorInside(Detection detection, float cursorX, float cursorY)
    {
        return cursorX >= detection.Left
            && cursorX <= detection.Right
            && cursorY >= detection.Top
            && cursorY <= detection.Bottom;
    }

    public static bool IsCrosshairInside(Detection detection, int frameWidth, int frameHeight)
    {
        return IsCursorInside(detection, frameWidth / 2f, frameHeight / 2f);
    }
}
