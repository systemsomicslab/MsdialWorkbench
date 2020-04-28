using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

using CompMs.Common.DataStructure;
using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.Dendrogram
{
    public class DendrogramVM2 : ViewModelBase
    {
        public double [,] DataMatrix { get; set; }
        public DirectedTree XDendrogram { get; set; }
        public DirectedTree YDendrogram { get; set; }
        public IReadOnlyList<string> XLabels { get; set; }
        public IReadOnlyList<string> YLabels { get; set; }
        public int XLabelLimit { get; set; } = -1;
        public int YLabelLimit { get; set; } = -1;

        public DendrogramVM2()
        {
            var path = @"../../data/testmatrix.txt";
            var statObj = getStatObject(path);
            var result = StatisticsMathematics.HierarchicalClusterAnalysis(statObj);

            var dataMatrix = result.StatisticsObject.XDataMatrix;
            var transposeMatrix = new double[dataMatrix.GetLength(1), dataMatrix.GetLength(0)];
            for (int i = 0; i < dataMatrix.GetLength(0); ++i) for (int j = 0; j < dataMatrix.GetLength(1); ++j)
                    transposeMatrix[j, i] = dataMatrix[i, j];

            DataMatrix = transposeMatrix;
            XDendrogram = result.XDendrogram;
            YDendrogram = result.YDendrogram;
            XLabels = result.StatisticsObject.YLabels;
            YLabels = result.StatisticsObject.XLabels;
        }

        private StatisticsObject getStatObject(string path)
        {
            var statobj = new StatisticsObject();
            using (var sr = new StreamReader(path, Encoding.ASCII, false))
            {
                for (int i = 1; i <= 9; i++)
                    sr.ReadLine();
                // statobj.YLabels = new ObservableCollection<string>(sr.ReadLine().Split('\t').Skip(31).ToArray());
                var ylabel = sr.ReadLine().Split('\t').Skip(31).ToList();
                ylabel.RemoveAt(4);
                statobj.YLabels = new ObservableCollection<string>(ylabel);
                statobj.YVariables = Enumerable.Repeat(1, statobj.YLabels.Count).Select(e => (double)e).ToArray();
                var mat = new List<double[]>();
                var names = new List<string>();
                while (sr.EndOfStream == false){
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var arr = line.Split('\t');
                    var id = arr[0];
                    var name = arr[3];
                    names.Add(id + name);
                    var vec = arr.Skip(31).Select(val => double.Parse(val)).ToList();
                    vec.RemoveAt(4);
                    mat.Add(vec.ToArray());
                }
                statobj.XLabels = new ObservableCollection<string>(names.ToArray());
                statobj.XDataMatrix = new double[statobj.YLabels.Count, statobj.XLabels.Count];
                for (int j = 0; j < statobj.XLabels.Count; ++j)
                    for (int i = 0; i < statobj.YLabels.Count; ++i)
                        statobj.XDataMatrix[i, j] = mat[j][i];
            }
            statobj.StatInitialization();
            return statobj;
        }
    }
}
