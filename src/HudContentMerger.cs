namespace FloatingHud;

internal static class HudContentMerger
{
    public static HudContent Merge(HudContent currentContent, HudCommandOutputPatch patch)
    {
        HudContent content = currentContent;

        if (patch.Text is not null)
        {
            content = content with { Text = patch.Text };
        }

        if (patch.TooltipText is not null)
        {
            content = content with { TooltipText = patch.TooltipText };
        }

        if (patch.FontName is not null)
        {
            content = content with { FontName = patch.FontName };
        }

        if (patch.IsBold is { } isBold)
        {
            content = content with { IsBold = isBold };
        }

        if (patch.IsItalic is { } isItalic)
        {
            content = content with { IsItalic = isItalic };
        }

        if (patch.FontColor is { } fontColor)
        {
            content = content with { FontColor = fontColor };
        }

        if (patch.StrokeThicknessRatio is { } strokeThicknessRatio)
        {
            content = content with { StrokeThicknessRatio = strokeThicknessRatio };
        }

        if (patch.StrokeColor is { } strokeColor)
        {
            content = content with { StrokeColor = strokeColor };
        }

        return content;
    }
}
