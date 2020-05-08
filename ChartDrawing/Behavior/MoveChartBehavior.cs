using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Behavior
{
    public class MoveChartBehavior : Behavior<FrameworkElement>
    {
        public IDrawingChart DrawingChart
        {
            get => (IDrawingChart)GetValue(DrawingChartProperty);
            set => SetValue(DrawingChartProperty, value);
        }
        public static readonly DependencyProperty DrawingChartProperty = DependencyProperty.Register(
            nameof(DrawingChart), typeof(IDrawingChart), typeof(MoveChartBehavior),
            new PropertyMetadata(default(IDrawingChart))
            );

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += MoveOnMouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp += MoveOnMouseLeftButtonUp;
            AssociatedObject.MouseMove += MoveOnMouseMove;
            AssociatedObject.MouseLeave += MoveOnMouseLeave;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= MoveOnMouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp -= MoveOnMouseLeftButtonUp;
            AssociatedObject.MouseMove -= MoveOnMouseMove;
            AssociatedObject.MouseLeave -= MoveOnMouseLeave;
        }

        void MoveOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DrawingChart == null || DrawingChart.ChartArea == default) return;
            isMoving = true;
            previous = e.GetPosition(AssociatedObject);
        }
        void MoveOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMoving = false;
        }
        void MoveOnMouseMove(object sender, MouseEventArgs e)
        {
            if (DrawingChart == null || DrawingChart.ChartArea == default) return;
            if (isMoving)
            {
                var current = e.GetPosition(AssociatedObject);
                var v = DrawingChart.RealToImagine(current) - DrawingChart.RealToImagine(previous);
                var suggested = Rect.Offset(DrawingChart.ChartArea, -v);
                if (DrawingChart.InitialArea.Contains(suggested))
                    DrawingChart.ChartArea = suggested;
                previous = current;
            }
        }
        void MoveOnMouseLeave(object sender, MouseEventArgs e)
        {
            isMoving = false;
        }

        bool isMoving = false;
        Point previous;
    }
}
