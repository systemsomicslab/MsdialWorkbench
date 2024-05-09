using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RangeSelectableChromatogramModel : BindableBase
    {
        private readonly ObservableCollection<RangeSelection> _selectedRanges;
        private readonly ObservableCollection<RangeSelection> _subtractRanges;

        public RangeSelectableChromatogramModel(ChromatogramsModel chromatogramModel) {
            ChromatogramModel = chromatogramModel;
            _selectedRanges = new ObservableCollection<RangeSelection>();
            SelectedRanges = new ReadOnlyObservableCollection<RangeSelection>(_selectedRanges);
            _subtractRanges = new ObservableCollection<RangeSelection>();
            SubtractRanges = new ReadOnlyObservableCollection<RangeSelection>(_subtractRanges);
        }

        public ChromatogramsModel ChromatogramModel { get; }

        public AxisRange? SelectedRange {
            get => selectedRange;
            set => SetProperty(ref selectedRange, value);
        }
        private AxisRange? selectedRange;

        public ReadOnlyObservableCollection<RangeSelection> SelectedRanges { get; }

        public RangeSelection? MainRange {
            get => _mainRange;
            private set => SetProperty(ref _mainRange, value);
        }
        private RangeSelection? _mainRange;

        public ReadOnlyObservableCollection<RangeSelection> SubtractRanges { get; }

        public bool CanSetMainRange() {
            return SelectedRange is not null;
        }

        public void SetMainRange() {
            if (SelectedRange is not null) {
                if (MainRange is not null) {
                    _selectedRanges.Remove(MainRange);
                }
                RangeSelection selection = new RangeSelection(SelectedRange) { Color = Colors.Blue, IsSelected = true, };
                _selectedRanges.Add(selection);
                MainRange = selection;
                SelectedRange = null;
            }
        }

        public void RemoveMainRange() {
            if (MainRange is null) {
                return;
            }
            _selectedRanges.Remove(MainRange);
            MainRange = null;
        }

        public bool CanSetSubstractRange() {
            return SelectedRange is not null;
        }

        public void SetSubtractRange() {
            if (SelectedRange != null) {
                RangeSelection selection = new RangeSelection(SelectedRange) { Color = Colors.Red, IsSelected = true, };
                _selectedRanges.Add(selection);
                _subtractRanges.Add(selection);
                SelectedRange = null;
            }
        }

        public void ClearSubtractRanges() {
            foreach (var range in _subtractRanges) {
                _selectedRanges.Remove(range);
            }
            _subtractRanges.Clear();
        }

        public void RemoveRanges() {
            _selectedRanges.Clear();
            _mainRange = null;
            _subtractRanges.Clear();
        }

        public (double, double) ConvertToRt(RangeSelection range) {
            var axis = ChromatogramModel.ChromAxisItemSelector.SelectedAxisItem.AxisManager;
            return range.ConvertBy(axis);
        }
    }
}
