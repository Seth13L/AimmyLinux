namespace Aimmy.Platform.Abstractions.Models;

public sealed record ModelStoreEntry(
    string Name,
    string DownloadUrl,
    string Type);
