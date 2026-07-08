using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using MediaColor = System.Windows.Media.Color;

namespace FloatingHud;

public static class HudSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    public static string SettingsDirectory { get; private set; } = GetDefaultSettingsDirectory();

    public static string SettingsPath => Path.Combine(
        SettingsDirectory,
        "settings.json");

    public static void ConfigureDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("目录不能为空。", nameof(directory));
        }

        SettingsDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory));
    }

    public static bool Exists()
    {
        return File.Exists(SettingsPath);
    }

    public static HudSettings? Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(SettingsPath);
            PersistedHudSettings? persistedSettings = JsonSerializer.Deserialize<PersistedHudSettings>(json, JsonOptions);
            return persistedSettings?.ToHudSettings();
        }
        catch
        {
            return null;
        }
    }

    public static void Save(HudSettings settings)
    {
        string? settingsDirectory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        PersistedHudSettings persistedSettings = PersistedHudSettings.FromHudSettings(settings);
        string json = JsonSerializer.Serialize(persistedSettings, JsonOptions);
        string tempPath = Path.Combine(
            settingsDirectory ?? SettingsDirectory,
            $".settings-{Environment.ProcessId}-{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(tempPath, json);
            if (File.Exists(SettingsPath))
            {
                File.Replace(tempPath, SettingsPath, null);
            }
            else
            {
                try
                {
                    File.Move(tempPath, SettingsPath);
                }
                catch (IOException) when (File.Exists(SettingsPath))
                {
                    File.Replace(tempPath, SettingsPath, null);
                }
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static string GetDefaultSettingsDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "floating-hud");
    }

    private sealed class PersistedHudSettings
    {
        public string? CommandLine { get; set; }

        public int? RefreshIntervalSeconds { get; set; }

        public string? DefaultText { get; set; }

        public double? FontSize { get; set; }

        public double? FontScale { get; set; }

        public string? HoverBackgroundColor { get; set; }

        public string? LockedBorderColor { get; set; }

        public string? ErrorBorderColor { get; set; }

        public string? WarningBorderColor { get; set; }

        public bool? IsLocked { get; set; }

        public double? AnchorX { get; set; }

        public double? AnchorY { get; set; }

        public double? PositionX { get; set; }

        public double? PositionY { get; set; }

        public static PersistedHudSettings FromHudSettings(HudSettings settings)
        {
            HudSettings normalizedSettings = settings.Normalize();

            return new PersistedHudSettings
            {
                CommandLine = normalizedSettings.CommandLine,
                RefreshIntervalSeconds = normalizedSettings.RefreshIntervalSeconds,
                DefaultText = normalizedSettings.DefaultText,
                FontSize = normalizedSettings.FontSize,
                FontScale = normalizedSettings.FontScale,
                HoverBackgroundColor = RgbaColor.ToString(normalizedSettings.HoverBackgroundColor),
                LockedBorderColor = RgbaColor.ToString(normalizedSettings.LockedBorderColor),
                ErrorBorderColor = RgbaColor.ToString(normalizedSettings.ErrorBorderColor),
                WarningBorderColor = RgbaColor.ToString(normalizedSettings.WarningBorderColor),
                IsLocked = normalizedSettings.IsLocked,
                AnchorX = normalizedSettings.AnchorX,
                AnchorY = normalizedSettings.AnchorY,
                PositionX = normalizedSettings.PositionX,
                PositionY = normalizedSettings.PositionY,
            };
        }

        public HudSettings ToHudSettings()
        {
            HudSettings defaults = new();
            HudSettings settings = new()
            {
                CommandLine = CommandLine ?? defaults.CommandLine,
                RefreshIntervalSeconds = RefreshIntervalSeconds ?? defaults.RefreshIntervalSeconds,
                DefaultText = DefaultText ?? defaults.DefaultText,
                FontSize = FontSize ?? defaults.FontSize,
                FontScale = FontScale ?? defaults.FontScale,
                HoverBackgroundColor = ParseColorOrDefault(HoverBackgroundColor, defaults.HoverBackgroundColor),
                LockedBorderColor = ParseColorOrDefault(LockedBorderColor, defaults.LockedBorderColor),
                ErrorBorderColor = ParseColorOrDefault(ErrorBorderColor, defaults.ErrorBorderColor),
                WarningBorderColor = ParseColorOrDefault(WarningBorderColor, defaults.WarningBorderColor),
                IsLocked = IsLocked ?? defaults.IsLocked,
                AnchorX = AnchorX ?? defaults.AnchorX,
                AnchorY = AnchorY ?? defaults.AnchorY,
                PositionX = PositionX ?? defaults.PositionX,
                PositionY = PositionY ?? defaults.PositionY,
            };
            return settings.Normalize();
        }

        private static MediaColor ParseColorOrDefault(string? value, MediaColor fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : RgbaColor.ParseOrDefault(value, fallback);
        }
    }
}
