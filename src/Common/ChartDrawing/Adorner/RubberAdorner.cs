using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Adorner;

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
        if (Invert) {
            drawingContext.DrawGeometry(
                GetRubberBrush(AdornedElement),
                GetBorderPen(AdornedElement),
                new CombinedGeometry(GeometryCombineMode.Exclude,
                    new RectangleGeometry(new Rect(AdornedElement.RenderSize)),
                    new RectangleGeometry(new Rect(GetInitialPoint(AdornedElement), Offset))));
        }
        else {
            drawingContext.DrawRectangle(
                GetRubberBrush(AdornedElement),
                GetBorderPen(AdornedElement),
                new Rect(GetInitialPoint(AdornedElement), Offset));
        }
    }

    readonly AdornerLayer layer;
}
