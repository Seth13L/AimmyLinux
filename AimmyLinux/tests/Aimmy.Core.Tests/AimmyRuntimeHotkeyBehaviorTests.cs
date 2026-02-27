using Aimmy.Core.Capabilities;
using Aimmy.Core.Config;
using Aimmy.Core.Models;
using Aimmy.Core.Prediction;
using Aimmy.Linux.App.Services.Runtime;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class AimmyRuntimeHotkeyBehaviorTests
{
    [Fact]
    public async Task RunAsync_AimMovementFollowsHoldSemantics()
    {
        var config = AimmyConfig.CreateDefault();
        config.Runtime.Fps = 240;
        config.Runtime.EnableDiagnosticsAssertions = false;
        config.Aim.Enabled = true;
        config.Aim.ConstantTracking = false;
        config.Trigger.Enabled = false;

        var detection = new Detection(420f, 320f, 80f, 100f, 0.95f, 0, "enemy");
        var capture = new StaticCaptureBackend();
        var inference = new StaticInferenceBackend(new[] { detection });
        var input = new RecordingInputBackend();
        var hotkeys = new ScriptedHotkeyBackend((bindingId, queryIndex) => bindingId switch
        {
            "Aim Keybind" => queryIndex < 2,
            "Emergency Stop Keybind" => queryIndex >= 5,
            _ => false
        });
        var overlay = new NoopOverlayBackend();
        var predictor = new PassThroughPredictor();
        var runtime = new AimmyRuntime(
            config,
            RuntimeCapabilities.CreateDefault(),
            capture,
            inference,
            input,
            hotkeys,
            overlay,
            predictor);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var exitCode = await runtime.RunAsync(cts.Token);

        Assert.Equal(0, exitCode);
        Assert.InRange(input.MoveCount, 1, 2);
    }

    [Fact]
    public async Task RunAsync_EmergencyStop_ReleasesHeldInputReliably()
    {
        var config = AimmyConfig.CreateDefault();
        config.Runtime.Fps = 240;
        config.Runtime.EnableDiagnosticsAssertions = false;
        config.Aim.Enabled = true;
        config.Aim.ConstantTracking = true;
        config.Trigger.Enabled = true;
        config.Trigger.SprayMode = true;

        var detection = new Detection(420f, 320f, 80f, 100f, 0.95f, 0, "enemy");
        var capture = new StaticCaptureBackend();
        var inference = new StaticInferenceBackend(new[] { detection });
        var input = new RecordingInputBackend();
        var hotkeys = new ScriptedHotkeyBackend((bindingId, queryIndex) => bindingId switch
        {
            "Emergency Stop Keybind" => queryIndex >= 1,
            _ => false
        });
        var overlay = new NoopOverlayBackend();
        var predictor = new PassThroughPredictor();
        var runtime = new AimmyRuntime(
            config,
            RuntimeCapabilities.CreateDefault(),
            capture,
            inference,
            input,
            hotkeys,
            overlay,
            predictor);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var exitCode = await runtime.RunAsync(cts.Token);

        Assert.Equal(0, exitCode);
        Assert.True(input.HoldCount >= 1, "Spray mode should hold left button before emergency stop.");
        Assert.True(input.ReleaseCount >= 1, "Emergency stop shutdown must release held left button.");
        Assert.Contains("hold", input.Operations);
        Assert.Contains("release", input.Operations);
        Assert.True(input.LastOperationIndex("release") > input.FirstOperationIndex("hold"));
    }

    [Fact]
    public async Task RunAsync_ModelSwitch_IsEdgeTriggeredAndHotSwapsOnce()
    {
        var modelDirectory = Path.Combine(Path.GetTempPath(), "aimmy-runtime-modelswitch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(modelDirectory);

        try
        {
            var modelA = Path.Combine(modelDirectory, "model-a.onnx");
            var modelB = Path.Combine(modelDirectory, "model-b.onnx");
            await File.WriteAllTextAsync(modelA, "fake-a");
            await File.WriteAllTextAsync(modelB, "fake-b");

            var config = AimmyConfig.CreateDefault();
            config.Runtime.Fps = 240;
            config.Runtime.EnableDiagnosticsAssertions = false;
            config.Aim.Enabled = false;
            config.Trigger.Enabled = false;
            config.Model.ModelPath = modelA;
            config.Store.LocalModelsDirectory = modelDirectory;

            var detection = new Detection(420f, 320f, 80f, 100f, 0.95f, 0, "enemy");
            var capture = new StaticCaptureBackend();
            var initialInference = new TrackingInferenceBackend("initial", new[] { detection });
            var createdModelPaths = new List<string>();
            var input = new RecordingInputBackend();
            var hotkeys = new ScriptedHotkeyBackend((bindingId, queryIndex) => bindingId switch
            {
                "Model Switch Keybind" => queryIndex < 3,
                "Emergency Stop Keybind" => queryIndex >= 4,
                _ => false
            });
            var overlay = new NoopOverlayBackend();
            var predictor = new PassThroughPredictor();

            IInferenceBackend Factory(AimmyConfig currentConfig)
            {
                createdModelPaths.Add(Path.GetFullPath(currentConfig.Model.ModelPath));
                return new TrackingInferenceBackend("replacement", new[] { detection });
            }

            var runtime = new AimmyRuntime(
                config,
                RuntimeCapabilities.CreateDefault(),
                capture,
                initialInference,
                input,
                hotkeys,
                overlay,
                predictor,
                Factory);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var exitCode = await runtime.RunAsync(cts.Token);

            Assert.Equal(0, exitCode);
            Assert.Single(createdModelPaths);
            Assert.Equal(Path.GetFullPath(modelB), Path.GetFullPath(config.Model.ModelPath));
            Assert.True(initialInference.Disposed, "Previous inference backend should be disposed after model hot-swap.");
        }
        finally
        {
            try
            {
                Directory.Delete(modelDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup issues on CI runners.
            }
        }
    }

    private sealed class StaticCaptureBackend : ICaptureBackend
    {
        public string Name => "test-capture";

        public Task<Image<Rgba32>> CaptureAsync(CaptureRegion region, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Image<Rgba32>(640, 640));
        }
    }

    private sealed class StaticInferenceBackend : IInferenceBackend
    {
        private readonly IReadOnlyList<Detection> _detections;

        public StaticInferenceBackend(IReadOnlyList<Detection> detections)
        {
            _detections = detections;
        }

        public string Name => "test-inference";

        public InferenceRuntimeInfo RuntimeInfo { get; } = new(
            SelectedMode: Aimmy.Core.Enums.GpuExecutionMode.Cpu,
            Provider: "CPUExecutionProvider",
            Message: "test",
            IsFallback: false);

        public IReadOnlyList<Detection> Detect(Image<Rgba32> frame, float minimumConfidence)
        {
            return _detections;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TrackingInferenceBackend : IInferenceBackend
    {
        private readonly IReadOnlyList<Detection> _detections;

        public TrackingInferenceBackend(string name, IReadOnlyList<Detection> detections)
        {
            Name = name;
            _detections = detections;
        }

        public string Name { get; }

        public bool Disposed { get; private set; }

        public InferenceRuntimeInfo RuntimeInfo { get; } = new(
            SelectedMode: Aimmy.Core.Enums.GpuExecutionMode.Cpu,
            Provider: "CPUExecutionProvider",
            Message: "test",
            IsFallback: false);

        public IReadOnlyList<Detection> Detect(Image<Rgba32> frame, float minimumConfidence)
        {
            return _detections;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingInputBackend : IInputBackend
    {
        private readonly List<string> _operations = new();

        public int MoveCount { get; private set; }
        public int HoldCount { get; private set; }
        public int ReleaseCount { get; private set; }
        public IReadOnlyList<string> Operations => _operations;

        public int FirstOperationIndex(string operation)
        {
            return _operations.IndexOf(operation);
        }

        public int LastOperationIndex(string operation)
        {
            return _operations.LastIndexOf(operation);
        }

        public string Name => "test-input";

        public Task MoveRelativeAsync(int dx, int dy, CancellationToken cancellationToken)
        {
            MoveCount++;
            _operations.Add("move");
            return Task.CompletedTask;
        }

        public Task ClickAsync(CancellationToken cancellationToken)
        {
            _operations.Add("click");
            return Task.CompletedTask;
        }

        public Task HoldLeftButtonAsync(CancellationToken cancellationToken)
        {
            HoldCount++;
            _operations.Add("hold");
            return Task.CompletedTask;
        }

        public Task ReleaseLeftButtonAsync(CancellationToken cancellationToken)
        {
            ReleaseCount++;
            _operations.Add("release");
            return Task.CompletedTask;
        }
    }

    private sealed class ScriptedHotkeyBackend : IHotkeyBackend
    {
        private readonly Func<string, int, bool> _resolver;
        private readonly Dictionary<string, int> _queryCounts = new(StringComparer.OrdinalIgnoreCase);

        public ScriptedHotkeyBackend(Func<string, int, bool> resolver)
        {
            _resolver = resolver;
        }

        public string Name => "scripted-hotkeys";

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public bool IsPressed(string bindingId)
        {
            var queryCount = _queryCounts.TryGetValue(bindingId, out var count) ? count : 0;
            _queryCounts[bindingId] = queryCount + 1;
            return _resolver(bindingId, queryCount);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class NoopOverlayBackend : IOverlayBackend
    {
        public string Name => "test-overlay";

        public Task ShowFovAsync(int size, string style, string color, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task HideFovAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ShowDetectionsAsync(IReadOnlyList<Detection> detections, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ClearDetectionsAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PassThroughPredictor : ITargetPredictor
    {
        public string Name => "test-predictor";

        public Detection Predict(Detection current, DateTime timestamp)
        {
            return current;
        }

        public void Reset()
        {
        }
    }
}
