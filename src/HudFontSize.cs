namespace FloatingHud;

public readonly record struct HudFontRendering(
    double LogicalFontSize,
    int IntegerFontSize,
    double Scale);

public static class HudFontSize
{
    public static double GetLogicalFontSize(HudSettings settings)
    {
        double fontSize = IsUsable(settings.FontSize) ? settings.FontSize : 24;
        double fontScale = IsUsable(settings.FontScale) && settings.FontScale > 0 ? settings.FontScale : 1;
        return Clamp(fontSize * fontScale);
    }

    public static HudSettings ApplyLogicalFontSize(HudSettings settings, double logicalFontSize)
    {
        HudFontRendering rendering = CreateRendering(logicalFontSize);
        return settings with
        {
            FontSize = rendering.IntegerFontSize,
            FontScale = rendering.Scale,
        };
    }

    public static HudFontRendering CreateRendering(double logicalFontSize)
    {
        double clampedFontSize = Clamp(logicalFontSize);
        int integerFontSize = (int)Math.Clamp(
            Math.Round(clampedFontSize, MidpointRounding.AwayFromZero),
            HudLayoutLimits.MinFontSize,
            HudLayoutLimits.MaxFontSize);
        double scale = clampedFontSize / integerFontSize;
        return new HudFontRendering(clampedFontSize, integerFontSize, scale);
    }

    public static double Clamp(double logicalFontSize)
    {
        return Math.Clamp(
            IsUsable(logicalFontSize) ? logicalFontSize : 24,
            HudLayoutLimits.MinFontSize,
            HudLayoutLimits.MaxFontSize);
    }

    private static bool IsUsable(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
