using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal class SimpleBarChartModel : BindableBase {
        public SimpleBarChartModel(ObservableCollection<SimpleBarItem> items, 
            string xAxisTitle, 
            string yAxisTitle,
            string graphTitle) { 
            barItems = items;
            XAxisTitle = xAxisTitle;
            YAxisTitle = yAxisTitle;
            GraphTitle = graphTitle;
            if (items.IsEmptyOrNull()) return;

            XAxis = new CategoryAxisManager<string>(items.Select(loading => loading.Legend).ToArray());
            YAxis = new AbsoluteAxisManager(new AxisRange(0d, items.Select(n => n.YValue).DefaultIfEmpty().Max(Math.Abs)), new ConstantMargin(0, 10));
        }
        public string? XAxisTitle {
            get => xAxisTitle;
            set => SetProperty(ref xAxisTitle, value);
        }
        private string? xAxisTitle;

        public string? YAxisTitle {
            get => yAxisTitle;
            set => SetProperty(ref yAxisTitle, value);
        }
        private string? yAxisTitle;

        public string? GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string? graphTitle;

        public ObservableCollection<SimpleBarItem> BarItems {
            get => barItems;
            set => SetProperty(ref barItems, value);
        }

        private ObservableCollection<SimpleBarItem> barItems;

        public IAxisManager<string>? XAxis {
            get => xAxis;
            set => SetProperty(ref xAxis, value);
        }
        private IAxisManager<string>? xAxis;

        public IAxisManager<double>? YAxis {
            get => yAxis;
            set => SetProperty(ref yAxis, value);
        }
        private IAxisManager<double>? yAxis;
    }

    internal class SimpleBarItem : BindableBase {
        public SimpleBarItem(int id, string legend, double value, double error) {
            ID = id;
            Legend = legend;
            YValue = value;
            Error = error;
        }
        public SimpleBarItem(int id, string legend, double value, double error, SolidColorBrush brush) : this(id, legend, value, error) {
            Brush = brush;
        }

        public SolidColorBrush Brush { get; set; } = Brushes.Blue;
        public int ID {
            get => id;
            set => SetProperty(ref id, value);
        }
        private int id;
        public string Legend {
            get => legend;
            set => SetProperty(ref legend, value);
        }

        private string legend = string.Empty;
        public double YValue {
            get => yValue;
            set => SetProperty(ref yValue, value);
        }
        private double yValue;
        public double Error {
            get => error;
            set => SetProperty(ref error, value);
        }
        private double error;
    }
}
