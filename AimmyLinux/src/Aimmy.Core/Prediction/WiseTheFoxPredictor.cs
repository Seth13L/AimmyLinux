using Aimmy.Core.Config;
using Aimmy.Core.Models;

namespace Aimmy.Core.Prediction;

public sealed class WiseTheFoxPredictor : ITargetPredictor
{
    private readonly AimmyConfig _config;
    private double _emaX;
    private double _emaY;
    private double _velocityX;
    private double _velocityY;
    private bool _initialized;
    private DateTime _last = DateTime.UtcNow;

    public WiseTheFoxPredictor(AimmyConfig config)
    {
        _config = config;
    }

    public string Name => "WiseTheFox";

    public Detection Predict(Detection current, DateTime timestamp)
    {
        if (!_initialized)
        {
            _emaX = current.CenterX;
            _emaY = current.CenterY;
            _velocityX = 0;
            _velocityY = 0;
            _initialized = true;
            _last = timestamp;
            return current;
        }

        const double alpha = 0.5;
        var dt = Math.Clamp((timestamp - _last).TotalSeconds, 0.001, 0.1);

        var prevX = _emaX;
        var prevY = _emaY;

        _emaX = (alpha * current.CenterX) + ((1.0 - alpha) * _emaX);
        _emaY = (alpha * current.CenterY) + ((1.0 - alpha) * _emaY);

        var newVelocityX = (_emaX - prevX) / dt;
        var newVelocityY = (_emaY - prevY) / dt;

        _velocityX = (alpha * newVelocityX) + ((1.0 - alpha) * _velocityX);
        _velocityY = (alpha * newVelocityY) + ((1.0 - alpha) * _velocityY);

        _last = timestamp;

        var lead = _config.Prediction.WiseTheFoxLeadTime;
        return current with
        {
            CenterX = (float)(_emaX + (_velocityX * lead)),
            CenterY = (float)(_emaY + (_velocityY * lead))
        };
    }

    public void Reset()
    {
        _emaX = 0;
        _emaY = 0;
        _velocityX = 0;
        _velocityY = 0;
        _initialized = false;
    }
}
