using System.Windows;

namespace CompMs.Graphics.IO;

public sealed class NoneVisualConverter : IVisualConverter {
    public static IVisualConverter Instance { get; } = new NoneVisualConverter();

    public FrameworkElement Convert(FrameworkElement element) => element;
}
