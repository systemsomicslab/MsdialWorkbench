using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Windows;

using CompMs.Common.DataStructure;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Core.Dendrogram;
using CompMs.Graphics.Core.GraphAxis;
using Rfx.Riken.OsakaUniv;

namespace ChartDrawingUiTest.Dendrogram
{
    public class DendrogramVM3 : ViewModelBase
    {
        public DrawingDendrogram XDrawingDendrogram
        {
            get => xDrawingDendrogram;
            set
            {
                var tmp = xDrawingDendrogram;
                if (SetProperty(ref xDrawingDendrogram, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged("XDrawingDendrogram");
                    if (xDrawingDendrogram != null)
                        xDrawingDendrogram.PropertyChanged += (s, e) => OnPropertyChanged("XDrawingDendrogram");
                }
            }
        }
        public DrawingDendrogram YDrawingDendrogram
        {
            get => yDrawingDendrogram;
            set
            {
                var tmp = yDrawingDendrogram;
                if (SetProperty(ref yDrawingDendrogram, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged("YDrawingDendrogram");
                    if (yDrawingDendrogram != null)
                        yDrawingDendrogram.PropertyChanged += (s, e) => OnPropertyChanged("YDrawingDendrogram");
                }
            }
        }
        public DrawingCategoryHorizontalAxis XDrawingHorizontalAxis
        {
            get => xDrawingHorizontalAxis;
            set
            {
                var tmp = xDrawingHorizontalAxis;
                if (SetProperty(ref xDrawingHorizontalAxis, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged("XDrawingHorizontalAxis");
                    if (xDrawingHorizontalAxis != null)
                        xDrawingHorizontalAxis.PropertyChanged += (s, e) => OnPropertyChanged("XDrawingHorizontalAxis");
                }
            }
        }
        public DrawingCategoryHorizontalAxis YDrawingHorizontalAxis
        {
            get => yDrawingHorizontalAxis;
            set
            {
                var tmp = yDrawingHorizontalAxis;
                if (SetProperty(ref yDrawingHorizontalAxis, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged("YDrawingHorizontalAxis");
                    if (yDrawingHorizontalAxis != null)
                        yDrawingHorizontalAxis.PropertyChanged += (s, e) => OnPropertyChanged("YDrawingHorizontalAxis");
                }
            }
        }

        private DrawingDendrogram xDrawingDendrogram;
        private DrawingDendrogram yDrawingDendrogram;
        private DrawingCategoryHorizontalAxis xDrawingHorizontalAxis;
        private DrawingCategoryHorizontalAxis yDrawingHorizontalAxis;

        void OnChartPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "XDrawingDendrogram":
                case "XDrawingHorizontalAxis":
                    if (XDrawingDendrogram == null || XDrawingHorizontalAxis == null) break;
                    XDrawingHorizontalAxis.InitialArea = new Rect(
                        XDrawingDendrogram.InitialArea.X, XDrawingHorizontalAxis.InitialArea.Y,
                        XDrawingDendrogram.InitialArea.Width, XDrawingHorizontalAxis.InitialArea.Height
                        );
                    break;
                case "YDrawingDendrogram":
                case "YDrawingHorizontalAxis":
                    if (YDrawingDendrogram == null || YDrawingHorizontalAxis == null) break;
                    YDrawingHorizontalAxis.InitialArea = new Rect(
                        YDrawingDendrogram.InitialArea.X, YDrawingHorizontalAxis.InitialArea.Y,
                        YDrawingDendrogram.InitialArea.Width, YDrawingHorizontalAxis.InitialArea.Height
                        );
                    break;
            }
            switch (e.PropertyName)
            {
                case "XDrawingDendrogram":
                    if (XDrawingDendrogram == null || XDrawingHorizontalAxis == null) break;
                    XDrawingHorizontalAxis.ChartArea = new Rect(
                        XDrawingDendrogram.ChartArea.X, XDrawingHorizontalAxis.ChartArea.Y,
                        XDrawingDendrogram.ChartArea.Width, XDrawingHorizontalAxis.ChartArea.Height
                        );
                    break;
                case "XDrawingHorizontalAxis":
                    if (XDrawingDendrogram == null || XDrawingHorizontalAxis == null) break;
                    XDrawingDendrogram.ChartArea = new Rect(
                        XDrawingHorizontalAxis.ChartArea.X, XDrawingDendrogram.ChartArea.Y,
                        XDrawingHorizontalAxis.ChartArea.Width, XDrawingDendrogram.ChartArea.Height
                        );
                    break;
                case "YDrawingDendrogram":
                    if (YDrawingDendrogram == null || YDrawingHorizontalAxis == null) break;
                    YDrawingHorizontalAxis.ChartArea = new Rect(
                        YDrawingDendrogram.ChartArea.X, YDrawingHorizontalAxis.ChartArea.Y,
                        YDrawingDendrogram.ChartArea.Width, YDrawingHorizontalAxis.ChartArea.Height
                        );
                    break;
                case "YDrawingHorizontalAxis":
                    if (YDrawingDendrogram == null || YDrawingHorizontalAxis == null) break;
                    YDrawingDendrogram.ChartArea = new Rect(
                        YDrawingHorizontalAxis.ChartArea.X, YDrawingDendrogram.ChartArea.Y,
                        YDrawingHorizontalAxis.ChartArea.Width, YDrawingDendrogram.ChartArea.Height
                        );
                    break;
            }
        }

        public DendrogramVM3()
        {
            var path = @"../../data/testmatrix.txt";
            var statObj = getStatObject(path);
            var result = StatisticsMathematics.HierarchicalClusterAnalysis(statObj);

            PropertyChanged += OnChartPropertyChanged;

            XDrawingDendrogram = new DrawingDendrogram()
            {
                Tree = result.XDendrogram,
                Series = Utility.CalculateTreeCoordinate(result.XDendrogram),
            };
            YDrawingDendrogram = new DrawingDendrogram()
            {
                Tree = result.YDendrogram,
                Series = Utility.CalculateTreeCoordinate(result.YDendrogram),
            };
            XDrawingHorizontalAxis = new DrawingCategoryHorizontalAxis()
            {
                XPositions = XDrawingDendrogram.Tree.Leaves.Select(leaf => (double)XDrawingDendrogram.Series[leaf].X).ToArray(),
                Labels = XDrawingDendrogram.Tree.Leaves.Select(leaf => result.StatisticsObject.YLabels[leaf]).ToArray(),
                NLabel = 10,
            };
            YDrawingHorizontalAxis = new DrawingCategoryHorizontalAxis()
            {
                XPositions = YDrawingDendrogram.Tree.Leaves.Select(leaf => (double)YDrawingDendrogram.Series[leaf].X).ToArray(),
                Labels = YDrawingDendrogram.Tree.Leaves.Select(leaf => result.StatisticsObject.XLabels[leaf]).ToArray(),
                NLabel = 10,
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
