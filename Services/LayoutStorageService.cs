using System.IO;
using System.Text.Json;
using StardewSurfacePad.Models;

namespace StardewSurfacePad.Services;

public sealed class LayoutStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _configPath;

    public LayoutStorageService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string dir = Path.Combine(appData, "StardewSurfacePad");
        _configPath = Path.Combine(dir, "layout.json");
    }

    public PadLayoutConfig LoadOrDefault(double screenWidth, double screenHeight)
    {
        try
        {
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                var loaded = JsonSerializer.Deserialize<PadLayoutConfig>(json, JsonOptions);
                if (loaded is not null)
                {
                    loaded.GlobalOpacity = Math.Clamp(loaded.GlobalOpacity, 0.30, 1.0);
                    loaded.ControlScale = Math.Clamp(loaded.ControlScale, 0.70, 1.40);
                    return loaded;
                }
            }
        }
        catch
        {
            // Fall back to default layout if config is corrupted
        }

        return PadLayoutConfig.CreateDefault(screenWidth, screenHeight);
    }

    public void Save(PadLayoutConfig config)
    {
        try
        {
            string? dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch
        {
            // Ignore non-fatal persistence errors
        }
    }
}
