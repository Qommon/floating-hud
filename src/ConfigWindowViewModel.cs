using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using MediaColor = System.Windows.Media.Color;

namespace FloatingHud;

internal sealed class ConfigWindowViewModel : INotifyPropertyChanged
{
    private string commandLine = string.Empty;
    private string defaultText = string.Empty;
    private string refreshIntervalText = string.Empty;
    private double fontSize;
    private double anchorX;
    private double anchorY;
    private string anchorXText = string.Empty;
    private string anchorYText = string.Empty;
    private string hoverBackgroundColorText = string.Empty;
    private string lockedBorderColorText = string.Empty;
    private string errorBorderColorText = string.Empty;
    private string warningBorderColorText = string.Empty;

    private ConfigWindowViewModel(HudSettings initialSettings)
    {
        InitialSettings = initialSettings;
        CommandLine = initialSettings.CommandLine;
        DefaultText = initialSettings.DefaultText;
        RefreshIntervalText = initialSettings.RefreshIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        FontSize = HudFontSize.GetLogicalFontSize(initialSettings);
        AnchorX = Math.Clamp(initialSettings.AnchorX, 0, 1);
        AnchorY = Math.Clamp(initialSettings.AnchorY, 0, 1);
        AnchorXText = AnchorX.ToString("0.###", CultureInfo.CurrentCulture);
        AnchorYText = AnchorY.ToString("0.###", CultureInfo.CurrentCulture);
        HoverBackgroundColorText = RgbaColor.ToString(initialSettings.HoverBackgroundColor);
        LockedBorderColorText = RgbaColor.ToString(initialSettings.LockedBorderColor);
        ErrorBorderColorText = RgbaColor.ToString(initialSettings.ErrorBorderColor);
        WarningBorderColorText = RgbaColor.ToString(initialSettings.WarningBorderColor);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public HudSettings InitialSettings { get; }

    public string CommandLine
    {
        get => commandLine;
        set => SetField(ref commandLine, value);
    }

    public string DefaultText
    {
        get => defaultText;
        set => SetField(ref defaultText, value);
    }

    public string RefreshIntervalText
    {
        get => refreshIntervalText;
        set => SetField(ref refreshIntervalText, value);
    }

    public double FontSize
    {
        get => fontSize;
        set => SetField(ref fontSize, value);
    }

    public double AnchorX
    {
        get => anchorX;
        set => SetField(ref anchorX, value);
    }

    public double AnchorY
    {
        get => anchorY;
        set => SetField(ref anchorY, value);
    }

    public string AnchorXText
    {
        get => anchorXText;
        set => SetField(ref anchorXText, value);
    }

    public string AnchorYText
    {
        get => anchorYText;
        set => SetField(ref anchorYText, value);
    }

    public string HoverBackgroundColorText
    {
        get => hoverBackgroundColorText;
        set => SetField(ref hoverBackgroundColorText, value);
    }

    public string LockedBorderColorText
    {
        get => lockedBorderColorText;
        set => SetField(ref lockedBorderColorText, value);
    }

    public string ErrorBorderColorText
    {
        get => errorBorderColorText;
        set => SetField(ref errorBorderColorText, value);
    }

    public string WarningBorderColorText
    {
        get => warningBorderColorText;
        set => SetField(ref warningBorderColorText, value);
    }

    public static ConfigWindowViewModel FromSettings(HudSettings settings)
    {
        return new ConfigWindowViewModel(settings.Normalize());
    }

    public static bool IsRefreshIntervalTextInRange(string text)
    {
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _);
    }

    public static bool TryParseAnchorText(string text, out double anchor)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out anchor) &&
            anchor is >= 0 and <= 1;
    }

    public bool TryCreateSettings(out HudSettings settings, out string errorMessage)
    {
        settings = InitialSettings;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(CommandLine))
        {
            errorMessage = "请输入命令行。";
            return false;
        }

        if (!int.TryParse(RefreshIntervalText, NumberStyles.None, CultureInfo.InvariantCulture, out int refreshIntervalSeconds))
        {
            errorMessage = $"刷新周期请输入 0 到 {int.MaxValue} 之间的整数。";
            return false;
        }

        if (!RgbaColor.TryParse(HoverBackgroundColorText, out MediaColor hoverBackgroundColor))
        {
            errorMessage = "请输入有效的悬停背景 RGBA 色值。";
            return false;
        }

        if (!RgbaColor.TryParse(LockedBorderColorText, out MediaColor lockedBorderColor))
        {
            errorMessage = "请输入有效的锁定外框 RGBA 色值。";
            return false;
        }

        if (!RgbaColor.TryParse(ErrorBorderColorText, out MediaColor errorBorderColor))
        {
            errorMessage = "请输入有效的错误外框 RGBA 色值。";
            return false;
        }

        if (!RgbaColor.TryParse(WarningBorderColorText, out MediaColor warningBorderColor))
        {
            errorMessage = "请输入有效的警告外框 RGBA 色值。";
            return false;
        }

        if (!TryParseAnchorText(AnchorXText, out double parsedAnchorX) ||
            !TryParseAnchorText(AnchorYText, out double parsedAnchorY))
        {
            errorMessage = "锚点请输入 0 到 1 之间的浮点数。";
            return false;
        }

        settings = HudFontSize.ApplyLogicalFontSize(
            InitialSettings with
            {
                CommandLine = CommandLine.Trim(),
                DefaultText = DefaultText,
                RefreshIntervalSeconds = refreshIntervalSeconds,
                AnchorX = parsedAnchorX,
                AnchorY = parsedAnchorY,
                HoverBackgroundColor = hoverBackgroundColor,
                LockedBorderColor = lockedBorderColor,
                ErrorBorderColor = errorBorderColor,
                WarningBorderColor = warningBorderColor,
            },
            FontSize).Normalize();
        return true;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
