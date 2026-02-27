using AimmyLinux.Config;
using AimmyLinux.Models;
using AimmyLinux.Services.Capture;
using AimmyLinux.Services.Inference;
using AimmyLinux.Services.Input;
using AimmyLinux.Services.Targeting;
using System.Diagnostics;

namespace AimmyLinux.App;

public sealed class AimmyLinuxApp
{
    private readonly AppConfig _config;
    private readonly ICaptureProvider _captureProvider;
    private readonly OnnxDetector _detector;
    private readonly IInputProvider _inputProvider;

    public AimmyLinuxApp(
        AppConfig config,
        ICaptureProvider captureProvider,
        OnnxDetector detector,
        IInputProvider inputProvider)
    {
        _config = config;
        _captureProvider = captureProvider;
        _detector = detector;
        _inputProvider = inputProvider;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            linkedCts.Cancel();
        };

        var token = linkedCts.Token;
        var region = CaptureRegion.Centered(
            _config.DisplayWidth,
            _config.DisplayHeight,
            _config.CaptureWidth,
            _config.CaptureHeight);

        Console.WriteLine("AimmyLinux started.");
        Console.WriteLine($"Model: {_config.ModelPath}");
        Console.WriteLine($"Capture: {region.Width}x{region.Height} at ({region.X},{region.Y})");
        Console.WriteLine($"Input backend: {_config.PreferredInputBackend}, dry-run: {_config.DryRun}");
        Console.WriteLine("Press Ctrl+C to stop.");

        var targetFrameTime = TimeSpan.FromMilliseconds(1000d / Math.Max(1, _config.Fps));
        var loopStopwatch = new Stopwatch();
        var statusStopwatch = Stopwatch.StartNew();
        long frames = 0;

        while (!token.IsCancellationRequested)
        {
            loopStopwatch.Restart();
            try
            {
                using var frame = await _captureProvider.CaptureAsync(region, token).ConfigureAwait(false);
                var detections = _detector.Detect(frame, _config.ConfidenceThreshold);
                var target = TargetSelector.ClosestToCenter(
                    detections,
                    frame.Width / 2f,
                    frame.Height / 2f);

                if (target is Detection selected)
                {
                    var dx = (int)Math.Round((selected.CenterX - (frame.Width / 2f)) * _config.MouseSensitivity);
                    var dy = (int)Math.Round((selected.CenterY - (frame.Height / 2f)) * _config.MouseSensitivity);

                    dx = Math.Clamp(dx, -_config.MaxDeltaPerAxis, _config.MaxDeltaPerAxis);
                    dy = Math.Clamp(dy, -_config.MaxDeltaPerAxis, _config.MaxDeltaPerAxis);

                    if (_config.AimAssistEnabled)
                    {
                        await _inputProvider.MoveRelativeAsync(dx, dy, token).ConfigureAwait(false);
                    }
                }

                frames++;
                if (statusStopwatch.ElapsedMilliseconds >= 1000)
                {
                    Console.WriteLine($"Loop FPS: {frames}, detections: {detections.Count}");
                    frames = 0;
                    statusStopwatch.Restart();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Loop error: {ex.Message}");
                await Task.Delay(250, token).ConfigureAwait(false);
            }

            var remaining = targetFrameTime - loopStopwatch.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(remaining, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        Console.WriteLine("AimmyLinux stopped.");
        return 0;
    }
}
