using Aimmy.Core.Capabilities;
using Aimmy.Core.Diagnostics;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.UI.Avalonia.Models;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class RuntimeStatusViewModel : ObservableObject
{
    private RuntimeSnapshot _snapshot;
    private bool _isRunning;
    private string _statusMessage = "Runtime is stopped.";
    private string _captureBackend = string.Empty;
    private string _inputBackend = string.Empty;
    private string _hotkeyBackend = string.Empty;
    private string _overlayBackend = string.Empty;
    private string _inferenceProvider = string.Empty;
    private string _currentModelPath = string.Empty;

    public List<CapabilityBadgeModel> Capabilities { get; } = new();

    public RuntimeSnapshot Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string CaptureBackend
    {
        get => _captureBackend;
        private set => SetProperty(ref _captureBackend, value);
    }

    public string InputBackend
    {
        get => _inputBackend;
        private set => SetProperty(ref _inputBackend, value);
    }

    public string HotkeyBackend
    {
        get => _hotkeyBackend;
        private set => SetProperty(ref _hotkeyBackend, value);
    }

    public string OverlayBackend
    {
        get => _overlayBackend;
        private set => SetProperty(ref _overlayBackend, value);
    }

    public string InferenceProvider
    {
        get => _inferenceProvider;
        private set => SetProperty(ref _inferenceProvider, value);
    }

    public string CurrentModelPath
    {
        get => _currentModelPath;
        private set => SetProperty(ref _currentModelPath, value);
    }

    public void UpdateCapabilities(RuntimeCapabilities runtimeCapabilities)
    {
        Capabilities.Clear();
        foreach (var item in runtimeCapabilities.Features.Values.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
        {
            Capabilities.Add(new CapabilityBadgeModel(item.Name, item.State, item.IsDegraded, item.Message));
        }
    }

    public void UpdateSnapshot(RuntimeSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public void UpdateHostSnapshot(RuntimeHostSnapshot snapshot)
    {
        IsRunning = snapshot.IsRunning;
        StatusMessage = snapshot.StatusMessage;
        CaptureBackend = snapshot.CaptureBackend;
        InputBackend = snapshot.InputBackend;
        HotkeyBackend = snapshot.HotkeyBackend;
        OverlayBackend = snapshot.OverlayBackend;
        InferenceProvider = snapshot.InferenceProvider;
        CurrentModelPath = snapshot.CurrentModelPath;
        Snapshot = snapshot.RuntimeSnapshot;
    }
}
