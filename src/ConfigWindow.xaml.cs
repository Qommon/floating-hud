using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MediaColor = System.Windows.Media.Color;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;
using FormsColor = System.Drawing.Color;
using FormsColorDialog = System.Windows.Forms.ColorDialog;
using FormsDialogResult = System.Windows.Forms.DialogResult;
using FormsIWin32Window = System.Windows.Forms.IWin32Window;
using WpfBorder = System.Windows.Controls.Border;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfControl = System.Windows.Controls.Control;
using WpfDataFormats = System.Windows.DataFormats;
using WpfSlider = System.Windows.Controls.Slider;
using WpfTextBox = System.Windows.Controls.TextBox;
using WindowsMessageBox = System.Windows.MessageBox;

namespace FloatingHud;

public partial class ConfigWindow : Window
{
    private bool isUpdatingAnchorControls;
    private bool isUpdatingColorControls;
    private IReadOnlyList<ColorSettingControls> colorControls = Array.Empty<ColorSettingControls>();

    public ConfigWindow(HudSettings settings)
    {
        ViewModel = ConfigWindowViewModel.FromSettings(settings);
        DataContext = ViewModel;
        InitializeComponent();

        Settings = ViewModel.InitialSettings;
        colorControls = CreateColorControls();
        UpdateFontSizeValueText();
        UpdateAllColorControls();
    }

    public HudSettings Settings { get; private set; }

    private ConfigWindowViewModel ViewModel { get; }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.TryCreateSettings(out HudSettings settings, out string errorMessage))
        {
            WindowsMessageBox.Show(this, errorMessage, "配置", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Settings = settings;
        DialogResult = true;
    }

    private void RefreshIntervalInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = sender is WpfTextBox textBox && !IsRefreshIntervalEditAllowed(textBox, e.Text);
    }

    private void RefreshIntervalInput_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not WpfTextBox textBox ||
            !e.DataObject.GetDataPresent(WpfDataFormats.Text) ||
            e.DataObject.GetData(WpfDataFormats.Text) is not string pastedText ||
            !IsRefreshIntervalEditAllowed(textBox, pastedText))
        {
            e.CancelCommand();
        }
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateFontSizeValueText();
    }

    private void AnchorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateAnchorValueText();
    }

    private void AnchorInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (isUpdatingAnchorControls || sender is not WpfTextBox input)
        {
            return;
        }

        if (!ConfigWindowViewModel.TryParseAnchorText(input.Text, out double anchor))
        {
            input.BorderBrush = WpfBrushes.Red;
            input.BorderThickness = new Thickness(1);
            return;
        }

        input.ClearValue(WpfControl.BorderBrushProperty);
        input.ClearValue(WpfControl.BorderThicknessProperty);

        isUpdatingAnchorControls = true;
        try
        {
            if (input == AnchorXValueText)
            {
                AnchorXSlider.Value = anchor;
            }
            else if (input == AnchorYValueText)
            {
                AnchorYSlider.Value = anchor;
            }
        }
        finally
        {
            isUpdatingAnchorControls = false;
        }
    }

    private void HoverBackgroundColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        PickColorForPreview(HoverBackgroundColorPreview);
        e.Handled = true;
    }

    private void LockedBorderColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        PickColorForPreview(LockedBorderColorPreview);
        e.Handled = true;
    }

    private void ErrorBorderColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        PickColorForPreview(ErrorBorderColorPreview);
        e.Handled = true;
    }

    private void ColorAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isUpdatingColorControls || sender is not WpfSlider slider)
        {
            return;
        }

        if (FindColorControls(slider) is { } controls)
        {
            ApplyAlphaSliderToInput(controls);
        }
    }

    private void ColorInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (isUpdatingColorControls || sender is not WpfTextBox input)
        {
            return;
        }

        if (FindColorControls(input) is { } controls)
        {
            UpdateColorControls(controls);
        }
    }

    private void UpdateFontSizeValueText()
    {
        if (FontSizeValueText is null)
        {
            return;
        }

        FontSizeValueText.Text = FontSizeSlider.Value.ToString("0.0", CultureInfo.CurrentCulture);
    }

    private void UpdateAnchorValueText()
    {
        if (AnchorXValueText is null || AnchorYValueText is null)
        {
            return;
        }

        if (isUpdatingAnchorControls)
        {
            return;
        }

        isUpdatingAnchorControls = true;
        try
        {
            AnchorXValueText.Text = AnchorXSlider.Value.ToString("0.###", CultureInfo.CurrentCulture);
            AnchorYValueText.Text = AnchorYSlider.Value.ToString("0.###", CultureInfo.CurrentCulture);
            AnchorXValueText.ClearValue(WpfControl.BorderBrushProperty);
            AnchorXValueText.ClearValue(WpfControl.BorderThicknessProperty);
            AnchorYValueText.ClearValue(WpfControl.BorderBrushProperty);
            AnchorYValueText.ClearValue(WpfControl.BorderThicknessProperty);
        }
        finally
        {
            isUpdatingAnchorControls = false;
        }
    }

    private void UpdateAllColorControls()
    {
        foreach (ColorSettingControls controls in colorControls)
        {
            UpdateColorControls(controls);
        }
    }

    private void UpdateColorControls(ColorSettingControls controls)
    {
        if (!RgbaColor.TryParse(controls.Input.Text, out MediaColor color))
        {
            controls.Input.BorderBrush = WpfBrushes.Red;
            controls.Input.BorderThickness = new Thickness(1);
            return;
        }

        isUpdatingColorControls = true;
        try
        {
            controls.Input.ClearValue(WpfControl.BorderBrushProperty);
            controls.Input.ClearValue(WpfControl.BorderThicknessProperty);
            controls.AlphaSlider.Value = color.A;
            controls.Preview.Background = new MediaSolidColorBrush(MediaColor.FromArgb(0xFF, color.R, color.G, color.B));
        }
        finally
        {
            isUpdatingColorControls = false;
        }
    }

    private void ApplyAlphaSliderToInput(ColorSettingControls controls)
    {
        MediaColor currentColor = RgbaColor.TryParse(controls.Input.Text, out MediaColor parsedColor)
            ? parsedColor
            : controls.FallbackColor;
        byte alpha = (byte)Math.Clamp(Math.Round(controls.AlphaSlider.Value), byte.MinValue, byte.MaxValue);
        controls.Input.Text = RgbaColor.ToString(MediaColor.FromArgb(
            alpha,
            currentColor.R,
            currentColor.G,
            currentColor.B));
    }

    private void PickColorForPreview(WpfBorder preview)
    {
        if (FindColorControls(preview) is { } controls)
        {
            PickColor(controls);
        }
    }

    private void PickColor(ColorSettingControls controls)
    {
        MediaColor currentColor = RgbaColor.TryParse(controls.Input.Text, out MediaColor parsedColor)
            ? parsedColor
            : controls.FallbackColor;

        using FormsColorDialog dialog = new()
        {
            AllowFullOpen = true,
            AnyColor = true,
            FullOpen = true,
            Color = FormsColor.FromArgb(currentColor.R, currentColor.G, currentColor.B),
        };

        Win32WindowOwner owner = new(new WindowInteropHelper(this).Handle);
        if (dialog.ShowDialog(owner) != FormsDialogResult.OK)
        {
            return;
        }

        controls.Input.Text = RgbaColor.ToString(MediaColor.FromArgb(
            currentColor.A,
            dialog.Color.R,
            dialog.Color.G,
            dialog.Color.B));
    }

    private IReadOnlyList<ColorSettingControls> CreateColorControls()
    {
        return new[]
        {
            new ColorSettingControls(
                HoverBackgroundColorInput,
                HoverBackgroundAlphaSlider,
                HoverBackgroundColorPreview,
                Settings.HoverBackgroundColor),
            new ColorSettingControls(
                LockedBorderColorInput,
                LockedBorderAlphaSlider,
                LockedBorderColorPreview,
                Settings.LockedBorderColor),
            new ColorSettingControls(
                ErrorBorderColorInput,
                ErrorBorderAlphaSlider,
                ErrorBorderColorPreview,
                Settings.ErrorBorderColor),
        };
    }

    private ColorSettingControls? FindColorControls(WpfSlider slider)
    {
        return colorControls.FirstOrDefault(controls => controls.AlphaSlider == slider);
    }

    private ColorSettingControls? FindColorControls(WpfTextBox input)
    {
        return colorControls.FirstOrDefault(controls => controls.Input == input);
    }

    private ColorSettingControls? FindColorControls(WpfBorder preview)
    {
        return colorControls.FirstOrDefault(controls => controls.Preview == preview);
    }

    private static bool IsRefreshIntervalEditAllowed(WpfTextBox textBox, string insertedText)
    {
        string nextText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, insertedText);
        return string.IsNullOrEmpty(nextText) || ConfigWindowViewModel.IsRefreshIntervalTextInRange(nextText);
    }

    private sealed class Win32WindowOwner(nint handle) : FormsIWin32Window
    {
        public nint Handle { get; } = handle;
    }

    private sealed class ColorSettingControls(
        WpfTextBox input,
        WpfSlider alphaSlider,
        WpfBorder preview,
        MediaColor fallbackColor)
    {
        public WpfTextBox Input { get; } = input;

        public WpfSlider AlphaSlider { get; } = alphaSlider;

        public WpfBorder Preview { get; } = preview;

        public MediaColor FallbackColor { get; } = fallbackColor;
    }
}
