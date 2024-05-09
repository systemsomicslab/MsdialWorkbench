using System.Windows;

namespace CompMs.Graphics.IO;

public interface IVisualConverter {
    FrameworkElement Convert(FrameworkElement element);
}
