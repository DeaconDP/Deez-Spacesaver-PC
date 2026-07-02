using System.Text.Json;
using System.Text.Json.Serialization;
using Spacesaver.Models;

namespace Spacesaver.Services;

public sealed class AppSettingsService
{
    private static readonly string StoragePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PCSpacesaver",
        "settings.json");

    private AppSettingsData _data = new();

    public bool SafetyInfoBarDismissed
    {
        get => _data.SafetyInfoBarDismissed;
        set
        {
            if (_data.SafetyInfoBarDismissed == value)
                return;

            _data.SafetyInfoBarDismissed = value;
            Save();
        }
    }

    public bool HasCompletedOnboarding
    {
        get => _data.HasCompletedOnboarding;
        set
        {
            if (_data.HasCompletedOnboarding == value)
                return;

            _data.HasCompletedOnboarding = value;
            Save();
        }
    }

    public ThemePreference ThemePreference
    {
        get => _data.ThemePreference;
        set
        {
            if (_data.ThemePreference == value)
                return;

            _data.ThemePreference = value;
            Save();
        }
    }

    public AppSettingsService()
    {
        Load();
    }

    private void Load()
    {
        if (!File.Exists(StoragePath))
            return;

        try
        {
            var json = File.ReadAllText(StoragePath);
            _data = JsonSerializer.Deserialize<AppSettingsData>(json) ?? new AppSettingsData();
        }
        catch
        {
            _data = new AppSettingsData();
        }
    }

    private void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(StoragePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_data);
            File.WriteAllText(StoragePath, json);
        }
        catch
        {
            // Ignore persistence failures.
        }
    }

    private sealed class AppSettingsData
    {
        public bool SafetyInfoBarDismissed { get; set; }

        public bool HasCompletedOnboarding { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ThemePreference ThemePreference { get; set; } = ThemePreference.Default;
    }
}
