using Aimmy.Core.Config;
using Aimmy.Core.Models;

namespace Aimmy.Core.Prediction;

public sealed class KalmanPredictor : ITargetPredictor
{
    private readonly AimmyConfig _config;
    private double _x;
    private double _y;
    private double _vx;
    private double _vy;
    private double _p00 = 1.0;
    private double _p11 = 1.0;
    private bool _initialized;
    private DateTime _lastUpdate = DateTime.UtcNow;

    public KalmanPredictor(AimmyConfig config)
    {
        _config = config;
    }

    public string Name => "Kalman";

    public Detection Predict(Detection current, DateTime timestamp)
    {
        if (!_initialized)
        {
            _x = current.CenterX;
            _y = current.CenterY;
            _vx = 0;
            _vy = 0;
            _lastUpdate = timestamp;
            _initialized = true;
            return current;
        }

        var dt = Math.Clamp((timestamp - _lastUpdate).TotalSeconds, 0.001, 0.1);

        var predictedX = _x + (_vx * dt);
        var predictedY = _y + (_vy * dt);

        _p00 += 0.1;
        _p11 += 0.1;

        var innovationX = current.CenterX - predictedX;
        var innovationY = current.CenterY - predictedY;

        var gain = _p00 / (_p00 + 0.5);

        _x = predictedX + (gain * innovationX);
        _y = predictedY + (gain * innovationY);

        _vx += (gain * innovationX) / dt;
        _vy += (gain * innovationY) / dt;

        _p00 *= (1 - gain);
        _p11 *= (1 - gain);

        _lastUpdate = timestamp;

        var leadTime = _config.Prediction.KalmanLeadTime;
        var finalX = (float)(_x + (_vx * leadTime));
        var finalY = (float)(_y + (_vy * leadTime));

        return current with { CenterX = finalX, CenterY = finalY };
    }

    public void Reset()
    {
        _x = 0;
        _y = 0;
        _vx = 0;
        _vy = 0;
        _p00 = 1.0;
        _p11 = 1.0;
        _initialized = false;
    }
}
