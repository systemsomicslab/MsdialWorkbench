using CompMs.CommonMVVM;
using System.Windows.Media;

namespace CompMs.Graphics.UI
{
    public interface IColorPickerItem {
        Color Color { get; }
        string Category { get; }
    }

    public sealed class ColorPickerItem : ViewModelBase, IColorPickerItem {
        public ColorPickerItem(Color color, string category) {
            Color = color;
            Category = category;
        }

        public Color Color { get; }
        public string Category { get; }
    }

    public sealed class CustomColorPickerItem : ViewModelBase, IColorPickerItem {
        public CustomColorPickerItem(string category) {
            Category = category;
        }

        public Color Color {
            get => _color;
            set => SetProperty(ref _color, value);
        }
        private Color _color = Colors.White;
        public string Category { get; }
    }
}
