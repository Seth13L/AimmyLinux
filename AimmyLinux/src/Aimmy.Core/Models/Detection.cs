namespace Aimmy.Core.Models;

public readonly record struct Detection(
    float CenterX,
    float CenterY,
    float Width,
    float Height,
    float Confidence,
    int ClassId,
    string ClassName = "Enemy")
{
    public float Left => CenterX - (Width / 2f);
    public float Top => CenterY - (Height / 2f);
    public float Right => CenterX + (Width / 2f);
    public float Bottom => CenterY + (Height / 2f);
}
