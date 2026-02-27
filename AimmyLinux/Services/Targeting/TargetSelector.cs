using AimmyLinux.Models;

namespace AimmyLinux.Services.Targeting;

public static class TargetSelector
{
    public static Detection? ClosestToCenter(IReadOnlyList<Detection> detections, float centerX, float centerY)
    {
        if (detections.Count == 0)
        {
            return null;
        }

        Detection? best = null;
        var bestDistance = float.MaxValue;

        foreach (var detection in detections)
        {
            var dx = detection.CenterX - centerX;
            var dy = detection.CenterY - centerY;
            var distance = (dx * dx) + (dy * dy);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = detection;
            }
        }

        return best;
    }
}
