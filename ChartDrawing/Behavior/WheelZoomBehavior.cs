using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Behavior
{
    public class WheelZoomBehavior : Behavior<FrameworkElement>
    {
        public IDrawingChart DrawingChart
        {
            get => (IDrawingChart)GetValue(DrawingChartProperty);
            set => SetValue(DrawingChartProperty, value);
        }
        public static readonly DependencyProperty DrawingChartProperty = DependencyProperty.Register(
            nameof(DrawingChart), typeof(IDrawingChart), typeof(WheelZoomBehavior),
            new PropertyMetadata(default(IDrawingChart))
            );

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseWheel += ZoomOnMouseWheel;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseWheel -= ZoomOnMouseWheel;
        }

        void ZoomOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DrawingChart == null || DrawingChart.ChartArea == default) return;
            var p = e.GetPosition(AssociatedObject);
            var delta = e.Delta;
            var scale = 1 - 0.1 * Math.Sign(delta);

            var xmin = p.X * (1 - scale);
            var xmax = p.X + (DrawingChart.RenderSize.Width - p.X) * scale;
            var ymin = p.Y * (1 - scale);
            var ymax = p.Y + (DrawingChart.RenderSize.Height - p.Y) * scale;

            DrawingChart.ChartArea = Rect.Intersect(
                new Rect(
                    DrawingChart.RealToImagine(new Point(xmin, ymin)),
                    DrawingChart.RealToImagine(new Point(xmax, ymax))
                    ),
                DrawingChart.InitialArea
                );
        }
    }
}
