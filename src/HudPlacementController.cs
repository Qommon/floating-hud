using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPoint = System.Windows.Point;

namespace FloatingHud;

internal sealed class HudPlacementController(Window window, FrameworkElement hudElement)
{
    public void PlaceFullscreenOverlay()
    {
        window.Left = SystemParameters.VirtualScreenLeft;
        window.Top = SystemParameters.VirtualScreenTop;
        window.Width = SystemParameters.VirtualScreenWidth;
        window.Height = SystemParameters.VirtualScreenHeight;
        window.UpdateLayout();
    }

    public WindowsPoint GetScreenPosition(HudSettings settings)
    {
        if (PresentationSource.FromVisual(hudElement) is null)
        {
            return new WindowsPoint(settings.PositionX, settings.PositionY);
        }

        return hudElement.PointToScreen(GetAnchorLocalPoint(settings));
    }

    public void SetScreenPosition(WindowsPoint screenPoint, HudSettings settings)
    {
        if (PresentationSource.FromVisual(hudElement) is null)
        {
            SetCanvasPosition(screenPoint);
            return;
        }

        WindowsPoint currentAnchorScreenPoint = GetScreenPosition(settings);
        Vector deviceDelta = screenPoint - currentAnchorScreenPoint;
        MoveBy(DeviceVectorToDip(deviceDelta));
    }

    public WindowsPoint GetCanvasPosition()
    {
        return new WindowsPoint(GetCanvasLeft(hudElement), GetCanvasTop(hudElement));
    }

    public void SetCanvasPosition(WindowsPoint position)
    {
        Canvas.SetLeft(hudElement, position.X);
        Canvas.SetTop(hudElement, position.Y);
    }

    public void MoveBy(Vector dipDelta)
    {
        Canvas.SetLeft(hudElement, GetCanvasLeft(hudElement) + dipDelta.X);
        Canvas.SetTop(hudElement, GetCanvasTop(hudElement) + dipDelta.Y);
    }

    public Vector DeviceVectorToDip(Vector deviceVector)
    {
        PresentationSource? presentationSource = PresentationSource.FromVisual(window);
        return presentationSource?.CompositionTarget?.TransformFromDevice.Transform(deviceVector) ?? deviceVector;
    }

    public Rect GetScreenBounds()
    {
        Rect localBounds = new(hudElement.RenderSize);
        Rect windowBounds = hudElement.TransformToAncestor(window).TransformBounds(localBounds);
        WindowsPoint topLeft = window.PointToScreen(windowBounds.TopLeft);
        WindowsPoint bottomRight = window.PointToScreen(windowBounds.BottomRight);
        return new Rect(topLeft, bottomRight);
    }

    private WindowsPoint GetAnchorLocalPoint(HudSettings settings)
    {
        return new WindowsPoint(
            hudElement.RenderSize.Width * settings.AnchorX,
            hudElement.RenderSize.Height * settings.AnchorY);
    }

    private static double GetCanvasLeft(UIElement element)
    {
        double value = Canvas.GetLeft(element);
        return double.IsNaN(value) ? 0 : value;
    }

    private static double GetCanvasTop(UIElement element)
    {
        double value = Canvas.GetTop(element);
        return double.IsNaN(value) ? 0 : value;
    }
}
