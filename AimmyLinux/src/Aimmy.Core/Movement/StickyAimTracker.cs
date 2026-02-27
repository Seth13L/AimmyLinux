using Aimmy.Core.Config;
using Aimmy.Core.Models;

namespace Aimmy.Core.Movement;

public static class StickyAimTracker
{
    public static Detection? Resolve(
        Detection? previous,
        Detection? candidate,
        IReadOnlyList<Detection> allCandidates,
        AimmyConfig config)
    {
        if (!config.Aim.StickyAimEnabled)
        {
            return candidate;
        }

        if (previous is null)
        {
            return candidate;
        }

        var nearestToPrevious = FindNearestToPrevious(previous.Value, allCandidates, config);
        if (nearestToPrevious is not null)
        {
            return nearestToPrevious;
        }

        return candidate;
    }

    private static Detection? FindNearestToPrevious(
        Detection previous,
        IReadOnlyList<Detection> allCandidates,
        AimmyConfig config)
    {
        if (allCandidates.Count == 0)
        {
            return null;
        }

        var threshold = config.Aim.StickyAimThreshold;
        var thresholdSq = threshold * threshold;
        var minimumConfidence = config.Model.ConfidenceThreshold;
        var targetClass = config.Model.TargetClass;

        Detection? nearest = null;
        var bestDistanceSq = float.MaxValue;

        foreach (var candidate in allCandidates)
        {
            if (candidate.Confidence < minimumConfidence)
            {
                continue;
            }

            if (targetClass != "Best Confidence" &&
                !string.Equals(targetClass, candidate.ClassName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dx = candidate.CenterX - previous.CenterX;
            var dy = candidate.CenterY - previous.CenterY;
            var distanceSq = (dx * dx) + (dy * dy);

            if (distanceSq > thresholdSq || distanceSq >= bestDistanceSq)
            {
                continue;
            }

            nearest = candidate;
            bestDistanceSq = distanceSq;
        }

        return nearest;
    }
}
