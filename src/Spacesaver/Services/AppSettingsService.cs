using System.Text.Json;
using System.Text.Json.Serialization;
using Spacesaver.Models;

namespace Spacesaver.Services;

/// <summary>
/// Persists UI preferences in %LocalAppData%\PCSpacesaver\settings.json.
/// </summary>
public sealed class AppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _storagePath;
    private AppSettings _settings = new();

    public AppSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PCSpacesaver",
            "settings.json"))
    {
    }

    public AppSettingsService(string storagePath)
    {
        _storagePath = storagePath;
        Load();
    }

    public OrganiserFolderSortMode OrganiserFolderSort => _settings.OrganiserFolderSort;

    public void SetOrganiserFolderSort(OrganiserFolderSortMode mode)
    {
        if (_settings.OrganiserFolderSort == mode)
            return;

        _settings.OrganiserFolderSort = mode;
        Save();
    }

    private void Load()
    {
        if (!File.Exists(_storagePath))
            return;

        try
        {
            var json = File.ReadAllText(_storagePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is not null)
                _settings = settings;
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    private void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            var tempPath = _storagePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _storagePath, overwrite: true);
        }
        catch
        {
            // Ignore persistence failures; prefs stay in memory for this session.
        }
    }
}
