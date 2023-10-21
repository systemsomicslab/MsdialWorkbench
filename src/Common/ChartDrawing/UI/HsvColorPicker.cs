using CompMs.Graphics.Helper;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

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
    ///     <MyNamespace:HsvColorPicker/>
    ///
    /// </summary>
    [TemplatePart(Name = SVControlThumbName, Type = typeof(Thumb))]
    public class HsvColorPicker : BaseColorPicker
    {
        private const string SVControlThumbName = "PART_SVThumb";

        static HsvColorPicker() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HsvColorPicker), new FrameworkPropertyMetadata(typeof(HsvColorPicker)));
            SelectedColorProperty.OverrideMetadata(typeof(HsvColorPicker), new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (HsvColorPicker)d;
            cp.OnSelectedColorChanged((Color)e.OldValue, (Color)e.NewValue);
        }

        private void OnSelectedColorChanged(Color oldValue, Color newValue) {
            if (_isEditting) {
                return;
            }
            _isEditting = true;
            ColorHelper.RGBtoHSV(newValue.R / 255d, newValue.G / 255d, newValue.B / 255d, out var h, out var s, out var v);
            SetCurrentValue(HueProperty, h);
            SetCurrentValue(SaturationProperty, s);
            SetCurrentValue(ValueProperty, v);
            _isEditting = false;
        }

        private Thumb _svControlThumb;
        private Canvas _svControlCanvas;
        private bool _isEditting;


        public static readonly DependencyProperty HueProperty =
            DependencyProperty.Register(
                nameof(Hue),
                typeof(double),
                typeof(HsvColorPicker),
                new FrameworkPropertyMetadata(
                    0d,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnHueChanged,
                    CoerceHue));

        public double Hue {
            get => (double)GetValue(HueProperty);
            set => SetValue(HueProperty, value);
        }

        private static void OnHueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (HsvColorPicker)d;
            cp.OnHueChanged((double)e.OldValue, (double)e.NewValue);
        }

        private void OnHueChanged(double oldValue, double newValue) {
            SetValue(HueColorPropertyKey, ColorHelper.FromNormalizedHsv(newValue, 1d, 1d));
            if (_isEditting) {
                return;
            }
            _isEditting = true;
            SetCurrentValue(SelectedColorProperty, ColorHelper.FromNormalizedHsv(newValue, Saturation, Value));
            _isEditting = false;
        }

        private static object CoerceHue(DependencyObject d, object value) {
            if (value is double v) {
                return Math.Max(Math.Min(v, 1d), 0d);
            }
            return d.GetValue(HueProperty);
        }

        public static readonly DependencyProperty SaturationProperty =
            DependencyProperty.Register(
                nameof(Saturation),
                typeof(double),
                typeof(HsvColorPicker),
                new FrameworkPropertyMetadata(
                    1d,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSaturationChanged,
                    CoerceSaturation));

        public double Saturation {
            get => (double)GetValue(SaturationProperty);
            set => SetValue(SaturationProperty, value);
        }

        private static void OnSaturationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (HsvColorPicker)d;
            cp.OnSaturationChanged((double)e.OldValue, (double)e.NewValue);
        }

        private void OnSaturationChanged(double oldValue, double newValue) {
            if (_svControlThumb is Thumb thumb && _svControlCanvas is Canvas canvas) {
                Canvas.SetLeft(thumb, newValue * canvas.ActualWidth - thumb.ActualWidth / 2);
            }
            if (_isEditting) {
                return;
            }
            _isEditting = true;
            SetCurrentValue(SelectedColorProperty, ColorHelper.FromNormalizedHsv(Hue, newValue, Value));
            _isEditting = false;
        }

        private static object CoerceSaturation(DependencyObject d, object value) {
            if (value is double v) {
                return Math.Max(Math.Min(v, 1d), 0d);
            }
            return d.GetValue(SaturationProperty);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(HsvColorPicker),
                new FrameworkPropertyMetadata(
                    1d,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged,
                    CoerceValue));

        public double Value {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (HsvColorPicker)d;
            cp.OnValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        private void OnValueChanged(double oldValue, double newValue) {
            if (_svControlThumb is Thumb thumb && _svControlCanvas is Canvas canvas) {
                Canvas.SetTop(thumb, (1d - newValue) * canvas.ActualHeight - thumb.ActualHeight / 2);
            }
            if (_isEditting) {
                return;
            }
            _isEditting = true;
            SetCurrentValue(SelectedColorProperty, ColorHelper.FromNormalizedHsv(Hue, Saturation, newValue));
            _isEditting = false;
        }

        private static object CoerceValue(DependencyObject d, object value) {
            if (value is double v) {
                return Math.Max(Math.Min(v, 1d), 0d);
            }
            return d.GetValue(ValueProperty);
        }

        private static readonly DependencyPropertyKey HueColorPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HueColor),
                typeof(Color),
                typeof(HsvColorPicker),
                new PropertyMetadata(Colors.Red));

        public static readonly DependencyProperty HueColorProperty = HueColorPropertyKey.DependencyProperty;

        public Color HueColor {
            get => (Color)GetValue(HueColorProperty);
            private set => SetValue(HueColorPropertyKey, value);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            if (_svControlThumb is Thumb) {
                _svControlThumb.DragDelta -= Thumb_DragDelta;
            }
            if (_svControlCanvas is Canvas) {
                _svControlCanvas.MouseDown -= Canvas_MouseDown;
                _svControlCanvas.MouseUp -= Canvas_MouseUp;
                _svControlCanvas.MouseLeave -= Canvas_MouseLeave;
            }
            _svControlThumb = GetTemplateChild(SVControlThumbName) as Thumb;
            if (_svControlThumb is Thumb) {
                _svControlCanvas = _svControlThumb.Parent as Canvas;
                _svControlThumb.DragDelta += Thumb_DragDelta;
            }
            if (_svControlCanvas is Canvas) {
                _svControlCanvas.MouseDown += Canvas_MouseDown;
                _svControlCanvas.MouseUp += Canvas_MouseUp;
                _svControlCanvas.MouseLeave += Canvas_MouseLeave;
            }
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e) {
            if (sender is Thumb thumb) {
                if (thumb.Parent is Canvas canvas) {
                    var x = Canvas.GetLeft(thumb) + thumb.ActualWidth / 2 + e.HorizontalChange;
                    var y = Canvas.GetTop(thumb) + thumb.ActualHeight / 2 + e.VerticalChange;
                    x = Math.Min(Math.Max(x, 0), canvas.ActualWidth);
                    y = Math.Min(Math.Max(y, 0), canvas.ActualHeight);

                    SetCurrentValue(SaturationProperty, x / canvas.ActualWidth);
                    SetCurrentValue(ValueProperty, 1d - y / canvas.ActualHeight);
                }
            }
        }

        private Point? _clickPoint;
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e) {
            if (sender is Canvas canvas) {
                _clickPoint = e.GetPosition(canvas);
                e.Handled = true;
            }
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e) {
            _clickPoint = null;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e) {
            if (sender is Canvas canvas && _clickPoint.HasValue) {
                var current = e.GetPosition(canvas);
                if (Math.Abs(_clickPoint.Value.X - current.X) < SystemParameters.MinimumHorizontalDragDistance
                    && Math.Abs(_clickPoint.Value.Y - current.Y) < SystemParameters.MinimumVerticalDragDistance) {

                    SetCurrentValue(SaturationProperty, current.X / canvas.ActualWidth);
                    SetCurrentValue(ValueProperty, 1d - current.Y / canvas.ActualHeight);
                }
            }
            _clickPoint = null;
        }
    }
}
