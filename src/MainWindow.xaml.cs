using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MediaColor = System.Windows.Media.Color;
using MediaFontFamily = System.Windows.Media.FontFamily;
using WindowsMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WindowsCursors = System.Windows.Input.Cursors;
using WindowsMenuItem = System.Windows.Controls.MenuItem;
using WindowsPoint = System.Windows.Point;

namespace FloatingHud;

public partial class MainWindow : Window
{
    private bool isDragging;
    private bool isDragRenderPending;
    private bool isCommandError;
    private bool isHudHovered;
    private bool isLocked;
    private bool isRefreshLoopRunning;
    private bool isRefreshRequested;
    private bool isShowingDefaultText = true;
    private HudSettings currentSettings = new();
    private HudContent currentContent = HudContent.CreateEmpty();
    private readonly CommandRunner commandRunner = new();
    private WindowsPoint dragStartScreenPoint;
    private WindowsPoint dragStartWindowPoint;
    private Vector pendingDragTranslation;
    private DispatcherTimer? refreshTimer;
    private DispatcherTimer? wheelAnchorResetTimer;
    private WindowsPoint? wheelAnchorScreenPoint;
    private WindowsPoint wheelAnchorCanvasPoint;
    private HudFontRendering currentFontRendering = HudFontSize.CreateRendering(24);
    private readonly ScaleTransform fontScaleTransform = new(1, 1);
    private readonly TranslateTransform dragTranslateTransform = new();
    private readonly TransformGroup hudRenderTransform = new();
    private readonly HudNativeWindowController nativeWindowController;
    private readonly HudPlacementController placementController;

    public MainWindow()
    {
        InitializeComponent();
        placementController = new HudPlacementController(this, HudText);
        nativeWindowController = new HudNativeWindowController(
            this,
            () => HudText.Visibility == Visibility.Visible,
            placementController.GetScreenBounds);
        InitializeHudRenderTransform();
        bool settingsFileExists = HudSettingsStore.Exists();
        currentSettings = HudSettingsStore.Load() ?? new HudSettings();
        ApplySettings(currentSettings);
        if (!settingsFileExists)
        {
            HudSettingsStore.Save(currentSettings);
        }

        Loaded += (_, _) =>
        {
            placementController.PlaceFullscreenOverlay();
            PlaceFromSettings();
            StartRefreshTimer();
            RequestRefreshFromCommand();
        };
        Closing += (_, _) =>
        {
            refreshTimer?.Stop();
            wheelAnchorResetTimer?.Stop();
            commandRunner.Dispose();
            ApplyPendingDragTranslation();
            CommitDragTranslate();
            HudText.CacheMode = null;
            nativeWindowController.Dispose();
            SaveCurrentSettings();
        };
        SourceInitialized += (_, _) => nativeWindowController.Initialize();
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
        };
    }

    private void PlaceFromSettings()
    {
        SetHudScreenPosition(new WindowsPoint(currentSettings.PositionX, currentSettings.PositionY));
    }

    private void InitializeHudRenderTransform()
    {
        hudRenderTransform.Children.Add(fontScaleTransform);
        hudRenderTransform.Children.Add(dragTranslateTransform);
        HudText.RenderTransform = hudRenderTransform;
    }

    private void HudText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (isLocked)
        {
            e.Handled = true;
            return;
        }

        CommitDragTranslate();
        isDragging = true;
        HudText.CacheMode = CreateDragBitmapCache();
        dragStartScreenPoint = PointToScreen(e.GetPosition(this));
        dragStartWindowPoint = placementController.GetCanvasPosition();
        HudText.CaptureMouse();
        e.Handled = true;
    }

    private void HudText_MouseMove(object sender, WindowsMouseEventArgs e)
    {
        if (isLocked || !isDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        WindowsPoint currentScreenPoint = PointToScreen(e.GetPosition(this));
        Vector deviceDelta = currentScreenPoint - dragStartScreenPoint;
        pendingDragTranslation = placementController.DeviceVectorToDip(deviceDelta);
        ScheduleDragRender();
    }

    private void HudText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        ApplyPendingDragTranslation();
        HudText.ReleaseMouseCapture();
        CommitDragTranslate();
        HudText.CacheMode = null;
        SaveCurrentSettings();
        e.Handled = true;
    }

    private void HudText_MouseEnter(object sender, WindowsMouseEventArgs e)
    {
        isHudHovered = true;
        UpdateHudInteractionVisuals();
    }

    private void HudText_MouseLeave(object sender, WindowsMouseEventArgs e)
    {
        isHudHovered = false;
        UpdateHudInteractionVisuals();
    }

    private void HudText_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (isLocked)
        {
            e.Handled = true;
            return;
        }

        double oldLogicalFontSize = HudFontSize.GetLogicalFontSize(currentSettings);
        double wheelNotches = e.Delta / 120d;
        double nextLogicalFontSize = Math.Clamp(
            oldLogicalFontSize + wheelNotches,
            HudLayoutLimits.MinFontSize,
            HudLayoutLimits.MaxFontSize);
        if (Math.Abs(nextLogicalFontSize - oldLogicalFontSize) < 0.001)
        {
            return;
        }

        StartOrContinueWheelAnchor(e.GetPosition(HudText));

        ApplyLogicalFontSize(nextLogicalFontSize);
        UpdateLayout();

        WindowsPoint adjustedAnchorLocalPoint = HudText.TextCanvasPointToLocalPoint(wheelAnchorCanvasPoint);
        WindowsPoint adjustedAnchorScreenPoint = HudText.PointToScreen(adjustedAnchorLocalPoint);
        Vector deviceDelta = wheelAnchorScreenPoint!.Value - adjustedAnchorScreenPoint;
        Vector dipDelta = placementController.DeviceVectorToDip(deviceDelta);
        placementController.MoveBy(dipDelta);

        e.Handled = true;
    }

    private void StartOrContinueWheelAnchor(WindowsPoint localPoint)
    {
        if (wheelAnchorScreenPoint is null)
        {
            wheelAnchorScreenPoint = HudNativeWindowController.GetCursorScreenPoint();
            wheelAnchorCanvasPoint = HudText.LocalPointToTextCanvasPoint(localPoint);
        }

        wheelAnchorResetTimer ??= CreateWheelAnchorResetTimer();
        wheelAnchorResetTimer.Stop();
        wheelAnchorResetTimer.Start();
    }

    private DispatcherTimer CreateWheelAnchorResetTimer()
    {
        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(180),
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            wheelAnchorScreenPoint = null;
            SaveCurrentSettings();
        };
        return timer;
    }

    private void LockMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ApplyPendingDragTranslation();
        CommitDragTranslate();
        HudText.CacheMode = null;
        isLocked = !isLocked;
        isDragging = false;
        HudText.ReleaseMouseCapture();
        HudText.Cursor = isLocked ? WindowsCursors.Arrow : WindowsCursors.SizeAll;
        UpdateHudInteractionVisuals();

        if (sender is WindowsMenuItem menuItem)
        {
            menuItem.Header = isLocked ? "解锁" : "锁定";
        }
    }

    private void ConfigureMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ConfigWindow configWindow = new(GetCurrentSettings())
        {
            Owner = this,
        };

        if (configWindow.ShowDialog() == true)
        {
            WindowsPoint anchorScreenPoint = GetHudScreenPosition();
            ApplySettings(configWindow.Settings);
            if (isShowingDefaultText && HudText.Visibility == Visibility.Visible)
            {
                ApplyContent(currentContent);
            }

            UpdateLayout();
            SetHudScreenPosition(anchorScreenPoint);
            SaveCurrentSettings();
            StartRefreshTimer();
            RequestRefreshFromCommand();
        }
    }

    private void ClearErrorLogsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        RefreshErrorLogger.Clear();
    }

    private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ApplySettings(HudSettings settings)
    {
        currentSettings = settings.Normalize();
        double logicalFontSize = HudFontSize.GetLogicalFontSize(currentSettings);
        ApplyLogicalFontSize(logicalFontSize);
        UpdateHudInteractionVisuals();
    }

    private void ApplyContent(HudContent content)
    {
        currentContent = content;
        HudText.Text = content.Text ?? currentSettings.DefaultText;
        HudText.ToolTip = string.IsNullOrEmpty(content.TooltipText) ? null : content.TooltipText;

        if (content.FontName is { Length: > 0 } fontName)
        {
            HudText.FontFamily = new MediaFontFamily(fontName);
        }
        else
        {
            HudText.ClearValue(OutlinedTextBlock.FontFamilyProperty);
        }

        if (content.IsBold is { } isBold)
        {
            HudText.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
        }
        else
        {
            HudText.ClearValue(OutlinedTextBlock.FontWeightProperty);
        }

        if (content.IsItalic is { } isItalic)
        {
            HudText.FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal;
        }
        else
        {
            HudText.ClearValue(OutlinedTextBlock.FontStyleProperty);
        }

        if (content.FontColor is { } fontColor)
        {
            HudText.Fill = new SolidColorBrush(fontColor);
        }
        else
        {
            HudText.ClearValue(OutlinedTextBlock.FillProperty);
        }

        ApplyContentStroke();
    }

    private void ApplyLogicalFontSize(double logicalFontSize)
    {
        currentFontRendering = HudFontSize.CreateRendering(logicalFontSize);
        HudText.FontSize = currentFontRendering.IntegerFontSize;
        fontScaleTransform.ScaleX = currentFontRendering.Scale;
        fontScaleTransform.ScaleY = currentFontRendering.Scale;
        currentSettings = HudFontSize.ApplyLogicalFontSize(currentSettings, currentFontRendering.LogicalFontSize);
        ApplyContentStroke();
    }

    private void ApplyContentStroke()
    {
        if (currentContent.StrokeThicknessRatio is { } strokeThicknessRatio &&
            currentContent.StrokeColor is { } strokeColor)
        {
            HudText.StrokeThickness = GetRenderLocalStrokeThickness(strokeThicknessRatio);
            HudText.Stroke = new SolidColorBrush(strokeColor);
            return;
        }

        HudText.StrokeThickness = 0;
        HudText.ClearValue(OutlinedTextBlock.StrokeProperty);
    }

    private double GetRenderLocalStrokeThickness(double strokeThicknessRatio)
    {
        double logicalStrokeThickness = currentFontRendering.LogicalFontSize * strokeThicknessRatio;
        return logicalStrokeThickness / currentFontRendering.Scale;
    }

    private void UpdateHudInteractionVisuals()
    {
        HudText.HighlightBackground = isHudHovered
            ? CreateFrozenBrush(currentSettings.HoverBackgroundColor)
            : null;
        HudText.HighlightBorder = isHudHovered && isLocked
            ? CreateFrozenBrush(currentSettings.LockedBorderColor)
            : null;
        HudText.ErrorBorder = isCommandError
            ? CreateFrozenBrush(currentSettings.ErrorBorderColor)
            : null;
    }

    private static SolidColorBrush CreateFrozenBrush(MediaColor color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

    private HudSettings GetCurrentSettings()
    {
        WindowsPoint screenPosition = GetHudScreenPosition();
        return currentSettings with
        {
            PositionX = screenPosition.X,
            PositionY = screenPosition.Y,
        };
    }

    private void SaveCurrentSettings()
    {
        currentSettings = GetCurrentSettings();
        HudSettingsStore.Save(currentSettings);
    }

    private void StartRefreshTimer()
    {
        refreshTimer?.Stop();
        refreshTimer = null;

        if (currentSettings.RefreshIntervalSeconds == 0)
        {
            return;
        }

        refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(currentSettings.RefreshIntervalSeconds),
        };
        refreshTimer.Tick += (_, _) => RequestRefreshFromCommand();
        refreshTimer.Start();
    }

    private void RequestRefreshFromCommand()
    {
        if (string.IsNullOrWhiteSpace(currentSettings.CommandLine))
        {
            return;
        }

        isRefreshRequested = true;
        if (isRefreshLoopRunning)
        {
            commandRunner.TimeoutActiveExecution();
            return;
        }

        _ = RunRefreshLoopAsync();
    }

    private async Task RunRefreshLoopAsync()
    {
        if (isRefreshLoopRunning)
        {
            return;
        }

        isRefreshLoopRunning = true;

        try
        {
            while (isRefreshRequested)
            {
                isRefreshRequested = false;
                await RefreshFromCommandAsync();
            }
        }
        finally
        {
            isRefreshLoopRunning = false;
            if (isRefreshRequested)
            {
                _ = RunRefreshLoopAsync();
            }
        }
    }

    private async Task RefreshFromCommandAsync()
    {
        string commandLine = currentSettings.CommandLine;
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return;
        }

        try
        {
            CommandResult result = await commandRunner.RunAsync(commandLine);
            HudContentParseResult parseResult = HudContentParser.ParseCommandOutput(result.StandardOutput, currentContent);
            HudContent content = parseResult.Content;
            List<string> errors = parseResult.Errors;
            if (result.TimedOut)
            {
                errors.Add("Command timed out and was terminated.");
            }

            isCommandError = errors.Count > 0;
            if (isCommandError)
            {
                RefreshErrorLogger.Write(commandLine, result, errors);
            }

            isShowingDefaultText = content.Text is null;
            WindowsPoint anchorScreenPoint = GetHudScreenPosition();
            ApplyContent(content);
            UpdateLayout();
            SetHudScreenPosition(anchorScreenPoint);
            UpdateHudInteractionVisuals();
            ShowHudText();
        }
        catch (Exception exception)
        {
            RefreshErrorLogger.Write(
                commandLine,
                null,
                new[] { $"Command refresh failed: {exception.GetType().Name}: {exception.Message}" });
            isCommandError = true;
            UpdateHudInteractionVisuals();
            if (HudText.Visibility != Visibility.Visible)
            {
                ApplyContent(currentContent);
                isShowingDefaultText = currentContent.Text is null;
                ShowHudText();
            }
        }
    }

    private void ShowHudText()
    {
        if (HudText.Visibility != Visibility.Visible)
        {
            HudText.Visibility = Visibility.Visible;
        }
    }

    private WindowsPoint GetHudScreenPosition()
    {
        return placementController.GetScreenPosition(currentSettings);
    }

    private void SetHudScreenPosition(WindowsPoint screenPoint)
    {
        placementController.SetScreenPosition(screenPoint, currentSettings);
    }

    private void ScheduleDragRender()
    {
        if (isDragRenderPending)
        {
            return;
        }

        isDragRenderPending = true;
        CompositionTarget.Rendering += DragRenderingFrame;
    }

    private void DragRenderingFrame(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= DragRenderingFrame;
        isDragRenderPending = false;
        ApplyPendingDragTranslation();
    }

    private void ApplyPendingDragTranslation()
    {
        dragTranslateTransform.X = pendingDragTranslation.X;
        dragTranslateTransform.Y = pendingDragTranslation.Y;
    }

    private void CommitDragTranslate()
    {
        if (Math.Abs(dragTranslateTransform.X) < 0.001 &&
            Math.Abs(dragTranslateTransform.Y) < 0.001)
        {
            return;
        }

        placementController.SetCanvasPosition(new WindowsPoint(
            dragStartWindowPoint.X + dragTranslateTransform.X,
            dragStartWindowPoint.Y + dragTranslateTransform.Y));
        dragTranslateTransform.X = 0;
        dragTranslateTransform.Y = 0;
        pendingDragTranslation = default;
    }

    private static BitmapCache CreateDragBitmapCache()
    {
        return new BitmapCache
        {
            EnableClearType = true,
            RenderAtScale = 1,
            SnapsToDevicePixels = true,
        };
    }
}
