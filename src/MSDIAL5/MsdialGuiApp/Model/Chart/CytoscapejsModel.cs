using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.DataObj.NodeEdge;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class CytoscapejsModel
    {
        private CytoscapejsModel() { }

        public static CompMs.Common.DataObj.NodeEdge.Chart GetBarGraphProperty(
            AlignmentSpotPropertyModel model,
            IBarItemsLoader loader,
            IReadOnlyDictionary<string, Color> ClassToColor) {
            var chart = loader.LoadBarItemsAsObservable(model);
            var items = chart.ObservableItems.ToReactiveProperty().Value;

            var chartJs = new CompMs.Common.DataObj.NodeEdge.Chart() { type = "bar", data = new ChartData() };
            chartJs.data.labels = new List<string>();
            chartJs.data.datasets = new List<ChartElement>();
            var chartJsElement = new ChartElement() { label = "", data = new List<double>(), backgroundColor = new List<string>() };

            foreach (var chartElem in items) {
                chartJs.data.labels.Add(chartElem.Class);
                chartJsElement.data.Add(chartElem.Height);
                chartJsElement.backgroundColor.Add("rgba(" + ClassToColor[chartElem.Class].R + "," + ClassToColor[chartElem.Class].G + "," + ClassToColor[chartElem.Class].B + ", 0.8)");
            }
            chartJs.data.datasets.Add(chartJsElement);

            return chartJs;
        }

        public static CompMs.Common.DataObj.NodeEdge.Chart GetBarGraphProperty(ChromatogramPeakFeatureModel model, string name) {
            var chartJs = new CompMs.Common.DataObj.NodeEdge.Chart() { type = "bar", data = new ChartData() };
            chartJs.data.labels = new List<string>();
            chartJs.data.datasets = new List<ChartElement>();
            var chartJsElement = new ChartElement() { label = "", data = new List<double>(), backgroundColor = new List<string>() };

            chartJs.data.labels.Add(name);
            chartJsElement.data.Add(model.Intensity);
            chartJsElement.backgroundColor.Add("rgba(0, 0, 255, 0.8)");

            chartJs.data.datasets.Add(chartJsElement);

            return chartJs;
        }
    }
}
