using Aimmy.Core.Config;
using Aimmy.Core.Models;

namespace Aimmy.Core.Prediction;

public sealed class ShalloePredictor : ITargetPredictor
{
    private readonly AimmyConfig _config;
    private readonly Queue<float> _vx = new();
    private readonly Queue<float> _vy = new();
    private Detection? _previous;
    private const int MaxHistory = 5;

    public ShalloePredictor(AimmyConfig config)
    {
        _config = config;
    }

    public string Name => "Shalloe";

    public Detection Predict(Detection current, DateTime timestamp)
    {
        if (_previous is null)
        {
            _previous = current;
            return current;
        }

        var velocityX = current.CenterX - _previous.Value.CenterX;
        var velocityY = current.CenterY - _previous.Value.CenterY;

        Push(_vx, velocityX);
        Push(_vy, velocityY);

        var avgX = _vx.Count == 0 ? 0 : _vx.Average();
        var avgY = _vy.Count == 0 ? 0 : _vy.Average();
        var multiplier = (float)_config.Prediction.ShalloeLeadMultiplier;

        _previous = current;

        return current with
        {
            CenterX = current.CenterX + (avgX * multiplier),
            CenterY = current.CenterY + (avgY * multiplier)
        };
    }

    public void Reset()
    {
        _previous = null;
        _vx.Clear();
        _vy.Clear();
    }

    private static void Push(Queue<float> queue, float value)
    {
        queue.Enqueue(value);
        while (queue.Count > MaxHistory)
        {
            queue.Dequeue();
        }
    }
}
