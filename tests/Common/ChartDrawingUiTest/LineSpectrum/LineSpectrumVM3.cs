using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace ChartDrawingUiTest.LineSpectrum
{
    class LineSpectrumVM3 : ViewModelBase
    {
        public ObservableCollection<DataPoint> Source { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public IBrushMapper SpectrumBrush {
            get => spectrumBrush;
            set => SetProperty(ref spectrumBrush, value);
        }
        private IBrushMapper spectrumBrush;

        public List<IBrushMapper> SpectrumBrushes { get; }

        public LineSpectrumVM3() {
            Source = new ObservableCollection<DataPoint>(
                Enumerable.Range(0, 63).Select(v => new DataPoint { X = v / 10d, Y = Math.Sin(v / 10d), Type = v / 10 })
            );

            HorizontalAxis = ContinuousAxisManager<double>.Build(Source, p => p.X);
            VerticalAxis = ContinuousAxisManager<double>.Build(Source, p => p.Y);

            var brush1 = new KeyBrushMapper<int>(new Dictionary<int, Brush> {
                { 0, Brushes.Red },
                { 1, Brushes.Green },
                { 2, Brushes.Blue },
                { 3, Brushes.Black },
                { 4, Brushes.DarkRed },
                { 5, Brushes.DarkGreen },
                { 6, Brushes.DarkBlue },
            });

            var brush2 = new SequentialBrushMapper(new List<Brush> {
                Brushes.Yellow, Brushes.Magenta, Brushes.Cyan, Brushes.Gray,
                Brushes.GreenYellow, Brushes.DarkMagenta, Brushes.DarkCyan,
            });

            var brush3 = new DelegateBrushMapper<int>(v => Color.FromRgb((byte)(255 / 6 * v), 100, 50));

            var brush4 = new ConstantBrushMapper<int>(Brushes.Pink);

            SpectrumBrushes = new List<IBrushMapper> { brush1, brush2, brush3, brush4, };
            SpectrumBrush = brush1;
        }
    }
}
