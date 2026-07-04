using System.Globalization;
using System.Windows;
using System.Windows.Media;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaFontFamily = System.Windows.Media.FontFamily;
using MediaPen = System.Windows.Media.Pen;
using WindowsFlowDirection = System.Windows.FlowDirection;
using WindowsFontStyle = System.Windows.FontStyle;
using WindowsPoint = System.Windows.Point;
using WindowsSize = System.Windows.Size;
using WindowsSystemFonts = System.Windows.SystemFonts;

namespace FloatingHud;

public sealed class OutlinedTextBlock : FrameworkElement
{
    private const double HighlightBackgroundOutset = 4;
    private const double HighlightBorderOutset = 6;
    private const double ErrorBorderOutset = 9;
    private const double HighlightCornerRadius = 4;
    private const double HighlightBorderThickness = 1;
    private static readonly MediaBrush HitTestBrush = CreateHitTestBrush();

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(
            nameof(Fill),
            typeof(MediaBrush),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                MediaBrushes.White,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(
            nameof(Stroke),
            typeof(MediaBrush),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                MediaBrushes.Black,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HighlightBackgroundProperty =
        DependencyProperty.Register(
            nameof(HighlightBackground),
            typeof(MediaBrush),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HighlightBorderProperty =
        DependencyProperty.Register(
            nameof(HighlightBorder),
            typeof(MediaBrush),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ErrorBorderProperty =
        DependencyProperty.Register(
            nameof(ErrorBorder),
            typeof(MediaBrush),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                2d,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty =
        DependencyProperty.Register(
            nameof(FontFamily),
            typeof(MediaFontFamily),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                WindowsSystemFonts.MessageFontFamily,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.Register(
            nameof(FontSize),
            typeof(double),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                WindowsSystemFonts.MessageFontSize,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontWeightProperty =
        DependencyProperty.Register(
            nameof(FontWeight),
            typeof(FontWeight),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                FontWeights.Normal,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontStyleProperty =
        DependencyProperty.Register(
            nameof(FontStyle),
            typeof(WindowsFontStyle),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                FontStyles.Normal,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public MediaBrush Fill
    {
        get => (MediaBrush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public MediaBrush Stroke
    {
        get => (MediaBrush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public MediaBrush? HighlightBackground
    {
        get => (MediaBrush?)GetValue(HighlightBackgroundProperty);
        set => SetValue(HighlightBackgroundProperty, value);
    }

    public MediaBrush? HighlightBorder
    {
        get => (MediaBrush?)GetValue(HighlightBorderProperty);
        set => SetValue(HighlightBorderProperty, value);
    }

    public MediaBrush? ErrorBorder
    {
        get => (MediaBrush?)GetValue(ErrorBorderProperty);
        set => SetValue(ErrorBorderProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public MediaFontFamily FontFamily
    {
        get => (MediaFontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public WindowsFontStyle FontStyle
    {
        get => (WindowsFontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public WindowsPoint LocalPointToTextCanvasPoint(WindowsPoint localPoint)
    {
        WindowsPoint origin = GetTextOrigin();
        double scale = NormalizePositive(FontSize, WindowsSystemFonts.MessageFontSize);
        return new WindowsPoint(
            (localPoint.X - origin.X) / scale,
            (localPoint.Y - origin.Y) / scale);
    }

    public WindowsPoint TextCanvasPointToLocalPoint(WindowsPoint canvasPoint)
    {
        WindowsPoint origin = GetTextOrigin();
        double scale = NormalizePositive(FontSize, WindowsSystemFonts.MessageFontSize);
        return new WindowsPoint(
            origin.X + canvasPoint.X * scale,
            origin.Y + canvasPoint.Y * scale);
    }

    protected override WindowsSize MeasureOverride(WindowsSize availableSize)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return new WindowsSize(0, 0);
        }

        FormattedText formattedText = CreateFormattedText();
        Geometry textGeometry = formattedText.BuildGeometry(new WindowsPoint(0, 0));
        Rect bounds = textGeometry.Bounds;
        double padding = NormalizeNonNegative(StrokeThickness) * 2;
        double width = NormalizeNonNegative(bounds.Width) + padding;
        double height = NormalizeNonNegative(bounds.Height) + padding;

        return new WindowsSize(
            Math.Ceiling(width),
            Math.Ceiling(height));
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        drawingContext.DrawRectangle(HitTestBrush, null, new Rect(RenderSize));

        if (HighlightBackground is not null)
        {
            drawingContext.DrawRoundedRectangle(
                HighlightBackground,
                null,
                CreateExpandedRect(HighlightBackgroundOutset),
                HighlightCornerRadius,
                HighlightCornerRadius);
        }

        if (HighlightBorder is not null)
        {
            drawingContext.DrawRoundedRectangle(
                null,
                new MediaPen(HighlightBorder, HighlightBorderThickness),
                CreateExpandedRect(HighlightBorderOutset),
                HighlightCornerRadius,
                HighlightCornerRadius);
        }

        if (ErrorBorder is not null)
        {
            drawingContext.DrawRectangle(
                null,
                new MediaPen(ErrorBorder, HighlightBorderThickness),
                CreateExpandedRect(ErrorBorderOutset));
        }

        FormattedText formattedText = CreateFormattedText();
        WindowsPoint origin = GetTextOrigin(formattedText);

        Geometry shiftedTextGeometry = formattedText.BuildGeometry(origin);

        double strokeThickness = NormalizeNonNegative(StrokeThickness);
        if (strokeThickness > 0)
        {
            MediaPen strokePen = new(Stroke, strokeThickness * 2)
            {
                LineJoin = PenLineJoin.Round,
                MiterLimit = 1,
            };
            Geometry outsideStrokeGeometry = Geometry.Combine(
                shiftedTextGeometry.GetWidenedPathGeometry(strokePen),
                shiftedTextGeometry,
                GeometryCombineMode.Exclude,
                null);
            drawingContext.DrawGeometry(Stroke, null, outsideStrokeGeometry);
        }

        drawingContext.DrawGeometry(Fill, null, shiftedTextGeometry);
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        Rect interactiveBounds = new(RenderSize);
        return interactiveBounds.Contains(hitTestParameters.HitPoint)
            ? new PointHitTestResult(this, hitTestParameters.HitPoint)
            : null;
    }

    private FormattedText CreateFormattedText()
    {
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        Typeface typeface = new(FontFamily, FontStyle, FontWeight, FontStretches.Normal);

        return new FormattedText(
            Text,
            CultureInfo.CurrentUICulture,
            WindowsFlowDirection.LeftToRight,
            typeface,
            NormalizePositive(FontSize, WindowsSystemFonts.MessageFontSize),
            Fill,
            pixelsPerDip);
    }

    private Rect CreateExpandedRect(double outset)
    {
        Rect rect = new(RenderSize);
        rect.Inflate(outset, outset);
        return rect;
    }

    private WindowsPoint GetTextOrigin()
    {
        return GetTextOrigin(CreateFormattedText());
    }

    private WindowsPoint GetTextOrigin(FormattedText formattedText)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return new WindowsPoint(StrokeThickness, StrokeThickness);
        }

        Geometry textGeometry = formattedText.BuildGeometry(new WindowsPoint(0, 0));
        Rect bounds = textGeometry.Bounds;
        double strokeThickness = NormalizeNonNegative(StrokeThickness);
        return new WindowsPoint(
            strokeThickness - NormalizeFinite(bounds.Left),
            strokeThickness - NormalizeFinite(bounds.Top));
    }

    private static double NormalizePositive(double value, double fallback)
    {
        return double.IsFinite(value) && value > 0 ? value : fallback;
    }

    private static double NormalizeNonNegative(double value)
    {
        return double.IsFinite(value) && value > 0 ? value : 0;
    }

    private static double NormalizeFinite(double value)
    {
        return double.IsFinite(value) ? value : 0;
    }

    private static MediaBrush CreateHitTestBrush()
    {
        SolidColorBrush brush = new(MediaColor.FromArgb(1, 0, 0, 0));
        brush.Freeze();
        return brush;
    }
}
