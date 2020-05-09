using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Rfx.Riken.OsakaUniv;

using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Core.Heatmap;

namespace ChartDrawingUiTest.Heatmap
{
    internal class HeatmapVM2 : ViewModelBase
    {
        public DrawingHeatmap DrawingHeatmap
        {
            get => drawingHeatmap;
            set
            {
                var tmp = drawingHeatmap;
                if (SetProperty(ref drawingHeatmap, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged("DrawingHeatmap");
                    if (drawingHeatmap != null)
                        drawingHeatmap.PropertyChanged += (s, e) => OnPropertyChanged("DrawingHeatmap");
                }
            }
        }

        private DrawingHeatmap drawingHeatmap;

        public HeatmapVM2()
        {
            var path = @"../../data/testmatrix.txt";
            var statObj = getStatObject(path);
            var result = StatisticsMathematics.HierarchicalClusterAnalysis(statObj);

            var dataMatrix =  MatrixCalculate.MatrixTranspose(result.StatisticsObject.CopyX());
            DrawingHeatmap = new DrawingHeatmap()
            {
                DataMatrix = dataMatrix,
                XPositions = Enumerable.Range(0, dataMatrix.GetLength(1)).Select(e => (double)e).ToArray(),
                YPositions = Enumerable.Range(0, dataMatrix.GetLength(0)).Select(e => (double)e).ToArray(),
            };
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

        bool SetProperty<T, U>(ref U property, T value, [CallerMemberName] string propertyname = null) where T : U
        {
            if (value.Equals(property)) return false;
            property = value;
            OnPropertyChanged(propertyname);
            return true;
        }       
    }
}
