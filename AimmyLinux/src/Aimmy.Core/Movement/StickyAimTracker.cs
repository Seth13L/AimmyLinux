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

        if (candidate is null)
        {
            return null;
        }

        if (previous is null)
        {
            return candidate;
        }

        var threshold = config.Aim.StickyAimThreshold;
        var thresholdSq = threshold * threshold;

        var dx = candidate.Value.CenterX - previous.Value.CenterX;
        var dy = candidate.Value.CenterY - previous.Value.CenterY;
        var distanceSq = (dx * dx) + (dy * dy);

        if (distanceSq <= thresholdSq)
        {
            return candidate;
        }

        return previous;
    }
}
