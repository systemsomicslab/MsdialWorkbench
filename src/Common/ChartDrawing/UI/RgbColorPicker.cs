using System;
using System.Windows;
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
    ///     <MyNamespace:RgbColorPicker/>
    ///
    /// </summary>
    public class RgbColorPicker : BaseColorPicker
    {
        static RgbColorPicker() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RgbColorPicker), new FrameworkPropertyMetadata(typeof(RgbColorPicker)));
            SelectedColorProperty.OverrideMetadata(typeof(RgbColorPicker), new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (RgbColorPicker)d;
            cp.OnSelectedColorChanged((Color)e.OldValue, (Color)e.NewValue);
        }

        private bool _isEditing;

        private void OnSelectedColorChanged(Color oldValue, Color newValue) {
            if (_isEditing) {
                return;
            }
            _isEditing = true;
            SetCurrentValue(RedProperty, newValue.R);
            SetCurrentValue(BlueProperty, newValue.B);
            SetCurrentValue(GreenProperty, newValue.G);
            SetCurrentValue(AlphaProperty, newValue.A);
            SetCurrentValue(ColorTextProperty, newValue.ToString());
            _isEditing = false;
        }


        public static readonly DependencyProperty RedProperty =
            DependencyProperty.Register(
                nameof(Red),
                typeof(byte),
                typeof(RgbColorPicker),
                new FrameworkPropertyMetadata(
                    (byte)0xFF,
                    OnRedChanged));

        public byte Red {
            get => (byte)GetValue(RedProperty);
            set => SetValue(RedProperty, value);
        }

        private static void OnRedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (RgbColorPicker)d;
            cp.OnRedChanged((byte)e.OldValue, (byte)e.NewValue);
        }

        private void OnRedChanged(byte oldValue, byte newValue) {
            if (_isEditing) {
                return;
            }
            _isEditing = true;
            Color color = Color.FromArgb(Alpha, newValue, Green, Blue);
            SetCurrentValue(SelectedColorProperty, color);
            SetCurrentValue(ColorTextProperty, color.ToString());
            _isEditing = false;
        }

        public static readonly DependencyProperty GreenProperty =
            DependencyProperty.Register(
                nameof(Green),
                typeof(byte),
                typeof(RgbColorPicker),
                new FrameworkPropertyMetadata(
                    (byte)0xFF,
                    OnGreenChanged));

        public byte Green {
            get => (byte)GetValue(GreenProperty);
            set => SetValue(GreenProperty, value);
        }

        private static void OnGreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (RgbColorPicker)d;
            cp.OnGreenChanged((byte)e.OldValue, (byte)e.NewValue);
        }

        private void OnGreenChanged(byte oldValue, byte newValue) {
            if (_isEditing) {
                return;
            }
            _isEditing = true;
            Color color = Color.FromArgb(Alpha, Red, newValue, Blue);
            SetCurrentValue(SelectedColorProperty, color);
            SetCurrentValue(ColorTextProperty, color.ToString());
            _isEditing = false;
        }

        public static readonly DependencyProperty BlueProperty =
            DependencyProperty.Register(
                nameof(Blue),
                typeof(byte),
                typeof(RgbColorPicker),
                new FrameworkPropertyMetadata(
                    (byte)0xFF,
                    OnBlueChanged));

        public byte Blue {
            get => (byte)GetValue(BlueProperty);
            set => SetValue(BlueProperty, value);
        }

        private static void OnBlueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (RgbColorPicker)d;
            cp.OnBlueChanged((byte)e.OldValue, (byte)e.NewValue);
        }

        private void OnBlueChanged(byte oldValue, byte newValue) {
            if (_isEditing) {
                return;
            }
            _isEditing = true;
            Color color = Color.FromArgb(Alpha, Red, Green, newValue);
            SetCurrentValue(SelectedColorProperty, color);
            SetCurrentValue(ColorTextProperty, color.ToString());
            _isEditing = false;
        }

        public static readonly DependencyProperty AlphaProperty =
            DependencyProperty.Register(
                nameof(Alpha),
                typeof(byte),
                typeof(RgbColorPicker),
                new FrameworkPropertyMetadata(
                    (byte)0xFF,
                    OnAlphaChanged));

        public byte Alpha {
            get => (byte)GetValue(AlphaProperty);
            set => SetValue(AlphaProperty, value);
        }

        private static void OnAlphaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (RgbColorPicker)d;
            cp.OnAlphaChanged((byte)e.OldValue, (byte)e.NewValue);
        }

        private void OnAlphaChanged(byte oldValue, byte newValue) {
            if (_isEditing) {
                return;
            }
            _isEditing = true;
            Color color = Color.FromArgb(newValue, Red, Green, Blue);
            SetCurrentValue(SelectedColorProperty, color);
            SetCurrentValue(ColorTextProperty, color.ToString());
            _isEditing = false;
        }

        public static readonly DependencyProperty ColorTextProperty =
            DependencyProperty.Register(
                nameof(ColorText),
                typeof(string),
                typeof(RgbColorPicker),
                new FrameworkPropertyMetadata(
                    "#FFFFFFFF",
                    OnColorTextChanged));
        public string ColorText {
            get => (string)GetValue(ColorTextProperty);
            set => SetValue(ColorTextProperty, value);
        }

        private static void OnColorTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var cp = (RgbColorPicker)d;
            cp.OnColorTextChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnColorTextChanged(string oldValue, string newValue) {
            if (_isEditing) {
                return;
            }
            _isEditing = true;
            var colorObject = ParseColor(newValue);
            if (colorObject is Color color) {
                SetCurrentValue(SelectedColorProperty, color);
                SetCurrentValue(RedProperty, color.R);
                SetCurrentValue(GreenProperty, color.G);
                SetCurrentValue(BlueProperty, color.B);
                SetCurrentValue(AlphaProperty, color.A);

            }
            _isEditing = false;
        }

        private object ParseColor(string text) {
            try {
                return ColorConverter.ConvertFromString(text);
            }
            catch (FormatException) {

            }
            return null;
        }
    }
}
