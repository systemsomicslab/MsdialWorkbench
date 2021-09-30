using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Common.DataStructure;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Chart
{
    public class DendrogramControl : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(DendrogramControl),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty TreeProperty = DependencyProperty.Register(
            nameof(Tree), typeof(DirectedTree), typeof(DendrogramControl),
            new PropertyMetadata(default, OnTreeChanged)
            );

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(DendrogramControl),
            new PropertyMetadata(default(string), OnHorizontalPropertyNameChanged)
            );

        public static readonly DependencyProperty IDPropertyNameProperty = DependencyProperty.Register(
            nameof(IDPropertyName), typeof(string), typeof(DendrogramControl),
            new PropertyMetadata(default(string), OnIDPropertyNameChanged)
            );

        public static readonly DependencyProperty LinePenProperty = DependencyProperty.Register(
            nameof(LinePen), typeof(Pen), typeof(DendrogramControl),
            new PropertyMetadata(new Pen(Brushes.Black, 1))
            );

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(DendrogramControl),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public static readonly DependencyProperty FocusedItemProperty = DependencyProperty.Register(
            nameof(FocusedItem), typeof(object), typeof(DendrogramControl),
            new PropertyMetadata(null)
            );
        #endregion

        #region Property
        public System.Collections.IEnumerable ItemsSource {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public DirectedTree Tree {
            get => (DirectedTree)GetValue(TreeProperty);
            set => SetValue(TreeProperty, value);
        }

        public string HorizontalPropertyName {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        public string IDPropertyName {
            get => (string)GetValue(IDPropertyNameProperty);
            set => SetValue(IDPropertyNameProperty, value);
        }

        public Pen LinePen {
            get => (Pen)GetValue(LinePenProperty);
            set => SetValue(LinePenProperty, value);
        }

        public object SelectedItem {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public object FocusedItem {
            get => GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }
        #endregion

        #region field
        private CollectionView cv;
        private Type dataType;
        private PropertyInfo hPropertyReflection;
        private PropertyInfo idPropertyReflection;
        #endregion

        public DendrogramControl() {
            MouseLeftButtonDown += VisualSelectOnClick;
            MouseMove += VisualFocusOnMouseOver;
        }

        protected override void Update() {
            if (hPropertyReflection == null
               || idPropertyReflection == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || cv == null
               || Tree == null
               )
                return;

            visualChildren.Clear();

            var root = Tree.Root;
            var xpos = Enumerable.Repeat(0, Tree.Count).Select(e => (double)e).ToArray();
            var ypos = Enumerable.Repeat(0, Tree.Count).Select(e => (double)e).ToArray();
            var used = Enumerable.Repeat(false, Tree.Count).ToArray();

            Tree.PreOrder(root, e => ypos[e.To] = ypos[e.From] + e.Distance);
            var ymax = ypos.Max();
            ypos = ypos.Select(y => VerticalAxis.TranslateToRenderPoint(ymax - y, FlippedY, ActualHeight)).ToArray();

            foreach (var o in cv) {
                var x = hPropertyReflection.GetValue(o);
                var id = (int)idPropertyReflection.GetValue(o);

                if (x is double)
                    xpos[id] = HorizontalAxis.TranslateToRenderPoint((double)x, FlippedX, ActualWidth);
                else if (x is string)
                    xpos[id] = HorizontalAxis.TranslateToRenderPoint(x, FlippedX, ActualWidth);
                else if (x is IConvertible)
                    xpos[id] = HorizontalAxis.TranslateToRenderPoint(x as IConvertible, FlippedX, ActualWidth);
                else
                    xpos[id] = HorizontalAxis.TranslateToRenderPoint(x, FlippedX, ActualWidth);

                used[id] = true;
            }

            Tree.PostOrder(root, e => {
                if (Tree[e.To].All(e_ => !used[e_.To])) return;
                xpos[e.To] = Tree[e.To].Select(e_ => e_.To).Where(v => used[v]).Average(v => xpos[v]);
                used[e.To] = true;
            });
            if (Tree[root].Any(e_ => used[e_.To])) {
                xpos[root] = Tree[root].Select(e_ => e_.To).Where(v => used[v]).Average(v => xpos[v]);
                used[root] = true;
            }

            for (int i = 0; i < Tree.Count; i++) {
                if (!used[i]) continue;

                var childs = Tree[i].Select(e => e.To).Where(v => used[v]);

                {
                    var dv = new AnnotatedDrawingVisual(i) { Center = new Point(xpos[i], ypos[i]) };
                    dv.Clip = new RectangleGeometry(new Rect(RenderSize));
                    var dc = dv.RenderOpen();
                    foreach (var child in childs)
                        dc.DrawLine(LinePen, new Point(xpos[i], ypos[i]), new Point(xpos[child], ypos[i]));
                    dc.Close();
                    visualChildren.Add(dv);
                }

                foreach (var child in childs) {
                    var dv = new AnnotatedDrawingVisual(child) { Center = new Point(xpos[child], ypos[child]) };
                    dv.Clip = new RectangleGeometry(new Rect(RenderSize));
                    var dc = dv.RenderOpen();
                    dc.DrawLine(LinePen, new Point(xpos[child], ypos[i]), new Point(xpos[child], ypos[child]));
                    dc.Close();
                    visualChildren.Add(dv);
                }
            }
        }

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as DendrogramControl;
            if (chart == null) return;

            var enumerator = chart.ItemsSource.GetEnumerator();
            enumerator.MoveNext();
            chart.dataType = enumerator.Current.GetType();
            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource) as CollectionView;

            if (chart.HorizontalPropertyName != null)
                chart.hPropertyReflection = chart.dataType.GetProperty(chart.HorizontalPropertyName);
            if (chart.IDPropertyName != null)
                chart.idPropertyReflection = chart.dataType.GetProperty(chart.IDPropertyName);
            if (chart.SelectedItem != null)
                chart.cv.MoveCurrentTo(chart.SelectedItem);

            chart.Update();
        }

        static void OnTreeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as DendrogramControl;
            if (chart == null) return;

            chart.Update();
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as DendrogramControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.hPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnIDPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as DendrogramControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.idPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as DendrogramControl;
            if (chart == null) return;

            if (chart.cv != null)
                chart.cv.MoveCurrentTo(e.NewValue);
        }
        #endregion

        #region Visual hit event
        void VisualFocusOnMouseOver(object sender, MouseEventArgs e) {
            var pt = e.GetPosition(this);

            VisualTreeHelper.HitTest(this,
                new HitTestFilterCallback(VisualHitTestFilter),
                new HitTestResultCallback(VisualFocusHitTest),
                new PointHitTestParameters(pt)
                );
        }

        void VisualSelectOnClick(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount == 1) {
                var pt = e.GetPosition(this);

                VisualTreeHelper.HitTest(this,
                    new HitTestFilterCallback(VisualHitTestFilter),
                    new HitTestResultCallback(VisualSelectHitTest),
                    new PointHitTestParameters(pt)
                    );
            }
        }

        HitTestFilterBehavior VisualHitTestFilter(DependencyObject d) {
            if (d is AnnotatedDrawingVisual)
                return HitTestFilterBehavior.Continue;
            return HitTestFilterBehavior.ContinueSkipSelf;
        }

        HitTestResultBehavior VisualFocusHitTest(HitTestResult result) {
            FocusedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
        }

        HitTestResultBehavior VisualSelectHitTest(HitTestResult result) {
            SelectedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
        }
        #endregion
    }
}
