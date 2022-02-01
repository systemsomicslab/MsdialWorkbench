using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    public class RangeSelectableChromatogramModel : BindableBase
    {
        public RangeSelectableChromatogramModel(ChromatogramsModel chromatogramModel) {
            ChromatogramModel = chromatogramModel;
            SelectedRanges = new ObservableCollection<RangeSelection>();
        }

        public ChromatogramsModel ChromatogramModel { get; }

        public Range SelectedRange {
            get => selectedRange;
            set => SetProperty(ref selectedRange, value);
        }
        private Range selectedRange;

        public ObservableCollection<RangeSelection> SelectedRanges {
            get => selectedRanges;
            set => SetProperty(ref selectedRanges, value);
        }
        private ObservableCollection<RangeSelection> selectedRanges;

        public bool CanSetMainRange() {
            return SelectedRanges.Count == 1;
        }

        public void SetMainRange() {
            if (SelectedRange != null) {
                SelectedRanges.Add(new RangeSelection(SelectedRange) { Color = Colors.Blue, IsSelected = true, });
            }
        }

        public bool CanSetSubstractRange() {
            return SelectedRanges.Count == 0;
        }

        public void SetSubtractRange() {
            if (SelectedRange != null) {
                SelectedRanges.Add(new RangeSelection(SelectedRange) { Color = Colors.Red, IsSelected = true, });
            }
        }

        public (double, double) ConvertToRt(RangeSelection range) {
            return (range.Range.Minimum.Value, range.Range.Maximum.Value);
        }
    }
}
