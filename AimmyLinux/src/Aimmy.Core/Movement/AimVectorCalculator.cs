using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Core.Models;
using System.Drawing;

namespace Aimmy.Core.Movement;

public static class AimVectorCalculator
{
    private static readonly Random JitterRandom = new();

    public static AimVector Calculate(Detection detection, AimmyConfig config, int frameWidth, int frameHeight)
    {
        var targetX = ComputeTargetX(detection, config);
        var targetY = ComputeTargetY(detection, config);

        var centerX = frameWidth / 2;
        var centerY = frameHeight / 2;

        var desiredX = targetX - centerX;
        var desiredY = targetY - centerY;

        var pathEnd = MovementPaths.Calculate(
            Point.Empty,
            new Point(desiredX, desiredY),
            config.Aim.MovementPath,
            1 - config.Aim.MouseSensitivity,
            JitterRandom,
            config.Aim.MouseJitter);

        var dx = Math.Clamp(pathEnd.X, -config.Aim.MaxDeltaPerAxis, config.Aim.MaxDeltaPerAxis);
        var dy = Math.Clamp(pathEnd.Y, -config.Aim.MaxDeltaPerAxis, config.Aim.MaxDeltaPerAxis);

        return new AimVector(dx, dy, targetX, targetY);
    }

    private static int ComputeTargetX(Detection detection, AimmyConfig config)
    {
        if (config.Aim.XAxisPercentageAdjustment)
        {
            var x = detection.Left + (detection.Width * (float)(config.Aim.XOffsetPercent / 100d));
            return (int)Math.Round(x);
        }

        return (int)Math.Round(detection.CenterX + config.Aim.XOffset);
    }

    private static int ComputeTargetY(Detection detection, AimmyConfig config)
    {
        if (config.Aim.YAxisPercentageAdjustment)
        {
            var y = detection.Top + detection.Height - (detection.Height * (float)(config.Aim.YOffsetPercent / 100d));
            return (int)Math.Round(y + config.Aim.YOffset);
        }

        float alignedY = config.Aim.AimingBoundariesAlignment switch
        {
            AimingBoundariesAlignment.Top => detection.Top,
            AimingBoundariesAlignment.Bottom => detection.Bottom,
            _ => detection.CenterY
        };

        return (int)Math.Round(alignedY + config.Aim.YOffset);
    }
}
