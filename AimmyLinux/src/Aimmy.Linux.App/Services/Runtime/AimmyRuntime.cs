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
    private readonly AimmyConfig _config;
    private readonly RuntimeCapabilities _capabilities;
    private readonly ICaptureBackend _capture;
    private readonly IInferenceBackend _inference;
    private readonly IInputBackend _input;
    private readonly IHotkeyBackend _hotkeys;
    private readonly IOverlayBackend _overlay;
    private readonly ITargetPredictor _predictor;
    private readonly RuntimeDiagnostics _diagnostics = new();
    private readonly RuntimeAssertionThresholds _runtimeThresholds;
    private readonly CaptureGeometry _captureGeometry;
    private readonly RuntimeDataCollector _dataCollector;

    private Detection? _stickyTarget;
    private DateTime _lastTriggerTime = DateTime.MinValue;
    private int _activeFovSize;

    public AimmyRuntime(
        AimmyConfig config,
        RuntimeCapabilities capabilities,
        ICaptureBackend capture,
        IInferenceBackend inference,
        IInputBackend input,
        IHotkeyBackend hotkeys,
        IOverlayBackend overlay,
        ITargetPredictor predictor)
    {
        _config = config;
        _capabilities = capabilities;
        _capture = capture;
        _inference = inference;
        _input = input;
        _hotkeys = hotkeys;
        _overlay = overlay;
        _predictor = predictor;
        _captureGeometry = CaptureGeometryResolver.Resolve(config.Capture);
        _dataCollector = new RuntimeDataCollector(config.DataCollection);
        _runtimeThresholds = new RuntimeAssertionThresholds(
            config.Runtime.DiagnosticsMinimumFps,
            config.Runtime.DiagnosticsMaxCaptureP95Ms,
            config.Runtime.DiagnosticsMaxInferenceP95Ms,
            config.Runtime.DiagnosticsMaxLoopP95Ms);
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

            if (_hotkeys.IsPressed("Emergency Stop Keybind"))
            {
                Console.WriteLine("Emergency stop keybind pressed. Stopping runtime.");
                break;
            }

            try
            {
                await ProcessFrameAsync(region, cancellationToken).ConfigureAwait(false);
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

    private async Task ProcessFrameAsync(CaptureRegion region, CancellationToken cancellationToken)
    {
        var captureStopwatch = Stopwatch.StartNew();
        using var frame = await _capture.CaptureAsync(region, cancellationToken).ConfigureAwait(false);
        captureStopwatch.Stop();
        _diagnostics.AddCaptureMs(captureStopwatch.Elapsed.TotalMilliseconds);

        var inferenceStopwatch = Stopwatch.StartNew();
        var detections = _inference.Detect(frame, _config.Model.ConfidenceThreshold);
        inferenceStopwatch.Stop();
        _diagnostics.AddInferenceMs(inferenceStopwatch.Elapsed.TotalMilliseconds);

        var resolvedFovSize = DynamicFovResolver.Resolve(_config, _hotkeys.IsPressed("Dynamic FOV Keybind"));
        var scaledFovSize = Math.Max(1, (int)Math.Round(resolvedFovSize * _captureGeometry.FovScale, MidpointRounding.AwayFromZero));
        if (_config.Fov.Enabled && _config.Fov.ShowFov && resolvedFovSize != _activeFovSize)
        {
            _activeFovSize = resolvedFovSize;
            await _overlay.ShowFovAsync(scaledFovSize, _config.Fov.Style, _config.Fov.Color, cancellationToken).ConfigureAwait(false);
        }

        var targetPoint = TargetPointResolver.Resolve(frame.Width, frame.Height, _config);
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

        if (ShouldMoveAim())
        {
            await _input.MoveRelativeAsync(aimVector.Dx, aimVector.Dy, cancellationToken).ConfigureAwait(false);
        }

        await HandleTriggerAsync(selected.Value, frame.Width, frame.Height, cancellationToken).ConfigureAwait(false);

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

    private bool ShouldMoveAim()
    {
        if (!_config.Aim.Enabled)
        {
            return false;
        }

        if (_config.Aim.ConstantTracking)
        {
            return true;
        }

        return _hotkeys.IsPressed("Aim Keybind") || _hotkeys.IsPressed("Second Aim Keybind");
    }

    private async Task HandleNoTargetAsync(CancellationToken cancellationToken)
    {
        if (_config.Trigger.Enabled && _config.Trigger.SprayMode)
        {
            await _input.ReleaseLeftButtonAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleTriggerAsync(Detection detection, int frameWidth, int frameHeight, CancellationToken cancellationToken)
    {
        if (!_config.Trigger.Enabled)
        {
            if (_config.Trigger.SprayMode)
            {
                await _input.ReleaseLeftButtonAsync(cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        var canTrigger = _config.Aim.ConstantTracking || _hotkeys.IsPressed("Aim Keybind");
        if (!canTrigger)
        {
            await _input.ReleaseLeftButtonAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_config.Trigger.CursorCheck && !TriggerCursorCheck.IsCrosshairInside(detection, frameWidth, frameHeight))
        {
            await _input.ReleaseLeftButtonAsync(cancellationToken).ConfigureAwait(false);
            return;
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
    }
}
