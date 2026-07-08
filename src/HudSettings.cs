using MediaColor = System.Windows.Media.Color;

namespace FloatingHud;

public sealed record HudSettings
{
    public string CommandLine { get; init; } = "echo {\"text\": \"Floating HUD\", \"fontColor\": \"#FFFFFFFF\", \"strokeThickness\": 0.1, \"strokeColor\": \"#000000FF\"}";

    public int RefreshIntervalSeconds { get; init; }

    public string DefaultText { get; init; } = "NO VALID OUTPUT";

    public double FontSize { get; init; } = 24;

    public double FontScale { get; init; } = 1;

    public MediaColor HoverBackgroundColor { get; init; } = MediaColor.FromArgb(0x40, 0x80, 0x80, 0x80);

    public MediaColor LockedBorderColor { get; init; } = MediaColor.FromArgb(0xBF, 0x80, 0x80, 0x80);

    public MediaColor ErrorBorderColor { get; init; } = MediaColor.FromArgb(0xFF, 0xFF, 0x00, 0x00);

    public MediaColor WarningBorderColor { get; init; } = MediaColor.FromArgb(0xFF, 0xFF, 0xD8, 0x00);

    public bool IsLocked { get; init; }

    public double AnchorX { get; init; }

    public double AnchorY { get; init; }

    public double PositionX { get; init; }

    public double PositionY { get; init; }

    public HudSettings Normalize()
    {
        HudFontRendering rendering = HudFontSize.CreateRendering(HudFontSize.GetLogicalFontSize(this));
        return this with
        {
            RefreshIntervalSeconds = Math.Max(0, RefreshIntervalSeconds),
            FontSize = rendering.IntegerFontSize,
            FontScale = rendering.Scale,
            AnchorX = ClampUnit(AnchorX),
            AnchorY = ClampUnit(AnchorY),
            PositionX = NormalizeCoordinate(PositionX),
            PositionY = NormalizeCoordinate(PositionY),
        };
    }

    private static double ClampUnit(double value)
    {
        return double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
    }

    private static double NormalizeCoordinate(double value)
    {
        return double.IsFinite(value) ? value : 0;
    }
}
