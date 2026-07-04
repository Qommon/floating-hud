using MediaColor = System.Windows.Media.Color;

namespace FloatingHud;

public sealed record HudContent
{
    public string? Text { get; init; }

    public string? TooltipText { get; init; }

    public string? FontName { get; init; }

    public bool? IsBold { get; init; }

    public bool? IsItalic { get; init; }

    public MediaColor? FontColor { get; init; }

    public double? StrokeThicknessRatio { get; init; }

    public MediaColor? StrokeColor { get; init; }

    public static HudContent CreateEmpty()
    {
        return new HudContent();
    }
}
