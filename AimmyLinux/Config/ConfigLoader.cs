using System.Text.Json;

namespace AimmyLinux.Config;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static AppConfig Load(string path)
    {
        var fallback = new AppConfig();
        try
        {
            if (!File.Exists(path))
            {
                Save(path, fallback);
                return fallback;
            }

            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            return config ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    public static void Save(string path, AppConfig config)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }
}
