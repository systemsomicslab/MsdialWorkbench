using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace ChartDrawingUiTest.Chart
{
    internal sealed class MultiLineChartControlViewModel1 : ViewModelBase
    {
        readonly int _datapoints = 10000;

        public MultiLineChartControlViewModel1() {
            var datapoints = _datapoints;
            Serieses = new ObservableCollection<Series>(
                Enumerable.Range(0, 12)
                    .Select(i => new Series(
                        Enumerable.Range(0, datapoints)
                            .Select(r => Math.PI / datapoints * r * 2)
                            .Select((r, j) => new DataPoint { X = r + i / 6d * Math.PI, Y = Math.Sin(r), Type = i * datapoints + j, })
                            .ToArray()) { Type = i, }));

            HorizontalAxis = new ContinuousAxisManager<double>(0, 4 * Math.PI, new ConstantMargin(10));
            VerticalAxis = new ContinuousAxisManager<double>(-1, 1, new ConstantMargin(10));
            Brush = new DelegateBrushMapper<Series>(s => s.Type % 2 == 0 ? Brushes.Red : Brushes.Blue);
        }

        public ObservableCollection<Series> Serieses { get; }

        public IAxisManager HorizontalAxis { get; }
        public IAxisManager VerticalAxis { get; }
        public IBrushMapper<Series> Brush { get; }

        public DelegateCommand AddCommand => _addCommand ?? (_addCommand = new DelegateCommand(Add));
        private DelegateCommand _addCommand;

        public DelegateCommand ClearCommand => _clearCommand ?? (_clearCommand = new DelegateCommand(Clear));
        private DelegateCommand _clearCommand;

        private void Add() {
            var rand = new Random();
            var i = rand.NextDouble() * 12d;
            var data = Enumerable.Range(0, _datapoints)
                .Select(r => Math.PI / _datapoints * r * 2)
                .Select((r, j) => new DataPoint { X = r + i / 6d * Math.PI, Y = Math.Sin(r), Type = (int)i * _datapoints + j, })
                .ToArray();
            Serieses.Add(new Series(data) { Type = -1 });
        }

        private void Clear() {
            Serieses.Clear();
        }
    }

    internal sealed class Series : ViewModelBase {
        public Series(DataPoint[] data) {
            Data = data;
        }

        public DataPoint[] Data { get; }
        public int Type { get; set; }
    }
}
