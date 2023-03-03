using CompMs.CommonMVVM;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class FileClassPropertyModel : BindableBase {
        public FileClassPropertyModel(string name, Color color, int order) {
            Name = name;
            Color = color;
            Order = order;
        }

        public string Name { get; }
        public Color Color {
            get => _color;
            set => SetProperty(ref _color, value);
        }
        private Color _color;
        public int Order {
            get => _order;
            set => SetProperty(ref _order, value);
        }
        private int _order;
    }
}
