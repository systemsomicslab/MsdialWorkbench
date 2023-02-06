using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChartDrawingUiTest.Chart
{
    class ErrorBarVM1 : INotifyPropertyChanged
    {
        public double MinX { get; } = 0d;
        public double MaxX { get; } = 10d;
        public double MinY { get; } = 0d;
        public double MaxY { get; } = 10d;

        public List<DataPoint> Series { get; }
        public List<double> XError { get; }
        public List<double> YError { get; }

        public double CapWidth {
            get => _capWidth;
            set {
                _capWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CapWidth)));
            }
        }
        private double _capWidth = 5d;

        public ErrorBarVM1() {
            Series = new List<DataPoint>
            {
                new DataPoint { X = 3, Y = 8 },
                new DataPoint { X = 2, Y = 4 },
                new DataPoint { X = 8, Y = 1 },
                new DataPoint { X = 5, Y = 4 },
            };

            XError = new List<double>
            {
                0.1, 0.1, 0.2, 0.4
            };

            YError = new List<double>
            {
                0.3, 0.2, 0.8, 0.1,
            };

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
