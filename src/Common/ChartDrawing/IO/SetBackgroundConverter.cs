using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CompMs.Graphics.IO;

public sealed class SetBackgroundConverter : DependencyObject, IVisualConverter
{
    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(
            nameof(Background),
            typeof(Brush),
            typeof(SetBackgroundConverter),
            new PropertyMetadata(Brushes.Transparent));

    public Brush Background {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public FrameworkElement Convert(FrameworkElement element) {
        var brush = new VisualBrush(element);
        var width = element.ActualWidth;
        var height = element.ActualHeight;
        var canvas = new Canvas
        {
            Width = width,
            Height = height,
            Background = Background,
        };

        var rectangle = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = brush,
        };
        Canvas.SetTop(rectangle, 0d);
        Canvas.SetLeft(rectangle, 0d);

        canvas.Children.Add(rectangle);
        canvas.Measure(new Size(width, height));
        canvas.Arrange(new Rect(0, 0, width, height));

        return canvas;
    }

    public static SetBackgroundConverter White { get; } = new SetBackgroundConverter { Background = Brushes.White };
}
