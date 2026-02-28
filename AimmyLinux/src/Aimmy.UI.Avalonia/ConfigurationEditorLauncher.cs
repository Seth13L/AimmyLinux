using Avalonia;
using Avalonia.Controls;
using Aimmy.Core.Capabilities;
using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;

namespace Aimmy.UI.Avalonia;

public static class ConfigurationEditorLauncher
{
    private static readonly object SyncLock = new();
    private static ConfigurationEditorLaunchOptions? _pendingLaunchOptions;

    public static int Run(
        AimmyConfig config,
        IReadOnlyList<DisplayInfo> displays,
        RuntimeCapabilities runtimeCapabilities,
        string configPath,
        Action<AimmyConfig> saveConfigCallback,
        IModelStoreClient? modelStoreClient = null,
        IUpdateService? updateService = null,
        IRuntimeHostService? runtimeHostService = null,
        IModelMetadataReader? modelMetadataReader = null,
        string currentVersion = "0.0.0",
        string[]? args = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(displays);
        ArgumentNullException.ThrowIfNull(runtimeCapabilities);
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        ArgumentNullException.ThrowIfNull(saveConfigCallback);

        lock (SyncLock)
        {
            if (_pendingLaunchOptions is not null)
            {
                throw new InvalidOperationException("Configuration editor launch is already in progress.");
            }

            _pendingLaunchOptions = new ConfigurationEditorLaunchOptions
            {
                Config = config,
                Displays = displays,
                RuntimeCapabilities = runtimeCapabilities,
                ConfigPath = configPath,
                SaveConfigCallback = saveConfigCallback,
                ModelStoreClient = modelStoreClient,
                UpdateService = updateService,
                RuntimeHostService = runtimeHostService,
                ModelMetadataReader = modelMetadataReader,
                CurrentVersion = currentVersion
            };
        }

        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(
                args ?? Array.Empty<string>(),
                ShutdownMode.OnMainWindowClose);
        }
        finally
        {
            lock (SyncLock)
            {
                _pendingLaunchOptions = null;
            }
        }
    }

    internal static bool TryConsumePendingLaunchOptions(out ConfigurationEditorLaunchOptions launchOptions)
    {
        lock (SyncLock)
        {
            if (_pendingLaunchOptions is null)
            {
                launchOptions = null!;
                return false;
            }

            launchOptions = _pendingLaunchOptions;
            return true;
        }
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
