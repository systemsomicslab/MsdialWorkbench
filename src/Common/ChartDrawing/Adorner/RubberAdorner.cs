using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Adorner
{
    internal class RubberAdorner : System.Windows.Documents.Adorner
    {
        public RubberAdorner(UIElement adornedElement, Point point) : base(adornedElement)
        {
            layer = AdornerLayer.GetAdornerLayer(adornedElement);
            SetInitialPoint(adornedElement, point);
            IsHitTestVisible = false;
            var brush = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
            brush.Freeze();
            SetRubberBrush(adornedElement, brush);
            var pen = new Pen(Brushes.DarkGray, 1);
            pen.Freeze();
            SetBorderPen(adornedElement, pen);
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

        public static readonly DependencyProperty RubberBrushProperty =
            DependencyProperty.RegisterAttached(
                "RubberBrush", typeof(Brush), typeof(RubberAdorner),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        public static Brush GetRubberBrush(DependencyObject d)
            => (Brush)d.GetValue(RubberBrushProperty);

        public static void SetRubberBrush(DependencyObject d, Brush value)
            => d.SetValue(RubberBrushProperty, value);

        public static readonly DependencyProperty BorderPenProperty =
            DependencyProperty.Register(
                "BorderPen", typeof(Pen), typeof(RubberAdorner),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

        public static Pen GetBorderPen(DependencyObject d)
            => (Pen)d.GetValue(BorderPenProperty);

        public static void SetBorderPen(DependencyObject d, Pen value)
            => d.SetValue(BorderPenProperty, value);

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
            drawingContext.DrawRectangle(
                GetRubberBrush(AdornedElement),
                GetBorderPen(AdornedElement),
                new Rect(GetInitialPoint(AdornedElement), Offset));
        }

        readonly AdornerLayer layer;
    }
}
