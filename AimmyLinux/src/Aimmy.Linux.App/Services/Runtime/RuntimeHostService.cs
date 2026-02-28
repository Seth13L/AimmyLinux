using Aimmy.Core.Capabilities;
using Aimmy.Core.Config;
using Aimmy.Core.Diagnostics;
using Aimmy.Core.Prediction;
using Aimmy.Inference.OnnxRuntime;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.Platform.Linux.X11.Capture;
using Aimmy.Platform.Linux.X11.Cursor;
using Aimmy.Platform.Linux.X11.Hotkeys;
using Aimmy.Platform.Linux.X11.Input;
using Aimmy.Platform.Linux.X11.Overlay;
using Aimmy.Platform.Linux.X11.Util;
using System.Text.Json;

namespace Aimmy.Linux.App.Services.Runtime;

public sealed class RuntimeHostService : IRuntimeHostService
{
    private readonly ICommandRunner _commandRunner;
    private readonly Func<string, string?> _environmentVariableReader;
    private readonly object _sync = new();

    private CancellationTokenSource? _runCts;
    private Task<int>? _runTask;
    private RuntimeHostSnapshot _snapshot = RuntimeHostSnapshot.Empty;
    private ICursorProvider? _cursorProvider;

    public RuntimeHostService(
        ICommandRunner? commandRunner = null,
        Func<string, string?>? environmentVariableReader = null)
    {
        _commandRunner = commandRunner ?? ProcessRunner.Instance;
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
    }

    public async Task StartAsync(
        AimmyConfig config,
        RuntimeCapabilities runtimeCapabilities,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(runtimeCapabilities);

        Task<int>? monitorTask = null;
        lock (_sync)
        {
            if (_runTask is { IsCompleted: false })
            {
                return;
            }

            var runtimeConfig = CloneConfig(config);
            var captureBackend = CaptureBackendFactory.Create(runtimeConfig, _commandRunner, _environmentVariableReader);
            var inputBackend = InputBackendFactory.Create(runtimeConfig, _commandRunner);
            _cursorProvider = CursorProviderFactory.Create(_environmentVariableReader);
            var hotkeyBackend = HotkeyBackendFactory.Create(runtimeConfig, _environmentVariableReader);
            var overlayBackend = OverlayBackendFactory.Create(runtimeConfig, _commandRunner, _environmentVariableReader);
            var inferenceBackend = InferenceBackendFactory.Create(runtimeConfig);
            var predictor = PredictorFactory.Create(runtimeConfig);

            var runtime = new AimmyRuntime(
                runtimeConfig,
                runtimeCapabilities,
                captureBackend,
                inferenceBackend,
                _cursorProvider,
                inputBackend,
                hotkeyBackend,
                overlayBackend,
                predictor,
                InferenceBackendFactory.Create,
                OnSnapshotAvailable);

            _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _runTask = Task.Run(() => runtime.RunAsync(_runCts.Token), CancellationToken.None);
            monitorTask = _runTask;

            _snapshot = _snapshot with
            {
                IsRunning = true,
                RuntimeSnapshot = RuntimeSnapshot.Empty,
                CaptureBackend = captureBackend.Name,
                InputBackend = inputBackend.Name,
                HotkeyBackend = hotkeyBackend.Name,
                OverlayBackend = overlayBackend.Name,
                InferenceProvider = inferenceBackend.RuntimeInfo.Provider,
                CurrentModelPath = runtimeConfig.Model.ModelPath,
                StatusMessage = "Runtime started.",
                LastUpdatedUtc = DateTime.UtcNow
            };
        }

        _ = MonitorRunTaskAsync(monitorTask!);
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Task<int>? runTask;
        CancellationTokenSource? runCts;
        lock (_sync)
        {
            runTask = _runTask;
            runCts = _runCts;
            _runTask = null;
            _runCts = null;
        }

        try
        {
            runCts?.Cancel();
            if (runTask is not null)
            {
                await runTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore caller cancellation.
        }
        finally
        {
            runCts?.Dispose();
            DisposeCursorProvider();
            lock (_sync)
            {
                _snapshot = _snapshot with
                {
                    IsRunning = false,
                    StatusMessage = "Runtime stopped.",
                    LastUpdatedUtc = DateTime.UtcNow
                };
            }
        }
    }

    public async Task ApplyConfigAsync(
        AimmyConfig config,
        RuntimeCapabilities runtimeCapabilities,
        CancellationToken cancellationToken)
    {
        await StopAsync(cancellationToken).ConfigureAwait(false);
        await StartAsync(config, runtimeCapabilities, cancellationToken).ConfigureAwait(false);
    }

    public RuntimeHostSnapshot GetStatusSnapshot()
    {
        lock (_sync)
        {
            return _snapshot;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private void OnSnapshotAvailable(RuntimeSnapshot runtimeSnapshot)
    {
        lock (_sync)
        {
            _snapshot = _snapshot with
            {
                RuntimeSnapshot = runtimeSnapshot,
                LastUpdatedUtc = DateTime.UtcNow
            };
        }
    }

    private async Task MonitorRunTaskAsync(Task<int> runTask)
    {
        var message = "Runtime exited.";
        try
        {
            var exitCode = await runTask.ConfigureAwait(false);
            message = $"Runtime exited with code {exitCode}.";
        }
        catch (Exception ex)
        {
            message = $"Runtime crashed: {ex.Message}";
        }
        finally
        {
            DisposeCursorProvider();
            lock (_sync)
            {
                _runTask = null;
                _runCts?.Dispose();
                _runCts = null;
                _snapshot = _snapshot with
                {
                    IsRunning = false,
                    StatusMessage = message,
                    LastUpdatedUtc = DateTime.UtcNow
                };
            }
        }
    }

    private static AimmyConfig CloneConfig(AimmyConfig source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<AimmyConfig>(json) ?? AimmyConfig.CreateDefault();
    }

    private void DisposeCursorProvider()
    {
        if (_cursorProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _cursorProvider = null;
    }
}
