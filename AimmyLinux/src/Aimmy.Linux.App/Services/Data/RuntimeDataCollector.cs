using Aimmy.Core.Config;
using Aimmy.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Globalization;

namespace Aimmy.Linux.App.Services.Data;

public readonly record struct DataCollectionResult(
    bool Saved,
    string? ImagePath,
    string? LabelPath,
    string? Message);

public sealed class RuntimeDataCollector
{
    private const int SaveFrameCooldownMs = 500;
    private const string DefaultImagesDirectory = "bin/images";
    private const string DefaultLabelsDirectory = "bin/labels";

    private readonly DataCollectionSettings _settings;
    private readonly Func<DateTime> _utcNowProvider;
    private readonly string _imagesDirectory;
    private readonly string _labelsDirectory;
    private readonly object _sync = new();

    private DateTime _lastSavedUtc = DateTime.MinValue;

    public string ImagesDirectory => _imagesDirectory;
    public string LabelsDirectory => _labelsDirectory;

    public RuntimeDataCollector(
        DataCollectionSettings settings,
        Func<DateTime>? utcNowProvider = null,
        string? baseDirectory = null)
    {
        _settings = settings;
        _utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);

        var root = string.IsNullOrWhiteSpace(baseDirectory)
            ? AppContext.BaseDirectory
            : baseDirectory;

        _imagesDirectory = ResolveDirectory(settings.ImagesDirectory, root, DefaultImagesDirectory);
        _labelsDirectory = ResolveDirectory(settings.LabelsDirectory, root, DefaultLabelsDirectory);
    }

    public DataCollectionResult CollectFrame(
        Image<Rgba32> frame,
        Detection? selectedDetection,
        bool constantTrackingEnabled)
    {
        if (!_settings.CollectDataWhilePlaying)
        {
            return new DataCollectionResult(false, null, null, "Data collection disabled.");
        }

        if (constantTrackingEnabled && !_settings.AutoLabelData)
        {
            return new DataCollectionResult(false, null, null, "Skipping collection during constant tracking without auto-label.");
        }

        var now = _utcNowProvider();
        lock (_sync)
        {
            if ((now - _lastSavedUtc).TotalMilliseconds < SaveFrameCooldownMs)
            {
                return new DataCollectionResult(false, null, null, "Save cooldown active.");
            }

            _lastSavedUtc = now;
        }

        try
        {
            Directory.CreateDirectory(_imagesDirectory);
            if (_settings.AutoLabelData)
            {
                Directory.CreateDirectory(_labelsDirectory);
            }

            var fileId = Guid.NewGuid().ToString("N");
            var imagePath = Path.Combine(_imagesDirectory, $"{fileId}.jpg");
            frame.SaveAsJpeg(imagePath);

            string? labelPath = null;
            if (_settings.AutoLabelData && selectedDetection.HasValue)
            {
                labelPath = Path.Combine(_labelsDirectory, $"{fileId}.txt");
                var line = BuildYoloLabelLine(selectedDetection.Value, frame.Width, frame.Height);
                File.WriteAllText(labelPath, line);
            }

            return new DataCollectionResult(true, imagePath, labelPath, "Saved frame.");
        }
        catch (Exception ex)
        {
            return new DataCollectionResult(false, null, null, $"Save failed: {ex.Message}");
        }
    }

    private static string BuildYoloLabelLine(Detection detection, int frameWidth, int frameHeight)
    {
        var safeWidth = Math.Max(1, frameWidth);
        var safeHeight = Math.Max(1, frameHeight);

        var centerX = Clamp01(detection.CenterX / safeWidth);
        var centerY = Clamp01(detection.CenterY / safeHeight);
        var width = Clamp01(detection.Width / safeWidth);
        var height = Clamp01(detection.Height / safeHeight);

        return string.Join(
            " ",
            detection.ClassId.ToString(CultureInfo.InvariantCulture),
            centerX.ToString("F6", CultureInfo.InvariantCulture),
            centerY.ToString("F6", CultureInfo.InvariantCulture),
            width.ToString("F6", CultureInfo.InvariantCulture),
            height.ToString("F6", CultureInfo.InvariantCulture));
    }

    private static string ResolveDirectory(string configuredPath, string baseDirectory, string fallbackRelativePath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? fallbackRelativePath
            : configuredPath.Trim();

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 1f)
        {
            return 1f;
        }

        return value;
    }
}
