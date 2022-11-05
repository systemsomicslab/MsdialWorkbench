using CompMs.CommonMVVM;
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
        }
        public string XAxisTitle { get; }
        public string YAxisTitle { get; }
        public string GraphTitle { get; }

        public ObservableCollection<SimpleBarItem> BarItems { get; }
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
