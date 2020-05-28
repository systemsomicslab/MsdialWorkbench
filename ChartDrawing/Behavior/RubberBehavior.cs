using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using CompMs.Graphics.Core.Adorner;

namespace CompMs.Graphics.Core.Behavior
{
    public class RubberBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseRightButtonDown += DrawRubberOnMouseRightButtonDown;
            AssociatedObject.MouseMove += DrawRubberOnMouseMove;
            AssociatedObject.MouseRightButtonUp += DrawRubberOnMouseRightButtonUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseRightButtonDown -= DrawRubberOnMouseRightButtonDown;
            AssociatedObject.MouseMove -= DrawRubberOnMouseMove;
            AssociatedObject.MouseRightButtonUp -= DrawRubberOnMouseRightButtonUp;
        }


        void DrawRubberOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            adornedElement = sender as FrameworkElement;
            if (adornedElement == null) return;
 
            current = e.GetPosition(adornedElement);
            adorner = new RubberAdorner(adornedElement, current);
            adornedElement.CaptureMouse();
        }

        void DrawRubberOnMouseMove(object sender, MouseEventArgs e)
        {
            if (adorner != null)
            {
                var previouse = current;
                current = e.GetPosition(adornedElement);
                adorner.Offset += current - previouse;
            }
        }

        void DrawRubberOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (adorner != null)
            {
                adornedElement.ReleaseMouseCapture();
                adorner.Detach();
                adorner = null;
            }
        }

        RubberAdorner adorner;
        Point current;
        FrameworkElement adornedElement;
    }
}
