using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Rfx.Riken.OsakaUniv;

using CompMs.Graphics.Core.Heatmap;
using CompMs.Graphics.Core.GraphAxis;

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
        public DrawingCategoryHorizontalAxis DrawingHorizontalAxis
        {
            get => drawingHorizontalAxis;
            set
            {
                var tmp = drawingHorizontalAxis;
                if (SetProperty(ref drawingHorizontalAxis, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged("DrawingHorizontalAxis");
                    if (drawingHorizontalAxis != null)
                        drawingHorizontalAxis.PropertyChanged += (s, e) => OnPropertyChanged("DrawingHorizontalAxis");
                }
            }
        }
        public DrawingCategoryVerticalAxis DrawingVerticalAxis
        {
            get => drawingVerticalAxis;
            set
            {
                var tmp = drawingVerticalAxis;
                if (SetProperty(ref drawingVerticalAxis, value))
                {
                    if (tmp != null)
                        tmp.PropertyChanged -= (s, e) => OnPropertyChanged("DrawingVerticalAxis");
                    if (drawingVerticalAxis != null)
                        drawingVerticalAxis.PropertyChanged += (s, e) => OnPropertyChanged("DrawingVerticalAxis");
                }
            }
        }
        public double X
        {
            get => x;
            set => SetProperty(ref x, value);
        }
        public double Y
        {
            get => y;
            set => SetProperty(ref y, value);
        }
        public double Width
        {
            get => width;
            set => SetProperty(ref width, value);
        }
        public double Height
        {
            get => height;
            set => SetProperty(ref height, value);
        }
        public double InitialX
        {
            get => initialX;
            set => SetProperty(ref initialX, value);
        }
        public double InitialY
        {
            get => initialY;
            set => SetProperty(ref initialY, value);
        }
        public double InitialWidth
        {
            get => initialWidth;
            set => SetProperty(ref initialWidth, value);
        }
        public double InitialHeight
        {
            get => initialHeight;
            set => SetProperty(ref initialHeight, value);
        }

        private DrawingHeatmap drawingHeatmap;
        private DrawingCategoryHorizontalAxis drawingHorizontalAxis;
        private DrawingCategoryVerticalAxis drawingVerticalAxis;
        private double x;
        private double y;
        private double width;
        private double height;
        private double initialX;
        private double initialY;
        private double initialWidth;
        private double initialHeight;

        void OnChartPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            System.Windows.Rect area;
            switch (e.PropertyName)
            {
                case "DrawingHeatmap":
                    X = DrawingHeatmap.ChartArea.X;
                    Y = DrawingHeatmap.ChartArea.Y;
                    Width = DrawingHeatmap.ChartArea.Width;
                    Height = DrawingHeatmap.ChartArea.Height;
                    InitialX = DrawingHeatmap.InitialArea.X;
                    InitialY = DrawingHeatmap.InitialArea.Y;
                    InitialWidth = DrawingHeatmap.InitialArea.Width;
                    InitialHeight = DrawingHeatmap.InitialArea.Height;
                    break;
                case "DrawingHorizontalAxis":
                    X = DrawingHorizontalAxis.ChartArea.X;
                    Width = DrawingHorizontalAxis.ChartArea.Width;
                    OnPropertyChanged("InitialX");
                    OnPropertyChanged("InitialWidth");
                    break;
                case "DrawingVerticalAxis":
                    Y = DrawingVerticalAxis.ChartArea.Y;
                    Height = DrawingVerticalAxis.ChartArea.Height;
                    OnPropertyChanged("InitialY");
                    OnPropertyChanged("InitialHeight");
                    break;
                case "X":
                    area = DrawingHeatmap.ChartArea;
                    area.X = X;
                    DrawingHeatmap.ChartArea = area;
                    area = DrawingHorizontalAxis.ChartArea;
                    area.X = X;
                    DrawingHorizontalAxis.ChartArea = area;
                    break;
                case "Y":
                    area = DrawingHeatmap.ChartArea;
                    area.Y = Y;
                    DrawingHeatmap.ChartArea = area;
                    area = DrawingVerticalAxis.ChartArea;
                    area.Y = Y;
                    DrawingVerticalAxis.ChartArea = area;
                    break;
                case "Width":
                    area = DrawingHeatmap.ChartArea;
                    area.Width = Width;
                    DrawingHeatmap.ChartArea = area;
                    area = DrawingHorizontalAxis.ChartArea;
                    area.Width = Width;
                    DrawingHorizontalAxis.ChartArea = area;
                    break;
                case "Height":
                    area = DrawingHeatmap.ChartArea;
                    area.Height = Height;
                    DrawingHeatmap.ChartArea = area;
                    area = DrawingVerticalAxis.ChartArea;
                    area.Height = Height;
                    DrawingVerticalAxis.ChartArea = area;
                    break;
                case "InitialX":
                    area = DrawingHorizontalAxis.InitialArea;
                    area.X = InitialX;
                    DrawingHorizontalAxis.InitialArea = area;
                    break;
                case "InitialY":
                    area = DrawingVerticalAxis.InitialArea;
                    area.Y = InitialY;
                    DrawingVerticalAxis.InitialArea = area;
                    break;
                case "InitialWidth":
                    area = DrawingHorizontalAxis.InitialArea;
                    area.Width = InitialWidth;
                    DrawingHorizontalAxis.InitialArea = area;
                    break;
                case "InitialHeight":
                    area = DrawingVerticalAxis.InitialArea;
                    area.Height = InitialHeight;
                    DrawingVerticalAxis.InitialArea = area;
                    break;
            }
        }

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
            DrawingHorizontalAxis = new DrawingCategoryHorizontalAxis()
            {
                XPositions = DrawingHeatmap.XPositions,
                Labels = result.StatisticsObject.YLabels,
                NLabel = 10,
            };
            DrawingVerticalAxis = new DrawingCategoryVerticalAxis()
            {
                YPositions = DrawingHeatmap.YPositions,
                Labels = result.StatisticsObject.XLabels,
                NLabel = 10,
            };
            PropertyChanged += OnChartPropertyChanged;
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
