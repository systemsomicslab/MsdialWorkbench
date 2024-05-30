using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace ChartDrawingUiTest.Chart
{
    public class RangeSelectorViewModel : ViewModelBase
    {
        public RangeSelectorViewModel() {
            Datas = new ObservableCollection<DataPoint>(Enumerable.Range(0, 360).Select(i => new DataPoint { X = i, Y = Math.Cos(i / 100d), }));

            HorizontalAxis = ContinuousAxisManager<double>.Build(Datas, d => d.X);
            VerticalAxis = ContinuousAxisManager<double>.Build(Datas, d => d.Y);

            SelectedRanges = new ObservableCollection<RangeSelection>();
        }

        public ObservableCollection<DataPoint> Datas { get; }
        public ContinuousAxisManager<double> HorizontalAxis { get; }
        public ContinuousAxisManager<double> VerticalAxis { get; }

        public AxisRange SelectedRange {
            get => selectedRange;
            set => SetProperty(ref selectedRange, value);
        }
        private AxisRange selectedRange;

        public ObservableCollection<RangeSelection> SelectedRanges {
            get => selectedRanges;
            set => SetProperty(ref selectedRanges, value);
        }
        private ObservableCollection<RangeSelection> selectedRanges;

        public DelegateCommand AddCommand => addCommand ?? (addCommand = new DelegateCommand(Add));
        private DelegateCommand addCommand;

        public DelegateCommand DeleteCommand => deleteCommand ?? (deleteCommand = new DelegateCommand(Delete));
        private DelegateCommand deleteCommand;

        private void Add() {
            if (SelectedRange != null && SelectedRanges != null) {
                SelectedRanges.Add(new RangeSelection(SelectedRange) { IsSelected = true, Color = Colors.Red });
                SelectedRange = null;
            }
        }

        private void Delete() {
            if (SelectedRanges != null) {
                var ranges = SelectedRanges.ToArray();
                foreach (var range in ranges) {
                    if (!range.IsSelected) {
                        SelectedRanges.Remove(range);
                    }
                }
            }
        }
    }
}
