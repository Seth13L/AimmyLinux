using Aimmy.Core.Config;
using Aimmy.Core.Models;

namespace Aimmy.Core.Targeting;

public static class TargetSelector
{
    public static Detection? ClosestToTarget(
        IReadOnlyList<Detection> detections,
        float targetX,
        float targetY,
        AimmyConfig config,
        int frameWidth,
        int frameHeight,
        int? fovSizeOverride = null)
    {
        if (detections.Count == 0)
        {
            return null;
        }

        var minConfidence = config.Model.ConfidenceThreshold;
        var fovEnabled = config.Fov.Enabled;
        var fovSize = Math.Max(1, fovSizeOverride ?? config.Fov.Size);

        var fovHalf = fovSize / 2f;
        var fovLeft = (frameWidth / 2f) - fovHalf;
        var fovRight = (frameWidth / 2f) + fovHalf;
        var fovTop = (frameHeight / 2f) - fovHalf;
        var fovBottom = (frameHeight / 2f) + fovHalf;

        Detection? best = null;
        var bestDistance = float.MaxValue;

        foreach (var detection in detections)
        {
            if (detection.Confidence < minConfidence)
            {
                continue;
            }

            if (config.Model.TargetClass != "Best Confidence" && !string.Equals(config.Model.TargetClass, detection.ClassName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (fovEnabled)
            {
                if (detection.Left < fovLeft || detection.Right > fovRight || detection.Top < fovTop || detection.Bottom > fovBottom)
                {
                    continue;
                }
            }

            var dx = detection.CenterX - targetX;
            var dy = detection.CenterY - targetY;
            var distance = (dx * dx) + (dy * dy);

            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            best = detection;
        }

        return best;
    }
}
