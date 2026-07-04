using System.Text.Json;

namespace FloatingHud;

internal sealed class HudCommandOutputDto
{
    public JsonElement? Text { get; init; }

    public JsonElement? TooltipText { get; init; }

    public JsonElement? FontName { get; init; }

    public JsonElement? IsBold { get; init; }

    public JsonElement? IsItalic { get; init; }

    public JsonElement? FontColor { get; init; }

    public JsonElement? StrokeThickness { get; init; }

    public JsonElement? StrokeColor { get; init; }

    public static HudCommandOutputDto FromJsonObject(JsonElement root)
    {
        return new HudCommandOutputDto
        {
            Text = GetOptionalProperty(root, "text"),
            TooltipText = GetOptionalProperty(root, "tooltipText"),
            FontName = GetOptionalProperty(root, "fontName"),
            IsBold = GetOptionalProperty(root, "isBold"),
            IsItalic = GetOptionalProperty(root, "isItalic"),
            FontColor = GetOptionalProperty(root, "fontColor"),
            StrokeThickness = GetOptionalProperty(root, "strokeThickness"),
            StrokeColor = GetOptionalProperty(root, "strokeColor"),
        };
    }

    private static JsonElement? GetOptionalProperty(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out JsonElement property)
            ? property.Clone()
            : null;
    }
}
