using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Adorner;

internal enum RubberShape {
    None,
    Rectangle,
    Horizontal,
    Vertical,
}

internal class RubberAdorner : System.Windows.Documents.Adorner
{
    public RubberAdorner(UIElement adornedElement, Point point) : base(adornedElement)
    {
        layer = AdornerLayer.GetAdornerLayer(adornedElement);
        SetInitialPoint(adornedElement, point);
        IsHitTestVisible = false;
    }

    public static readonly DependencyProperty InitialPointProperty =
        DependencyProperty.RegisterAttached(
            "InitialPoint", typeof(Point), typeof(RubberAdorner),
            new FrameworkPropertyMetadata(
                default(Point),
                FrameworkPropertyMetadataOptions.AffectsRender));
    
    public static Point GetInitialPoint(DependencyObject d)
        => (Point)d.GetValue(InitialPointProperty);

    public static void SetInitialPoint(DependencyObject d, Point value)
        => d.SetValue(InitialPointProperty, value);

    public static readonly DependencyProperty OffsetProperty =
        DependencyProperty.Register(
            nameof(Offset), typeof(Vector), typeof(RubberAdorner),
            new FrameworkPropertyMetadata(
                default(Vector),
                FrameworkPropertyMetadataOptions.AffectsRender));

    public Vector Offset
    {
        get => (Vector)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    private static Brush CreateDefaultRubberBrush() {
        var brush = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
        brush.Freeze();
        return brush;
    }

    public static readonly DependencyProperty RubberBrushProperty =
        DependencyProperty.RegisterAttached(
            "RubberBrush", typeof(Brush), typeof(RubberAdorner),
            new FrameworkPropertyMetadata(
                CreateDefaultRubberBrush(),
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

    public static Brush GetRubberBrush(DependencyObject d)
        => (Brush)d.GetValue(RubberBrushProperty);

    public static void SetRubberBrush(DependencyObject d, Brush value)
        => d.SetValue(RubberBrushProperty, value);

    private static Pen CreateDefaultBorderPen() {
        var pen = new Pen(Brushes.DarkGray, 1);
        pen.Freeze();
        return pen;
    }

    public static readonly DependencyProperty BorderPenProperty =
        DependencyProperty.Register(
            "BorderPen", typeof(Pen), typeof(RubberAdorner),
            new FrameworkPropertyMetadata(
                CreateDefaultBorderPen(),
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

    public static Pen GetBorderPen(DependencyObject d)
        => (Pen)d.GetValue(BorderPenProperty);

    public static void SetBorderPen(DependencyObject d, Pen value)
        => d.SetValue(BorderPenProperty, value);

    public bool Invert { get; set; } = false;

    public RubberShape Shape { get; set; } = RubberShape.Rectangle;

    public void Attach()
    {
        if (layer != null)
            layer.Add(this);
    }

    public void Detach()
    {
        if (layer != null)
            layer.Remove(this);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        Rect area = default;
        switch (Shape) {
            case RubberShape.Rectangle:
                area = new Rect(GetInitialPoint(AdornedElement), Offset);
                break;
            case RubberShape.Horizontal:
                if (Offset.X < 0d) {
                    area = new Rect(GetInitialPoint(AdornedElement).X + Offset.X, 0d, -Offset.X, AdornedElement.RenderSize.Height);
                }
                else {
                    area = new Rect(GetInitialPoint(AdornedElement).X, 0d, Offset.X, AdornedElement.RenderSize.Height);
                }
                break;
            case RubberShape.Vertical:
                if (Offset.Y < 0d) {
                    area = new Rect(0d, GetInitialPoint(AdornedElement).Y + Offset.Y, AdornedElement.RenderSize.Width, -Offset.Y);
                }
                else {
                    area = new Rect(0d, GetInitialPoint(AdornedElement).Y, AdornedElement.RenderSize.Width, Offset.Y);
                }
                break;
        }
        if (Invert) {
            drawingContext.DrawGeometry(
                GetRubberBrush(AdornedElement),
                GetBorderPen(AdornedElement),
                new CombinedGeometry(GeometryCombineMode.Exclude,
                    new RectangleGeometry(new Rect(AdornedElement.RenderSize)),
                    new RectangleGeometry(area)));
        }
        else {
            drawingContext.DrawRectangle(
                GetRubberBrush(AdornedElement),
                GetBorderPen(AdornedElement),
                area);
        }
    }

    readonly AdornerLayer layer;
}
