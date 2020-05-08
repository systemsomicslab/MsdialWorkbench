using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

using CompMs.Graphics.Core.Base;


namespace CompMs.Graphics.Core.Behavior
{
    public class ZoomChartBehavior : Behavior<FrameworkElement>
    {
        public IDrawingChart DrawingChart
        {
            get => (IDrawingChart)GetValue(DrawingChartProperty);
            set => SetValue(DrawingChartProperty, value);
        }
        public static readonly DependencyProperty DrawingChartProperty = DependencyProperty.Register(
            nameof(DrawingChart), typeof(IDrawingChart), typeof(ZoomChartBehavior),
            new PropertyMetadata(default(IDrawingChart))
            );

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseRightButtonDown += ZoomOnMouseRightButtonDown;
            AssociatedObject.MouseRightButtonUp += ZoomOnMouseRightButtonUp;
            AssociatedObject.MouseMove += ZoomOnMouseMove;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseRightButtonDown -= ZoomOnMouseRightButtonDown;
            AssociatedObject.MouseRightButtonUp -= ZoomOnMouseRightButtonUp;
            AssociatedObject.MouseMove -= ZoomOnMouseMove;
        }


        void ZoomOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DrawingChart == null || DrawingChart.ChartArea == default) return;

            adornedElement = AssociatedObject;
            initial = e.GetPosition(AssociatedObject);
            current = initial;
            adorner = new RubberAdorner(adornedElement, current);
            adornedElement.CaptureMouse();
        }

        void ZoomOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (adorner != null)
            {
                adornedElement.ReleaseMouseCapture();
                adorner.Detach();
                adorner = null;

                if (DrawingChart != null)
                {
                    var current = e.GetPosition(AssociatedObject);
                    var newarea = new Rect(DrawingChart.RealToImagine(initial), DrawingChart.RealToImagine(current));
                    if (Math.Abs(current.X - initial.X) <= 10)
                    {
                        newarea.X = DrawingChart.ChartArea.X;
                        newarea.Width = DrawingChart.ChartArea.Width;
                    }
                    if (Math.Abs(current.Y - initial.Y) <= 10)
                    {
                        newarea.Y = DrawingChart.ChartArea.Y;
                        newarea.Height = DrawingChart.ChartArea.Height;
                    }
                    DrawingChart.ChartArea = Rect.Intersect(newarea, DrawingChart.InitialArea);
                }
            }
        }

        void ZoomOnMouseMove(object sender, MouseEventArgs e)
        {
            if (adorner != null)
            {
                var previouse = current;
                current = e.GetPosition(adornedElement);
                adorner.Offset += current - previouse;
            }
        }

        RubberAdorner adorner;
        Point current;
        FrameworkElement adornedElement;
        Point initial;
    }
}
