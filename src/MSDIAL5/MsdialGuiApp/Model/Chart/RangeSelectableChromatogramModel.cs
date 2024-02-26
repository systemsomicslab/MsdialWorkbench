using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal class RangeSelectableChromatogramModel : BindableBase
    {
        public RangeSelectableChromatogramModel(ChromatogramsModel chromatogramModel) {
            ChromatogramModel = chromatogramModel;
            SelectedRanges = new ObservableCollection<RangeSelection>();
        }

        public ChromatogramsModel ChromatogramModel { get; }

        public AxisRange? SelectedRange {
            get => selectedRange;
            set => SetProperty(ref selectedRange, value);
        }
        private AxisRange? selectedRange;

        public ObservableCollection<RangeSelection> SelectedRanges { get; }

        public bool CanSetMainRange() {
            return SelectedRanges.Count == 1;
        }

        public void SetMainRange() {
            if (SelectedRange != null) {
                SelectedRanges.Add(new RangeSelection(SelectedRange) { Color = Colors.Blue, IsSelected = true, });
                SelectedRange = null;
            }
        }

        public bool CanSetSubstractRange() {
            return SelectedRanges.Count == 0;
        }

        public void SetSubtractRange() {
            if (SelectedRange != null) {
                SelectedRanges.Add(new RangeSelection(SelectedRange) { Color = Colors.Red, IsSelected = true, });
                SelectedRange = null;
            }
        }

        public void RemoveRanges() {
            SelectedRanges.Clear();
        }

        public (double, double) ConvertToRt(RangeSelection range) {
            var axis = ChromatogramModel.ChromAxisItemSelector.SelectedAxisItem.AxisManager;
            return (FindRt(axis, range.Range.Minimum), FindRt(axis, range.Range.Maximum));
        }

        private double FindRt(IAxisManager<double> axis, AxisValue value) {
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
    }
}
