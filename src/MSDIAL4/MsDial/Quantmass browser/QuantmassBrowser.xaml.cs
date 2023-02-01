using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for QuantmassBrowser.xaml
    /// </summary>
    public partial class QuantmassBrowser : Window
    {
        public MainWindow MainWindow { get; set; }
        public AlignmentResultBean AlignmentResult { get; set; }
        public QuantmassBrowserVM QuantmassBrowserVM { get; set; }
        public List<MspFormatCompoundInformationBean> MspDB { get; set; }
        public Dictionary<int, float> AlignmentidToOriginalQuantmass { get; set; }
        public AnalysisParamOfMsdialGcms Param { get; set; }
        public AlignmentFileBean AlignmentFile { get; set; }

        public QuantmassBrowser()
        {
            InitializeComponent();
        }

        public QuantmassBrowser(MainWindow mainWindow, AlignmentFileBean file, AlignmentResultBean alignmentResult, int id, List<MspFormatCompoundInformationBean> msps, AnalysisParamOfMsdialGcms param)
        {
            InitializeComponent();
            this.MainWindow = mainWindow;
            this.AlignmentResult = alignmentResult;
            this.AlignmentFile = file;
            this.MspDB = msps;
            this.Param = param;

            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            this.AlignmentidToOriginalQuantmass = new Dictionary<int, float>();
            var spotRows = new ObservableCollection<AlignmentSpotRow>();
            foreach (var spot in alignedSpots) {
                spotRows.Add(new AlignmentSpotRow(spot, msps));
                this.AlignmentidToOriginalQuantmass[spot.AlignmentID] = spot.QuantMass;
            }
            this.QuantmassBrowserVM = new QuantmassBrowserVM(this, spotRows);
            this.DataContext = this.QuantmassBrowserVM;
            this.QuantmassBrowserVM.SelectedData = this.QuantmassBrowserVM.Source[id];
        }

        private void Closing_Method(object sender, CancelEventArgs e)
        {
            this.QuantmassBrowserVM.closingmethod();
        }
    }

    public class QuantmassBrowserVM : ViewModelBase
    {
        #region member variables and properties
        private QuantmassBrowser quantmassBrowser;
        private QuantmassBrowserVM quantmassBrowserVM;
        private AlignmentSpotRow selectedData;
        private ObservableCollection<AlignmentSpotRow> source;

        public TableViewer.FilteredTable FilteredTable { get; set; }
        public TableViewer.FilterSettings Settings { get; set; }

        public AlignmentSpotRow SelectedData {
            get { return selectedData; }
            set {
                if (value != null && selectedData != value) {
                    selectedData = value;
                    OnPropertyChanged("SelectedData");
                }
            }
        }
        public ObservableCollection<AlignmentSpotRow> Source {
            get { return source; }
            set { source = value; }
        }
        public ICollectionView SourceView {
            get { return FilteredTable.View; }
        }

        public int NumRows {
            get { return Settings.NumRows; }
        }
        #endregion

        public QuantmassBrowserVM(QuantmassBrowser quantmassBrowser, ObservableCollection<AlignmentSpotRow> source)
        {
            this.quantmassBrowser = quantmassBrowser;
            this.quantmassBrowserVM = this;
            this.Source = source;
            this.FilteredTable = new TableViewer.FilteredTable(source);
            this.Settings = new TableViewer.FilterSettings(this.FilteredTable.View, TableViewer.FilterSettings.ViewerType.Alignment);
            this.FilteredTable.View.Filter = Settings.SpotFilter;
        }

        public void ChangeSelectedData(int id)
        {
            SelectedData = this.Source[id];
            if (this.quantmassBrowser.DataGrid_RawData.SelectedItem != null)
                this.quantmassBrowser.DataGrid_RawData.ScrollIntoView(this.quantmassBrowser.DataGrid_RawData.SelectedItem);
            this.quantmassBrowser.UpdateLayout();
        }

        private DelegateCommand update;
        public DelegateCommand Update {
            get {
                return update ?? (update = new DelegateCommand(obj => {
                    var view = (QuantmassBrowser)obj;

                    new QuantmassUpdateProcess().Execute(this.quantmassBrowser.MainWindow,
                        this.quantmassBrowser.AlignmentFile);

                    view.Close();
                }, CanRun));
            }
        }

        private bool CanRun(object obj)
        {
            var param = this.quantmassBrowser.Param;
            foreach (var spot in this.Source) {
                if (spot.AlignmentPropertyBean.QuantMass < param.MassRangeBegin || spot.AlignmentPropertyBean.QuantMass > param.MassRangeEnd) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Closes the window (on Cancel)
        /// </summary>
        private DelegateCommand cancel;
        public DelegateCommand Cancel {
            get {
                return cancel ?? (cancel = new DelegateCommand(obj => {
                    Window view = (Window)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        public void closingmethod()
        {
            var alignmentid2Quantmass = this.quantmassBrowser.AlignmentidToOriginalQuantmass;
            foreach (var row in this.source) {
                row.AlignmentPropertyBean.QuantMass = alignmentid2Quantmass[row.AlignmentPropertyBean.AlignmentID];
            }
        }
    }
}
