using AimmyLinux.App;
using AimmyLinux.Config;
using AimmyLinux.Services.Capture;
using AimmyLinux.Services.Inference;
using AimmyLinux.Services.Input;

static Dictionary<string, string> ParseArgs(string[] args)
{
    var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < args.Length; i++)
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

var parsedArgs = ParseArgs(args);
var configPath = parsedArgs.TryGetValue("config", out var customConfig)
    ? customConfig
    : Path.Combine(AppContext.BaseDirectory, "aimmylinux.json");

var config = ConfigLoader.Load(configPath);

if (parsedArgs.TryGetValue("model", out var modelPath))
{
    config.ModelPath = modelPath;
}

if (parsedArgs.TryGetValue("dry-run", out var dryRunText) && bool.TryParse(dryRunText, out var dryRun))
{
    config.DryRun = dryRun;
}

if (parsedArgs.TryGetValue("fps", out var fpsText) && int.TryParse(fpsText, out var fps))
{
    config.Fps = Math.Clamp(fps, 1, 240);
}

if (string.IsNullOrWhiteSpace(config.ModelPath))
{
    Console.Error.WriteLine("No model path was configured. Set ModelPath in aimmylinux.json or pass --model.");
    return 1;
}

if (!File.Exists(config.ModelPath))
{
    Console.Error.WriteLine($"Model file does not exist: {config.ModelPath}");
    return 1;
}

try
{
    IInputProvider inputProvider = config.DryRun
        ? new NoopInputProvider()
        : config.PreferredInputBackend.Equals("ydotool", StringComparison.OrdinalIgnoreCase)
            ? new YDotoolInputProvider()
            : new XDotoolInputProvider();

    ICaptureProvider captureProvider = new ExternalScreenshotCaptureProvider(config.PreferredCaptureBackend);
    using var detector = new OnnxDetector(config.ModelPath, config.CaptureWidth);

    var app = new AimmyLinuxApp(config, captureProvider, detector, inputProvider);
    return await app.RunAsync(CancellationToken.None);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    return 2;
}
