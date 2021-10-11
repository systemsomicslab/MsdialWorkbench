using System;
using System.Windows;
using System.Windows.Media;


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
            if (e.OldValue != e.NewValue) {
                if (d is ChartBaseControl bc) {
                    bc.OnHorizontalAxisChanged((IAxisManager)e.OldValue, (IAxisManager)e.NewValue);
                    bc.InvalidateVisual();
                }
            }
        }

        void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            if (oldValue != null) {
                oldValue.RangeChanged -= OnHorizontalRangeChanged;
                oldValue.InitialRangeChanged -= OnHorizontalInitialRangeChanged;
            }

            if (newValue != null) {
                newValue.RangeChanged += OnHorizontalRangeChanged;
                newValue.InitialRangeChanged += OnHorizontalInitialRangeChanged;
                newValue.Recalculate(ActualWidth);
                newValue.Reset();
            }
        }

        void OnHorizontalRangeChanged(object sender, EventArgs e) {
            InvalidateVisual();
        }

        void OnHorizontalInitialRangeChanged(object sender, EventArgs e) {
            HorizontalAxis.Recalculate(ActualWidth);
            HorizontalAxis.Reset();
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
            if (e.OldValue != e.NewValue) {
                if (d is ChartBaseControl bc) {
                    bc.OnVerticalAxisChanged((IAxisManager)e.OldValue, (IAxisManager)e.NewValue);
                    bc.InvalidateVisual();
                }
            }
        }

        void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            if (oldValue != null) {
                oldValue.RangeChanged -= OnVerticalRangeChanged;
                oldValue.InitialRangeChanged -= OnVerticalInitialRangeChanged;
            }

            if (newValue != null) {
                newValue.RangeChanged += OnVerticalRangeChanged;
                newValue.InitialRangeChanged += OnVerticalInitialRangeChanged;
                newValue.Recalculate(ActualHeight);
                newValue.Reset();
            }
        }

        void OnVerticalRangeChanged(object sender, EventArgs e) {
            InvalidateVisual();
        }

        void OnVerticalInitialRangeChanged(object sender, EventArgs e) {
            VerticalAxis.Recalculate(ActualHeight);
            VerticalAxis.Reset();
            InvalidateVisual();
        }

        public Range RangeX {
            get => HorizontalAxis?.Range;
            set {
                if (HorizontalAxis != null)
                    HorizontalAxis.Focus(value);
            }
        }

        public Range RangeY {
            get => VerticalAxis?.Range;
            set {
                if (VerticalAxis != null)
                    VerticalAxis.Focus(value);
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

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            base.OnRenderSizeChanged(sizeInfo);
            if (!(HorizontalAxis is null)) {
                HorizontalAxis.Recalculate(sizeInfo.NewSize.Width);
                if (sizeInfo.PreviousSize.Width == 0) {
                    HorizontalAxis.Reset();
                }
            }
            if (!(VerticalAxis is null)) {
                VerticalAxis.Recalculate(sizeInfo.NewSize.Height);
                if (sizeInfo.PreviousSize.Height == 0) {
                    VerticalAxis.Reset();
                }
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
