using CompMs.CommonMVVM;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public class RangeSelection : BindableBase
    {
        public RangeSelection(AxisRange range) {
            Range = range;
        }
        
        public AxisRange Range { get; }

        public bool IsSelected {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        private bool isSelected;

        public Color Color {
            get => color;
            set => SetProperty(ref color, value);
        }
        private Color color = Colors.Gray;
    }
}
