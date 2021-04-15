using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CompMs.Graphics.Core.Adorner;


namespace CompMs.Graphics.Core.Base
{
    public abstract class ChartBaseControl : FrameworkElement
    {
        public static readonly DependencyProperty HorizontalAxisProperty =
            DependencyProperty.RegisterAttached(
                nameof(HorizontalAxis), typeof(IAxisManager), typeof(ChartBaseControl),
                new FrameworkPropertyMetadata(
                    default(IAxisManager),
                    FrameworkPropertyMetadataOptions.AffectsRender
                    | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender
                    | FrameworkPropertyMetadataOptions.Inherits,
                    OnHorizontalAxisChanged));

        public static IAxisManager GetHorizontalAxis(DependencyObject d)
            => (IAxisManager)d.GetValue(HorizontalAxisProperty);

        public static void SetHorizontalAxis(DependencyObject d, IAxisManager value)
            => d.SetValue(HorizontalAxisProperty, value);

        public IAxisManager HorizontalAxis
        {
            get => (IAxisManager)GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
        }

        static void OnHorizontalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ChartBaseControl bc) {
                bc.OnHorizontalAxisChanged((IAxisManager)e.OldValue, (IAxisManager)e.NewValue);
                bc.InvalidateVisual();
            }
        }

        void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            if (oldValue != null) {
                oldValue.RangeChanged -= OnHorizontalRangeChanged;
            }

            if (newValue != null) {
                newValue.RangeChanged += OnHorizontalRangeChanged;
            }
        }

        void OnHorizontalRangeChanged(object sender, EventArgs e) {
            InvalidateVisual();
        }

        public static readonly DependencyProperty VerticalAxisProperty =
            DependencyProperty.RegisterAttached(
                nameof(VerticalAxis), typeof(IAxisManager), typeof(ChartBaseControl),
                new FrameworkPropertyMetadata(
                    default(IAxisManager),
                    FrameworkPropertyMetadataOptions.AffectsRender
                    | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender
                    | FrameworkPropertyMetadataOptions.Inherits,
                    OnVerticalAxisChanged));

        public static IAxisManager GetVerticalAxis(DependencyObject d)
            => (IAxisManager)d.GetValue(VerticalAxisProperty);

        public static void SetVerticalAxis(DependencyObject d, IAxisManager value)
            => d.SetValue(VerticalAxisProperty, value);

        public IAxisManager VerticalAxis
        {
            get => (IAxisManager)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        static void OnVerticalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ChartBaseControl bc) {
                bc.OnVerticalAxisChanged((IAxisManager)e.OldValue, (IAxisManager)e.NewValue);
                bc.InvalidateVisual();
            }
        }

        void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            if (oldValue != null) {
                oldValue.RangeChanged -= OnVerticalRangeChanged;
            }

            if (newValue != null) {
                newValue.RangeChanged += OnVerticalRangeChanged;
            }
        }

        void OnVerticalRangeChanged(object sender, EventArgs e) {
            InvalidateVisual();
        }

        [Obsolete("Range infomation move to Axis.")]
        public static readonly DependencyProperty RangeXProperty = DependencyProperty.Register(
            nameof(RangeX), typeof(Range), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(new Range(minimum: 0d, maximum: 1d), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );

        public Range RangeX {
            get => HorizontalAxis?.Range;
            set {
                if (HorizontalAxis != null)
                    HorizontalAxis.Range = value;
            }
        }

        [Obsolete("Range infomation move to Axis.")]
        public static readonly DependencyProperty RangeYProperty = DependencyProperty.Register(
            nameof(RangeY), typeof(Range), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(new Range(minimum: 0d, maximum: 1d), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );

        public Range RangeY {
            get => VerticalAxis?.Range;
            set {
                if (VerticalAxis != null)
                    VerticalAxis.Range = value;
            }
        }

        public static readonly DependencyProperty FlippedXProperty =
            DependencyProperty.RegisterAttached(
                nameof(FlippedX), typeof(bool), typeof(ChartBaseControl),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ChartUpdate));

        public static bool GetFlippedX(DependencyObject d)
            => (bool)d.GetValue(FlippedXProperty);

        public static void SetFlippedX(DependencyObject d, IAxisManager value)
            => d.SetValue(FlippedXProperty, value);

        public bool FlippedX {
            get => (bool)GetValue(FlippedXProperty);
            set => SetValue(FlippedXProperty, value);
        }

        public static readonly DependencyProperty FlippedYProperty =
            DependencyProperty.RegisterAttached(
                nameof(FlippedY), typeof(bool), typeof(ChartBaseControl),
                new FrameworkPropertyMetadata(
                    true,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ChartUpdate));

        public static bool GetFlippedY(DependencyObject d)
            => (bool)d.GetValue(FlippedYProperty);

        public static void SetFlippedY(DependencyObject d, IAxisManager value)
            => d.SetValue(FlippedYProperty, value);

        public bool FlippedY {
            get => (bool)GetValue(FlippedYProperty);
            set => SetValue(FlippedYProperty, value);
        }

        public Rect InitialArea
        {
            get
            {
                var rangeX = HorizontalAxis?.InitialRange ?? new Range(0, 1);
                var rangeY = VerticalAxis?.InitialRange ?? new Range(0, 1);
                return new Rect(new Point(rangeX.Minimum, rangeY.Minimum), new Point(rangeX.Maximum, rangeY.Maximum));
            }
        }

        #region field
        protected VisualCollection visualChildren;
        #endregion

        public ChartBaseControl()
        {
            visualChildren = new VisualCollection(this);
        }

        protected virtual void Update() { }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            Update();
        }

        #region PropertyChanged event
        protected static void ChartUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe) {
                fe.InvalidateVisual();
            }
        }
        #endregion

        #region VisualCollection
        protected override int VisualChildrenCount => visualChildren.Count;
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || visualChildren.Count <= index)
                throw new ArgumentOutOfRangeException();
            return visualChildren[index];
        }
        #endregion
    }
}
