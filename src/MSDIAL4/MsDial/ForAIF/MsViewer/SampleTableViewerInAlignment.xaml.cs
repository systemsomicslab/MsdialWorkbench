using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
    /// SampleTableViewerInAlignment.xaml の相互作用ロジック
    /// </summary>
    public partial class SampleTableViewerInAlignment : Window
    {
        public SampleTableViewerInAlignmentVM SampleTableViewerInAlignmentVM;
        private AnalysisParamOfMsdialGcms gcParam;
        public SampleTableViewerInAlignment() {
            InitializeComponent();
        }

        public SampleTableViewerInAlignment(ObservableCollection<SampleTableRow> source, 
            AnalysisParamOfMsdialGcms gcParam = null) {
            InitializeComponent();
            if (source == null || source.Count == 0) return;

            if (source[0].AlignedPeakPropertyBeanCollection.QuantMass > 0) {
                changeColumnForGC();
                this.gcParam = gcParam;
            } else if (source[0].AlignedDriftSpotPropertyBean != null) {
                changeColumnForIonMobility();
            }

            this.SampleTableViewerInAlignmentVM = new SampleTableViewerInAlignmentVM(this, source);
            this.DataContext = this.SampleTableViewerInAlignmentVM;
        }

        public void ChangeSource(ObservableCollection<SampleTableRow> source) {
            this.SampleTableViewerInAlignmentVM = new SampleTableViewerInAlignmentVM(this, source);
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

            var window = new ChromatogramManualPeakPickViewer(selectedData, this.gcParam);
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


    public class SampleTableViewerInAlignmentVM : ViewModelBase
    {
        #region member variables and properties
        private SampleTableViewerInAlignment sampleTableViewerInAlignment;
        private SampleTableRow selectedData;
        private ObservableCollection<SampleTableRow> source;
        private int pwPId = 0;
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
            set { source = value;
                OnPropertyChanged("Source");
            }
        }
        #endregion

        public SampleTableViewerInAlignmentVM(SampleTableViewerInAlignment sampleTableViewerInAlignment, ObservableCollection<SampleTableRow> source) {
            this.sampleTableViewerInAlignment = sampleTableViewerInAlignment;
            this.Source = source;
        }

        public void ChangeSelectedData(int id) {
            SelectedData = this.Source[id];
            this.sampleTableViewerInAlignment.DataGrid_RawData.ScrollIntoView(this.sampleTableViewerInAlignment.DataGrid_RawData.SelectedItem);
        }

        public void IsPropertyChanged() {
            OnPropertyChanged("IsPropertyChanged");
        }

        public void UpdateCentralRetentionInformation() {
            var isMobility = source[0].AlignedPeakPropertyBeanCollection.DriftTime > 0 ? true : false;
            var isRi = source[0].AlignedPeakPropertyBeanCollection.RetentionIndex > 0 ? true : false;

            var aveRt = 0.0;
            var aveRi = 0.0;
            var aveDt = 0.0;
            foreach (var prop in source) {
                var peakProp = prop.AlignedPeakPropertyBeanCollection;
                if (isMobility) {
                    aveDt += peakProp.DriftTime;
                }
                else {
                    aveRt += peakProp.RetentionTime;
                    if (isRi) {
                        aveRi += peakProp.RetentionIndex;
                    }
                }
            }

            aveRt /= (double)source.Count;
            aveRi /= (double)source.Count;
            aveDt /= (double)source.Count;

            if (isMobility) {
                source[0].AlignedDriftSpotPropertyBean.CentralDriftTime = (float)aveDt;
            }
            else {
                source[0].AlignmentProperty.CentralRetentionTime = (float)aveRt;
                if (isRi)
                    source[0].AlignmentProperty.CentralRetentionIndex = (float)aveRi;
            }
        }
    }


    public class SampleTableRow : ViewModelBase {
        #region member variables and properties
        public AlignmentPropertyBean AlignmentProperty { get; set; }
        public AlignedDriftSpotPropertyBean AlignedDriftSpotPropertyBean { get; set; }
        public AlignedPeakPropertyBean AlignedPeakPropertyBeanCollection { set; get; }
        public ChromatogramXicViewModel ChromVM { get; set; }
        public string AnalysisClass { set; get; }
        public int CheckForRep { get; set; }
        public BitmapSource Image { set; get; }
        public SolidColorBrush BackgroundColInt { set; get; }
        public SolidColorBrush BackgroundColArea { set; get; }
        #endregion
        private List<SolidColorBrush> redColorList = new List<SolidColorBrush>();
        
        public SampleTableRow(AlignmentPropertyBean alignmentProperty, AlignedPeakPropertyBean alignedPeakPropertyBeanCollection, ChromatogramXicViewModel chromatogramXicVM, string className, int check) {
            AlignmentProperty = alignmentProperty;
            AlignedPeakPropertyBeanCollection = alignedPeakPropertyBeanCollection;
            ChromVM = chromatogramXicVM;
            AnalysisClass = className;
            CheckForRep = check;
            var log2Int = (int)Math.Log(AlignedPeakPropertyBeanCollection.Variable, 2);
            var log2Area = (int)Math.Log(AlignedPeakPropertyBeanCollection.Area, 2);

            if (AlignedPeakPropertyBeanCollection.IsManuallyModified) {
                //if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorYellow(log2Int);
                //if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorYellow(log2Area);
                BackgroundColInt = setColorYellow(15);
                BackgroundColArea = setColorYellow(15);
            }
            else if (AlignedPeakPropertyBeanCollection.PeakID >= 0) {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorRed(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorRed(log2Area);

            }
            else {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorBlue(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorBlue(log2Area);
            }
            Image = new PlainChromatogramXicForTableViewer(40, 200, 100, 100).DrawChromatogramXic2BitmapSource(chromatogramXicVM);
        }

        public SampleTableRow(AlignedDriftSpotPropertyBean alignmentProperty, AlignedPeakPropertyBean alignedPeakPropertyBeanCollection, ChromatogramXicViewModel chromatogramXicVM, string className, int check) {
            AlignedDriftSpotPropertyBean = alignmentProperty;
            AlignedPeakPropertyBeanCollection = alignedPeakPropertyBeanCollection;
            ChromVM = chromatogramXicVM;
            AnalysisClass = className;
            CheckForRep = check;
            var log2Int = (int)Math.Log(AlignedPeakPropertyBeanCollection.Variable, 2);
            var log2Area = (int)Math.Log(AlignedPeakPropertyBeanCollection.Area, 2);

            if (AlignedPeakPropertyBeanCollection.IsManuallyModified) {
                //if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorYellow(log2Int);
                //if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorYellow(log2Area);
                BackgroundColInt = setColorYellow(15);
                BackgroundColArea = setColorYellow(15);
            }
            else if (AlignedPeakPropertyBeanCollection.PeakID >= 0) {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorRed(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorRed(log2Area);

            }
            else {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorBlue(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorBlue(log2Area);
            }
            Image = new PlainChromatogramXicForTableViewer(40, 200, 100, 100).DrawChromatogramXic2BitmapSource(chromatogramXicVM);
        }

        public void UpdateBackgroundColor() {
            var log2Int = (int)Math.Log(AlignedPeakPropertyBeanCollection.Variable, 2);
            var log2Area = (int)Math.Log(AlignedPeakPropertyBeanCollection.Area, 2);
            if (AlignedPeakPropertyBeanCollection.IsManuallyModified) {
                //if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorYellow(log2Int);
                //if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorYellow(log2Area);
                BackgroundColInt = setColorYellow(15);
                BackgroundColArea = setColorYellow(15);
            }
            else if (AlignedPeakPropertyBeanCollection.PeakID >= 0) {
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
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, (byte)(255 - ((i - 10)  * 15)), (byte)(255 - ((i-10) * 15))));
        }

        private SolidColorBrush setColorYellow(int i) {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, (byte)(255 - ((i - 10) * 15)), 255, (byte)(255 - ((i - 10) * 15))));
        }

        private SolidColorBrush setColorBlue(int i) {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, (byte)(255 - ((i - 10) * 15)), (byte)(255 - ((i - 10) * 15)), 255));
        }
    }
}