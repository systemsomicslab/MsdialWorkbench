using CompMs.Graphics.Chart;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CompMs.Graphics.Adorner
{
    class ScatterFocusAdorner : System.Windows.Documents.Adorner
    {
        public static readonly DependencyProperty AdornerProperty =
            DependencyProperty.Register("Adorner", typeof(ScatterFocusAdorner), typeof(ScatterFocusAdorner));

        public bool IsAttached { get; private set; }

        public Point? TargetPoint { get; set; } = null;

        public ScatterFocusAdorner(ScatterControl adornedElement, double outerScale, double innerScale) : base(adornedElement) {
            this.outerScale = outerScale;
            this.innerScale = innerScale;
            scatter = adornedElement;
            layer = AdornerLayer.GetAdornerLayer(adornedElement);
            IsHitTestVisible = false;
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

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            RenderOuter(drawingContext);
        }

        private void RenderOuter(DrawingContext drawingContext) {
            var large_mark = scatter.PointGeometry.Clone();
            var small_mark = large_mark.Clone();
            var bound_center = new Point(large_mark.Bounds.Left + large_mark.Bounds.Width / 2, large_mark.Bounds.Top + large_mark.Bounds.Height / 2);
            large_mark.Transform = new ScaleTransform(outerScale, outerScale, bound_center.X, bound_center.Y);
            small_mark.Transform = new ScaleTransform(innerScale, innerScale, bound_center.X, bound_center.Y);
            var mark = new CombinedGeometry(GeometryCombineMode.Exclude, large_mark, small_mark);

            var brush = new DrawingBrush(new GeometryDrawing(scatter.PointBrush, null, mark));
            brush.Freeze();
            double radius = scatter.Radius * outerScale;
            var center = TargetPoint;
            if (center.HasValue) {
                drawingContext.DrawRectangle(brush, null, new Rect(center.Value.X - radius, center.Value.Y - radius, radius * 2, radius * 2));
            }
        }

        AdornerLayer layer;
        ScatterControl scatter;
        double outerScale, innerScale;
    }
}
