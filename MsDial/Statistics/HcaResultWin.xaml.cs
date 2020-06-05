using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using CompMs.Common.DataStructure;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for HcaResultWin.xaml
    /// </summary>
    public partial class HcaResultWin : Window
    {
        public HcaResultWin()
        {
            InitializeComponent();
        }

        public HcaResultWin(MultivariateAnalysisResult result)
        {
            InitializeComponent();
            DataContext = new HcaResultVM(result);
        }
    }

    public class HcaResultVM : ViewModelBase
    {
        #region Property
        public ObservableCollection<MatrixData> HeatmapSource
        {
            get => heatmapSource;
            set => SetProperty(ref heatmapSource, value);
        }

        public ICollectionView FileView
        {
            get => fileView;
        }

        public ICollectionView MetaboliteView
        {
            get => metaboliteView;
        }

        public DirectedTree FileTree
        {
            get => fileTree;
            set => SetProperty(ref fileTree, value);
        }

        public DirectedTree MetaboliteTree
        {
            get => metaboliteTree;
            set => SetProperty(ref metaboliteTree, value);
        }

        public double FileDendrogramMinimum
        {
            get => fileDendrogramMinimum;
            set => SetProperty(ref fileDendrogramMinimum, value);
        }

        public double FileDendrogramMaximum
        {
            get => fileDendrogramMaximum;
            set => SetProperty(ref fileDendrogramMaximum, value);
        }

        public double MetaboliteDendrogramMinimum
        {
            get => metaboliteDendrogramMinimum;
            set => SetProperty(ref metaboliteDendrogramMinimum, value);
        }

        public double MetaboliteDendrogramMaximum
        {
            get => metaboliteDendrogramMaximum;
            set => SetProperty(ref metaboliteDendrogramMaximum, value);
        }

        public int NumberOfDisplayMetabolite
        {
            get => numberOfDisplayMetabolite;
            set => SetProperty(ref numberOfDisplayMetabolite, value);
        }

        public string XLabel
        {
            get => xLabel;
            set => SetProperty(ref xLabel, value);
        }

        public string YLabel
        {
            get => yLabel;
            set => SetProperty(ref yLabel, value);
        }

        public string DisplayFileProperty
        {
            get => displayFileProperty;
            set => SetProperty(ref displayFileProperty, value);
        }
        #endregion

        #region Field
        private ObservableCollection<MatrixData> heatmapSource;
        private ObservableCollection<FileLeaf> fileSource;
        private ObservableCollection<MetaboliteLeaf> metaboliteSource;
        private ICollectionView fileView, metaboliteView;
        private double fileDendrogramMinimum, fileDendrogramMaximum, metaboliteDendrogramMinimum, metaboliteDendrogramMaximum;
        private DirectedTree fileTree, metaboliteTree;
        private int numberOfDisplayMetabolite;
        private string xLabel, yLabel, displayFileProperty;
        #endregion

        public HcaResultVM(MultivariateAnalysisResult result)
        {
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
            for (int i = 0; i < files.Count; i++)
            {
                var brush = new SolidColorBrush(Color.FromArgb(filecolors[i][3], filecolors[i][0], filecolors[i][1], filecolors[i][2]));
                brush.Freeze();
                filesource.Add(new FileLeaf()
                {
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
            foreach((int rank, int index) in metabolitestdevs.Select((stdev, index) => (stdev, index))
                                                             .OrderByDescending(p => p.stdev)
                                                             .Select((p, rank) => (rank, p.index)))
            {
                metabolitesource.Add(new MetaboliteLeaf()
                {
                    MetaboliteName = metabolites[index],
                    ID = index,
                    Order = metaboliteorder[index],
                    Rank = rank + 1,
                });
            }
            var ydendro = new double[metabolitetree.Count];
            metabolitetree.PreOrder(e => ydendro[e.To] = ydendro[e.From] + e.Distance);

            var datamatrix = result.StatisticsObject.CopyX();
            var heatmapsource = new List<MatrixData>();

            for (int j = 0; j < datamatrix.GetLength(1); j++)
            {
                for (int i = 0; i < datamatrix.GetLength(0); i++)
                {
                    heatmapsource.Add(new MatrixData()
                    {
                        FileId = i,
                        MetaboliteId = j,
                        FileName = files[i],
                        MetaboliteName = metabolites[j],
                        Intensity = datamatrix[i, j]
                    });
                }
            }

            fileSource = new ObservableCollection<FileLeaf>(filesource);
            FileDendrogramMinimum = xdendro.Min();
            FileDendrogramMaximum = xdendro.Max();
            FileTree = filetree;
            fileView = CollectionViewSource.GetDefaultView(fileSource);
            fileView.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
            XLabel = "Samples";
            DisplayFileProperty = "FileName";

            metaboliteSource = new ObservableCollection<MetaboliteLeaf>(metabolitesource);
            NumberOfDisplayMetabolite = Math.Min(metabolitesource.Count, 100);
            MetaboliteTree = metabolitetree;
            MetaboliteDendrogramMinimum = ydendro.Min();
            MetaboliteDendrogramMaximum = ydendro.Max();
            metaboliteView = CollectionViewSource.GetDefaultView(metaboliteSource);
            metaboliteView.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
            metaboliteView.Filter += VarianceFiltering;
            YLabel = "Metabolites";

            HeatmapSource = new ObservableCollection<MatrixData>(heatmapsource);
        }

        bool VarianceFiltering(object leaf)
        {
            return ((MetaboliteLeaf)leaf).Rank <= NumberOfDisplayMetabolite;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            switch (e.PropertyName)
            {
                case nameof(NumberOfDisplayMetabolite):
                    if (MetaboliteView != null)
                        MetaboliteView.Refresh();
                    break;
            }
        }

        protected bool SetProperty<T>(ref T prop, T value, [CallerMemberName]string propertyname = "")
        {
            if (prop == null && value != null || !prop.Equals(value))
            {
                prop = value;
                OnPropertyChanged(propertyname);
                return true;
            }
            return false;
        }
    }

    public class MatrixData
    {
        public int FileId { get; set; }
        public int MetaboliteId { get; set; }
        public string FileName { get; set; }
        public string MetaboliteName { get; set; }
        public double Intensity { get; set; }
    }

    public class FileLeaf
    {
        public string FileName { get; set; }
        public string ClassName { get; set; }
        public Brush TypeBrush { get; set; }
        public int ID { get; set; }
        public int Order { get; set; }
    }

    public class MetaboliteLeaf
    {
        public string MetaboliteName { get; set; }
        public int ID { get; set; }
        public int Order { get; set; }
        public int Rank { get; set; }
    }
    
}
