using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Algorithm.Function;
using CompMs.Common.DataObj.NodeEdge;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    public static class CytoscapejsModel
    {
        public static CompMs.Common.DataObj.NodeEdge.Chart GetBarGraphProperty(AlignmentSpotPropertyModel model, IBarItemsLoader loader, IReadOnlyDictionary<string, Color> ClassToColor) {
            var chart = loader.LoadBarItemsAsObservable(model);
            var items = chart.ObservableItems.ToReactiveProperty().Value;
            return GetBarGraphPropertyCore(items.Select(item => item.Height), items.Select(item => ToCytoscapeColor(ClassToColor[item.Class])), items.Select(item => item.Class));
        }

        public static CompMs.Common.DataObj.NodeEdge.Chart GetBarGraphProperty(ChromatogramPeakFeatureModel model, string name) {
            return GetBarGraphPropertyCore(new[] { model.Intensity }, new[] { "rgba(0, 0, 255, 0.8)" }, new[] { name });
        }

        private static CompMs.Common.DataObj.NodeEdge.Chart GetBarGraphPropertyCore(IEnumerable<double> intensities, IEnumerable<string> colors, IEnumerable<string> classes) {
            var chartJsElement = new ChartElement {
                label = "",
                data = intensities.ToList(),
                backgroundColor = colors.ToList(),
            };
            var chartJs = new CompMs.Common.DataObj.NodeEdge.Chart {
                type = "bar",
                data = new ChartData
                {
                    labels = classes.ToList(),
                    datasets = new List<ChartElement> { chartJsElement, },
                }
            };
            return chartJs;
        }

        private static string ToCytoscapeColor(Color color) {
            return $"rgba({color.R},{color.G},{color.B}, 0.8)";
        }

        public static void SendToCytoscapeJs(MolecularNetworkInstance network) {
            var curDir = AppDomain.CurrentDomain.BaseDirectory;
            var cytoDir = Path.Combine(curDir, "CytoscapeLocalBrowser");
            var cyjsexportpath = Path.Combine(cytoDir, "data", "elements.js");
            if (!network.SaveCytoscapeJs(cyjsexportpath)) {
                return;
            }
            var url = Path.Combine(cytoDir, "MsdialCytoscapeViewer.html");
            System.Diagnostics.Process.Start(url);
        }
    }
}
