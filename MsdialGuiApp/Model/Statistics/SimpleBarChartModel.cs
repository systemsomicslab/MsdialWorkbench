using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Statistics {
    internal class SimpleBarChartModel : BindableBase {
        public SimpleBarChartModel(ObservableCollection<SimpleBarItem> items, 
            string xAxisTitle, 
            string yAxisTitle,
            string graphTitle) { 
            BarItems = items;
            XAxisTitle = xAxisTitle;
            YAxisTitle = yAxisTitle;
            GraphTitle = graphTitle;
            if (items.IsEmptyOrNull()) return;

            XAxis = new CategoryAxisManager<string>(items.Select(loading => loading.Legend).ToArray());
            YAxis = new Lazy<IAxisManager<double>>(() => new AbsoluteAxisManager(new Range(0d, items.Select(n => n.Value).DefaultIfEmpty().Max(Math.Abs)), new ConstantMargin(0, 10)));
        }
        public string XAxisTitle {
            get => xAxisTitle;
            set => SetProperty(ref xAxisTitle, value);
        }
        private string xAxisTitle;

        public string YAxisTitle {
            get => yAxisTitle;
            set => SetProperty(ref yAxisTitle, value);
        }
        private string yAxisTitle;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;

        public ObservableCollection<SimpleBarItem> BarItems {
            get => barItems;
            set => SetProperty(ref barItems, value);
        }

        private ObservableCollection<SimpleBarItem> barItems;

        public IAxisManager<string> XAxis {
            get => xAxis;
            set => SetProperty(ref xAxis, value);
        }
        private IAxisManager<string> xAxis;

        public Lazy<IAxisManager<double>> YAxis {
            get => yAxis;
            set => SetProperty(ref yAxis, value);
        }
        private Lazy<IAxisManager<double>> yAxis;
    }

    internal class SimpleBarItem : BindableBase {
        public SimpleBarItem() { }
        public SimpleBarItem(int id, string legend, double value, double error) {
            ID = id;
            Legend = legend;
            Value = value;
            Error = error;
        }
        public SimpleBarItem(int id, string legend, double value, double error, SolidColorBrush brush) : this(id, legend, value, error) {
            Brush = brush;
        }

        public SolidColorBrush Brush { get; set; } = Brushes.Blue;
        public int ID { get; set; }
        public string Legend { get; set; }
        public double Value { get; set; }
        public double Error { get; set; }
    }
}
