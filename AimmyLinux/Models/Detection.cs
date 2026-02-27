namespace AimmyLinux.Models;

public readonly record struct Detection(
    float CenterX,
    float CenterY,
    float Width,
    float Height,
    float Confidence,
    int ClassId
);
