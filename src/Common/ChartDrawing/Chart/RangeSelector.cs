using CompMs.Graphics.Adorner;
using CompMs.Graphics.Base;
using CompMs.Graphics.Behavior;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Chart
{
    public class RangeSelector : ChartBaseControl
    {
        public RangeSelector() {
            SetValue(MoveByDragBehavior.IsEnabledProperty, BooleanBoxes.FalseBox);
            MouseLeftButtonDown += RangeSelector_MouseLeftButtonDown;
            MouseMove += RangeSelector_MouseMove;
            MouseLeftButtonUp += RangeSelector_MouseLeftButtonUp;
        }

        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(
                nameof(Background),
                typeof(Brush),
                typeof(RangeSelector),
                new FrameworkPropertyMetadata(
                    Brushes.WhiteSmoke,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Background {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly DependencyProperty SelectedRangeProperty =
            DependencyProperty.Register(
                nameof(SelectedRange),
                typeof(AxisRange),
                typeof(RangeSelector),
                new PropertyMetadata(
                    null,
                    OnSelectedRangePropertyChanged));

        public AxisRange SelectedRange {
            get => (AxisRange)GetValue(SelectedRangeProperty);
            set => SetValue(SelectedRangeProperty, value);
        }

        private static void OnSelectedRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSelector rs) {
                rs.OnSelectedRangePropertyChanged((AxisRange)e.OldValue, (AxisRange)e.NewValue);
            }
        }

        private void OnSelectedRangePropertyChanged(AxisRange oldValue, AxisRange newValue) {
            if (!(SelectedRangeAdorner is null) && SelectedRangeAdorner.HorizontalRange.Equals(oldValue) && oldValue != newValue && !dragging) {
                SelectedRangeAdorner.Detach();
                ClearValue(SelectedRangeAdornerProperty);
            }
        }

        public static readonly Color SelectedColor = Colors.Gray;

        public static readonly DependencyProperty DisplayRangesProperty =
            DependencyProperty.Register(
                nameof(DisplayRanges),
                typeof(IReadOnlyList<RangeSelection>),
                typeof(RangeSelector),
                new FrameworkPropertyMetadata(
                    new List<RangeSelection>(),
                    OnDisplayRangesChanged));

        public IReadOnlyList<RangeSelection> DisplayRanges {
            get => (IReadOnlyList<RangeSelection>)GetValue(DisplayRangesProperty);
            set => SetValue(DisplayRangesProperty, value);
        }

        private static void OnDisplayRangesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSelector rs) {
                rs.OnDisplayRangesChanged((IReadOnlyList<RangeSelection>)e.OldValue, (IReadOnlyList<RangeSelection>)e.NewValue);
            }
        }

        private void OnDisplayRangesChanged(IReadOnlyList<RangeSelection> oldValues, IReadOnlyList<RangeSelection> newValues) {
            if (oldValues is INotifyCollectionChanged notifyChangedOld) {
                notifyChangedOld.CollectionChanged -= OnDisplayRangesCollectionChanged;
            }
            if (newValues is INotifyCollectionChanged notifyChangedNew) {
                notifyChangedNew.CollectionChanged += OnDisplayRangesCollectionChanged;
            }

            if (oldValues is null) {
                oldValues = new List<RangeSelection>();
            }
            if (newValues is null) {
                newValues = new List<RangeSelection>();
            }

            var adorners = Adorners.ToArray();
            var delValues = oldValues.Except(newValues);
            foreach (var adorner in delValues.SelectMany(v => adorners.Where(a => a.RangeSelection == v))) {
                adorner.Detach();
                Adorners.Remove(adorner);
            }

            var addValues = newValues.Except(oldValues);
            var newAdorners = new List<RangeSelectAdorner>();
            foreach (var addValue in addValues) {
                var adorner = new RangeSelectAdorner(this, addValue);
                adorner.Attach();
                Adorners.Add(adorner);
                newAdorners.Add(adorner);
            }
            foreach (var adorner in newAdorners) {
                try {
                    adorner.IsClipEnabled = true;
                }
                catch (NullReferenceException e) {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }
        }

        private void OnDisplayRangesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                var adorners = Adorners.ToArray();
                foreach (var adorner in e.OldItems.OfType<RangeSelection>().SelectMany(v => adorners.Where(a => a.RangeSelection == v))) {
                    adorner.Detach();
                    Adorners.Remove(adorner);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Reset) {
                var adorners = Adorners;
                adorners.ForEach(adorner => adorner.Detach());
                adorners.Clear();
            }
            if (e.NewItems != null) {
                foreach (var item in e.NewItems.OfType<RangeSelection>()) {
                    var adorner = new RangeSelectAdorner(this, item) { IsClipEnabled = true };
                    adorner.Attach();
                    Adorners.Add(adorner);
                }
            }
        }

        private static readonly DependencyProperty SelectedRangeAdornerProperty =
            DependencyProperty.Register(
                nameof(SelectedRangeAdorner),
                typeof(RangeSelectAdorner),
                typeof(RangeSelector),
                new PropertyMetadata(null));

        private RangeSelectAdorner SelectedRangeAdorner {
            get => (RangeSelectAdorner)GetValue(SelectedRangeAdornerProperty);
            set => SetValue(SelectedRangeAdornerProperty, value);
        }

        private static readonly DependencyProperty AdornersProperty =
            DependencyProperty.Register(
                nameof(Adorners),
                typeof(List<RangeSelectAdorner>),
                typeof(RangeSelector),
                new PropertyMetadata(
                    new List<RangeSelectAdorner>()));

        private List<RangeSelectAdorner> Adorners {
            get => (List<RangeSelectAdorner>)GetValue(AdornersProperty);
            set => SetValue(AdornersProperty, value);
        }

        private static readonly DependencyProperty InitialPointProperty =
            DependencyProperty.Register(
                nameof(InitialPoint),
                typeof(Point),
                typeof(RangeSelector));

        private Point InitialPoint {
            get => (Point)GetValue(InitialPointProperty);
            set => SetValue(InitialPointProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            drawingContext.DrawRectangle(Background, null, new Rect(RenderSize));
        }

        private bool dragging = false;
        private void RangeSelector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe) {
                if (SelectedRangeAdorner != null) {
                    SelectedRangeAdorner.Detach();
                }
                var adorners = Adorners;
                var initial = e.GetPosition(fe);
                InitialPoint = initial;
                var x = HorizontalAxis.TranslateFromRenderPoint(initial.X, FlippedX, ActualWidth);
                var adorner = new RangeSelectAdorner(fe, new AxisRange(x, x) , SelectedColor, false) { IsClipEnabled = true, };
                adorner.Attach();
                SelectedRangeAdorner = adorner;
                fe.CaptureMouse();
                dragging = true;

                foreach (var adorner_ in adorners) {
                    if (!adorner_.IsSelected) {
                        adorner_.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void RangeSelector_MouseMove(object sender, MouseEventArgs e) {
            if (sender is FrameworkElement fe) {
                var adorner = SelectedRangeAdorner;
                if (dragging && adorner != null) {
                    adorner.CurrentX = HorizontalAxis.TranslateFromRenderPoint(e.GetPosition(fe).X, FlippedX, ActualWidth);
                }
            }
        }

        private void RangeSelector_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe) {
                var adorner = SelectedRangeAdorner;
                if (dragging && adorner != null) {
                    fe.ReleaseMouseCapture();
                    dragging = false;

                    var offset = Math.Abs(e.GetPosition(fe).X - InitialPoint.X);
                    ClearValue(InitialPointProperty);

                    if (offset < SystemParameters.MinimumHorizontalDragDistance) {
                        ClearValue(SelectedRangeAdornerProperty);
                        adorner.Detach();
                    }
                    else {
                        SelectedRange = adorner.HorizontalRange;
                    }

                    foreach (var adorner_ in Adorners) {
                        if (!adorner_.IsSelected) {
                            adorner_.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }
    }
}
