using Aimmy.Core.Enums;
using System.Drawing;

namespace Aimmy.Core.Movement;

public static class MovementPaths
{
    private static readonly int[] Permutation = BuildPermutation();

    public static Point Calculate(
        Point start,
        Point end,
        MovementPathStrategy strategy,
        double t,
        Random random,
        int jitterAmount)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        var basePoint = strategy switch
        {
            MovementPathStrategy.CubicBezier => CubicBezier(start, end, t),
            MovementPathStrategy.Exponential => Exponential(start, end, t),
            MovementPathStrategy.Adaptive => Adaptive(start, end, t),
            MovementPathStrategy.PerlinNoise => PerlinNoise(start, end, t),
            _ => Linear(start, end, t)
        };

        if (jitterAmount <= 0)
        {
            return basePoint;
        }

        basePoint.X += random.Next(-jitterAmount, jitterAmount + 1);
        basePoint.Y += random.Next(-jitterAmount, jitterAmount + 1);
        return basePoint;
    }

    private static Point CubicBezier(Point start, Point end, double t)
    {
        var c1 = new Point(start.X + ((end.X - start.X) / 3), start.Y + ((end.Y - start.Y) / 3));
        var c2 = new Point(start.X + ((2 * (end.X - start.X)) / 3), start.Y + ((2 * (end.Y - start.Y)) / 3));

        var u = 1 - t;
        var tt = t * t;
        var uu = u * u;

        var x = (uu * u * start.X) + (3 * uu * t * c1.X) + (3 * u * tt * c2.X) + (tt * t * end.X);
        var y = (uu * u * start.Y) + (3 * uu * t * c1.Y) + (3 * u * tt * c2.Y) + (tt * t * end.Y);
        return new Point((int)x, (int)y);
    }

    private static Point Linear(Point start, Point end, double t)
    {
        var x = start.X + ((end.X - start.X) * t);
        var y = start.Y + ((end.Y - start.Y) * t);
        return new Point((int)x, (int)y);
    }

    private static Point Exponential(Point start, Point end, double t, double exponent = 2.0)
    {
        var x = start.X + ((end.X - start.X) * Math.Pow(t, exponent));
        var y = start.Y + ((end.Y - start.Y) * Math.Pow(t, exponent));
        return new Point((int)x, (int)y);
    }

    private static Point Adaptive(Point start, Point end, double t, double threshold = 100.0)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        return distance < threshold ? Linear(start, end, t) : CubicBezier(start, end, t);
    }

    private static Point PerlinNoise(Point start, Point end, double t, double amplitude = 20.0, double frequency = 0.2)
    {
        var baseX = start.X + ((end.X - start.X) * t);
        var baseY = start.Y + ((end.Y - start.Y) * t);

        var noiseX = Noise(t * frequency, 0) * amplitude;
        var noiseY = Noise(t * frequency, 100) * amplitude;

        double perpX = -(end.Y - start.Y);
        double perpY = end.X - start.X;
        var perpLen = Math.Sqrt((perpX * perpX) + (perpY * perpY));

        if (perpLen > 0)
        {
            perpX /= perpLen;
            perpY /= perpLen;
        }

        return new Point(
            (int)(baseX + (perpX * noiseX) + (noiseY * 0.3)),
            (int)(baseY + (perpY * noiseX) + (noiseY * 0.3)));
    }

    private static double Fade(double value)
    {
        return value * value * value * (value * ((value * 6) - 15) + 10);
    }

    private static double Lerp(double from, double to, double amount)
    {
        return from + (amount * (to - from));
    }

    private static double Grad(int hash, double x, double y)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : h is 12 or 14 ? x : 0;
        return (((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v));
    }

    private static double Noise(double x, double y)
    {
        var intX = (int)Math.Floor(x) & 255;
        var intY = (int)Math.Floor(y) & 255;

        x -= Math.Floor(x);
        y -= Math.Floor(y);

        var u = Fade(x);
        var v = Fade(y);

        var a = Permutation[intX] + intY;
        var aa = Permutation[a];
        var ab = Permutation[a + 1];
        var b = Permutation[intX + 1] + intY;
        var ba = Permutation[b];
        var bb = Permutation[b + 1];

        return Lerp(
            Lerp(Grad(Permutation[aa], x, y), Grad(Permutation[ba], x - 1, y), u),
            Lerp(Grad(Permutation[ab], x, y - 1), Grad(Permutation[bb], x - 1, y - 1), u),
            v);
    }

    private static int[] BuildPermutation()
    {
        var p = Enumerable.Range(0, 256).ToArray();
        var random = new Random(1337);

        for (var i = p.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }

        var result = new int[512];
        for (var i = 0; i < 512; i++)
        {
            result[i] = p[i & 255];
        }

        return result;
    }
}
