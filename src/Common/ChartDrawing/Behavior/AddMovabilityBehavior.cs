using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Adorner;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Behavior
{
    public static class AddMovabilityBehavior
    {
        public static readonly DependencyProperty PositionBaseProperty =
            DependencyProperty.RegisterAttached(
                "PositionBase",
                typeof(FrameworkElement),
                typeof(AddMovabilityBehavior),
                new PropertyMetadata(
                    null,
                    OnPositionBaseChanged));

        public static FrameworkElement GetPositionBase(DependencyObject d)
            => (FrameworkElement)d.GetValue(PositionBaseProperty);
        public static void SetPositionBase(DependencyObject d, FrameworkElement value)
            => d.SetValue(PositionBaseProperty, value);

        private static void OnPositionBaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe) {
                if (e.OldValue != null) {
                    OnDetached(fe);
                }
                if (e.OldValue is null && e.NewValue != null) {
                    fe.RenderTransform = new MatrixTransform(new Matrix(1, 0, 0, 1, 0, 0));
                }
                if (e.NewValue is null) {
                    fe.RenderTransform = null;
                }
                if (e.NewValue != null && GetIsEnabled(d)) {
                    OnAttaching(fe);
                }
            }
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(AddMovabilityBehavior),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.TrueBox,
                    FrameworkPropertyMetadataOptions.Inherits,
                    OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject d)
            => (bool)d.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject d, bool value)
            => d.SetValue(IsEnabledProperty, BooleanBoxes.Box(value));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe) {
                FrameworkElement positionBase = GetPositionBase(d);
                if ((bool)e.NewValue) {
                    if (positionBase != null) {
                        OnAttaching(fe);
                    }
                }
                else {
                    OnDetached(fe);
                }
            }
        }

        private static readonly DependencyProperty IsMovingProperty =
            DependencyProperty.RegisterAttached("IsMoving", typeof(bool), typeof(AddMovabilityBehavior));
        private static bool GetIsMoving(DependencyObject d)
            => (bool)d.GetValue(IsMovingProperty);
        private static void SetIsMoving(DependencyObject d, bool value)
            => d.SetValue(IsMovingProperty, value);

        private static readonly DependencyProperty MoveInitialPointProperty =
            DependencyProperty.RegisterAttached("MoveInitialPoint", typeof(Point), typeof(AddMovabilityBehavior));
        private static Point GetMoveInitialPoint(DependencyObject d)
            => (Point)d.GetValue(MoveInitialPointProperty);
        private static void SetMoveInitialPoint(DependencyObject d, Point value)
            => d.SetValue(MoveInitialPointProperty, value);

        private static readonly DependencyProperty MoveInitialMatrixProperty =
            DependencyProperty.RegisterAttached("MoveInitialMatrix", typeof(Matrix), typeof(AddMovabilityBehavior));
        private static Matrix GetMoveInitialMatrix(DependencyObject d)
            => (Matrix)d.GetValue(MoveInitialMatrixProperty);
        private static void SetMoveInitialMatrix(DependencyObject d, Matrix value)
            => d.SetValue(MoveInitialMatrixProperty, value);

        private static readonly DependencyProperty DragRubberProperty =
            DependencyProperty.RegisterAttached("DragRubber", typeof(RubberAdorner), typeof(AddMovabilityBehavior));
        private static RubberAdorner GetDragRubber(DependencyObject d)
            => (RubberAdorner)d.GetValue(DragRubberProperty);
        private static void SetDragRubber(DependencyObject d, RubberAdorner value)
            => d.SetValue(DragRubberProperty, value);

        private static readonly DependencyProperty DragInitialPointProperty =
            DependencyProperty.RegisterAttached("DragInitialPoint", typeof(Point), typeof(AddMovabilityBehavior));
        private static Point GetDragInitialPoint(DependencyObject d)
            => (Point)d.GetValue(DragInitialPointProperty);
        private static void SetDragInitialPoint(DependencyObject d, Point value)
            => d.SetValue(DragInitialPointProperty, value);

        private static void OnDetached(FrameworkElement fe) {
            fe.MouseWheel -= OnMouseWheel;
            fe.MouseMove -= OnMouseMove;
            fe.MouseLeave -= OnMouseLeave;
            fe.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            fe.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            fe.MouseRightButtonUp -= OnMouseRightButtonUp;
            fe.MouseRightButtonDown -= OnMouseRightButtonDown;
        }

        private static void OnAttaching(FrameworkElement fe) {
            fe.MouseWheel += OnMouseWheel;
            fe.MouseMove += OnMouseMove;
            fe.MouseLeave += OnMouseLeave;
            fe.MouseLeftButtonUp += OnMouseLeftButtonUp;
            fe.MouseLeftButtonDown += OnMouseLeftButtonDown;
            fe.MouseRightButtonUp += OnMouseRightButtonUp;
            fe.MouseRightButtonDown += OnMouseRightButtonDown;
        }

        private static void OnMouseWheel(object sender, MouseWheelEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }

            var position = e.GetPosition(fe);
            if (e.Delta > 0) {
                var matrix = GetTransformMatrix(fe);
                var displayPosition = matrix.Transform(position);
                matrix.ScaleAt(1.1d, 1.1d, displayPosition.X, displayPosition.Y);
                ((MatrixTransform)fe.RenderTransform).Matrix = matrix;
            }
            else if (e.Delta < 0) {
                var matrix = GetTransformMatrix(fe);
                var displayPosition = matrix.Transform(position);
                matrix.ScaleAt(1 / 1.1d, 1 / 1.1d, displayPosition.X, displayPosition.Y);
                ((MatrixTransform)fe.RenderTransform).Matrix = matrix;
            }
        }

        private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            if (e.ClickCount == 2) {
                fe.RenderTransform = new MatrixTransform();
            }
            var matrix = GetTransformMatrix(fe);
            SetIsMoving(fe, true);
            SetMoveInitialPoint(fe, e.GetPosition(GetPositionBase(fe)));
            SetMoveInitialMatrix(fe, matrix);
        }

        private static void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            SetIsMoving(fe, false);
            SetMoveInitialPoint(fe, default);
            SetMoveInitialMatrix(fe, default);
        }

        private static void OnMouseLeave(object sender, MouseEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            SetIsMoving(fe, false);
            SetMoveInitialPoint(fe, default);
            SetMoveInitialMatrix(fe, default);
        }

        private static void OnMouseMove(object sender, MouseEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            if (GetIsMoving(fe)) {
                var transition = e.GetPosition(GetPositionBase(fe)) - GetMoveInitialPoint(fe);
                var matrix = GetMoveInitialMatrix(fe);
                matrix.Translate(transition.X, transition.Y);
                fe.RenderTransform = new MatrixTransform(matrix);
            }
            if (GetDragRubber(fe) != null) {
                var adorner = GetDragRubber(fe);
                var initial = GetDragInitialPoint(fe);
                adorner.Offset = e.GetPosition(fe) - initial;
            }
        }

        private static void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            var adorner = new RubberAdorner(fe, e.GetPosition(fe));
            adorner.Attach();
            fe.CaptureMouse();
            SetDragRubber(fe, adorner);
            SetDragInitialPoint(fe, e.GetPosition(fe));
        }

        private static void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            var rubber = GetDragRubber(fe);
            if (rubber != null) {
                var initial = GetDragInitialPoint(fe);
                var current = e.GetPosition(fe);
                SetDragRubber(fe, null);
                fe.ReleaseMouseCapture();
                rubber.Detach();
                var transition = current - initial;
                    if (Math.Abs(transition.X) < SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(transition.Y) < SystemParameters.MinimumVerticalDragDistance)
                        return;

                var matrix = GetTransformMatrix(fe);
                var center = initial + transition / 2;
                var displayCenter = matrix.Transform(center);
                var displayTransition = matrix.Transform(transition);
                var scalex = fe.ActualWidth / displayTransition.X;
                var scaley = fe.ActualHeight / displayTransition.Y;
                matrix.ScaleAt(scalex, scaley, displayCenter.X, displayCenter.Y);
                fe.RenderTransform = new MatrixTransform(matrix);
            }
        }

        class Dot : System.Windows.Documents.Adorner
        {
            private readonly Rect _r;

            public Dot(UIElement adornedElement, Rect rect) : base(adornedElement) {
                _r = rect;
            }

            protected override void OnRender(DrawingContext drawingContext) {
                base.OnRender(drawingContext);
                drawingContext.DrawGeometry(Brushes.Red, null, new RectangleGeometry(_r));
            }
        }

        private static Matrix GetTransformMatrix(FrameworkElement fe) {
            return ((MatrixTransform)fe.RenderTransform).Matrix;
        }
    }
}
