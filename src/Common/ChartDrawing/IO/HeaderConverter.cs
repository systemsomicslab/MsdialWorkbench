using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CompMs.Graphics.IO;

public sealed class HeaderConverter : DependencyObject, IVisualConverter
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(HeaderConverter),
            new PropertyMetadata(default(string)));

    public string Header {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public FrameworkElement Convert(FrameworkElement element) {
        var brush = new VisualBrush(element);
        var height = element.ActualHeight;
        var width = element.ActualWidth;
        var canvas = new Canvas
        {
            Width = width,
            Height = height + 32d,
        };

        var text = new TextBlock
        {
            Text = Header,
            FontSize = 18,
            Foreground = Brushes.Black,
            Background = null,
            TextAlignment = TextAlignment.Left,
        };
        var viewbox = new Viewbox
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = width,
            Height = 32d,
            Child = text,
        };

        Canvas.SetTop(viewbox, 0);
        Canvas.SetLeft(viewbox, 0);

        var rectangle = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = brush,
        };
        Canvas.SetTop(rectangle, 32d);
        Canvas.SetLeft(rectangle, 0);

        canvas.Children.Add(rectangle);
        canvas.Children.Add(viewbox);

        canvas.Measure(new Size(width, height + 32d));
        canvas.Arrange(new Rect(0, 0, width, height + 32d));

        return canvas;
    }

}
