using System.Text.Json;
using MediaColor = System.Windows.Media.Color;

namespace FloatingHud;

internal static class HudCommandOutputValidator
{
    private const int MaxTextLength = 256;
    private const int MaxTooltipTextLength = 1024;
    private const double MaxStrokeThicknessRatio = 0.25;

    public static HudCommandOutputValidationResult Validate(HudCommandOutputDto dto)
    {
        List<string> errors = new();
        HudCommandOutputPatch patch = new();

        if (TryGetStringProperty(dto.Text, "text", errors, out string text))
        {
            patch = patch with
            {
                Text = LimitLength(text, MaxTextLength),
            };
        }

        if (TryGetStringProperty(dto.TooltipText, "tooltipText", errors, out string tooltipText))
        {
            patch = patch with
            {
                TooltipText = LimitLength(tooltipText, MaxTooltipTextLength),
            };
        }

        if (TryGetStringProperty(dto.FontName, "fontName", errors, out string fontName))
        {
            if (!string.IsNullOrWhiteSpace(fontName))
            {
                patch = patch with
                {
                    FontName = fontName,
                };
            }
            else
            {
                errors.Add("fontName must not be empty when provided.");
            }
        }

        if (TryGetBooleanProperty(dto.IsBold, "isBold", errors, out bool isBold))
        {
            patch = patch with
            {
                IsBold = isBold,
            };
        }

        if (TryGetBooleanProperty(dto.IsItalic, "isItalic", errors, out bool isItalic))
        {
            patch = patch with
            {
                IsItalic = isItalic,
            };
        }

        if (TryGetStringProperty(dto.FontColor, "fontColor", errors, out string fontColor))
        {
            if (RgbaColor.TryParse(fontColor, out MediaColor parsedFontColor))
            {
                patch = patch with
                {
                    FontColor = parsedFontColor,
                };
            }
            else
            {
                errors.Add("fontColor must be a valid RGB/RGBA hex color.");
            }
        }

        if (TryGetDoubleProperty(dto.StrokeThickness, "strokeThickness", errors, out double strokeThicknessRatio))
        {
            if (strokeThicknessRatio >= 0 &&
                !double.IsNaN(strokeThicknessRatio) &&
                !double.IsInfinity(strokeThicknessRatio))
            {
                patch = patch with
                {
                    StrokeThicknessRatio = Math.Min(strokeThicknessRatio, MaxStrokeThicknessRatio),
                };
            }
            else
            {
                errors.Add("strokeThickness must be a finite non-negative number.");
            }
        }

        if (TryGetStringProperty(dto.StrokeColor, "strokeColor", errors, out string strokeColor))
        {
            if (RgbaColor.TryParse(strokeColor, out MediaColor parsedStrokeColor))
            {
                patch = patch with
                {
                    StrokeColor = parsedStrokeColor,
                };
            }
            else
            {
                errors.Add("strokeColor must be a valid RGB/RGBA hex color.");
            }
        }

        return new HudCommandOutputValidationResult(patch, errors);
    }

    private static string LimitLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static bool TryGetStringProperty(
        JsonElement? property,
        string propertyName,
        List<string> errors,
        out string value)
    {
        if (property is null)
        {
            value = string.Empty;
            return false;
        }

        if (property.Value.ValueKind == JsonValueKind.String)
        {
            value = property.Value.GetString() ?? string.Empty;
            return true;
        }

        errors.Add($"{propertyName} must be a string.");
        value = string.Empty;
        return false;
    }

    private static bool TryGetDoubleProperty(
        JsonElement? property,
        string propertyName,
        List<string> errors,
        out double value)
    {
        if (property is null)
        {
            value = 0;
            return false;
        }

        if (property.Value.ValueKind == JsonValueKind.Number &&
            property.Value.TryGetDouble(out value))
        {
            return true;
        }

        errors.Add($"{propertyName} must be a number.");
        value = 0;
        return false;
    }

    private static bool TryGetBooleanProperty(
        JsonElement? property,
        string propertyName,
        List<string> errors,
        out bool value)
    {
        if (property is null)
        {
            value = false;
            return false;
        }

        if (property.Value.ValueKind == JsonValueKind.True ||
            property.Value.ValueKind == JsonValueKind.False)
        {
            value = property.Value.GetBoolean();
            return true;
        }

        errors.Add($"{propertyName} must be a boolean.");
        value = false;
        return false;
    }
}
