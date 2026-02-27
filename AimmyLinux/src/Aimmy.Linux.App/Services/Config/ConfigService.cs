using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aimmy.Linux.App.Services.Config;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    static ConfigService()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    private readonly IReadOnlyList<IConfigMigrator> _migrators;

    public ConfigService(IEnumerable<IConfigMigrator> migrators)
    {
        _migrators = migrators.ToList();
    }

    public AimmyConfig Load(string path, out string message)
    {
        var fallback = AimmyConfig.CreateDefault();

        try
        {
            if (!File.Exists(path))
            {
                Save(path, fallback);
                message = "Config not found. Created default typed config.";
                return fallback;
            }

            var raw = File.ReadAllText(path);

            foreach (var migrator in _migrators)
            {
                if (!migrator.CanMigrate(path, raw))
                {
                    continue;
                }

                if (migrator.TryMigrate(path, raw, out var migrated, out var migrationMessage))
                {
                    migrated.Normalize();
                    message = migrationMessage;
                    return migrated;
                }
            }

            var config = JsonSerializer.Deserialize<AimmyConfig>(raw, JsonOptions);
            if (config is not null)
            {
                config.Normalize();
                message = "Loaded typed JSON config.";
                return config;
            }

            if (TryMigrateFlatV1(raw, out var flatV1))
            {
                flatV1.Normalize();
                message = "Migrated flat AimmyLinux JSON into typed config.";
                return flatV1;
            }

            fallback.Normalize();
            message = "Failed to parse config; using defaults.";
            return fallback;
        }
        catch (Exception ex)
        {
            fallback.Normalize();
            message = $"Config load failed: {ex.Message}. Using defaults.";
            return fallback;
        }
    }

    public void Save(string path, AimmyConfig config)
    {
        config.Normalize();

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static bool TryMigrateFlatV1(string rawJson, out AimmyConfig config)
    {
        config = AimmyConfig.CreateDefault();

        try
        {
            var old = JsonSerializer.Deserialize<FlatConfigV1>(rawJson, JsonOptions);
            if (old is null || string.IsNullOrWhiteSpace(old.ModelPath))
            {
                return false;
            }

            config.Model.ModelPath = old.ModelPath;
            config.Capture.Width = old.CaptureWidth;
            config.Capture.Height = old.CaptureHeight;
            config.Capture.DisplayWidth = old.DisplayWidth;
            config.Capture.DisplayHeight = old.DisplayHeight;
            config.Model.ConfidenceThreshold = old.ConfidenceThreshold;
            config.Aim.MouseSensitivity = old.MouseSensitivity;
            config.Aim.MaxDeltaPerAxis = old.MaxDeltaPerAxis;
            config.Runtime.Fps = old.Fps;
            config.Aim.Enabled = old.AimAssistEnabled;
            config.Runtime.DryRun = old.DryRun;
            config.Input.PreferredMethod = old.PreferredInputBackend switch
            {
                "ydotool" => InputMethod.Ydotool,
                "xdotool" => InputMethod.Xdotool,
                _ => InputMethod.UInput
            };

            config.Capture.ExternalBackendPreference = old.PreferredCaptureBackend;
            config.Capture.Method = CaptureMethod.ExternalToolFallback;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class FlatConfigV1
    {
        public string ModelPath { get; set; } = string.Empty;
        public int CaptureWidth { get; set; } = 640;
        public int CaptureHeight { get; set; } = 640;
        public int DisplayWidth { get; set; } = 1920;
        public int DisplayHeight { get; set; } = 1080;
        public float ConfidenceThreshold { get; set; } = 0.45f;
        public double MouseSensitivity { get; set; } = 0.8;
        public int MaxDeltaPerAxis { get; set; } = 150;
        public int Fps { get; set; } = 60;
        public bool AimAssistEnabled { get; set; } = true;
        public bool DryRun { get; set; }
        public string PreferredInputBackend { get; set; } = "xdotool";
        public string PreferredCaptureBackend { get; set; } = "auto";
    }
}
