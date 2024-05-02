using CompMs.Common.DataStructure;
using CompMs.Common.Mathematics.Statistics;
using CompMs.CommonMVVM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.View.Statistics
{
    /// <summary>
    /// Interaction logic for HcaResultWin.xaml
    /// </summary>
    public partial class HcaResultWin : Window {
        public HcaResultWin() {
            InitializeComponent();
        }

        public HcaResultWin(MultivariateAnalysisResult result) {
            InitializeComponent();
            DataContext = new HcaResultVM(result);
        }

       
    }
    public class HcaResultVM : ViewModelBase {
        public IEnumerable? HeatmapView {
            get => _heatmapView;
            set => SetProperty(ref _heatmapView, value);
        }

        public IEnumerable? FileView {
            get => _fileView;
            private set => SetProperty(ref _fileView, value);
        }

        public IEnumerable? MetaboliteView {
            get => _metaboliteView;
            private set => SetProperty(ref _metaboliteView, value);
        }

        public DirectedTree? FileTree {
            get => _fileTree;
            set => SetProperty(ref _fileTree, value);
        }

        public DirectedTree? MetaboliteTree {
            get => _metaboliteTree;
            set => SetProperty(ref _metaboliteTree, value);
        }

        public double FileDendrogramMinimum {
            get => _fileDendrogramMinimum;
            set => SetProperty(ref _fileDendrogramMinimum, value);
        }

        public double FileDendrogramMaximum {
            get => _fileDendrogramMaximum;
            set => SetProperty(ref _fileDendrogramMaximum, value);
        }

        public double MetaboliteDendrogramMinimum {
            get => _metaboliteDendrogramMinimum;
            set => SetProperty(ref _metaboliteDendrogramMinimum, value);
        }

        public double MetaboliteDendrogramMaximum {
            get => _metaboliteDendrogramMaximum;
            set => SetProperty(ref _metaboliteDendrogramMaximum, value);
        }

        public double HeatmapValueMinimum {
            get => _heatmapValueMinimum;
            set => SetProperty(ref _heatmapValueMinimum, value);
        }

        public double HeatmapValueMaximum {
            get => _heatmapValueMaximum;
            set => SetProperty(ref _heatmapValueMaximum, value);
        }

        public int NumberOfDisplayMetabolite {
            get => _numberOfDisplayMetabolite;
            set => SetProperty(ref _numberOfDisplayMetabolite, value);
        }

        public string? XLabel {
            get => _xLabel;
            set => SetProperty(ref _xLabel, value);
        }

        public string? YLabel {
            get => _yLabel;
            set => SetProperty(ref _yLabel, value);
        }

        public double FileLabelSize {
            get => _fileLabelSize;
            set => SetProperty(ref _fileLabelSize, value);
        }

        public double MetaboliteLabelSize {
            get => _metaboliteLabelSize;
            set => SetProperty(ref _metaboliteLabelSize, value);
        }

        public string? DisplayFileProperty {
            get => _displayFileProperty;
            set => SetProperty(ref _displayFileProperty, value);
        }

        private readonly ObservableCollection<MatrixData> _heatmapSource;
        private readonly ObservableCollection<FileLeaf> _fileSource;
        private readonly ObservableCollection<MetaboliteLeaf> _metaboliteSource;
        private IEnumerable? _fileView, _metaboliteView, _heatmapView;
        private double _fileDendrogramMinimum, _fileDendrogramMaximum,
            _metaboliteDendrogramMinimum, _metaboliteDendrogramMaximum,
            _heatmapValueMinimum, _heatmapValueMaximum;
        private DirectedTree? _fileTree, _metaboliteTree;
        private int _numberOfDisplayMetabolite;
        private string? _xLabel, _yLabel, _displayFileProperty;
        private double _fileLabelSize, _metaboliteLabelSize;

        public HcaResultVM(MultivariateAnalysisResult result) {
            var files = result.StatisticsObject.YLabels;
            var classnames = result.StatisticsObject.YLabels2;
            var filecolors = result.StatisticsObject.YColors;
            var filesource = new List<FileLeaf>(files.Count);
            var filetree = result.XDendrogram;
            var fileroot = filetree.Root;
            var fileleaves = new HashSet<int>(filetree.Leaves);
            var fileorder = Enumerable.Repeat(0, fileleaves.Count).ToArray();
            var counter = 0;
            if (fileleaves.Contains(fileroot)) fileorder[fileroot] = counter++;
            filetree.PreOrder(fileroot, e =>
            {
                if (fileleaves.Contains(e.To)) fileorder[e.To] = counter++;
            });
            for (int i = 0; i < files.Count; i++) {
                var brush = new SolidColorBrush(Color.FromArgb(filecolors[i][3], filecolors[i][0], filecolors[i][1], filecolors[i][2]));
                brush.Freeze();
                filesource.Add(new FileLeaf() {
                    FileName = files[i],
                    ClassName = classnames[i],
                    ID = i,
                    TypeBrush = brush,
                    Order = fileorder[i],
                });
            }
            var xdendro = new double[filetree.Count];
            filetree.PreOrder(e => xdendro[e.To] = xdendro[e.From] + e.Distance);

            var metabolites = result.StatisticsObject.XLabels;
            var metabolitecolors = result.StatisticsObject.XColors;
            var metabolitesource = new List<MetaboliteLeaf>(metabolites.Count);
            var metabolitetree = result.YDendrogram;
            var metaboliteroot = metabolitetree.Root;
            var metaboliteleaves = new HashSet<int>(metabolitetree.Leaves);
            var metaboliteorder = Enumerable.Repeat(0, metaboliteleaves.Count).ToArray();
            var metabolitestdevs = result.StatisticsObject.XStdevs;
            counter = 0;
            if (metaboliteleaves.Contains(metaboliteroot)) metaboliteorder[metaboliteroot] = counter++;
            metabolitetree.PreOrder(metaboliteroot, e =>
            {
                if (metaboliteleaves.Contains(e.To)) metaboliteorder[e.To] = counter++;
            });
            foreach ((int rank, int index) in metabolitestdevs.Select((stdev, index) => (stdev, index))
                                                             .OrderByDescending(p => p.stdev)
                                                             .Select((p, rank) => (rank, p.index))) {
                var brush = new SolidColorBrush(Color.FromArgb(metabolitecolors[index][3], metabolitecolors[index][0], metabolitecolors[index][1], metabolitecolors[index][2]));
                brush.Freeze();
                metabolitesource.Add(new MetaboliteLeaf() {
                    MetaboliteName = metabolites[index],
                    TypeBrush = brush,
                    ID = index,
                    Order = metaboliteorder[index],
                    Rank = rank + 1,
                });
            }
            var ydendro = new double[metabolitetree.Count];
            metabolitetree.PreOrder(e => ydendro[e.To] = ydendro[e.From] + e.Distance);

            var datamatrix = result.StatisticsObject.CopyX();
            var heatmapsource = new List<MatrixData>();

            for (int j = 0; j < datamatrix.GetLength(1); j++) {
                for (int i = 0; i < datamatrix.GetLength(0); i++) {
                    heatmapsource.Add(new MatrixData() {
                        FileId = i,
                        MetaboliteId = j,
                        FileName = files[i],
                        MetaboliteName = metabolites[j],
                        Intensity = datamatrix[i, j]
                    });
                }
            }

            _fileSource = new ObservableCollection<FileLeaf>(filesource.OrderBy(leaf => leaf.Order));
            FileDendrogramMinimum = xdendro.Min();
            FileDendrogramMaximum = xdendro.Max();
            FileTree = filetree;
            FileView = _fileSource;
            XLabel = "Samples";
            FileLabelSize = 12d;
            DisplayFileProperty = "FileName";

            _metaboliteSource = new ObservableCollection<MetaboliteLeaf>(metabolitesource.OrderBy(leaf => leaf.Order));
            NumberOfDisplayMetabolite = Math.Min(metabolitesource.Count, 100);
            MetaboliteTree = metabolitetree;
            MetaboliteDendrogramMinimum = ydendro.Min();
            MetaboliteDendrogramMaximum = ydendro.Max();
            MetaboliteView = _metaboliteSource;
            YLabel = "Metabolites";
            MetaboliteLabelSize = 12d;

            _heatmapSource = new ObservableCollection<MatrixData>(heatmapsource);
            HeatmapView = _heatmapSource;
            HeatmapValueMinimum = _heatmapSource.Min(data => data.Intensity);
            HeatmapValueMaximum = _heatmapSource.Max(data => data.Intensity);

            NumberOfDisplayMetabolite = 50;
            TopNMetaboliteFilter(NumberOfDisplayMetabolite);
        }

        void TopNMetaboliteFilter(int n) {
            if (_metaboliteSource != null) {
                var metabolite = _metaboliteSource.Where(leaf => leaf.Rank <= n);
                MetaboliteView = metabolite;
                if (_heatmapSource != null) {
                    var metaboliteIds = metabolite.Select(leaf => leaf.ID).ToHashSet();
                    HeatmapView = _heatmapSource.Where(data => metaboliteIds.Contains(data.MetaboliteId));
                }
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            switch (e.PropertyName) {
                case nameof(NumberOfDisplayMetabolite):
                    TopNMetaboliteFilter(NumberOfDisplayMetabolite);
                    break;
            }
        }
    }

    public class MatrixData {
        public int FileId { get; set; }
        public int MetaboliteId { get; set; }
        public string? FileName { get; set; }
        public string? MetaboliteName { get; set; }
        public double Intensity { get; set; }
    }

    public class FileLeaf {
        public string? FileName { get; set; }
        public string? ClassName { get; set; }
        public Brush? TypeBrush { get; set; }
        public int ID { get; set; }
        public int Order { get; set; }
    }

    public class MetaboliteLeaf {
        public string? MetaboliteName { get; set; }
        public int ID { get; set; }
        public Brush? TypeBrush { get; set; }
        public int Order { get; set; }
        public int Rank { get; set; }
    }
}
