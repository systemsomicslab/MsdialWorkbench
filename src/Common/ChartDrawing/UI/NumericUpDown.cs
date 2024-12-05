using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace CompMs.Graphics.UI
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI;assembly=CompMs.Graphics.UI"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:NumericUpDown/>
    ///
    /// </summary>
    [TemplatePart(Name = PART_UP_BUTTON, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PART_DOWN_BUTTON, Type = typeof(ButtonBase))]
    public class NumericUpDown : Control
    {
        private const string PART_UP_BUTTON = "PART_UpButton";
        private const string PART_DOWN_BUTTON = "PART_DownButton";

        static NumericUpDown() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(typeof(NumericUpDown)));
            MinWidthProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(64d));
            VerticalContentAlignmentProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(VerticalAlignment.Center));
        }

        public NumericUpDown() {
            _baseValue = Value;
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value), typeof(double), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public double Value {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((NumericUpDown)d).OnValueChanged((double)e.OldValue, (double)e.NewValue);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private void OnValueChanged(double oldValue, double newValue) {
            if (!_updating) {
                _baseValue = newValue;
                _counter = 0;
            }
        }
#pragma warning restore IDE0060 // Remove unused parameter

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step), typeof(double), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(1d, OnStepChanged));

        public double Step {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        private static void OnStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((NumericUpDown)d).OnStepChanged((double)e.OldValue, (double)e.NewValue);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private void OnStepChanged(double oldValue, double newValue) {
            _baseValue = Value;
            _counter = 0;
        }
#pragma warning restore IDE0060 // Remove unused parameter

        public static readonly DependencyProperty IntervalProperty =
            DependencyProperty.Register(
                nameof(Interval), typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(SystemParameters.KeyboardSpeed));

        public int Interval {
            get => (int)GetValue(IntervalProperty);
            set => SetValue(IntervalProperty, value);
        }

        public static readonly DependencyProperty DelayProperty =
            DependencyProperty.Register(
                nameof(Delay), typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(SystemParameters.KeyboardDelay));

        public int Delay {
            get => (int)GetValue(DelayProperty);
            set => SetValue(DelayProperty, value);
        }

        private void UpButtonElement_Click(object sender, RoutedEventArgs e) {
            UpdateValue(1);
        }

        private void DownButtonElement_Click(object sender, RoutedEventArgs e) {
            UpdateValue(-1);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e) {
            base.OnMouseWheel(e);
            UpdateValue(Math.Sign(e.Delta));
        }

        private double _baseValue;
        private int _counter = 0;
        private bool _updating = false;
        private void UpdateValue(int count) {
            _updating = true;
            _counter += count;
            var value = _baseValue + _counter * Step;
            Value = value;
            if (Value != value) {
                _baseValue = Value;
                _counter = 0;
            }
            _updating = false;
        }

        private ButtonBase _upButtonElement;
        private ButtonBase _downButtonElement;

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            if (_upButtonElement != null) {
                _upButtonElement.Click -= UpButtonElement_Click;
            }
            if (_downButtonElement != null) {
                _downButtonElement.Click -= DownButtonElement_Click;
            }
            _upButtonElement = GetTemplateChild(PART_UP_BUTTON) as ButtonBase;
            _downButtonElement = GetTemplateChild(PART_DOWN_BUTTON) as ButtonBase;
            if (_upButtonElement != null) {
                _upButtonElement.Click += UpButtonElement_Click;
            }
            if (_downButtonElement != null) {
                _downButtonElement.Click += DownButtonElement_Click;
            }
        }
    }
}
