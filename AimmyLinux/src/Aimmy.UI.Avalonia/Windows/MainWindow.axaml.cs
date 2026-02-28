using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Aimmy.Core.Capabilities;
using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.UI.Avalonia.ViewModels;

namespace Aimmy.UI.Avalonia.Windows;

public partial class MainWindow : Window
{
    private readonly AimmyConfig _config;
    private readonly ConfigurationEditorViewModel _viewModel;
    private readonly Action<AimmyConfig> _saveConfigCallback;
    private readonly IModelStoreClient? _modelStoreClient;
    private readonly IUpdateService? _updateService;
    private readonly IRuntimeHostService? _runtimeHostService;
    private readonly IModelMetadataReader? _modelMetadataReader;
    private readonly RuntimeCapabilities _runtimeCapabilities;
    private readonly DispatcherTimer _runtimeStatusTimer;
    private readonly string _configPath;
    private readonly CheckBox _useDiscoveredDisplayCheckBox;
    private readonly ComboBox _discoveredDisplayComboBox;
    private readonly Grid _manualDisplayGrid;
    private readonly TextBlock _statusTextBlock;

    public MainWindow()
        : this(CreateDefaultLaunchOptions())
    {
    }

    public MainWindow(ConfigurationEditorLaunchOptions launchOptions)
    {
        ArgumentNullException.ThrowIfNull(launchOptions);

        _config = launchOptions.Config;
        _saveConfigCallback = launchOptions.SaveConfigCallback;
        _modelStoreClient = launchOptions.ModelStoreClient;
        _updateService = launchOptions.UpdateService;
        _runtimeHostService = launchOptions.RuntimeHostService;
        _modelMetadataReader = launchOptions.ModelMetadataReader;
        _configPath = launchOptions.ConfigPath;
        _viewModel = new ConfigurationEditorViewModel();
        _runtimeCapabilities = BuildUiCapabilities(
            launchOptions.RuntimeCapabilities,
            _modelStoreClient is not null,
            _updateService is not null,
            _runtimeHostService is not null);
        _viewModel.Load(
            _config,
            launchOptions.Displays,
            _runtimeCapabilities);
        _viewModel.StoreUpdate.CurrentVersion = launchOptions.CurrentVersion;

        InitializeComponent();
        DataContext = _viewModel;

        _useDiscoveredDisplayCheckBox = FindRequiredControl<CheckBox>("UseDiscoveredDisplayCheckBox");
        _discoveredDisplayComboBox = FindRequiredControl<ComboBox>("DiscoveredDisplayComboBox");
        _manualDisplayGrid = FindRequiredControl<Grid>("ManualDisplayGrid");
        _statusTextBlock = FindRequiredControl<TextBlock>("StatusTextBlock");

        var configPathTextBlock = FindRequiredControl<TextBlock>("ConfigPathTextBlock");
        configPathTextBlock.Text = $"Config: {launchOptions.ConfigPath}";

        _statusTextBlock.Text = launchOptions.Displays.Count == 0
            ? "No X11 displays discovered. Use manual geometry values."
            : $"Loaded {launchOptions.Displays.Count} discovered display(s).";

        Topmost = _config.Runtime.UiTopMost;
        SyncDisplaySelectionState();

        _runtimeStatusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _runtimeStatusTimer.Tick += OnRuntimeStatusTick;
        _runtimeStatusTimer.Start();
    }

    private static ConfigurationEditorLaunchOptions CreateDefaultLaunchOptions()
    {
        return new ConfigurationEditorLaunchOptions
        {
            Config = AimmyConfig.CreateDefault(),
            Displays = Array.Empty<DisplayInfo>(),
            RuntimeCapabilities = RuntimeCapabilities.CreateDefault(),
            ConfigPath = "(design-time)",
            SaveConfigCallback = _ => { }
        };
    }

    private static RuntimeCapabilities BuildUiCapabilities(
        RuntimeCapabilities runtimeCapabilities,
        bool hasModelStoreClient,
        bool hasUpdateService,
        bool hasRuntimeHostService)
    {
        var merged = RuntimeCapabilities.CreateDefault();
        foreach (var feature in runtimeCapabilities.Features.Values)
        {
            merged.Set(feature.Name, feature.State, feature.IsDegraded, feature.Message);
        }

        if (!hasModelStoreClient)
        {
            merged.Set("ModelStore", FeatureState.Unavailable, true, "Model/config store client is unavailable.");
        }

        if (!hasUpdateService)
        {
            merged.Set("Updater", FeatureState.Unavailable, true, "Updater service is unavailable.");
        }

        if (!hasRuntimeHostService)
        {
            merged.Set("RuntimeUi", FeatureState.Unavailable, true, "Runtime host service is unavailable.");
        }

        merged.Set("StreamGuard", FeatureState.Unavailable, false, "StreamGuard equivalent is unsupported on Linux v1.");
        return merged;
    }

    private T FindRequiredControl<T>(string name) where T : Control
    {
        return this.FindControl<T>(name) ?? throw new InvalidOperationException($"Missing control '{name}'.");
    }

    private void OnDisplayModeChanged(object? sender, RoutedEventArgs e)
    {
        SyncDisplaySelectionState();
    }

    private void OnApplyClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.Apply(_config);
        Topmost = _config.Runtime.UiTopMost;
        _statusTextBlock.Text = $"Applied in memory at {DateTime.Now:HH:mm:ss}.";
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.Apply(_config);
        Topmost = _config.Runtime.UiTopMost;
        _saveConfigCallback(_config);
        _statusTextBlock.Text = $"Saved config at {DateTime.Now:HH:mm:ss}.";
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override async void OnClosed(EventArgs e)
    {
        _runtimeStatusTimer.Stop();
        _runtimeStatusTimer.Tick -= OnRuntimeStatusTick;
        if (_runtimeHostService is not null)
        {
            try
            {
                await _runtimeHostService.StopAsync(CancellationToken.None);
                await _runtimeHostService.DisposeAsync();
            }
            catch
            {
                // Ignore teardown errors on close.
            }
        }

        base.OnClosed(e);
    }

    private void OnRuntimeStatusTick(object? sender, EventArgs e)
    {
        if (_runtimeHostService is null)
        {
            return;
        }

        var snapshot = _runtimeHostService.GetStatusSnapshot();
        _viewModel.RuntimeStatus.UpdateHostSnapshot(snapshot);
    }

    private async void OnStartRuntimeClicked(object? sender, RoutedEventArgs e)
    {
        if (_runtimeHostService is null)
        {
            _statusTextBlock.Text = "Integrated runtime host is unavailable.";
            return;
        }

        try
        {
            _viewModel.Apply(_config);
            Topmost = _config.Runtime.UiTopMost;

            if (_config.Runtime.StreamGuardEnabled)
            {
                _statusTextBlock.Text = "StreamGuard is unsupported on Linux v1; continuing without StreamGuard.";
            }

            await _runtimeHostService.StartAsync(_config, _runtimeCapabilities, CancellationToken.None);
            _statusTextBlock.Text = $"Runtime start requested at {DateTime.Now:HH:mm:ss}.";
        }
        catch (Exception ex)
        {
            _statusTextBlock.Text = $"Runtime start failed: {ex.Message}";
        }
    }

    private async void OnApplyRuntimeClicked(object? sender, RoutedEventArgs e)
    {
        if (_runtimeHostService is null)
        {
            _statusTextBlock.Text = "Integrated runtime host is unavailable.";
            return;
        }

        try
        {
            _viewModel.Apply(_config);
            Topmost = _config.Runtime.UiTopMost;
            var snapshot = _runtimeHostService.GetStatusSnapshot();
            if (snapshot.IsRunning)
            {
                await _runtimeHostService.ApplyConfigAsync(_config, _runtimeCapabilities, CancellationToken.None);
                _statusTextBlock.Text = $"Runtime restarted with applied config at {DateTime.Now:HH:mm:ss}.";
            }
            else
            {
                await _runtimeHostService.StartAsync(_config, _runtimeCapabilities, CancellationToken.None);
                _statusTextBlock.Text = $"Runtime started with applied config at {DateTime.Now:HH:mm:ss}.";
            }
        }
        catch (Exception ex)
        {
            _statusTextBlock.Text = $"Runtime apply failed: {ex.Message}";
        }
    }

    private async void OnStopRuntimeClicked(object? sender, RoutedEventArgs e)
    {
        if (_runtimeHostService is null)
        {
            _statusTextBlock.Text = "Integrated runtime host is unavailable.";
            return;
        }

        try
        {
            await _runtimeHostService.StopAsync(CancellationToken.None);
            _statusTextBlock.Text = $"Runtime stop requested at {DateTime.Now:HH:mm:ss}.";
        }
        catch (Exception ex)
        {
            _statusTextBlock.Text = $"Runtime stop failed: {ex.Message}";
        }
    }

    private async void OnStoreRefreshClicked(object? sender, RoutedEventArgs e)
    {
        if (_modelStoreClient is null || !_viewModel.StoreSectionEnabled)
        {
            _statusTextBlock.Text = "Store is unavailable in this runtime.";
            return;
        }

        try
        {
            _viewModel.Apply(_config);
            _statusTextBlock.Text = "Refreshing model/config store entries...";

            var modelEntries = await _modelStoreClient.GetModelEntriesAsync(CancellationToken.None);
            var configEntries = await _modelStoreClient.GetConfigEntriesAsync(CancellationToken.None);
            _viewModel.StoreUpdate.SetModelEntries(modelEntries);
            _viewModel.StoreUpdate.SetConfigEntries(configEntries);

            _statusTextBlock.Text = $"Store refreshed at {DateTime.Now:HH:mm:ss}. Models: {modelEntries.Count}, Configs: {configEntries.Count}.";
        }
        catch (Exception ex)
        {
            _statusTextBlock.Text = $"Store refresh failed: {ex.Message}";
        }
    }

    private async void OnRefreshModelMetadataClicked(object? sender, RoutedEventArgs e)
    {
        if (_modelMetadataReader is null)
        {
            _statusTextBlock.Text = "Model metadata service is unavailable.";
            return;
        }

        try
        {
            _viewModel.Apply(_config);
            var modelPath = ResolveModelPath(_viewModel.ModelSettings.ModelPath);
            _statusTextBlock.Text = $"Reading metadata from '{Path.GetFileName(modelPath)}'...";
            var metadata = await _modelMetadataReader.ReadAsync(modelPath, CancellationToken.None);
            _viewModel.ModelSettings.ApplyMetadata(metadata);
            _statusTextBlock.Text = metadata.Message;
        }
        catch (Exception ex)
        {
            _statusTextBlock.Text = $"Metadata refresh failed: {ex.Message}";
        }
    }

    private async void OnDownloadModelClicked(object? sender, RoutedEventArgs e)
    {
        if (_modelStoreClient is null || !_viewModel.StoreSectionEnabled)
        {
            _statusTextBlock.Text = "Model download is unavailable in this runtime.";
            return;
        }

        var selected = _viewModel.StoreUpdate.SelectedModelEntry;
        if (selected is null)
        {
            _statusTextBlock.Text = "Select a model entry before downloading.";
            return;
        }

        try
        {
            _viewModel.Apply(_config);
            var destinationDirectory = ResolveDestinationDirectory(_viewModel.StoreUpdate.LocalModelsDirectory);
            _statusTextBlock.Text = $"Downloading model '{selected.Name}'...";
            var destinationPath = await _modelStoreClient.DownloadAsync(selected, destinationDirectory, CancellationToken.None);
            _statusTextBlock.Text = $"Model downloaded to {destinationPath}";
        }
        catch (Exception ex)
        {
            _statusTextBlock.Text = $"Model download failed: {ex.Message}";
        }
    }

    private async void OnDownloadConfigClicked(object? sender, RoutedEventArgs e)
    {
        if (_modelStoreClient is null || !_viewModel.StoreSectionEnabled)
        {
            _statusTextBlock.Text = "Config download is unavailable in this runtime.";
            return;
        }

        var selected = _viewModel.StoreUpdate.SelectedConfigEntry;
        if (selected is null)
        {
            _statusTextBlock.Text = "Select a config entry before downloading.";
            return;
        }

        try
        {
            _viewModel.Apply(_config);
            var destinationDirectory = ResolveDestinationDirectory(_viewModel.StoreUpdate.LocalConfigsDirectory);
            _statusTextBlock.Text = $"Downloading config '{selected.Name}'...";
            var destinationPath = await _modelStoreClient.DownloadAsync(selected, destinationDirectory, CancellationToken.None);
            _statusTextBlock.Text = $"Config downloaded to {destinationPath}";
        }
        catch (Exception ex)
        {
            _statusTextBlock.Text = $"Config download failed: {ex.Message}";
        }
    }

    private async void OnCheckUpdateClicked(object? sender, RoutedEventArgs e)
    {
        if (_updateService is null || !_viewModel.UpdateSectionEnabled)
        {
            _statusTextBlock.Text = "Updater is unavailable in this runtime.";
            return;
        }

        try
        {
            _viewModel.Apply(_config);
            var currentVersion = string.IsNullOrWhiteSpace(_viewModel.StoreUpdate.CurrentVersion)
                ? "0.0.0"
                : _viewModel.StoreUpdate.CurrentVersion;

            _statusTextBlock.Text = $"Checking for updates from {currentVersion}...";
            var result = await _updateService.CheckForUpdatesAsync(currentVersion, CancellationToken.None);
            _viewModel.StoreUpdate.ApplyUpdateResult(result);

            _statusTextBlock.Text = result.UpdateAvailable
                ? $"Update available: {result.LatestVersion}. {result.Notes}"
                : $"{result.Notes} ({DateTime.Now:HH:mm:ss}).";
        }
        catch (Exception ex)
        {
            _statusTextBlock.Text = $"Update check failed: {ex.Message}";
        }
    }

    private void SyncDisplaySelectionState()
    {
        var hasDiscoveredDisplays = _viewModel.DisplaySelection.DisplayOptions.Count > 0;
        if (!hasDiscoveredDisplays)
        {
            _viewModel.DisplaySelection.UseDiscoveredDisplay = false;
            _useDiscoveredDisplayCheckBox.IsChecked = false;
        }

        _useDiscoveredDisplayCheckBox.IsEnabled = hasDiscoveredDisplays;
        var useDiscoveredDisplay = hasDiscoveredDisplays && (_useDiscoveredDisplayCheckBox.IsChecked ?? false);
        _discoveredDisplayComboBox.IsEnabled = useDiscoveredDisplay;
        _manualDisplayGrid.IsEnabled = !useDiscoveredDisplay;
    }

    private string ResolveDestinationDirectory(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(AppContext.BaseDirectory, "bin");
        }

        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var configDirectory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(configDirectory))
        {
            return Path.GetFullPath(Path.Combine(configDirectory, configuredPath));
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }

    private string ResolveModelPath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        var configDirectory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrWhiteSpace(configDirectory))
        {
            var candidate = Path.GetFullPath(Path.Combine(configDirectory, configuredPath));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var cwdCandidate = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, configuredPath));
        if (File.Exists(cwdCandidate))
        {
            return cwdCandidate;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }
}
