using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Expressions = System.Linq.Expressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections.Specialized;

namespace CompMs.Graphics.Chart
{
    public sealed class SpotLinker {
        private static readonly double DEFAULT_ARROW_HORIZONTAL_OFFSET = 30d, DEFAULT_ARROW_VERTICAL_OFFSET = 10d;

        public SpotLinker(object from, object to, string label, int type) {
            From = from;
            To = to;
            Label = label;
            Type = type;
        }

        public object From { get; }
        public object To { get; }
        public string Label { get; }
        public int Type { get; }
        public double ArrowHorizontalOffset { get; set; } = DEFAULT_ARROW_HORIZONTAL_OFFSET;
        public double ArrowVerticalOffset { get; set; } = DEFAULT_ARROW_VERTICAL_OFFSET;

        public double LabelHorizontalOffset { get; set; } = 3d;
        public double LabelVerticalOffset { get; set; } = 0d;
    }

    public sealed class SpotAnnotator {
        public SpotAnnotator(object target, string label, int type) {
            Target = target;
            Label = label;
            Type = type;
        }

        public object Target { get; }
        public string Label { get; }
        public int Type { get; }

        public double LabelHorizontalOffset { get; set; } = 0d;
        public double LabelVerticalOffset { get; set; } = 20d;
    }

    public sealed class SpotLinkedAnnotator : ChartBaseControl {
        public SpotLinkedAnnotator() {
            ClipToBounds = true;
            IsHitTestVisible = false;
        }

        public static readonly DependencyProperty SpotsProperty =
            DependencyProperty.Register(
                nameof(Spots),
                typeof(IEnumerable),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSpotsChanged));

        public IEnumerable Spots {
            get => (IEnumerable)GetValue(SpotsProperty);
            set => SetValue(SpotsProperty, value);
        }

        private static void OnSpotsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var annotator = (SpotLinkedAnnotator)d;
            annotator.OnSpotsChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
        }

        private ICollectionView _spotsCollectionView;

        private void OnSpotsChanged(IEnumerable oldValue, IEnumerable newValue) {
            if (_spotsCollectionView != null) {
                _spotsCollectionView.CollectionChanged -= OnSpotsCollectionChanged;
            }
            _spotsCollectionView = (newValue as ICollectionView) ?? CollectionViewSource.GetDefaultView(newValue);
            if (_spotsCollectionView != null) {
                _spotsCollectionView.CollectionChanged += OnSpotsCollectionChanged;
            }
        }

        private void OnSpotsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                default:
                    InvalidateVisual();
                    break;
            }
        }

        public static readonly DependencyProperty LinksProperty =
            DependencyProperty.Register(
                nameof(Links),
                typeof(IEnumerable<SpotLinker>),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnLinksChanged));

        public IEnumerable<SpotLinker> Links {
            get => (IEnumerable<SpotLinker>)GetValue(LinksProperty);
            set => SetValue(LinksProperty, value);
        }

        private static void OnLinksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var annotator = (SpotLinkedAnnotator)d;
            annotator.OnLinksChanged((IEnumerable<SpotLinker>)e.OldValue, (IEnumerable<SpotLinker>)e.NewValue);
        }

        private ICollectionView _linksCollectionView;

        private void OnLinksChanged(IEnumerable<SpotLinker> oldValue, IEnumerable<SpotLinker> newValue) {
            if (_linksCollectionView != null) {
                _linksCollectionView.CollectionChanged -= OnLinksCollectionChanged;
            }
            _linksCollectionView = (newValue as ICollectionView) ?? CollectionViewSource.GetDefaultView(newValue);
            if (_linksCollectionView != null) {
                _linksCollectionView.CollectionChanged += OnLinksCollectionChanged;
            }

            CoerceGraph();
        }

        private void OnLinksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                default:
                    CoerceGraph();
                    break;
            }
        }

        public static readonly DependencyProperty AnnotatorsProperty =
            DependencyProperty.Register(
                nameof(Annotators),
                typeof(IEnumerable<SpotAnnotator>),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnAnnotatorsChanged));

        public IEnumerable<SpotAnnotator> Annotators {
            get => (IEnumerable<SpotAnnotator>)GetValue(AnnotatorsProperty);
            set => SetValue(AnnotatorsProperty, value);
        }

        private static void OnAnnotatorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var annoator = (SpotLinkedAnnotator)d;
            annoator.OnAnnotatorsChanged((IEnumerable<SpotAnnotator>)e.OldValue, (IEnumerable<SpotAnnotator>)e.NewValue);
        }

        private ICollectionView _nodesCollectionView;
        private void OnAnnotatorsChanged(IEnumerable<SpotAnnotator> oldValue, IEnumerable<SpotAnnotator> newValue) {
            if (_nodesCollectionView != null) {
                _nodesCollectionView.CollectionChanged -= OnNodesCollectionChanged;
            }
            _nodesCollectionView = (newValue as ICollectionView) ?? CollectionViewSource.GetDefaultView(newValue);
            if (_nodesCollectionView != null) {
                _nodesCollectionView.CollectionChanged += OnNodesCollectionChanged;
            }

            CoerceNodes();
        }

        private void OnNodesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                default:
                    CoerceNodes();
                    break;
            }
        }

        private Lazy<Dictionary<object, List<SpotLinker>>> _graph;

        private void CoerceGraph() {
            if (_linksCollectionView is null
                || xLambda is null
                || yLambda is null) {
                _graph = new Lazy<Dictionary<object, List<SpotLinker>>>(() => new Dictionary<object, List<SpotLinker>>());
                return;
            }

            var xlambda = xLambda.Value;
            var ylambda = yLambda.Value;

            _graph = new Lazy<Dictionary<object, List<SpotLinker>>>(() =>
                _linksCollectionView.Cast<SpotLinker>().GroupBy(link => link.From).ToDictionary(group => group.Key, group => group.ToList())
            );
        }

        private Lazy<Dictionary<object, List<SpotAnnotator>>> _nodes;

        private void CoerceNodes() {
            if (_nodesCollectionView is null
                || xLambda is null
                || yLambda is null) {
                _nodes = new Lazy<Dictionary<object, List<SpotAnnotator>>>(() => new Dictionary<object, List<SpotAnnotator>>());
                return;
            }

            var xlambda = xLambda.Value;
            var ylambda = yLambda.Value;

            _nodes = new Lazy<Dictionary<object, List<SpotAnnotator>>>(() =>
                _nodesCollectionView.Cast<SpotAnnotator>().GroupBy(link => link.Target).ToDictionary(group => group.Key, group => group.ToList())
            );
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(
                nameof(Target),
                typeof(object),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnTargetChanged));

        public object Target {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var annotator = (SpotLinkedAnnotator)d;
            annotator.OnTargetChanged(e.OldValue, e.NewValue);
        }

        private void OnTargetChanged(object oldValue, object newValue) {

        }

        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register(
                nameof(DataType),
                typeof(Type),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnDataTypeChanged));

        public Type DataType {
            get => (Type)GetValue(DataTypeProperty);
            set => SetValue(DataTypeProperty, value);
        }

        private static void OnDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var annotator = (SpotLinkedAnnotator)d;
            annotator.OnDataTypeChanged((Type)e.OldValue, (Type)e.NewValue);
        }

        private void OnDataTypeChanged(Type oldValue, Type newValue) {
            CoerceHorizontalValue(newValue, HorizontalProperty);
            CoerceVerticalValue(newValue, VerticalProperty);
            CoerceGraph();
            CoerceNodes();
        }

        public static readonly DependencyProperty HorizontalPropertyProperty =
            DependencyProperty.Register(
                nameof(HorizontalProperty),
                typeof(string),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalPropertyChanged));

        public string HorizontalProperty {
            get => (string)GetValue(HorizontalPropertyProperty);
            set => SetValue(HorizontalPropertyProperty, value);
        }

        private static void OnHorizontalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var annotator = (SpotLinkedAnnotator)d;
            annotator.OnHorizontalPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnHorizontalPropertyChanged(string oldValue, string newValue) {
            CoerceHorizontalValue(DataType, newValue);
            CoerceGraph();
            CoerceNodes();
        }

        private Lazy<Func<object, IAxisManager, AxisValue>> xLambda;

        private void CoerceHorizontalValue(Type type, string hprop) {
            xLambda = BuildGetter(type, hprop);
        }

        public static readonly DependencyProperty VerticalPropertyProperty =
            DependencyProperty.Register(
                nameof(VerticalProperty),
                typeof(string),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyChanged));

        public string VerticalProperty {
            get => (string)GetValue(VerticalPropertyProperty);
            set => SetValue(VerticalPropertyProperty, value);
        }

        private static void OnVerticalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var annotator = (SpotLinkedAnnotator)d;
            annotator.OnVerticalPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnVerticalPropertyChanged(string oldValue, string newValue) {
            CoerceVerticalValue(DataType, newValue);
            CoerceGraph();
            CoerceNodes();
        }

        private Lazy<Func<object, IAxisManager, AxisValue>> yLambda;
        private void CoerceVerticalValue(Type type, string vprop) {
            yLambda = BuildGetter(type, vprop);
        }

        protected override void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnHorizontalAxisChanged(oldValue, newValue);
            CoerceGraph();
            CoerceNodes();
        }

        protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnVerticalAxisChanged(oldValue, newValue);
            CoerceGraph();
            CoerceNodes();
        }

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register(
                nameof(Radius),
                typeof(double),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    3d,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public double Radius {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register(
                nameof(LineThickness),
                typeof(double),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    1d,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public double LineThickness {
            get => (double)GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }

        public static readonly DependencyProperty FontsizeProperty =
            DependencyProperty.Register(
                nameof(Fontsize),
                typeof(double),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    13d,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public double Fontsize {
            get => (double)GetValue(FontsizeProperty);
            set => SetValue(FontsizeProperty, value);
        }

        public static readonly DependencyProperty LinkBrushProperty =
            DependencyProperty.Register(
                nameof(LinkBrush),
                typeof(IBrushMapper<SpotLinker>),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    new ConstantBrushMapper<SpotLinker>(Brushes.Black),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public IBrushMapper<SpotLinker> LinkBrush {
            get => (IBrushMapper<SpotLinker>)GetValue(LinkBrushProperty);
            set => SetValue(LinkBrushProperty, value);
        }

        public static readonly DependencyProperty LinkLabelBrushProperty =
            DependencyProperty.Register(
                nameof(LinkLabelBrush),
                typeof(IBrushMapper<SpotLinker>),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    new ConstantBrushMapper<SpotLinker>(Brushes.Gray),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public IBrushMapper<SpotLinker> LinkLabelBrush {
            get => (IBrushMapper<SpotLinker>)GetValue(LinkLabelBrushProperty);
            set => SetValue(LinkLabelBrushProperty, value);
        }

        public static readonly DependencyProperty SpotLabelBrushProperty =
            DependencyProperty.Register(
                nameof(SpotLabelBrush),
                typeof(IBrushMapper<SpotAnnotator>),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    new ConstantBrushMapper<SpotAnnotator>(Brushes.Gray),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public IBrushMapper<SpotAnnotator> SpotLabelBrush {
            get => (IBrushMapper<SpotAnnotator>)GetValue(SpotLabelBrushProperty);
            set => SetValue(SpotLabelBrushProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            if (Target != null && HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis) {
                var hr = haxis.Range;
                var vr = vaxis.Range;

                bool flippedX = FlippedX, flippedY = FlippedY;
                double radius = Radius, actualWidth = ActualWidth, actualHeight = ActualHeight;
                double lineThickness = LineThickness;
                var fontsize = Fontsize;
                
                var dpiInfo = VisualTreeHelper.GetDpi(this);
                var x = haxis.TranslateToRenderPoint(xLambda.Value(Target, haxis), flippedX, actualWidth);
                var y = vaxis.TranslateToRenderPoint(yLambda.Value(Target, vaxis), flippedY, actualHeight);

                if (_graph != null && _graph.Value.TryGetValue(Target, out var links)) {
                    var linkBrush = LinkBrush ?? new ConstantBrushMapper<SpotLinker>(Brushes.Black);
                    var labelBrush = LinkLabelBrush ?? new ConstantBrushMapper<SpotLinker>(Brushes.Gray);
                    var spots = links.Select(link => link.To).ToList();
                    var groups = links.GroupBy(link => linkBrush.Map(link));
                    foreach (var group in groups) {
                        var innerGeometries = new GeometryGroup();
                        var outerGeometries = new GeometryGroup();
                        innerGeometries.Children.Add(new EllipseGeometry(new Point(x, y), radius, radius));
                        outerGeometries.Children.Add(new EllipseGeometry(new Point(x, y), radius + lineThickness, radius + lineThickness));
                        var pathGeometry = new PathGeometry();
                        foreach (var link in group) {
                            var x_ = haxis.TranslateToRenderPoint(xLambda.Value(link.To, haxis), flippedX, actualWidth);
                            var y_ = vaxis.TranslateToRenderPoint(yLambda.Value(link.To, vaxis), flippedY, actualHeight);
                            innerGeometries.Children.Add(new EllipseGeometry(new Point(x_, y_), radius, radius));
                            outerGeometries.Children.Add(new EllipseGeometry(new Point(x_, y_), radius + lineThickness, radius + lineThickness));

                            if (Math.Abs(y_ - y) >= link.ArrowVerticalOffset * 2) {
                                var yOffset = y_ > y ? -link.ArrowVerticalOffset : link.ArrowVerticalOffset;
                                var path = new PathFigure
                                {
                                    StartPoint = new Point(x, y),
                                    Segments = new PathSegmentCollection
                                {
                                    new LineSegment { Point = new Point(x + link.ArrowHorizontalOffset, y - yOffset) },
                                    new LineSegment { Point = new Point(x + link.ArrowHorizontalOffset, y_ + yOffset) },
                                    new LineSegment { Point = new Point(x_, y_) },
                                }
                                };
                                path.Freeze();
                                pathGeometry.Figures.Add(path);

                                var text = new FormattedText(
                                    link.Label,
                                    System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                                    FlowDirection.LeftToRight, new Typeface("Calibri"), fontsize,
                                    labelBrush.Map(link), dpiInfo.PixelsPerDip)
                                {
                                    TextAlignment = link.LabelHorizontalOffset >= 0 ? TextAlignment.Left : TextAlignment.Right,
                                };
                                drawingContext.DrawText(text, new Point(x + link.ArrowHorizontalOffset + link.LabelHorizontalOffset, (y + y_) / 2 + link.LabelVerticalOffset - text.Height / 2));
                            }
                        }
                        var geometry = new CombinedGeometry(GeometryCombineMode.Exclude, outerGeometries, innerGeometries);
                        geometry.Freeze();
                        pathGeometry.Freeze();
                        var b = group.Key;
                        var pen = new Pen(b, lineThickness);
                        pen.Freeze();
                        drawingContext.DrawGeometry(null, pen, pathGeometry);
                        drawingContext.DrawGeometry(b, null, geometry);
                    }
                }

                if (_nodes != null && _nodes.Value.TryGetValue(Target, out var nodes)) {
                    var labelBrush = SpotLabelBrush ?? new ConstantBrushMapper<SpotAnnotator>(Brushes.Gray);
                    foreach (var node in nodes) {
                        var text = new FormattedText(
                            node.Label,
                            System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight, new Typeface("Calibri"), fontsize,
                            labelBrush.Map(node), dpiInfo.PixelsPerDip)
                        {
                            TextAlignment = TextAlignment.Center,
                        };
                        drawingContext.DrawText(text, new Point(x + node.LabelHorizontalOffset, y + node.LabelVerticalOffset - text.Height / 2));
                    }
                }
            }
        }

        private Expressions.LambdaExpression GetExpression(Type type, string property) {
            var parameter = Expressions.Expression.Parameter(typeof(object));
            var casted = Expressions.Expression.Convert(parameter, type);
            var getter = Expressions.Expression.Property(casted, property);
            var axis = Expressions.Expression.Parameter(typeof(IAxisManager));

            var prop = Expressions.Expression.Convert(getter, typeof(object));
            var axisvalue = Expressions.Expression.Call(axis, typeof(IAxisManager).GetMethod(nameof(IAxisManager.TranslateToAxisValue)), prop);

            var axistype = typeof(IAxisManager<>).MakeGenericType(((System.Reflection.PropertyInfo)getter.Member).PropertyType);
            var castedaxis = Expressions.Expression.TypeAs(axis, axistype);
            var castedaxisvalue = Expressions.Expression.Call(castedaxis, axistype.GetMethod(nameof(IAxisManager<object>.TranslateToAxisValue)), getter);

            var val = Expressions.Expression.Condition(
                Expressions.Expression.Equal(castedaxis, Expressions.Expression.Constant(null)),
                axisvalue,
                castedaxisvalue);

            return Expressions.Expression.Lambda<Func<object, IAxisManager, AxisValue>>(val, parameter, axis);
        }

        private Lazy<Func<object, IAxisManager, AxisValue>> BuildGetter(Type type, string prop) {
            if (type is null
                || string.IsNullOrEmpty(prop)
                || !type.GetProperties().Any(m => m.Name == prop)) {
                return null;
            }
            return new Lazy<Func<object, IAxisManager, AxisValue>>(() => (Func<object, IAxisManager, AxisValue>)GetExpression(type, prop).Compile());
        }
    }
}
