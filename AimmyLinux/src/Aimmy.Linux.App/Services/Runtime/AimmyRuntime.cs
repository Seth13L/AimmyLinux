using Aimmy.Core.Capabilities;
using Aimmy.Core.Capture;
using Aimmy.Core.Config;
using Aimmy.Core.Diagnostics;
using Aimmy.Core.Models;
using Aimmy.Core.Movement;
using Aimmy.Core.Prediction;
using Aimmy.Core.Targeting;
using Aimmy.Core.Trigger;
using Aimmy.Linux.App.Services.Data;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using System.Diagnostics;

namespace Aimmy.Linux.App.Services.Runtime;

public sealed class AimmyRuntime
{
    private const string AimKeybindId = "Aim Keybind";
    private const string SecondaryAimKeybindId = "Second Aim Keybind";
    private const string DynamicFovKeybindId = "Dynamic FOV Keybind";
    private const string EmergencyStopKeybindId = "Emergency Stop Keybind";
    private const string ModelSwitchKeybindId = "Model Switch Keybind";

    private readonly AimmyConfig _config;
    private readonly RuntimeCapabilities _capabilities;
    private readonly ICaptureBackend _capture;
    private readonly Func<AimmyConfig, IInferenceBackend>? _inferenceFactory;
    private readonly ICursorProvider _cursorProvider;
    private readonly IInputBackend _input;
    private readonly IHotkeyBackend _hotkeys;
    private readonly IOverlayBackend _overlay;
    private readonly ITargetPredictor _predictor;
    private readonly Action<RuntimeSnapshot>? _snapshotCallback;
    private readonly AimEmaSmoother _aimEmaSmoother = new();
    private readonly RuntimeDiagnostics _diagnostics = new();
    private readonly RuntimeAssertionThresholds _runtimeThresholds;
    private readonly CaptureGeometry _captureGeometry;
    private readonly RuntimeDataCollector _dataCollector;
    private readonly Dictionary<string, bool> _hotkeyStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _availableModels = new();

    private IInferenceBackend _inference;
    private Detection? _stickyTarget;
    private DateTime _lastTriggerTime = DateTime.MinValue;
    private int _activeFovSize;
    private int _activeModelIndex;

    public AimmyRuntime(
        AimmyConfig config,
        RuntimeCapabilities capabilities,
        ICaptureBackend capture,
        IInferenceBackend inference,
        ICursorProvider? cursorProvider,
        IInputBackend input,
        IHotkeyBackend hotkeys,
        IOverlayBackend overlay,
        ITargetPredictor predictor,
        Func<AimmyConfig, IInferenceBackend>? inferenceFactory = null,
        Action<RuntimeSnapshot>? snapshotCallback = null)
    {
        _config = config;
        _capabilities = capabilities;
        _capture = capture;
        _inference = inference;
        _inferenceFactory = inferenceFactory;
        _cursorProvider = cursorProvider ?? new NoCursorProvider();
        _input = input;
        _hotkeys = hotkeys;
        _overlay = overlay;
        _predictor = predictor;
        _snapshotCallback = snapshotCallback;
        _captureGeometry = CaptureGeometryResolver.Resolve(config.Capture);
        _dataCollector = new RuntimeDataCollector(config.DataCollection);
        _runtimeThresholds = new RuntimeAssertionThresholds(
            config.Runtime.DiagnosticsMinimumFps,
            config.Runtime.DiagnosticsMaxCaptureP95Ms,
            config.Runtime.DiagnosticsMaxInferenceP95Ms,
            config.Runtime.DiagnosticsMaxLoopP95Ms);
        RefreshModelCatalog();
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await _hotkeys.StartAsync(cancellationToken).ConfigureAwait(false);

        if (_config.Fov.Enabled && _config.Fov.ShowFov)
        {
            _activeFovSize = Math.Max(1, _config.Fov.Size);
            var scaledInitialFov = Math.Max(1, (int)Math.Round(_activeFovSize * _captureGeometry.FovScale, MidpointRounding.AwayFromZero));
            await _overlay.ShowFovAsync(scaledInitialFov, _config.Fov.Style, _config.Fov.Color, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _activeFovSize = Math.Max(1, _config.Fov.Size);
        }

        var region = CaptureRegion.Centered(
            _captureGeometry.DisplayWidth,
            _captureGeometry.DisplayHeight,
            _captureGeometry.CaptureWidth,
            _captureGeometry.CaptureHeight,
            _captureGeometry.DisplayOriginX,
            _captureGeometry.DisplayOriginY);

        Console.WriteLine("Aimmy Linux runtime started.");
        Console.WriteLine($"Model: {_config.Model.ModelPath}");
        Console.WriteLine($"Capture backend: {_capture.Name}");
        Console.WriteLine($"Input backend: {_input.Name}");
        Console.WriteLine($"Hotkey backend: {_hotkeys.Name}");
        Console.WriteLine($"Overlay backend: {_overlay.Name}");
        Console.WriteLine($"Inference provider: {_inference.RuntimeInfo.Provider} ({_inference.RuntimeInfo.Message})");
        Console.WriteLine($"Model switch catalog entries: {_availableModels.Count}");

        foreach (var capability in _capabilities.Features.Values.OrderBy(v => v.Name))
        {
            Console.WriteLine($"Capability[{capability.Name}]: {capability.State} (degraded={capability.IsDegraded}) {capability.Message}");
        }

        Console.WriteLine(
            $"Capture geometry: display={_captureGeometry.DisplayWidth}x{_captureGeometry.DisplayHeight}@({_captureGeometry.DisplayOriginX},{_captureGeometry.DisplayOriginY}) " +
            $"capture={_captureGeometry.CaptureWidth}x{_captureGeometry.CaptureHeight}@({_captureGeometry.CaptureX},{_captureGeometry.CaptureY}) " +
            $"dpi={_captureGeometry.DpiScaleX:F2}x{_captureGeometry.DpiScaleY:F2}");
        if (_config.DataCollection.CollectDataWhilePlaying)
        {
            Console.WriteLine($"Data collection: images={_dataCollector.ImagesDirectory}");
            Console.WriteLine($"Data collection auto-label: {_config.DataCollection.AutoLabelData}");
            if (_config.DataCollection.AutoLabelData)
            {
                Console.WriteLine($"Data collection labels={_dataCollector.LabelsDirectory}");
            }
        }

        var frameInterval = TimeSpan.FromMilliseconds(1000d / Math.Max(1, _config.Runtime.Fps));
        var loopStopwatch = new Stopwatch();

        while (!cancellationToken.IsCancellationRequested)
        {
            loopStopwatch.Restart();

            var hotkeys = SnapshotHotkeys();

            if (hotkeys.EmergencyStopEdge)
            {
                Console.WriteLine("Emergency stop keybind pressed. Stopping runtime.");
                break;
            }

            if (hotkeys.ModelSwitchEdge)
            {
                if (_config.Input.EnableModelSwitchKeybind)
                {
                    await TrySwitchModelAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Console.WriteLine("Model switch keybind pressed but disabled by configuration.");
                }
            }

            try
            {
                await ProcessFrameAsync(region, hotkeys, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Runtime frame error: {ex.Message}");
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }

            loopStopwatch.Stop();
            _diagnostics.AddLoopMs(loopStopwatch.Elapsed.TotalMilliseconds);
            _diagnostics.IncrementFrame();

            var snapshot = _diagnostics.SnapshotAndResetIntervalIfNeeded();
            if (snapshot.HasData)
            {
                Console.WriteLine(
                    $"FPS {snapshot.Fps:F1} | Capture p50/p95 {snapshot.CaptureP50Ms:F2}/{snapshot.CaptureP95Ms:F2} ms | " +
                    $"Inference p50/p95 {snapshot.InferenceP50Ms:F2}/{snapshot.InferenceP95Ms:F2} ms | " +
                    $"Loop p50/p95 {snapshot.LoopP50Ms:F2}/{snapshot.LoopP95Ms:F2} ms");

                if (_config.Runtime.EnableDiagnosticsAssertions)
                {
                    var warnings = RuntimeDiagnosticsAssertions.Evaluate(snapshot, _runtimeThresholds);
                    foreach (var warning in warnings)
                    {
                        Console.WriteLine($"Runtime warning: {warning}");
                    }
                }

                _snapshotCallback?.Invoke(snapshot);
            }

            var delay = frameInterval - loopStopwatch.Elapsed;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        await ShutdownAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine("Aimmy Linux runtime stopped.");
        return 0;
    }

    private async Task ProcessFrameAsync(CaptureRegion region, HotkeySnapshot hotkeys, CancellationToken cancellationToken)
    {
        var captureStopwatch = Stopwatch.StartNew();
        using var frame = await _capture.CaptureAsync(region, cancellationToken).ConfigureAwait(false);
        captureStopwatch.Stop();
        _diagnostics.AddCaptureMs(captureStopwatch.Elapsed.TotalMilliseconds);

        if (_config.Aim.ThirdPersonSupport)
        {
            ThirdPersonMaskApplier.Apply(frame);
        }

        var inferenceStopwatch = Stopwatch.StartNew();
        var detections = _inference.Detect(frame, _config.Model.ConfidenceThreshold);
        inferenceStopwatch.Stop();
        _diagnostics.AddInferenceMs(inferenceStopwatch.Elapsed.TotalMilliseconds);

        var resolvedFovSize = DynamicFovResolver.Resolve(_config, hotkeys.DynamicFovPressed);
        var scaledFovSize = Math.Max(1, (int)Math.Round(resolvedFovSize * _captureGeometry.FovScale, MidpointRounding.AwayFromZero));
        if (_config.Fov.Enabled && _config.Fov.ShowFov && resolvedFovSize != _activeFovSize)
        {
            _activeFovSize = resolvedFovSize;
            await _overlay.ShowFovAsync(scaledFovSize, _config.Fov.Style, _config.Fov.Color, cancellationToken).ConfigureAwait(false);
        }

        var cursorFramePosition = ResolveCursorFramePosition(region, frame.Width, frame.Height);
        var targetPoint = TargetPointResolver.Resolve(frame.Width, frame.Height, _config, cursorFramePosition);
        var candidate = TargetSelector.ClosestToTarget(
            detections,
            targetPoint.X,
            targetPoint.Y,
            _config,
            frame.Width,
            frame.Height,
            scaledFovSize);
        var selected = StickyAimTracker.Resolve(_stickyTarget, candidate, detections, _config);
        _stickyTarget = selected;

        if (selected is null)
        {
            await HandleNoTargetAsync(cancellationToken).ConfigureAwait(false);
            HandleDataCollectionResult(_dataCollector.CollectFrame(frame, null, _config.Aim.ConstantTracking));
            await _overlay.ClearDetectionsAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        HandleDataCollectionResult(_dataCollector.CollectFrame(frame, selected, _config.Aim.ConstantTracking));

        var predicted = _config.Prediction.Enabled
            ? _predictor.Predict(selected.Value, DateTime.UtcNow)
            : selected.Value;

        var aimVector = AimVectorCalculator.Calculate(predicted, _config, frame.Width, frame.Height);
        var (smoothedDx, smoothedDy) = _aimEmaSmoother.Apply(
            aimVector.Dx,
            aimVector.Dy,
            _config.Prediction.EmaSmoothingEnabled,
            _config.Prediction.EmaSmoothingAmount);

        if (ShouldMoveAim(hotkeys))
        {
            await _input.MoveRelativeAsync(smoothedDx, smoothedDy, cancellationToken).ConfigureAwait(false);
        }

        await HandleTriggerAsync(selected.Value, hotkeys, cursorFramePosition, cancellationToken).ConfigureAwait(false);

        if (_config.Overlay.ShowDetectedPlayer)
        {
            await _overlay.ShowDetectionsAsync(new[] { selected.Value }, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void HandleDataCollectionResult(DataCollectionResult result)
    {
        if (result.Saved)
        {
            return;
        }

        if (result.Message is not null && result.Message.StartsWith("Save failed:", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Data collection warning: {result.Message}");
        }
    }

    private bool ShouldMoveAim(HotkeySnapshot hotkeys)
    {
        if (!_config.Aim.Enabled)
        {
            return false;
        }

        if (_config.Aim.ConstantTracking)
        {
            return true;
        }

        return hotkeys.AimPressed || hotkeys.SecondaryAimPressed;
    }

    private async Task HandleNoTargetAsync(CancellationToken cancellationToken)
    {
        if (_config.Trigger.Enabled && _config.Trigger.SprayMode)
        {
            await _input.ReleaseLeftButtonAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleTriggerAsync(
        Detection detection,
        HotkeySnapshot hotkeys,
        (float X, float Y)? cursorFramePosition,
        CancellationToken cancellationToken)
    {
        if (!_config.Trigger.Enabled)
        {
            if (_config.Trigger.SprayMode)
            {
                await _input.ReleaseLeftButtonAsync(cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        var canTrigger = _config.Aim.ConstantTracking || hotkeys.AimPressed;
        if (!canTrigger)
        {
            await _input.ReleaseLeftButtonAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_config.Trigger.CursorCheck)
        {
            if (!cursorFramePosition.HasValue ||
                !TriggerCursorCheck.IsCursorInside(detection, cursorFramePosition.Value.X, cursorFramePosition.Value.Y))
            {
                await _input.ReleaseLeftButtonAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
        }

        if (_config.Trigger.SprayMode)
        {
            await _input.HoldLeftButtonAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var elapsed = (DateTime.UtcNow - _lastTriggerTime).TotalSeconds;
        if (elapsed < _config.Trigger.AutoTriggerDelaySeconds)
        {
            return;
        }

        await _input.ClickAsync(cancellationToken).ConfigureAwait(false);
        _lastTriggerTime = DateTime.UtcNow;
    }

    private HotkeySnapshot SnapshotHotkeys()
    {
        var aimPressed = _hotkeys.IsPressed(AimKeybindId);
        var secondaryAimPressed = _hotkeys.IsPressed(SecondaryAimKeybindId);
        var dynamicFovPressed = _hotkeys.IsPressed(DynamicFovKeybindId);
        var emergencyStopPressed = _hotkeys.IsPressed(EmergencyStopKeybindId);
        var modelSwitchPressed = _hotkeys.IsPressed(ModelSwitchKeybindId);

        var emergencyStopEdge = IsRisingEdge(EmergencyStopKeybindId, emergencyStopPressed);
        var modelSwitchEdge = IsRisingEdge(ModelSwitchKeybindId, modelSwitchPressed);

        _hotkeyStates[AimKeybindId] = aimPressed;
        _hotkeyStates[SecondaryAimKeybindId] = secondaryAimPressed;
        _hotkeyStates[DynamicFovKeybindId] = dynamicFovPressed;
        _hotkeyStates[EmergencyStopKeybindId] = emergencyStopPressed;
        _hotkeyStates[ModelSwitchKeybindId] = modelSwitchPressed;

        return new HotkeySnapshot(
            aimPressed,
            secondaryAimPressed,
            dynamicFovPressed,
            emergencyStopEdge,
            modelSwitchEdge);
    }

    private bool IsRisingEdge(string bindingId, bool currentState)
    {
        var previousState = _hotkeyStates.TryGetValue(bindingId, out var existing) && existing;
        return currentState && !previousState;
    }

    private async Task TrySwitchModelAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_inferenceFactory is null)
        {
            Console.WriteLine("Model switch ignored: inference factory not available.");
            return;
        }

        RefreshModelCatalog();
        if (_availableModels.Count < 2)
        {
            Console.WriteLine("Model switch ignored: at least two model files are required in the runtime catalog.");
            return;
        }

        var previousModelPath = _config.Model.ModelPath;
        var currentIndex = _activeModelIndex;

        for (var step = 1; step <= _availableModels.Count; step++)
        {
            var nextIndex = (currentIndex + step) % _availableModels.Count;
            var candidatePath = _availableModels[nextIndex];
            if (!File.Exists(candidatePath) || string.Equals(candidatePath, previousModelPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                _config.Model.ModelPath = candidatePath;
                var replacement = _inferenceFactory(_config);
                var previousInference = _inference;
                _inference = replacement;
                _activeModelIndex = nextIndex;
                _stickyTarget = null;
                _predictor.Reset();
                _aimEmaSmoother.Reset();
                await previousInference.DisposeAsync().ConfigureAwait(false);

                Console.WriteLine(
                    $"Model switch success: '{Path.GetFileName(previousModelPath)}' -> '{Path.GetFileName(candidatePath)}' " +
                    $"provider={replacement.RuntimeInfo.Provider} fallback={replacement.RuntimeInfo.IsFallback}");
                return;
            }
            catch (Exception ex)
            {
                _config.Model.ModelPath = previousModelPath;
                Console.Error.WriteLine($"Model switch failed for '{candidatePath}': {ex.Message}");
            }
        }

        Console.WriteLine("Model switch requested but no valid alternate model could be loaded.");
    }

    private void RefreshModelCatalog()
    {
        var absoluteCurrentPath = NormalizePath(_config.Model.ModelPath);

        var discovered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddModel(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var normalized = NormalizePath(path);
            if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
            {
                return;
            }

            discovered.Add(normalized);
        }

        AddModel(absoluteCurrentPath);

        foreach (var searchDirectory in ResolveModelSearchDirectories(absoluteCurrentPath))
        {
            if (!Directory.Exists(searchDirectory))
            {
                continue;
            }

            foreach (var modelPath in Directory.EnumerateFiles(searchDirectory, "*.onnx", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                AddModel(modelPath);
            }
        }

        _availableModels.Clear();
        _availableModels.AddRange(discovered);

        _activeModelIndex = _availableModels.FindIndex(path => string.Equals(path, absoluteCurrentPath, StringComparison.OrdinalIgnoreCase));
        if (_activeModelIndex < 0)
        {
            _activeModelIndex = 0;
        }
    }

    private IEnumerable<string> ResolveModelSearchDirectories(string absoluteCurrentPath)
    {
        var searchDirectories = new List<string>();

        var currentModelDirectory = Path.GetDirectoryName(absoluteCurrentPath);
        if (!string.IsNullOrWhiteSpace(currentModelDirectory))
        {
            searchDirectories.Add(currentModelDirectory);
        }

        if (!string.IsNullOrWhiteSpace(_config.Store.LocalModelsDirectory))
        {
            searchDirectories.Add(_config.Store.LocalModelsDirectory);
            searchDirectories.Add(Path.Combine(Environment.CurrentDirectory, _config.Store.LocalModelsDirectory));
            searchDirectories.Add(Path.Combine(AppContext.BaseDirectory, _config.Store.LocalModelsDirectory));
        }

        return searchDirectories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }

    private async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _input.ReleaseLeftButtonAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Ignore shutdown failures.
        }

        await _overlay.ClearDetectionsAsync(cancellationToken).ConfigureAwait(false);
        await _overlay.HideFovAsync(cancellationToken).ConfigureAwait(false);
        await _overlay.DisposeAsync().ConfigureAwait(false);
        await _hotkeys.DisposeAsync().ConfigureAwait(false);
        await _inference.DisposeAsync().ConfigureAwait(false);
        if (_cursorProvider is IDisposable disposableCursor)
        {
            disposableCursor.Dispose();
        }
    }

    private (float X, float Y)? ResolveCursorFramePosition(CaptureRegion region, int frameWidth, int frameHeight)
    {
        if (!_cursorProvider.TryGetPosition(out var screenX, out var screenY))
        {
            return null;
        }

        var maxX = Math.Max(0, frameWidth - 1);
        var maxY = Math.Max(0, frameHeight - 1);
        var frameX = Math.Clamp(screenX - region.X, 0, maxX);
        var frameY = Math.Clamp(screenY - region.Y, 0, maxY);
        return (frameX, frameY);
    }

    private readonly record struct HotkeySnapshot(
        bool AimPressed,
        bool SecondaryAimPressed,
        bool DynamicFovPressed,
        bool EmergencyStopEdge,
        bool ModelSwitchEdge);

    private sealed class NoCursorProvider : ICursorProvider
    {
        public bool TryGetPosition(out int screenX, out int screenY)
        {
            screenX = 0;
            screenY = 0;
            return false;
        }
    }
}
