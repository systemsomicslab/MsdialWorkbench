using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CompMs.Graphics.Adorner
{
    class ContentAdorner : System.Windows.Documents.Adorner
    {
        public static readonly DependencyProperty AdornerProperty =
            DependencyProperty.Register("Adorner", typeof(ContentAdorner), typeof(ContentAdorner));

        public bool IsAttached { get; private set; }
        public System.Drawing.ContentAlignment Alignment { get; set; } = System.Drawing.ContentAlignment.TopCenter;
        public Point? TargetPoint { get; set; } = null;
        public double TargetRadius { get; set; } = 0d;

        private readonly AdornerLayer layer;
        private readonly Canvas canvas;
        private readonly FrameworkElement content;
        private readonly VisualCollection visualChildren;

        public ContentAdorner(UIElement adornedElement, FrameworkElement content) : base(adornedElement) {
            layer = AdornerLayer.GetAdornerLayer(adornedElement);
            IsHitTestVisible = false;

            canvas = new Canvas { Background = Brushes.Transparent };
            canvas.IsHitTestVisible = false;

            this.content = content;
            this.content.DataContext = DataContext;
            canvas.Children.Add(this.content);

            visualChildren = new VisualCollection(this) { canvas };

            DataContextChanged += OnDataContextChanged;
        }

        private static void OnDataContextChanged(object obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ContentAdorner adorner) {
                if (adorner.content != null)
                    adorner.content.DataContext = adorner.DataContext;
            }
        }

        public void Attach() {
            if (layer != null && !IsAttached) {
                layer.Add(this);
                IsAttached = true;
            }
        }

        public void Detach() {
            if (layer != null && IsAttached) {
                layer.Remove(this);
                IsAttached = false;
            }
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => canvas;
        protected override Size ArrangeOverride(Size finalSize) {
            canvas.Arrange(new Rect(finalSize));
            Canvas.SetTop(canvas, 0);
            Canvas.SetLeft(canvas, 0);

            return finalSize;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            SetPosition();
        }

        private void SetPosition() {
            if (!TargetPoint.HasValue) return;

            var center = TargetPoint.Value;
            var radius = TargetRadius;
            var size = content.DesiredSize;
            var width = size.Width;
            var height = size.Height;

            switch (Alignment) {
                case System.Drawing.ContentAlignment.TopLeft:
                case System.Drawing.ContentAlignment.TopCenter:
                case System.Drawing.ContentAlignment.TopRight:
                    Canvas.SetTop(content, center.Y - height - radius);
                    break;
                case System.Drawing.ContentAlignment.MiddleLeft:
                case System.Drawing.ContentAlignment.MiddleCenter:
                case System.Drawing.ContentAlignment.MiddleRight:
                    Canvas.SetTop(content, center.Y - height / 2);
                    break;
                case System.Drawing.ContentAlignment.BottomLeft:
                case System.Drawing.ContentAlignment.BottomCenter:
                case System.Drawing.ContentAlignment.BottomRight:
                    Canvas.SetTop(content, center.Y + radius);
                    break;
            }
            switch (Alignment) {
                case System.Drawing.ContentAlignment.TopLeft:
                case System.Drawing.ContentAlignment.MiddleLeft:
                case System.Drawing.ContentAlignment.BottomLeft:
                    Canvas.SetLeft(content, center.X - width - radius);
                    break;
                case System.Drawing.ContentAlignment.TopCenter:
                case System.Drawing.ContentAlignment.MiddleCenter:
                case System.Drawing.ContentAlignment.BottomCenter:
                    Canvas.SetLeft(content, center.X - width / 2);
                    break;
                case System.Drawing.ContentAlignment.TopRight:
                case System.Drawing.ContentAlignment.MiddleRight:
                case System.Drawing.ContentAlignment.BottomRight:
                    Canvas.SetLeft(content, center.X + radius);
                    break;
            }
        }
    }
}
