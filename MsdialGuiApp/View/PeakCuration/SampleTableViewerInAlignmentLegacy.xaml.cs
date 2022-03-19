using CompMs.CommonMVVM;
using CompMs.Graphics.Legacy;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace CompMs.App.Msdial.View.PeakCuration {
    /// <summary>
    /// Interaction logic for SampleTableViewerInAlignmentLegacy.xaml
    /// </summary>
    public partial class SampleTableViewerInAlignmentLegacy : Window {
        public SampleTableViewerInAlignmentViewModelLegacy SampleTableViewerInAlignmentVM;
        public SampleTableViewerInAlignmentLegacy() {
            InitializeComponent();
        }

        public SampleTableViewerInAlignmentLegacy(ObservableCollection<SampleTableRow> source, ParameterBase param) {
            InitializeComponent();
            if (source == null || source.Count == 0) return;

            if (source[0].AlignedPeakProperty.ChromXsTop.Type == CompMs.Common.Components.ChromXType.RI) {
                changeColumnForGC();
            }
            else if (source[0].AlignmentProperty.IsMultiLayeredData()) {
                changeColumnForIonMobility();
            }

            this.SampleTableViewerInAlignmentVM = new SampleTableViewerInAlignmentViewModelLegacy(this, source, param);
            this.DataContext = this.SampleTableViewerInAlignmentVM;
        }

        public void ChangeSource(ObservableCollection<SampleTableRow> source, ParameterBase param) {
            this.SampleTableViewerInAlignmentVM = new SampleTableViewerInAlignmentViewModelLegacy(this, source, param);
            this.DataContext = this.SampleTableViewerInAlignmentVM;
        }

        private void DataGrid_RawData_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.DataGrid_RawData == null || SampleTableViewerInAlignmentVM.SelectedData == null) return;
            // this.SampleTableViewerInAlignmentVM.PwPID = SampleTableViewerInAlignmentVM.SelectedData.PeakAreaBean.PeakID;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            foreach (var s in this.SampleTableViewerInAlignmentVM.Source) {
                s.Image = null;
            }
            this.DataContext = null;
        }

        private void DataGrid_RawData_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            var selectedData = this.SampleTableViewerInAlignmentVM.SelectedData;
            var param = this.SampleTableViewerInAlignmentVM.Parameter;

            var window = new ChromatogramManualPeakPickViewerLegacy(selectedData, param);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            if (window.ShowDialog() == true) {

                this.DataGrid_RawData.CommitEdit();
                this.DataGrid_RawData.CommitEdit();
                this.DataGrid_RawData.Items.Refresh();
                selectedData.UpdateBackgroundColor();
                this.SampleTableViewerInAlignmentVM.UpdateCentralRetentionInformation();
                this.SampleTableViewerInAlignmentVM.IsPropertyChanged();
            }
        }

        #region changeColumnForGC
        private void changeColumnForGC() {

            var dg = DataGrid_RawData;

            // Quant mass
            //((DataGridTextColumn)dg.Columns[6]).Binding = new Binding("AlignedPeakPropertyBeanCollection.QuantMass") { StringFormat = "0.0" };
            //((DataGridTextColumn)dg.Columns[6]).Header = "Quant mass";
            // add RI
            var RIcolumn = new DataGridTextColumn() {
                Header = "RI",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding("AlignedPeakPropertyBeanCollection.RetentionIndex") { StringFormat = "0.0" },
                HeaderStyle = dg.ColumnHeaderStyle,
                CellStyle = dg.CellStyle,
                Width = new DataGridLength(0.8, DataGridLengthUnitType.Star)
            };
            dg.Columns.Insert(8, RIcolumn);

            dg.Columns.RemoveAt(6); // remove mz column

            dg.UpdateLayout();
        }
        #endregion

        #region changeColumnForIonMobility
        private void changeColumnForIonMobility() {

            var dg = DataGrid_RawData;
            this.Width = 1100;

            // add Mobility
            var mobilityColumn = new DataGridTextColumn() {
                Header = "Mobility",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding("AlignedPeakPropertyBeanCollection.DriftTime") { StringFormat = "0.000" },
                HeaderStyle = dg.ColumnHeaderStyle,
                CellStyle = dg.CellStyle,
                Width = new DataGridLength(0.8, DataGridLengthUnitType.Star)
            };
            dg.Columns.Insert(8, mobilityColumn);

            // add Mobility
            var ccsColumn = new DataGridTextColumn() {
                Header = "CCS",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding("AlignedPeakPropertyBeanCollection.Ccs") { StringFormat = "0.000" },
                HeaderStyle = dg.ColumnHeaderStyle,
                CellStyle = dg.CellStyle,
                Width = new DataGridLength(0.8, DataGridLengthUnitType.Star)
            };
            dg.Columns.Insert(9, ccsColumn);

            dg.Columns.RemoveAt(2); // remove peak id column

            var idColumn = new DataGridTextColumn() { // add peak id column again
                Header = "Master ID",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding("AlignedPeakPropertyBeanCollection.MasterPeakID") { StringFormat = "0" },
                HeaderStyle = dg.ColumnHeaderStyle,
                CellStyle = dg.CellStyle,
                Width = new DataGridLength(0.8, DataGridLengthUnitType.Star)
            };
            dg.Columns.Insert(2, idColumn);

            dg.UpdateLayout();
        }
        #endregion
    }


    public class SampleTableViewerInAlignmentViewModelLegacy : ViewModelBase {
        #region member variables and properties
        private SampleTableViewerInAlignmentLegacy sampleTableViewerInAlignment;
        private SampleTableRow selectedData;
        private ObservableCollection<SampleTableRow> source;
        private int pwPId = 0;
        public ParameterBase Parameter { get; set; }
        public int PwPID {
            get { return pwPId; }
            set { if (pwPId != value) { pwPId = value; OnPropertyChanged("SelectedPlotId"); } }
        }
        public SampleTableRow SelectedData {
            get { return selectedData; }
            set {
                selectedData = value;
                OnPropertyChanged("SelectedData");
                //Debug.WriteLine("Object {0}, Sample ID {1}", SelectedData.GetType(), SelectedData.AlignedPeakPropertyBeanCollection.FileID);
            }
        }
        public ObservableCollection<SampleTableRow> Source {
            get { return source; }
            set {
                source = value;
                OnPropertyChanged("Source");
            }
        }
        #endregion

        public SampleTableViewerInAlignmentViewModelLegacy(
            SampleTableViewerInAlignmentLegacy sampleTableViewerInAlignment, 
            ObservableCollection<SampleTableRow> source, 
            ParameterBase param) {
            this.sampleTableViewerInAlignment = sampleTableViewerInAlignment;
            this.Source = source;
            this.Parameter = param;
        }

        public void ChangeSelectedData(int id) {
            SelectedData = this.Source[id];
            this.sampleTableViewerInAlignment.DataGrid_RawData.ScrollIntoView(this.sampleTableViewerInAlignment.DataGrid_RawData.SelectedItem);
        }

        public void IsPropertyChanged() {
            OnPropertyChanged("IsPropertyChanged");
        }

        public void UpdateCentralRetentionInformation() {
            var isMobility = source[0].AlignedPeakProperty.ChromXsTop.Type == CompMs.Common.Components.ChromXType.Drift ? true : false;
            var isRi = source[0].AlignedPeakProperty.ChromXsTop.Type == CompMs.Common.Components.ChromXType.RI ? true : false;

            var aveRt = 0.0;
            var aveRi = 0.0;
            var aveDt = 0.0;
            foreach (var prop in source) {
                var peakProp = prop.AlignedPeakProperty;
                if (isMobility) {
                    aveDt += peakProp.ChromXsTop.Drift.Value;
                }
                else {
                    aveRt += peakProp.ChromXsTop.RT.Value;
                    if (isRi) {
                        aveRi += peakProp.ChromXsTop.RI.Value;
                    }
                }
            }

            aveRt /= (double)source.Count;
            aveRi /= (double)source.Count;
            aveDt /= (double)source.Count;

            if (isMobility) {
                source[0].AlignmentProperty.TimesCenter.Drift.Value = (float)aveDt;
            }
            else {
                source[0].AlignmentProperty.TimesCenter.RT.Value = (float)aveRt;
                if (isRi)
                    source[0].AlignmentProperty.TimesCenter.RI.Value = (float)aveRi;
            }
        }
    }


    public class SampleTableRow : ViewModelBase {
        #region member variables and properties
        public AlignmentSpotProperty AlignmentProperty { get; set; }
        public AlignmentChromPeakFeature AlignedPeakProperty { set; get; }
        public ChromatogramXicViewModelLegacy ChromVM { get; set; }
        public string AnalysisClass { set; get; }
        public int CheckForRep { get; set; }
        public BitmapSource Image { set; get; }
        public SolidColorBrush BackgroundColInt { set; get; }
        public SolidColorBrush BackgroundColArea { set; get; }
        #endregion
        private List<SolidColorBrush> redColorList = new List<SolidColorBrush>();

        public SampleTableRow(AlignmentSpotProperty alignmentProperty, AlignmentChromPeakFeature alignedPeakPropertyBeanCollection,
            ChromatogramXicViewModelLegacy chromatogramXicVM, string className, int check) {
            AlignmentProperty = alignmentProperty;
            AlignedPeakProperty = alignedPeakPropertyBeanCollection;
            ChromVM = chromatogramXicVM;
            AnalysisClass = className;
            CheckForRep = check;
            var log2Int = (int)Math.Log(AlignedPeakProperty.PeakHeightTop, 2);
            var log2Area = (int)Math.Log(AlignedPeakProperty.PeakAreaAboveZero, 2);

            if (AlignedPeakProperty.IsManuallyModifiedForQuant) {
                //if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorYellow(log2Int);
                //if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorYellow(log2Area);
                BackgroundColInt = setColorYellow(15);
                BackgroundColArea = setColorYellow(15);
            }
            else if (AlignedPeakProperty.PeakID >= 0) {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorRed(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorRed(log2Area);

            }
            else {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorBlue(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorBlue(log2Area);
            }
            Image = new PlainChromatogramXicForTableViewerLegacy(40, 200, 100, 100).DrawChromatogramXic2BitmapSource(chromatogramXicVM);
        }
        
        public void UpdateBackgroundColor() {
            var log2Int = (int)Math.Log(AlignedPeakProperty.PeakHeightTop, 2);
            var log2Area = (int)Math.Log(AlignedPeakProperty.PeakAreaAboveZero, 2);
            if (AlignedPeakProperty.IsManuallyModifiedForQuant) {
                //if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorYellow(log2Int);
                //if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorYellow(log2Area);
                BackgroundColInt = setColorYellow(15);
                BackgroundColArea = setColorYellow(15);
            }
            else if (AlignedPeakProperty.PeakID >= 0) {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorRed(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorRed(log2Area);

            }
            else {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorBlue(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorBlue(log2Area);
            }
        }

        private SolidColorBrush setColorRed() {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, 0, 0));
        }
        private SolidColorBrush setColorRed(int i) {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, (byte)(255 - ((i - 10) * 15)), (byte)(255 - ((i - 10) * 15))));
        }

        private SolidColorBrush setColorYellow(int i) {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, (byte)(255 - ((i - 10) * 15)), 255, (byte)(255 - ((i - 10) * 15))));
        }

        private SolidColorBrush setColorBlue(int i) {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, (byte)(255 - ((i - 10) * 15)), (byte)(255 - ((i - 10) * 15)), 255));
        }
    }
}
