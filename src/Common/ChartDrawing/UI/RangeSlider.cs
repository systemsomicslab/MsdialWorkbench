using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CompMs.Graphics.UI.RangeSlider
{
    [TemplatePart(Name = "PART_LowerRange", Type = typeof(RangeBase))]
    [TemplatePart(Name = "PART_UpperRange", Type = typeof(RangeBase))]
    public class RangeSlider : Control
    {
        private bool _edittingLower, _edittingUpper;

        static RangeSlider() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum), typeof(double), typeof(RangeSlider),
                new FrameworkPropertyMetadata(0d, MinimumChanged));
        public double Minimum {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        private static void MinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSlider slider) {
                slider.LowerRangeElement?.SetValue(RangeBase.MinimumProperty, e.NewValue);
                slider.UpperRangeElement?.SetValue(RangeBase.MinimumProperty, e.NewValue);
                if (slider.LowerValue == (double)e.OldValue) {
                    slider.SetCurrentValue(LowerValueProperty, e.NewValue);
                }
            }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum), typeof(double), typeof(RangeSlider),
                new FrameworkPropertyMetadata(1d, MaximumChanged));
        public double Maximum {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        private static void MaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSlider slider) {
                slider.LowerRangeElement?.SetValue(RangeBase.MaximumProperty, e.NewValue);
                slider.UpperRangeElement?.SetValue(RangeBase.MaximumProperty, e.NewValue);
                if (slider.UpperValue == (double)e.OldValue) {
                    slider.SetCurrentValue(UpperValueProperty, e.NewValue);
                }
            }
        }

        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register(
                nameof(LowerValue), typeof(double), typeof(RangeSlider),
                new FrameworkPropertyMetadata(0d, LowerValueChanged, CoerceValue));
        public double LowerValue {
            get => (double)GetValue(LowerValueProperty);
            set => SetValue(LowerValueProperty, value);
        }

        private static void LowerValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSlider slider) {
                if (!slider._edittingLower) {
                    slider._edittingLower = true;
                    slider.LowerRangeElement?.SetValue(RangeBase.ValueProperty, e.NewValue);
                    slider.CoerceValue(IntervalValueProperty);
                    slider._edittingLower = false;
                }
            }
        }

        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register(
                nameof(UpperValue), typeof(double), typeof(RangeSlider),
                new FrameworkPropertyMetadata(1d, UpperValueChanged, CoerceValue));
        public double UpperValue {
            get => (double)GetValue(UpperValueProperty);
            set => SetValue(UpperValueProperty, value);
        }

        private static void UpperValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSlider slider) {
                if (!slider._edittingUpper) {
                    slider._edittingUpper = true;
                    slider.UpperRangeElement?.SetValue(RangeBase.ValueProperty, e.NewValue);
                    slider.CoerceValue(IntervalValueProperty);
                    slider._edittingUpper = false;
                }
            }
        }

        private static object CoerceValue(DependencyObject d, object value) {
            var slider = (RangeSlider)d;
            var lower = (double)value;
            if (double.IsNaN(lower)) {
                return slider.LowerValue;
            } 
            if (slider.Minimum > lower) {
                return slider.Minimum;
            }
            if (slider.Maximum < lower) {
                return slider.Maximum;
            }
            return value;
        }

        private static readonly DependencyPropertyKey IntervalValuePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IntervalValue), typeof(double), typeof(RangeSlider),
                new PropertyMetadata(1d, OnIntervalValueChanged, CoerceIntervalValue));
        public static readonly DependencyProperty IntervalValueProperty = IntervalValuePropertyKey.DependencyProperty;
        public double IntervalValue {
            get => (double)GetValue(IntervalValueProperty);
            private set => SetValue(IntervalValuePropertyKey, value);
        }

        private static void OnIntervalValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {

        }

        private static object CoerceIntervalValue(DependencyObject d, object value) {
            var slider = (RangeSlider)d;
            return slider.UpperValue - slider.LowerValue;
        }

        private RangeBase _lowerRangeElement, _upperRangeElement;

        public RangeBase LowerRangeElement {
            get => _lowerRangeElement;
            set {
                if (_lowerRangeElement != null) {
                    _lowerRangeElement.ValueChanged -= LowerRangeElement_ValueChanged;
                }
                _lowerRangeElement = value;
                if (_lowerRangeElement != null) {
                    InitializeLowerRangeElement();
                    _lowerRangeElement.ValueChanged += LowerRangeElement_ValueChanged;
                }
            }
        }

        public RangeBase UpperRangeElement {
            get => _upperRangeElement;
            set {
                if (_upperRangeElement != null) {
                    _upperRangeElement.ValueChanged -= UpperRangeElement_ValueChanged;
                }
                _upperRangeElement = value;
                if (_upperRangeElement != null) {
                    InitializeUpperRangeElement();
                    _upperRangeElement.ValueChanged += UpperRangeElement_ValueChanged;
                }
            }
        }

        void InitializeLowerRangeElement() {
            LowerRangeElement.SetValue(RangeBase.MinimumProperty, Minimum);
            LowerRangeElement.SetValue(RangeBase.MaximumProperty, Maximum);
            LowerRangeElement.SetValue(RangeBase.ValueProperty, LowerValue);
        }

        void InitializeUpperRangeElement() {
            UpperRangeElement.SetValue(RangeBase.MinimumProperty, Minimum);
            UpperRangeElement.SetValue(RangeBase.MaximumProperty, Maximum);
            UpperRangeElement.SetValue(RangeBase.ValueProperty, UpperValue);
        }

        void LowerRangeElement_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            SetCurrentValue(LowerValueProperty, Math.Min(LowerRangeElement.Value, UpperRangeElement.Value));
        }
        void UpperRangeElement_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            SetCurrentValue(UpperValueProperty, Math.Max(LowerRangeElement.Value, UpperRangeElement.Value));
        }

        public override void OnApplyTemplate() {
            LowerRangeElement = GetTemplateChild("PART_LowerRange") as RangeBase;
            UpperRangeElement = GetTemplateChild("PART_UpperRange") as RangeBase;
        }
    }
}
