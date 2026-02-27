using Aimmy.Core.Config;
using Aimmy.Core.Prediction;
using Aimmy.Inference.OnnxRuntime;
using Aimmy.Linux.App.Services.Config;
using Aimmy.Linux.App.Services.Network;
using Aimmy.Linux.App.Services.Runtime;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Linux.X11.Capture;
using Aimmy.Platform.Linux.X11.Config;
using Aimmy.Platform.Linux.X11.Display;
using Aimmy.Platform.Linux.X11.Hotkeys;
using Aimmy.Platform.Linux.X11.Input;
using Aimmy.Platform.Linux.X11.Overlay;
using Aimmy.Platform.Linux.X11.Runtime;
using Aimmy.Platform.Linux.X11.Util;

static Dictionary<string, string> ParseArgs(string[] args)
{
    var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var current = args[i];
        if (!current.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = current[2..];
        var value = "true";
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = args[i + 1];
            i++;
        }

        parsed[key] = value;
    }

    return parsed;
}

static void ApplyOverrides(AimmyConfig config, Dictionary<string, string> args)
{
    if (args.TryGetValue("model", out var modelPath) && !string.IsNullOrWhiteSpace(modelPath))
    {
        config.Model.ModelPath = modelPath;
    }

    if (args.TryGetValue("config-version", out var configVersion) && !string.IsNullOrWhiteSpace(configVersion))
    {
        config.ConfigVersion = configVersion;
    }

    if (args.TryGetValue("fps", out var fpsText) && int.TryParse(fpsText, out var fps))
    {
        config.Runtime.Fps = fps;
    }

    if (args.TryGetValue("dry-run", out var dryRunText) && bool.TryParse(dryRunText, out var dryRun))
    {
        config.Runtime.DryRun = dryRun;
    }

    if (args.TryGetValue("capture-backend", out var captureBackend) && !string.IsNullOrWhiteSpace(captureBackend))
    {
        config.Capture.ExternalBackendPreference = captureBackend;
    }

    config.Normalize();
}

static string ResolvePath(string configuredPath, string configPath)
{
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return configuredPath;
    }

    if (Path.IsPathRooted(configuredPath))
    {
        return configuredPath;
    }

    var candidates = new List<string>();

    var configDirectory = Path.GetDirectoryName(configPath);
    if (!string.IsNullOrWhiteSpace(configDirectory))
    {
        candidates.Add(Path.Combine(configDirectory, configuredPath));
    }

    candidates.Add(Path.Combine(Environment.CurrentDirectory, configuredPath));
    candidates.Add(Path.Combine(AppContext.BaseDirectory, configuredPath));

    foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }

    return configuredPath;
}

var parsedArgs = ParseArgs(args);
var configPath = parsedArgs.TryGetValue("config", out var customConfig)
    ? customConfig
    : Path.Combine(AppContext.BaseDirectory, "aimmylinux.json");

var configService = new ConfigService(new IConfigMigrator[]
{
    new LegacyCfgMigrator()
});

var config = configService.Load(configPath, out var loadMessage);
ApplyOverrides(config, parsedArgs);
config.Model.ModelPath = ResolvePath(config.Model.ModelPath, configPath);
Console.WriteLine(loadMessage);

var commandRunner = ProcessRunner.Instance;
var displayDiscoveryService = new X11DisplayDiscoveryService(commandRunner);

if (parsedArgs.ContainsKey("list-displays"))
{
    var displays = await displayDiscoveryService.DiscoverAsync(CancellationToken.None);
    if (displays.Count == 0)
    {
        Console.WriteLine("No X11 displays discovered.");
        return 0;
    }

    foreach (var display in displays)
    {
        Console.WriteLine(
            $"{display.Id} primary={display.IsPrimary} geometry={display.Width}x{display.Height}+{display.OriginX}+{display.OriginY} " +
            $"dpi={display.DpiScaleX:F2}x{display.DpiScaleY:F2}");
    }

    return 0;
}

if (parsedArgs.TryGetValue("select-display", out var selectedDisplayId) || parsedArgs.ContainsKey("use-primary-display"))
{
    var displays = await displayDiscoveryService.DiscoverAsync(CancellationToken.None);
    var selectedDisplay = parsedArgs.ContainsKey("use-primary-display")
        ? displays.FirstOrDefault(d => d.IsPrimary)
        : displays.FirstOrDefault(d => string.Equals(d.Id, selectedDisplayId, StringComparison.OrdinalIgnoreCase));

    if (selectedDisplay is null)
    {
        Console.Error.WriteLine($"Unable to resolve display selection '{selectedDisplayId ?? "primary"}'.");
        return 1;
    }

    config.Capture.DisplayWidth = selectedDisplay.Width;
    config.Capture.DisplayHeight = selectedDisplay.Height;
    config.Capture.DisplayOffsetX = selectedDisplay.OriginX;
    config.Capture.DisplayOffsetY = selectedDisplay.OriginY;
    config.Capture.DpiScaleX = selectedDisplay.DpiScaleX;
    config.Capture.DpiScaleY = selectedDisplay.DpiScaleY;
    config.Normalize();

    Console.WriteLine(
        $"Selected display {selectedDisplay.Id}: {selectedDisplay.Width}x{selectedDisplay.Height}+{selectedDisplay.OriginX}+{selectedDisplay.OriginY} " +
        $"dpi={selectedDisplay.DpiScaleX:F2}x{selectedDisplay.DpiScaleY:F2}");
}

if (parsedArgs.ContainsKey("save-config"))
{
    configService.Save(configPath, config);
    Console.WriteLine($"Saved typed config to {configPath}");
    return 0;
}

if (parsedArgs.ContainsKey("check-update"))
{
    var updateService = new PackageAwareUpdateService(config.Update);
    var currentVersion = parsedArgs.TryGetValue("current-version", out var versionText) ? versionText : "0.0.0";
    var result = await updateService.CheckForUpdatesAsync(currentVersion, CancellationToken.None);
    Console.WriteLine($"Update available: {result.UpdateAvailable}");
    Console.WriteLine($"Current: {result.CurrentVersion}");
    Console.WriteLine($"Latest: {result.LatestVersion}");
    Console.WriteLine($"URL: {result.DownloadUrl}");
    Console.WriteLine(result.Notes);
    return 0;
}

if (parsedArgs.ContainsKey("list-model-store"))
{
    var storeClient = new GitHubModelStoreClient(config.Store);
    var models = await storeClient.GetModelEntriesAsync(CancellationToken.None);
    var configs = await storeClient.GetConfigEntriesAsync(CancellationToken.None);

    Console.WriteLine($"Models: {models.Count}");
    foreach (var entry in models.Take(20))
    {
        Console.WriteLine($"  [model] {entry.Name}");
    }

    Console.WriteLine($"Configs: {configs.Count}");
    foreach (var entry in configs.Take(20))
    {
        Console.WriteLine($"  [config] {entry.Name}");
    }

    return 0;
}

if (string.IsNullOrWhiteSpace(config.Model.ModelPath) || !File.Exists(config.Model.ModelPath))
{
    Console.Error.WriteLine($"Model file does not exist: {config.Model.ModelPath}");
    return 1;
}

var environmentVariableReader = new Func<string, string?>(Environment.GetEnvironmentVariable);

var capabilityProbe = new LinuxRuntimeCapabilityProbe(commandRunner, environmentVariableReader);
var capabilities = capabilityProbe.Probe();

var captureBackend = CaptureBackendFactory.Create(config, commandRunner, environmentVariableReader);
var inputBackend = InputBackendFactory.Create(config, commandRunner);
var hotkeyBackend = HotkeyBackendFactory.Create(config, environmentVariableReader);
var overlayBackend = OverlayBackendFactory.Create(config, commandRunner, environmentVariableReader);
var inferenceBackend = InferenceBackendFactory.Create(config);
var predictor = PredictorFactory.Create(config);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

var runtime = new AimmyRuntime(
    config,
    capabilities,
    captureBackend,
    inferenceBackend,
    inputBackend,
    hotkeyBackend,
    overlayBackend,
    predictor);

return await runtime.RunAsync(cts.Token);
