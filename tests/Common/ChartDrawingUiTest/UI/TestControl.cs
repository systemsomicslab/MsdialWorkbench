using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ChartDrawingUiTest.UI
{
    public class TestControl : FrameworkElement
    {
        public static readonly DependencyProperty Value1Property =
            DependencyProperty.Register(
                nameof(Value1), typeof(string), typeof(TestControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public string Value1 {
            get => (string)GetValue(Value1Property);
            set => SetValue(Value1Property, value);
        }

        public static readonly DependencyProperty Value2Property =
            DependencyProperty.Register(
                nameof(Value2), typeof(string), typeof(TestControl),
                new PropertyMetadata(string.Empty, OnValue2Changed));

        public string Value2 {
            get => (string)GetValue(Value2Property);
            set => SetValue(Value2Property, value);
        }

        private static void OnValue2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var tc = (TestControl)d;
            tc.InvalidateVisual();
        }

        public static readonly DependencyProperty Value3Property =
            DependencyProperty.Register(
                nameof(Value3), typeof(string), typeof(TestControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public string Value3 {
            get => (string)GetValue(Value3Property);
            set => SetValue(Value3Property, value);
        }

        public static readonly DependencyProperty Value4Property =
            DependencyProperty.Register(
                nameof(Value4), typeof(string), typeof(TestControl),
                new PropertyMetadata(string.Empty, OnValue4Changed));

        public string Value4 {
            get => (string)GetValue(Value4Property);
            set => SetValue(Value4Property, value);
        }

        private static void OnValue4Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var tc = (TestControl)d;
            tc.InvalidateVisual();
        }

        public static readonly DependencyProperty Value5Property =
            DependencyProperty.Register(
                nameof(Value5), typeof(string), typeof(TestControl),
                new PropertyMetadata(string.Empty));

        public string Value5 {
            get => (string)GetValue(Value5Property);
            set => SetValue(Value5Property, value);
        }

        public static readonly DependencyProperty Value6Property =
            DependencyProperty.Register(
                nameof(Value6), typeof(string), typeof(TestControl),
                new FrameworkPropertyMetadata(string.Empty));

        public string Value6 {
            get => (string)GetValue(Value6Property);
            set => SetValue(Value6Property, value);
        }

        public static readonly DependencyProperty Value7Property =
            DependencyProperty.Register(
                nameof(Value7), typeof(string), typeof(TestControl),
                new PropertyMetadata(string.Empty));

        public string Value7 {
            get => (string)GetValue(Value7Property);
            set => SetValue(Value7Property, value);
        }

        public static readonly DependencyProperty Value8Property =
            DependencyProperty.Register(
                nameof(Value8), typeof(string), typeof(TestControl),
                new FrameworkPropertyMetadata(string.Empty));

        public string Value8 {
            get => (string)GetValue(Value8Property);
            set => SetValue(Value8Property, value);
        }

        public static readonly DependencyProperty Value9Property =
            DependencyProperty.Register(
                nameof(Value9), typeof(string), typeof(TestControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public string Value9 {
            get => (string)GetValue(Value9Property);
            set => SetValue(Value9Property, value);
        }

        public static readonly DependencyProperty Value10Property =
            DependencyProperty.Register(
                nameof(Value10), typeof(string), typeof(TestControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public string Value10 {
            get => (string)GetValue(Value10Property);
            set => SetValue(Value10Property, value);
        }

        public static readonly DependencyProperty Value11Property =
            DependencyProperty.Register(
                nameof(Value11), typeof(string), typeof(TestControl),
                new PropertyMetadata(string.Empty, OnValue11Changed));

        public string Value11 {
            get => (string)GetValue(Value11Property);
            set => SetValue(Value11Property, value);
        }

        private static void OnValue11Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var tc = (TestControl)d;
            tc.InvalidateVisual();
        }

        public static readonly DependencyProperty Value12Property =
            DependencyProperty.Register(
                nameof(Value12), typeof(string), typeof(TestControl),
                new PropertyMetadata(string.Empty, OnValue12Changed));

        public string Value12 {
            get => (string)GetValue(Value12Property);
            set => SetValue(Value12Property, value);
        }

        private static void OnValue12Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var tc = (TestControl)d;
            tc.InvalidateVisual();
        }

        public static readonly DependencyProperty Brush1Property =
            DependencyProperty.Register(
                nameof(Brush1), typeof(Brush), typeof(TestControl),
                new PropertyMetadata(Brushes.Black, OnBrush1Changed));

        public Brush Brush1 {
            get => (Brush)GetValue(Brush1Property);
            set => SetValue(Brush1Property, value);
        }

        private static void OnBrush1Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var tc = (TestControl)d;
            if (tc.drawing != null && e.NewValue is Brush b) {
                tc.drawing.Brush = b;
                tc.InvalidateVisual();
            }
        }

        public static readonly DependencyProperty Brush2Property =
            DependencyProperty.Register(
                nameof(Brush2), typeof(Brush), typeof(TestControl),
                new PropertyMetadata(Brushes.Black, OnBrush2Changed));

        public Brush Brush2 {
            get => (Brush)GetValue(Brush2Property);
            set => SetValue(Brush2Property, value);
        }

        private static void OnBrush2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var tc = (TestControl)d;
            if (tc.drawing != null && e.NewValue is Brush b) {
                tc.drawing.Brush = b;
            }
        }

        private int counter = 0;
        private string BuildText() {
            return $"{Value1}, {Value2}, {Value3}, {Value4}, {Value5}, {Value6}, {Value7}, {Value8}, {Value9}, {Value10}, {Value11}, {Value12}\nRender count: {counter}";
        }

        private FormattedText formattedText;
        private GeometryDrawing drawing = null;
        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            counter++;
            formattedText = new FormattedText(
                BuildText(),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Calibri"), 14, Brushes.Black, 1);
            drawing = new GeometryDrawing(Brushes.Gray, null, new RectangleGeometry(new Rect(0, 0, formattedText.Width, formattedText.Height)));
            drawingContext.DrawDrawing(drawing);
            drawingContext.DrawText(formattedText, new Point());
        }
    }
}
