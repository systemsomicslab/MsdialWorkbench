using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Runtime.CompilerServices;

using CompMs.Common.DataStructure;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Core.Dendrogram;
using Rfx.Riken.OsakaUniv;
using System.ComponentModel;

namespace ChartDrawingUiTest.Dendrogram
{
    public class DendrogramVM3 : ViewModelBase
    {
        public DrawingImage XDendrogram
        {
            get => xDendrogram;
            set => SetProperty(ref xDendrogram, value);
            
        }
        public DrawingImage YDendrogram
        {
            get => yDendrogram;
            set => SetProperty(ref yDendrogram, value);
            
        }
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

        private DrawingImage xDendrogram;
        private DrawingImage yDendrogram;
        private DrawingDendrogram xDrawingDendrogram;
        private DrawingDendrogram yDrawingDendrogram;

        void OnDrawingDendrogramChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "XDrawingDendrogram")
            {
                XDendrogram = new DrawingImage(XDrawingDendrogram.CreateChart());
            }
            else if(e.PropertyName == "YDrawingDendrogram")
            {
                YDendrogram = new DrawingImage(YDrawingDendrogram.CreateChart());
            }
        }

        public DendrogramVM3()
        {
            var path = @"../../data/testmatrix.txt";
            var statObj = getStatObject(path);
            var result = StatisticsMathematics.HierarchicalClusterAnalysis(statObj);

            XDrawingDendrogram = new DrawingDendrogram()
            {
                Tree = result.XDendrogram,
                Series = Utility.CalculateTreeCoordinate(result.XDendrogram),
            };
            XDendrogram = new DrawingImage(XDrawingDendrogram.CreateChart());
            YDrawingDendrogram = new DrawingDendrogram()
            {
                Tree = result.YDendrogram,
                Series = Utility.CalculateTreeCoordinate(result.YDendrogram),
            };
            YDendrogram = new DrawingImage(YDrawingDendrogram.CreateChart());

            PropertyChanged += OnDrawingDendrogramChanged;
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
