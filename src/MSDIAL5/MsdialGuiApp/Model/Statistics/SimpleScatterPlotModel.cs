using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal class SimpleScatterPlotModel : BindableBase {
        public SimpleScatterPlotModel(ObservableCollection<SimplePlotItem> items,
            string xAxisTitle,
            string yAxisTitle,
            string graphTitle) {
            PlotItems = items;
            XAxisTitle = xAxisTitle;
            YAxisTitle = yAxisTitle;
            GraphTitle = graphTitle;
            if (items.IsEmptyOrNull()) return;

            XAxis = new ContinuousAxisManager<double>(items.Select(n => n.XValue).ToList(), new ConstantMargin(10, 10));
            YAxis = new ContinuousAxisManager<double>(items.Select(n => n.YValue).ToList(), new ConstantMargin(10, 10));
      
            PointBrush = new DelegateBrushMapper<SimplePlotItem>((SimplePlotItem item) => item.Brush);
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

        public ObservableCollection<SimplePlotItem>? PlotItems {
            get => plotItems;
            set => SetProperty(ref plotItems, value);
        }

        private ObservableCollection<SimplePlotItem>? plotItems;

        public IAxisManager<double>? XAxis {
            get => xAxis;
            set => SetProperty(ref xAxis, value);
        }
        private IAxisManager<double>? xAxis;

        public IAxisManager<double>? YAxis {
            get => yAxis;
            set => SetProperty(ref yAxis, value);
        }
        private IAxisManager<double>? yAxis;

        public IBrushMapper<SimplePlotItem>? PointBrush {
            get => _pointBrush;
            set => SetProperty(ref _pointBrush, value);
        }
        private IBrushMapper<SimplePlotItem>? _pointBrush;
    }

    internal class SimplePlotItem : BindableBase {
        public SimplePlotItem(int id, string legend, double xValue, double yValue) {
            ID = id;
            Legend = legend;
            XValue = xValue;
            YValue = yValue;
        }

        public SimplePlotItem(int id, string legend, double xValue, double yValue, SolidColorBrush brush) : this(id, legend, xValue, yValue) {
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
        public double XValue {
            get => xValue;
            set => SetProperty(ref xValue, value);
        }
        private double xValue;
    }
}
