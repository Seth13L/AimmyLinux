namespace Aimmy.Core.Movement;

public sealed class AimEmaSmoother
{
    private double _emaX;
    private double _emaY;
    private bool _initialized;

    public (int Dx, int Dy) Apply(int dx, int dy, bool enabled, double smoothingAmount)
    {
        if (!enabled)
        {
            return (dx, dy);
        }

        var alpha = Math.Clamp(smoothingAmount, 0.01, 1.0);
        if (!_initialized)
        {
            _initialized = true;
            _emaX = dx;
            _emaY = dy;
            return (dx, dy);
        }

        _emaX = (dx * alpha) + (_emaX * (1.0 - alpha));
        _emaY = (dy * alpha) + (_emaY * (1.0 - alpha));
        return ((int)Math.Round(_emaX), (int)Math.Round(_emaY));
    }

    public void Reset()
    {
        _emaX = 0;
        _emaY = 0;
        _initialized = false;
    }
}
