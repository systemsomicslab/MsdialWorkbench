using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Legacy;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
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

        public SampleTableViewerInAlignmentLegacy(SampleTableViewerInAlignmentViewModelLegacy vm) {
            InitializeComponent();

            if (vm.Source == null || vm.Source.Count == 0) return;

            if (vm.Source[0].AlignedPeakProperty.ChromXsTop.Type == CompMs.Common.Components.ChromXType.RI) {
                changeColumnForGC();
            }
            else if (vm.Source[0].AlignmentProperty.IsMultiLayeredData) {
                changeColumnForIonMobility();
            }

            this.SampleTableViewerInAlignmentVM = vm;
            this.DataContext = this.SampleTableViewerInAlignmentVM;
        }

        public void ChangeSource(SampleTableViewerInAlignmentViewModelLegacy vm) {
            this.SampleTableViewerInAlignmentVM = vm;
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

    public class SampleTableViewerInAlignmentModelLegacy : BindableBase {
        public SampleTableRow SelectedData { get; set; }
        public ObservableCollection<SampleTableRow> Source { get; set; }
        public ParameterBase Parameter { get; set; }
        public int PwPID { get; set; } = 0;

        public SampleTableViewerInAlignmentModelLegacy(
            AlignmentSpotPropertyModel alignmentProp,
            List<Chromatogram> chromatoramSource,
            List<AnalysisFileBean> files,
            ParameterBase param) {
            this.Source = GetSourceOfAlignedSampleTableViewer(alignmentProp, chromatoramSource, files, param);
            this.Parameter = param;
        }

        public static ObservableCollection<SampleTableRow> GetSourceOfAlignedSampleTableViewer(
            AlignmentSpotPropertyModel alignmentProp,
            List<Chromatogram> chromatoramSource,
            List<AnalysisFileBean> files,
            ParameterBase param) {
            var source = new ObservableCollection<SampleTableRow>();
            var vms = GetAlignedEicChromatogramList(alignmentProp, chromatoramSource, files, param);
            for (var i = 0; i < files.Count; i++) {
                var check = alignmentProp.RepresentativeFileID == i ? i : 0;
                source.Add(new SampleTableRow(
                    alignmentProp, 
                    alignmentProp.AlignedPeakPropertiesModel[i],
                    vms[i], 
                    files[i].AnalysisFileClass, 
                    check));
            }
            return source;
        }

        public static List<ChromatogramXicViewModelLegacy> GetAlignedEicChromatogramList(
            AlignmentSpotPropertyModel alignmentProp,
            List<Chromatogram> chromatograms, 
            List<AnalysisFileBean> files,
            ParameterBase param) {

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBeanLegacy>();
            var targetMz = alignmentProp.MassCenter;
            var numAnalysisfiles = files.Count;
            var vms = new ChromatogramXicViewModelLegacy[numAnalysisfiles];
            var classnameToBytes = param.ClassnameToColorBytes;
            var classnameToBrushes = ChartBrushes.ConvertToSolidBrushDictionary(classnameToBytes);

            System.Threading.Tasks.Parallel.For(0, numAnalysisfiles, (i) => {
                //for (int i = 0; i < numAnalysisfiles; i++) { // draw the included samples
                var peaks = chromatograms[i].Peaks.Select(n => n.Chrom).ToList();
                var speaks = DataAccess.GetSmoothedPeaklist(peaks, param.SmoothingMethod, param.SmoothingLevel);
                var chromatogramBean = new ChromatogramBeanLegacy(
                    true, 
                    classnameToBrushes[files[i].AnalysisFileClass], 
                    1.0, 
                    files[i].AnalysisFileName,
                    (float)targetMz, 
                    param.CentroidMs1Tolerance,
                    speaks);
                var vm = new ChromatogramXicViewModelLegacy(
                    chromatogramBean, 
                    ChromatogramEditMode.Display,
                    ChromatogramDisplayLabel.None,
                    ChromatogramQuantitativeMode.Height, 
                    ChromatogramIntensityMode.Absolute,
                    0, 
                    "", 
                    (float)targetMz,
                    param.CentroidMs1Tolerance,
                    (float)alignmentProp.AlignedPeakPropertiesModel[i].ChromXsTop.Value,
                    (float)alignmentProp.AlignedPeakPropertiesModel[i].ChromXsLeft.Value,
                    (float)alignmentProp.AlignedPeakPropertiesModel[i].ChromXsRight.Value);
                vms[i] = vm;
            });
            return vms.ToList();
        }
    }

    public class SampleTableViewerInAlignmentViewModelLegacy : ViewModelBase {
        private SampleTableViewerInAlignmentModelLegacy model;
        public SampleTableViewerInAlignmentModelLegacy Model { 
            get => model;
            set {
                model = value;
                OnPropertyChanged("Model");
                OnPropertyChanged("Source");
            }
        }
        #region member variables and properties
        public ParameterBase Parameter => Model.Parameter;
        public int PwPID {
            get => Model.PwPID;
            set { 
                if (Model.PwPID != value) {
                    Model.PwPID = value; 
                    OnPropertyChanged("SelectedPlotId");
                }
            }
        }
        public SampleTableRow SelectedData {
            get => Model.SelectedData;
            set {
                Model.SelectedData = value;
                OnPropertyChanged("SelectedData");
            }
        }
        public ObservableCollection<SampleTableRow> Source {
            get => Model.Source;
            set {
                Model.Source = value;
                OnPropertyChanged("Source");
            }
        }
        #endregion

        public SampleTableViewerInAlignmentViewModelLegacy(
            SampleTableViewerInAlignmentModelLegacy model) {
            this.Model = model;
        }

        private void UpdateModel(SampleTableViewerInAlignmentModelLegacy model) {
            this.Model = model;
        }

        public SampleTableViewerInAlignmentViewModelLegacy(IObservable<SampleTableViewerInAlignmentModelLegacy> model) {
            model.ObserveOnDispatcher().Subscribe(UpdateModel).AddTo(Disposables);
        }

        public void IsPropertyChanged() {
            OnPropertyChanged("IsPropertyChanged");
        }

        public void UpdateCentralRetentionInformation() {
            var isMobility = Source[0].AlignedPeakProperty.ChromXsTop.Type == CompMs.Common.Components.ChromXType.Drift ? true : false;
            var isRi = Source[0].AlignedPeakProperty.ChromXsTop.Type == CompMs.Common.Components.ChromXType.RI ? true : false;

            var aveRt = 0.0;
            var aveRi = 0.0;
            var aveDt = 0.0;
            foreach (var prop in Source) {
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

            aveRt /= (double)Source.Count;
            aveRi /= (double)Source.Count;
            aveDt /= (double)Source.Count;

            if (isMobility) {
                Source[0].AlignmentProperty.TimesCenter = (float)aveDt;
            }
            else {
                Source[0].AlignmentProperty.TimesCenter = (float)aveRt;
                if (isRi)
                    Source[0].AlignmentProperty.TimesCenter = (float)aveRi;
            }
        }
    }


    public class SampleTableRow : ViewModelBase {
        #region member variables and properties
        public AlignmentSpotPropertyModel AlignmentProperty { get; set; }
        public AlignmentChromPeakFeatureModel AlignedPeakProperty { set; get; }
        public ChromatogramXicViewModelLegacy ChromVM { get; set; }
        public string AnalysisClass { set; get; }
        public int CheckForRep { get; set; }
        public BitmapSource Image { set; get; }
        public SolidColorBrush BackgroundColInt { set; get; }
        public SolidColorBrush BackgroundColArea { set; get; }
        #endregion
        //private List<SolidColorBrush> redColorList = new List<SolidColorBrush>();

        public SampleTableRow(
            AlignmentSpotPropertyModel alignmentProperty, 
            AlignmentChromPeakFeatureModel alignedPeakPropertyBeanCollection,
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
            BackgroundColInt?.Freeze();
            BackgroundColArea?.Freeze();

            Image = new PlainChromatogramXicForTableViewerLegacy(40, 200, 100, 100).DrawChromatogramXic2BitmapSource(chromatogramXicVM);
            Image?.Freeze();
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
            BackgroundColInt?.Freeze();
            BackgroundColArea?.Freeze();
        }

        //private SolidColorBrush setColorRed() {
        //    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, 0, 0));
        //}
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
