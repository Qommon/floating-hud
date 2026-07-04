using System.Globalization;
using MediaColor = System.Windows.Media.Color;

namespace FloatingHud;

public static class RgbaColor
{
    public static bool TryParse(string value, out MediaColor color)
    {
        string trimmedValue = value.Trim();
        if (trimmedValue.Count(static character => character == '#') > 1)
        {
            color = default;
            return false;
        }

        string normalizedValue = trimmedValue.StartsWith('#')
            ? trimmedValue[1..]
            : trimmedValue;
        if (normalizedValue.Contains('#'))
        {
            color = default;
            return false;
        }

        if (normalizedValue.Length == 6 &&
            uint.TryParse(normalizedValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
        {
            color = MediaColor.FromArgb(
                0xFF,
                (byte)(rgb >> 16),
                (byte)(rgb >> 8),
                (byte)rgb);
            return true;
        }

        if (normalizedValue.Length == 8 &&
            uint.TryParse(normalizedValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgba))
        {
            color = MediaColor.FromArgb(
                (byte)rgba,
                (byte)(rgba >> 24),
                (byte)(rgba >> 16),
                (byte)(rgba >> 8));
            return true;
        }

        color = default;
        return false;
    }

    public static MediaColor ParseOrDefault(string value, MediaColor fallback)
    {
        return TryParse(value, out MediaColor color) ? color : fallback;
    }

    public static string ToString(MediaColor color)
    {
        return FormattableString.Invariant($"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}");
    }
}
