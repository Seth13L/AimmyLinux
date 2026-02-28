using Aimmy.Core.Diagnostics;

namespace Aimmy.Platform.Abstractions.Models;

public readonly record struct RuntimeHostSnapshot(
    bool IsRunning,
    RuntimeSnapshot RuntimeSnapshot,
    string CaptureBackend,
    string InputBackend,
    string HotkeyBackend,
    string OverlayBackend,
    string InferenceProvider,
    string CurrentModelPath,
    string StatusMessage,
    DateTime LastUpdatedUtc)
{
    public static RuntimeHostSnapshot Empty => new(
        IsRunning: false,
        RuntimeSnapshot.Empty,
        CaptureBackend: string.Empty,
        InputBackend: string.Empty,
        HotkeyBackend: string.Empty,
        OverlayBackend: string.Empty,
        InferenceProvider: string.Empty,
        CurrentModelPath: string.Empty,
        StatusMessage: "Runtime is stopped.",
        LastUpdatedUtc: DateTime.MinValue);
}
