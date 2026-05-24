using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using EtherWarp.Models;

namespace EtherWarp.Services;

public class PresetStorageService
{
    private readonly string _storagePath;
    private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

    public PresetStorageService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EtherWarp");
        Directory.CreateDirectory(dir);
        _storagePath = Path.Combine(dir, "presets.json");
    }

    public List<NetworkPreset> LoadPresets()
    {
        if (!File.Exists(_storagePath)) return [];

        try
        {
            var json = File.ReadAllText(_storagePath);
            return JsonSerializer.Deserialize<List<NetworkPreset>>(json) ?? [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PresetStorageService] Failed to load presets: {ex.Message}");
            return [];
        }
    }

    public void SavePresets(List<NetworkPreset> presets)
    {
        var tmp = _storagePath + ".tmp";
        var json = JsonSerializer.Serialize(presets, _writeOptions);
        File.WriteAllText(tmp, json);

        if (File.Exists(_storagePath))
            File.Replace(tmp, _storagePath, null);
        else
            File.Move(tmp, _storagePath);
    }
}
