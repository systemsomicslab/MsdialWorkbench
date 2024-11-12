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
    public interface ISpotLinker {
        object From { get; }
        object To { get; }
        string Label { get; }
        int Type { get; }
        bool CanDraw(Vector v);
        (TextAlignment, Point) GetLabelPosition(Point src, Point dest, Size labelSize);
        PathFigure GetLink(Point src, Point dest);
    }

    public enum LinkerLabelPlacementMode {
        Absolute = 0,
        MiddleLeft = 1,
        MiddleCenter = 2,
        MiddleRight = 3,
        SourceCornerLeft = 4,
        SourceCornerCenter = 5,
        SourceCornerRight = 6,
        TargetCornerLeft = 7,
        TargetCornerCenter = 8,
        TargetCornerRight = 9,
        SourceTopLeft = 10,
        SourceTopCenter = 11,
        SourceTopRight = 12,
        SourceMiddleLeft = 13,
        SourceMiddleCenter = 14,
        SourceMiddleRight = 15,
        SourceBottomLeft = 16,
        SourceBottomCenter = 17,
        SourceBottomRight = 18,
        TargetTopLeft = 19,
        TargetTopCenter = 20,
        TargetTopRight = 21,
        TargetMiddleLeft = 22,
        TargetMiddleCenter = 23,
        TargetMiddleRight = 24,
        TargetBottomLeft = 25,
        TargetBottomCenter = 26,
        TargetBottomRight = 27,
    }

    public sealed class SpotLinker : ISpotLinker {
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
        public double ArrowHorizontalOffset { get; set; } = 0d;
        public double ArrowVerticalOffset { get; set; } = 0d;

        public double LabelHorizontalOffset { get; set; } = 0d;
        public double LabelVerticalOffset { get; set; } = 0d;

        public LinkerLabelPlacementMode Placement { get; set; } = LinkerLabelPlacementMode.MiddleRight;

        public bool CanDraw(Vector v) {
            return Math.Abs(v.Y) >= ArrowVerticalOffset * 2;
        }

        public (TextAlignment, Point) GetLabelPosition(Point src, Point dest, Size labelSize) {
            var verticalCornerOffset = dest.Y > src.Y ? -ArrowVerticalOffset : ArrowVerticalOffset;
            switch (Placement) {
                case LinkerLabelPlacementMode.Absolute:
                    return (TextAlignment.Left, new Point(LabelHorizontalOffset, LabelVerticalOffset));
                case LinkerLabelPlacementMode.MiddleLeft:
                    return (TextAlignment.Right, new Point(dest.X - ArrowHorizontalOffset + LabelHorizontalOffset, (src.Y + dest.Y) / 2 + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.MiddleCenter:
                    return (TextAlignment.Center, new Point(dest.X - ArrowHorizontalOffset + LabelHorizontalOffset, (src.Y + dest.Y) / 2 + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.MiddleRight:
                    return (TextAlignment.Left, new Point(dest.X - ArrowHorizontalOffset + LabelHorizontalOffset, (src.Y + dest.Y) / 2 + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.SourceCornerLeft:
                    return (TextAlignment.Right, new Point(dest.X - ArrowHorizontalOffset + LabelHorizontalOffset, src.Y - verticalCornerOffset + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.SourceCornerCenter:
                    return (TextAlignment.Center, new Point(dest.X - ArrowHorizontalOffset + LabelHorizontalOffset, src.Y - verticalCornerOffset + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.SourceCornerRight:
                    return (TextAlignment.Left, new Point(dest.X - ArrowHorizontalOffset + LabelHorizontalOffset, src.Y - verticalCornerOffset + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.TargetCornerLeft:
                    return (TextAlignment.Right, new Point(dest.X - ArrowHorizontalOffset + LabelHorizontalOffset, dest.Y + verticalCornerOffset + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.TargetCornerCenter:
                    return (TextAlignment.Center, new Point(dest.X - ArrowHorizontalOffset + LabelHorizontalOffset, dest.Y + verticalCornerOffset + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.TargetCornerRight:
                    return (TextAlignment.Left, new Point(dest.X - ArrowHorizontalOffset + LabelHorizontalOffset, dest.Y + verticalCornerOffset + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.SourceTopLeft:
                    return (TextAlignment.Right, new Point(src.X + LabelHorizontalOffset, src.Y + LabelVerticalOffset - labelSize.Height));
                case LinkerLabelPlacementMode.SourceTopCenter:
                    return (TextAlignment.Center, new Point(src.X + LabelHorizontalOffset, src.Y + LabelVerticalOffset - labelSize.Height));
                case LinkerLabelPlacementMode.SourceTopRight:
                    return (TextAlignment.Left, new Point(src.X + LabelHorizontalOffset, src.Y + LabelVerticalOffset - labelSize.Height));
                case LinkerLabelPlacementMode.SourceMiddleLeft:
                    return (TextAlignment.Right, new Point(src.X + LabelHorizontalOffset, src.Y + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.SourceMiddleCenter:
                    return (TextAlignment.Center, new Point(src.X + LabelHorizontalOffset, src.Y + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.SourceMiddleRight:
                    return (TextAlignment.Left, new Point(src.X + LabelHorizontalOffset, src.Y + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.SourceBottomLeft:
                    return (TextAlignment.Right, new Point(src.X + LabelHorizontalOffset, src.Y + LabelVerticalOffset));
                case LinkerLabelPlacementMode.SourceBottomCenter:
                    return (TextAlignment.Center, new Point(src.X + LabelHorizontalOffset, src.Y + LabelVerticalOffset));
                case LinkerLabelPlacementMode.SourceBottomRight:
                    return (TextAlignment.Left, new Point(src.X + LabelHorizontalOffset, src.Y + LabelVerticalOffset));
                case LinkerLabelPlacementMode.TargetTopLeft:
                    return (TextAlignment.Right, new Point(dest.X + LabelHorizontalOffset, dest.Y + LabelVerticalOffset - labelSize.Height));
                case LinkerLabelPlacementMode.TargetTopCenter:
                    return (TextAlignment.Center, new Point(dest.X + LabelHorizontalOffset, dest.Y + LabelVerticalOffset - labelSize.Height));
                case LinkerLabelPlacementMode.TargetTopRight:
                    return (TextAlignment.Left, new Point(dest.X + LabelHorizontalOffset, dest.Y + LabelVerticalOffset - labelSize.Height));
                case LinkerLabelPlacementMode.TargetMiddleLeft:
                    return (TextAlignment.Right, new Point(dest.X + LabelHorizontalOffset, dest.Y + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.TargetMiddleCenter:
                    return (TextAlignment.Center, new Point(dest.X + LabelHorizontalOffset, dest.Y + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.TargetMiddleRight:
                    return (TextAlignment.Left, new Point(dest.X + LabelHorizontalOffset, dest.Y + LabelVerticalOffset - labelSize.Height / 2));
                case LinkerLabelPlacementMode.TargetBottomLeft:
                    return (TextAlignment.Right, new Point(dest.X + LabelHorizontalOffset, dest.Y + LabelVerticalOffset));
                case LinkerLabelPlacementMode.TargetBottomCenter:
                    return (TextAlignment.Center, new Point(dest.X + LabelHorizontalOffset, dest.Y + LabelVerticalOffset));
                case LinkerLabelPlacementMode.TargetBottomRight:
                    return (TextAlignment.Left, new Point(dest.X + LabelHorizontalOffset, dest.Y + LabelVerticalOffset));
            }
            return (TextAlignment.Left, new Point(src.X + ArrowHorizontalOffset + LabelHorizontalOffset, (src.Y + dest.Y) / 2 + LabelVerticalOffset - labelSize.Height / 2));
        }

        public PathFigure GetLink(Point src, Point dest) {
            var yOffset = dest.Y > src.Y ? -ArrowVerticalOffset : ArrowVerticalOffset;
            var path = new PathFigure
            {
                StartPoint = src,
                Segments = new PathSegmentCollection
            {
                new LineSegment { Point = new Point(dest.X - ArrowHorizontalOffset, src.Y - yOffset) },
                new LineSegment { Point = new Point(dest.X - ArrowHorizontalOffset, dest.Y + yOffset) },
                new LineSegment { Point = dest },
            }
            };
            path.Freeze();
            return path;
        }
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
                typeof(IEnumerable<ISpotLinker>),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnLinksChanged));

        public IEnumerable<ISpotLinker> Links {
            get => (IEnumerable<ISpotLinker>)GetValue(LinksProperty);
            set => SetValue(LinksProperty, value);
        }

        private static void OnLinksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var annotator = (SpotLinkedAnnotator)d;
            annotator.OnLinksChanged((IEnumerable<ISpotLinker>)e.OldValue, (IEnumerable<ISpotLinker>)e.NewValue);
        }

        private ICollectionView _linksCollectionView;

        private void OnLinksChanged(IEnumerable<ISpotLinker> oldValue, IEnumerable<ISpotLinker> newValue) {
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

        private Lazy<Dictionary<object, List<ISpotLinker>>> _graph;

        private void CoerceGraph() {
            if (_linksCollectionView is null
                || xLambda is null
                || yLambda is null) {
                _graph = new Lazy<Dictionary<object, List<ISpotLinker>>>(() => new Dictionary<object, List<ISpotLinker>>());
                return;
            }

            var xlambda = xLambda.Value;
            var ylambda = yLambda.Value;

            _graph = new Lazy<Dictionary<object, List<ISpotLinker>>>(() =>
                _linksCollectionView.Cast<ISpotLinker>().GroupBy(link => link.From).ToDictionary(group => group.Key, group => group.ToList())
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

        protected override void OnHorizontalMappingChanged(object sender, EventArgs e) {
            base.OnHorizontalMappingChanged(sender, e);
            CoerceGraph();
            CoerceNodes();
            InvalidateVisual();
        }

        protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnVerticalAxisChanged(oldValue, newValue);
            CoerceGraph();
            CoerceNodes();
        }

        protected override void OnVerticalMappingChanged(object sender, EventArgs e) {
            base.OnVerticalMappingChanged(sender, e);
            CoerceGraph();
            CoerceNodes();
            InvalidateVisual();
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
                typeof(IBrushMapper<ISpotLinker>),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    new ConstantBrushMapper<ISpotLinker>(Brushes.Black),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public IBrushMapper<ISpotLinker> LinkBrush {
            get => (IBrushMapper<ISpotLinker>)GetValue(LinkBrushProperty);
            set => SetValue(LinkBrushProperty, value);
        }

        public static readonly DependencyProperty LinkLabelBrushProperty =
            DependencyProperty.Register(
                nameof(LinkLabelBrush),
                typeof(IBrushMapper<ISpotLinker>),
                typeof(SpotLinkedAnnotator),
                new FrameworkPropertyMetadata(
                    new ConstantBrushMapper<ISpotLinker>(Brushes.Gray),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public IBrushMapper<ISpotLinker> LinkLabelBrush {
            get => (IBrushMapper<ISpotLinker>)GetValue(LinkLabelBrushProperty);
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
                var src = new Point(x, y);

                if (_graph != null && _graph.Value.TryGetValue(Target, out var links)) {
                    var linkBrush = LinkBrush ?? new ConstantBrushMapper<ISpotLinker>(Brushes.Black);
                    var labelBrush = LinkLabelBrush ?? new ConstantBrushMapper<ISpotLinker>(Brushes.Gray);
                    var spots = links.Select(link => link.To).ToList();
                    var groups = links.GroupBy(link => linkBrush.Map(link));
                    foreach (var group in groups) {
                        var innerGeometries = new GeometryGroup();
                        var outerGeometries = new GeometryGroup();
                        innerGeometries.Children.Add(new EllipseGeometry(src, radius, radius));
                        outerGeometries.Children.Add(new EllipseGeometry(src, radius + lineThickness, radius + lineThickness));
                        var pathGeometry = new PathGeometry();
                        foreach (var link in group) {
                            var x_ = haxis.TranslateToRenderPoint(xLambda.Value(link.To, haxis), flippedX, actualWidth);
                            var y_ = vaxis.TranslateToRenderPoint(yLambda.Value(link.To, vaxis), flippedY, actualHeight);
                            var dest = new Point(x_, y_);
                            innerGeometries.Children.Add(new EllipseGeometry(dest, radius, radius));
                            outerGeometries.Children.Add(new EllipseGeometry(dest, radius + lineThickness, radius + lineThickness));

                            if (link.CanDraw(dest - src)) {
                                pathGeometry.Figures.Add(link.GetLink(src, dest));

                                var text = new FormattedText(
                                    link.Label,
                                    System.Globalization.CultureInfo.GetCultureInfo("en-us"),
                                    FlowDirection.LeftToRight, new Typeface("Calibri"), fontsize,
                                    labelBrush.Map(link), dpiInfo.PixelsPerDip);
                                var (alignment, point) = link.GetLabelPosition(src, dest, new Size(text.Width, text.Height));
                                text.TextAlignment = alignment;
                                drawingContext.DrawText(text, point);
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
                            TextAlignment = node.LabelHorizontalOffset > 0 ? TextAlignment.Left : node.LabelHorizontalOffset < 0 ? TextAlignment.Right : TextAlignment.Center,
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
