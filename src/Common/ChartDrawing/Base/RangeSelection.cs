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

        public (double, double) ConvertBy(IAxisManager<double> axis) {
            return (FindLowerCore(axis, Range.Minimum), FindUpperCore(axis, Range.Maximum));
        }

        private double FindLowerCore(IAxisManager<double> axis, AxisValue value) {
            double lo = 0d, hi = 1e9;
            while (hi - lo > 1e-6) {
                var mid = (lo + hi) / 2;
                if (axis.TranslateToAxisValue(mid) <= value) {
                    lo = mid;
                }
                else {
                    hi = mid;
                }
            }
            return lo;
        } 

        private double FindUpperCore(IAxisManager<double> axis, AxisValue value) {
            double lo = 0d, hi = 1e9;
            while (hi - lo > 1e-6) {
                var mid = (lo + hi) / 2;
                if (axis.TranslateToAxisValue(mid) >= value) {
                    hi = mid;
                }
                else {
                    lo = mid;
                }
            }
            return hi;
        } 
    }
}
