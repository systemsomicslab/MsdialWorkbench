using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.Legacy;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.App.Msdial.View.PeakCuration
{
    /// <summary>
    /// Interaction logic for SampleTableViewerInAlignmentLegacy.xaml
    /// </summary>
    public partial class SampleTableViewerInAlignmentLegacy : Window {
        private SampleTableViewerInAlignmentViewModelLegacy? _sampleTableViewerInAlignmentVM;
        private IDisposable? _gcViewUnsubscriber, _driftViewUnsubscriber;

        public SampleTableViewerInAlignmentLegacy() {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (e.NewValue is SampleTableViewerInAlignmentViewModelLegacy viewmodel) {
                _sampleTableViewerInAlignmentVM = viewmodel;
                _gcViewUnsubscriber?.Dispose();
                _driftViewUnsubscriber?.Dispose();
                _gcViewUnsubscriber = viewmodel.ViewType.Where(type => type == SampleChromatogramType.RI).Subscribe(_ => ChangeColumnForGC());
                _driftViewUnsubscriber = viewmodel.ViewType.Where(type => type == SampleChromatogramType.Drift).Subscribe(_ => ChangeColumnForIonMobility());
            }
        }

        private void DataGrid_RawData_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (DataGrid_RawData is null || _sampleTableViewerInAlignmentVM?.SelectedData is null) return;
            // this.SampleTableViewerInAlignmentVM.PwPID = SampleTableViewerInAlignmentVM.SelectedData.PeakAreaBean.PeakID;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            //foreach (var s in this.SampleTableViewerInAlignmentVM.Source) {
            //    s.Image = null;
            //}
            DataContext = null;
            _gcViewUnsubscriber = null;
            _driftViewUnsubscriber = null;
        }

        private void DataGrid_RawData_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            var selectedData = _sampleTableViewerInAlignmentVM?.SelectedData;
            var parameter = _sampleTableViewerInAlignmentVM?.Parameter;
            if (selectedData is null || parameter is null) {
                return;
            }

            var window = new ChromatogramManualPeakPickViewerLegacy(selectedData, parameter)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            if (window.ShowDialog() == true) {
                DataGrid_RawData.CommitEdit();
                DataGrid_RawData.CommitEdit();
                DataGrid_RawData.Items.Refresh();
                selectedData.UpdateBackgroundColor();
                _sampleTableViewerInAlignmentVM?.UpdateCentralRetentionInformation();
                _sampleTableViewerInAlignmentVM?.RaisePropertyChanged();
                DataGrid_RawData.ScrollIntoView(selectedData);
            }
        }

        #region changeColumnForGC
        private void ChangeColumnForGC() {

            var dg = DataGrid_RawData;

            // Quant mass
            //((DataGridTextColumn)dg.Columns[6]).Binding = new Binding("AlignedPeakPropertyBeanCollection.QuantMass") { StringFormat = "0.0" };
            //((DataGridTextColumn)dg.Columns[6]).Header = "Quant mass";
            // add RI
            var RIcolumn = new DataGridTextColumn() {
                Header = "RI",
                IsReadOnly = true,
                CanUserSort = true,
                Binding = new Binding($"{nameof(SampleTableRow.AlignedPeakProperty)}.{nameof(AlignmentChromPeakFeatureModel.ChromXsTop)}.{nameof(CompMs.Common.Components.ChromXs.RI)}.{nameof(CompMs.Common.Components.RetentionIndex.Value)}") { StringFormat = "0.0" },
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
        private void ChangeColumnForIonMobility() {

            var dg = DataGrid_RawData;
            Width = 1100;

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

    enum SampleChromatogramType {
        None,
        RI,
        Drift,
    }

    internal sealed class SampleTableViewerInAlignmentModelLegacy : DisposableModelBase {
        public SampleTableViewerInAlignmentModelLegacy(IObservable<AlignedChromatograms?> spotChromatograms, List<AnalysisFileBean> files, ParameterBase parameter) {
            Source = GetSourceOfAlignedSampleTableViewer(spotChromatograms, files, parameter).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Parameter = parameter;
            switch (parameter) {
                case MsdialGcMsApi.Parameter.MsdialGcmsParameter gcparameter:
                    if (gcparameter.AlignmentIndexType == CompMs.Common.Enum.AlignmentIndexType.RI) {
                        ViewType = SampleChromatogramType.RI;
                    }
                    break;
                case MsdialLcImMsApi.Parameter.MsdialLcImMsParameter _:
                    ViewType = SampleChromatogramType.Drift;
                    break;
                default:
                    ViewType = SampleChromatogramType.None;
                    break;
            }
        }

        public ReadOnlyReactivePropertySlim<SampleTableRows?> Source { get; }
        public ParameterBase Parameter { get; }
        public SampleChromatogramType ViewType { get; } 

        public void UpdateCentralRetentionInformation() {
            var isRi = ViewType == SampleChromatogramType.RI;
            var isMobility = ViewType == SampleChromatogramType.Drift;
            if (Source.Value is SampleTableRows rows) {
                rows.UpdateCentralRetentionInformation(isRi, isMobility);
            }
        }

        private static IObservable<SampleTableRows> GetSourceOfAlignedSampleTableViewer(IObservable<AlignedChromatograms?> spotChromatograms, List<AnalysisFileBean> files, ParameterBase parameter) {
            var classnameToBytes = parameter.ClassnameToColorBytes;
            var classnameToBrushes = ChartBrushes.ConvertToSolidBrushDictionary(classnameToBytes);
            return spotChromatograms.ObserveOn(TaskPoolScheduler.Default).DefaultIfNull(s =>
            s.Spot.AlignedPeakPropertiesModelProperty.CombineLatest(s.Chromatograms, (peaks, chromatograms) => {
                if (peaks is null) {
                    return new SampleTableRows(new ObservableCollection<SampleTableRow>());
                }
                var brushes = Enumerable.Range(0, files.Count).Select(ChartBrushes.GetChartBrush);
                var rows = files.Zip(peaks, brushes).Where(triple => triple.Item1.AnalysisFileIncluded)
                    .Zip(chromatograms, (triple, chromatogram) =>
                        GetSampleTableRow(s.Spot, triple.Item2, chromatogram, triple.Item1, classnameToBrushes.TryGetValue(triple.Item1.AnalysisFileClass, out var b) ? b : triple.Item3, parameter.CentroidMs1Tolerance));
                return new SampleTableRows(new ObservableCollection<SampleTableRow>(rows));
            }), Observable.Return(new SampleTableRows(new ObservableCollection<SampleTableRow>()))).Switch();
        }

        private static SampleTableRow GetSampleTableRow(
            AlignmentSpotPropertyModel alignmentProp,
            AlignmentChromPeakFeatureModel peak,
            PeakChromatogram chromatogram,
            AnalysisFileBean file,
            SolidColorBrush brush,
            float ms1Tolerance) {

            var chromatogramBean = new ChromatogramBeanLegacy(
                true,
                brush,
                1.0,
                file.AnalysisFileName,
                (float)alignmentProp.MassCenter,
                ms1Tolerance,
                chromatogram.Convert().AsPeakArray());
            var vm = new ChromatogramXicViewModelLegacy(
                chromatogramBean,
                ChromatogramEditMode.Display,
                ChromatogramDisplayLabel.None,
                ChromatogramQuantitativeMode.Height,
                ChromatogramIntensityMode.Absolute,
                0,
                "",
                (float)alignmentProp.MassCenter,
                ms1Tolerance,
                (float)peak.ChromXsTop.Value,
                (float)peak.ChromXsLeft.Value,
                (float)peak.ChromXsRight.Value);
            return new SampleTableRow(
                alignmentProp,
                peak,
                vm,
                file.AnalysisFileClass,
                alignmentProp.RepresentativeFileID == peak.FileID ? peak.FileID : 0);
        }
    }

    internal sealed class SampleTableViewerInAlignmentViewModelLegacy : ViewModelBase {
        private readonly SampleTableViewerInAlignmentModelLegacy _model;

        public SampleTableViewerInAlignmentViewModelLegacy(SampleTableViewerInAlignmentModelLegacy model) {
            _model = model;
            Source = model.Source.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        #region member variables and properties
        public ParameterBase Parameter => _model.Parameter;

        public SampleTableRow? SelectedData {
            get => _selectedData;
            set => SetProperty(ref _selectedData, value);
        }
        private SampleTableRow? _selectedData;

        public ReadOnlyReactivePropertySlim<SampleTableRows?> Source { get; }

        public IObservable<SampleChromatogramType> ViewType => Observable.Return(_model.ViewType);
        #endregion

        public void RaisePropertyChanged() {
            OnPropertyChanged(string.Empty);
        }

        public void UpdateCentralRetentionInformation() {
            _model.UpdateCentralRetentionInformation();
        }
    }

    public sealed class SampleTableRows : ViewModelBase {
        public SampleTableRows(ObservableCollection<SampleTableRow> rows) {
            Rows = rows;
        }

        public ObservableCollection<SampleTableRow> Rows { get; }

        public void UpdateCentralRetentionInformation(bool isRi, bool isMobility) {
            var aveRt = 0d;
            var aveRi = 0d;
            var aveDt = 0d;
            foreach (var row in Rows) {
                var peakProp = row.AlignedPeakProperty;
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

            if (Rows.Count == 0) {
                return;
            }

            aveRt /= Rows.Count;
            aveRi /= Rows.Count;
            aveDt /= Rows.Count;

            if (isMobility) {
                Rows[0].AlignmentProperty.TimesCenter = (float)aveDt;
            }
            else {
                Rows[0].AlignmentProperty.TimesCenter = (float)aveRt;
                if (isRi)
                    Rows[0].AlignmentProperty.TimesCenter = (float)aveRi;
            }
        }
    }

    public sealed class SampleTableRow : ViewModelBase {
        #region member variables and properties
        public AlignmentSpotPropertyModel AlignmentProperty { get; set; }
        public AlignmentChromPeakFeatureModel AlignedPeakProperty { set; get; }
        public ChromatogramXicViewModelLegacy ChromVM { get; set; }
        public string AnalysisClass { set; get; }
        public int CheckForRep { get; set; }
        public Drawing? Drawing {
            get => _drawing;
            set => SetProperty(ref _drawing, value);
        }
        private Drawing? _drawing;
        public SolidColorBrush? BackgroundColInt { set; get; }
        public SolidColorBrush? BackgroundColArea { set; get; }
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
                BackgroundColInt = SetColorYellow(15);
                BackgroundColArea = SetColorYellow(15);
            }
            else if (AlignedPeakProperty.PeakID >= 0) {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = SetColorRed(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = SetColorRed(log2Area);

            }
            else {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = SetColorBlue(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = SetColorBlue(log2Area);
            }
            BackgroundColInt?.Freeze();
            BackgroundColArea?.Freeze();

            Task.Run(UpdateDrawing);
        }

        public void UpdateDrawing() {
            Drawing = new PlainChromatogramXicForTableViewerLegacy(40, 200, 100, 100).GetChromatogramDrawing(ChromVM);
        }
        
        public void UpdateBackgroundColor() {
            var log2Int = (int)Math.Log(AlignedPeakProperty.PeakHeightTop, 2);
            var log2Area = (int)Math.Log(AlignedPeakProperty.PeakAreaAboveZero, 2);
            if (AlignedPeakProperty.IsManuallyModifiedForQuant) {
                //if (log2Int >= 10 && log2Int < 27) BackgroundColInt = setColorYellow(log2Int);
                //if (log2Area >= 10 && log2Area < 27) BackgroundColArea = setColorYellow(log2Area);
                BackgroundColInt = SetColorYellow(15);
                BackgroundColArea = SetColorYellow(15);
            }
            else if (AlignedPeakProperty.PeakID >= 0) {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = SetColorRed(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = SetColorRed(log2Area);

            }
            else {
                if (log2Int >= 10 && log2Int < 27) BackgroundColInt = SetColorBlue(log2Int);
                if (log2Area >= 10 && log2Area < 27) BackgroundColArea = SetColorBlue(log2Area);
            }
            BackgroundColInt?.Freeze();
            BackgroundColArea?.Freeze();
        }

        //private SolidColorBrush setColorRed() {
        //    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, 0, 0));

        //}
        private SolidColorBrush SetColorRed(int i) {
            return new SolidColorBrush(Color.FromArgb(180, 255, (byte)(255 - ((i - 10) * 15)), (byte)(255 - ((i - 10) * 15))));
        }

        private SolidColorBrush SetColorYellow(int i) {
            return new SolidColorBrush(Color.FromArgb(180, (byte)(255 - ((i - 10) * 15)), 255, (byte)(255 - ((i - 10) * 15))));
        }

        private SolidColorBrush SetColorBlue(int i) {
            return new SolidColorBrush(Color.FromArgb(180, (byte)(255 - ((i - 10) * 15)), (byte)(255 - ((i - 10) * 15)), 255));
        }
    }
}
