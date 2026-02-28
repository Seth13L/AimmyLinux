namespace Aimmy.Platform.Abstractions.Models;

public sealed record ModelMetadataInfo(
    bool Exists,
    bool IsDynamic,
    int? FixedImageSize,
    IReadOnlyList<string> Classes,
    string Message);
