using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CompMs.Graphics.UI.RangeSlider
{
    [TemplatePart(Name = "PART_LowerRange", Type = typeof(RangeBase))]
    [TemplatePart(Name = "PART_UpperRange", Type = typeof(RangeBase))]
    public class RangeSlider : Control
    {
        static RangeSlider() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));
        }

        public RangeSlider() {
            DefaultStyleKey = typeof(RangeSlider);
        }

        public double Minimum {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(RangeSlider), new PropertyMetadata(0d, MinimumChanged));

        public double Maximum {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(RangeSlider), new PropertyMetadata(1d, MaximumChanged));

        public double LowerValue {
            get => (double)GetValue(LowerValueProperty);
            set => SetValue(LowerValueProperty, value);
        }
        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register(nameof(LowerValue), typeof(double), typeof(RangeSlider), new PropertyMetadata(0d, ValueChanged));

        public double UpperValue {
            get => (double)GetValue(UpperValueProperty);
            set => SetValue(UpperValueProperty, value);
        }
        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register(nameof(UpperValue), typeof(double), typeof(RangeSlider), new PropertyMetadata(1d, ValueChanged));

        public double IntervalValue {
            get => (double)GetValue(IntervalValueProperty);
            private set => SetValue(IntervalValuePropertyKey, value);
        }
        private static readonly DependencyPropertyKey IntervalValuePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IntervalValue), typeof(double), typeof(RangeSlider), new PropertyMetadata(0d));
        public static readonly DependencyProperty IntervalValueProperty = IntervalValuePropertyKey.DependencyProperty;


        private RangeBase lowerRangeElement, upperRangeElement;

        public RangeBase LowerRangeElement {
            get => lowerRangeElement;
            set {
                if (lowerRangeElement != null)
                    lowerRangeElement.ValueChanged -= LowerRangeElement_ValueChanged;
                lowerRangeElement = value;
                if (lowerRangeElement != null)
                    lowerRangeElement.ValueChanged += LowerRangeElement_ValueChanged;
            }
        }

        public RangeBase UpperRangeElement {
            get => upperRangeElement;
            set {
                if (upperRangeElement != null)
                    upperRangeElement.ValueChanged -= UpperRangeElement_ValueChanged;
                upperRangeElement = value;
                if (upperRangeElement != null)
                    upperRangeElement.ValueChanged += UpperRangeElement_ValueChanged;
            }
        }

        public override void OnApplyTemplate() {
            LowerRangeElement = GetTemplateChild("PART_LowerRange") as RangeBase;
            UpperRangeElement = GetTemplateChild("PART_UpperRange") as RangeBase;
        }

        void LowerRangeElement_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            LowerValue = Math.Min(LowerRangeElement.Value, UpperRangeElement.Value);
        }
        void UpperRangeElement_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            UpperValue = Math.Max(LowerRangeElement.Value, UpperRangeElement.Value);
        }

        static void MaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSlider slider) {
                if (slider.UpperValue == (double)e.OldValue) {
                    slider.UpperValue = (double)e.NewValue;
                }
            }
        }

        static void MinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSlider slider) {
                if (slider.LowerValue == (double)e.OldValue) {
                    slider.LowerValue = (double)e.NewValue;
                }
            }
        }

        static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSlider slider) {
                slider.IntervalValue = slider.UpperValue - slider.LowerValue;
            }
        }
    }
}
