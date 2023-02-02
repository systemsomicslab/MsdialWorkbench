using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Rfx.Riken.OsakaUniv.ForAIF
{
    /// <summary>
    /// MsFinderExporterSettingWin.xaml の相互作用ロジック
    /// </summary>
    public partial class MsFinderExporterSettingWin : Window
    {
        private bool isAlignment = false;
        private MsFinderExporterSettingVM msFinderExporterSettingVM;
        public MsFinderExporterSettingWin() {
            InitializeComponent();
        }

        public MsFinderExporterSettingWin(ObservableCollection<PeakSpotRow> source) {
            InitializeComponent();
            this.msFinderExporterSettingVM = new MsFinderExporterSettingVM(this, source);
            this.DataContext = this.msFinderExporterSettingVM;
            this.msFinderExporterSettingVM.PropertyChanged -= propertyChanged;
            this.msFinderExporterSettingVM.PropertyChanged += propertyChanged;
        }

        public MsFinderExporterSettingWin(ObservableCollection<AlignmentSpotRow> source) {
            InitializeComponent();
            isAlignment = true;
            this.msFinderExporterSettingVM = new MsFinderExporterSettingVM(this, source);
            this.DataContext = this.msFinderExporterSettingVM;
            this.msFinderExporterSettingVM.PropertyChanged -= propertyChanged;
            this.msFinderExporterSettingVM.PropertyChanged += propertyChanged;
            this.TextBox_PeakArea.Text = "Average Peak Intensity";
            this.TextBoxForPeak1.Text = "ANOVA P-Valuse";

            Binding binding = new Binding();
            binding.Path = new PropertyPath("AnovaStart");
            binding.Source = this.DataContext;
            binding.StringFormat = "0.0E00";
            BindingOperations.SetBinding(this.TextBoxForPeak2, TextBox.TextProperty, binding);

            Binding binding2 = new Binding();
            binding2.Path = new PropertyPath("AnovaEnd");
            binding2.Source = this.msFinderExporterSettingVM;
            binding2.StringFormat = "0.0E00";
            BindingOperations.SetBinding(this.TextBoxForPeak3, TextBox.TextProperty, binding2);
            this.UpdateLayout();
        }

        private void propertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (isAlignment)
                this.msFinderExporterSettingVM.ChangeSetting2();
            else
                this.msFinderExporterSettingVM.ChangeSetting();

        }

        private void Button_Finish_Click(object sender, RoutedEventArgs e) {
            if (isAlignment)
                this.msFinderExporterSettingVM.ChangeSetting2();
            else
                this.msFinderExporterSettingVM.ChangeSetting();
            this.Close();
        }
        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }

    public class MsFinderExporterSettingVM : ViewModelBase
    {
        private int _ids;
        private int _ide;
        private float _rts;
        private float _rte;
        private float _mzs;
        private float _mze;
        private float _areas;
        private float _areae;
        private float _ints;
        private float _inte;
        private int _numExport;
        private float _anovas;
        private float _anovae;
        private bool _identifiedFilter = false;
        private bool _annotatedFilter = false;
        private bool _ms2filter = false;
        private bool _blankfilter = false;
        private bool _uniquefilter = false;


        public int PeakIdStart { get { return _ids; } set { _ids = value; OnPropertyChanged("PeakIdStart"); } }
        public int PeakIdEnd { get { return _ide; } set { _ide = value; OnPropertyChanged("PeakIdEnd"); } }
        public float RtStart { get { return _rts; } set { _rts = value; OnPropertyChanged("RtStart"); } }
        public float RtEnd { get { return _rte; } set { _rte = value; OnPropertyChanged("RtEnd"); } }
        public float MzStart { get { return _mzs; } set { _mzs = value; OnPropertyChanged("MzStart"); } }
        public float MzEnd { get { return _mze; } set { _mze = value; OnPropertyChanged("MzEnd"); } }
        public float AreaStart { get { return _areas; } set { _areas = value; OnPropertyChanged("AreaStart"); } }
        public float AreaEnd { get { return _areae; } set { _areae = value; OnPropertyChanged("AreaEnd"); } }
        public float IntStart { get { return _ints; } set { _ints = value; OnPropertyChanged("IntStart"); } }
        public float IntEnd { get { return _inte; } set { _inte = value; OnPropertyChanged("IntEnd"); } }
        public float AnovaStart { get { return _anovas; } set { _anovas = value; OnPropertyChanged("AnovaStart"); } }
        public float AnovaEnd { get { return _anovae; } set { _anovae = value; OnPropertyChanged("AnovaEnd"); } }
        public bool AnnotatedFilter { get { return _annotatedFilter; } set { _annotatedFilter = value; OnPropertyChanged("AnnotatedFilter"); } }
        public bool IdentifiedFilter { get { return _identifiedFilter; } set { _identifiedFilter = value; OnPropertyChanged(nameof(IdentifiedFilter)); } }
        public bool BlankFilter { get => _blankfilter; set { _blankfilter = value; OnPropertyChanged(nameof(BlankFilter)); } }
        public bool UniqueFilter { get => _uniquefilter; set { _uniquefilter = value; OnPropertyChanged(nameof(UniqueFilter)); } }
        public bool Ms2Filter { get => _ms2filter; set { _ms2filter = value; OnPropertyChanged(nameof(Ms2Filter)); } }
        public int NumExportPeaks { get { return _numExport; } set { if (_numExport == value) return; _numExport = value; OnPropertyChanged("NumExportPeaks"); } }

        private ObservableCollection<PeakSpotRow> source;
        private ObservableCollection<AlignmentSpotRow> source2;


        private MsFinderExporterSettingWin window;
        public MsFinderExporterSettingVM() { }
        public MsFinderExporterSettingVM(MsFinderExporterSettingWin w, ObservableCollection<PeakSpotRow> source) {
            this.source = source;
            this.window = w;
            setValues(source);
        }

        public MsFinderExporterSettingVM(MsFinderExporterSettingWin w, ObservableCollection<AlignmentSpotRow> source) {
            this.source2 = source;
            this.window = w;
            setValues(source2);
        }


        private void setValues(ObservableCollection<PeakSpotRow> source) {
            PeakIdStart = source.Min(x => x.PeakID);
            PeakIdEnd = source.Max(x => x.PeakID);
            RtStart = source.Min(x => x.PeakAreaBean.RtAtPeakTop);
            RtEnd = source.Max(x => x.PeakAreaBean.RtAtPeakTop);
            MzStart = source.Min(x => x.PeakAreaBean.AccurateMass);
            MzEnd = source.Max(x => x.PeakAreaBean.AccurateMass);
            AreaStart = source.Min(x => x.PeakAreaBean.AreaAboveZero);
            AreaEnd = source.Max(x => x.PeakAreaBean.AreaAboveZero);
            IntStart = source.Min(x => x.PeakAreaBean.IntensityAtPeakTop);
            IntEnd = source.Max(x => x.PeakAreaBean.IntensityAtPeakTop);
            NumExportPeaks = source.Count;
        }


        private void setValues(ObservableCollection<AlignmentSpotRow> source) {
            PeakIdStart = source.Min(x => x.AlignmentPropertyBean.AlignmentID);
            PeakIdEnd = source.Max(x => x.AlignmentPropertyBean.AlignmentID);
            RtStart = source.Min(x => x.AlignmentPropertyBean.CentralRetentionTime);
            RtEnd = source.Max(x => x.AlignmentPropertyBean.CentralRetentionTime);
            MzStart = source.Min(x => x.AlignmentPropertyBean.CentralAccurateMass);
            MzEnd = source.Max(x => x.AlignmentPropertyBean.CentralAccurateMass);
            AreaStart = source.Min(x => x.AlignmentPropertyBean.AverageValiable);
            AreaEnd = source.Max(x => x.AlignmentPropertyBean.AverageValiable);
            IntStart = 1;
            IntEnd = 1;
            AnovaStart = source.Min(x => x.AlignmentPropertyBean.AnovaPval);
            AnovaEnd = source.Max(x => x.AlignmentPropertyBean.AnovaPval);
            NumExportPeaks = source.Count;
        }

        public void ChangeSetting() {
            foreach (var row in this.source) {
                if (row.PeakID < PeakIdStart || row.PeakID > PeakIdEnd) { row.Checked = false; continue; }
                else if (row.PeakAreaBean.RtAtPeakTop < this.RtStart || row.PeakAreaBean.RtAtPeakTop > this.RtEnd) { row.Checked = false; continue; }
                else if (row.PeakAreaBean.AccurateMass < this.MzStart || row.PeakAreaBean.AccurateMass > this.MzEnd) { row.Checked = false; continue; }
                else if (row.PeakAreaBean.AreaAboveZero < this.AreaStart || row.PeakAreaBean.AreaAboveZero > this.AreaEnd) { row.Checked = false; continue; }
                else if (row.PeakAreaBean.IntensityAtPeakTop < this.IntStart || row.PeakAreaBean.IntensityAtPeakTop > this.IntEnd) { row.Checked = false; continue; }
                else if (Ms2Filter && row.PeakAreaBean.Ms2LevelDatapointNumber < 0) { row.Checked = false; continue; }
                else if (UniqueFilter && !row.PeakAreaBean.IsFragmentQueryExist) { row.Checked = false; continue; }
                else { row.Checked = true; }

                if (IdentifiedFilter && AnnotatedFilter)
                {
                    if (row.PeakAreaBean.MetaboliteName != string.Empty) { row.Checked = true; }
                    else row.Checked = false;
                }
                else if (AnnotatedFilter && (row.PeakAreaBean.MetaboliteName == string.Empty || (!row.PeakAreaBean.MetaboliteName.Contains("w/o") && !row.PeakAreaBean.MetaboliteName.Contains("Unknown")))) { row.Checked = false; continue; }
                else if (IdentifiedFilter && (row.PeakAreaBean.MetaboliteName == string.Empty || row.PeakAreaBean.MetaboliteName.Contains("w/o") || row.PeakAreaBean.MetaboliteName.Contains("Unknown"))) { row.Checked = false; continue; }

            }
            NumExportPeaks = source.Count(x => x.Checked == true);
        }


        public void ChangeSetting2() {
            foreach (var row in this.source2) {
                if (row.AlignmentPropertyBean.AlignmentID < PeakIdStart || row.AlignmentPropertyBean.AlignmentID > PeakIdEnd) { row.Checked = false; continue; }
                else if (row.AlignmentPropertyBean.CentralRetentionTime < this.RtStart || row.AlignmentPropertyBean.CentralRetentionTime > this.RtEnd) { row.Checked = false; continue; }
                else if (row.AlignmentPropertyBean.CentralAccurateMass < this.MzStart || row.AlignmentPropertyBean.CentralAccurateMass > this.MzEnd) { row.Checked = false; continue; }
                else if (row.AlignmentPropertyBean.AverageValiable < this.AreaStart || row.AlignmentPropertyBean.AverageValiable > this.AreaEnd) { row.Checked = false; continue; }
                else if (row.AlignmentPropertyBean.AnovaPval < this.AnovaStart || row.AlignmentPropertyBean.AnovaPval > this.AnovaEnd) { row.Checked = false; continue; }
                else if (Ms2Filter && !row.AlignmentPropertyBean.MsmsIncluded) { row.Checked = false; continue; }
                else if (BlankFilter && row.AlignmentPropertyBean.IsBlankFiltered) { row.Checked = false; continue; }
                else if (UniqueFilter && !row.AlignmentPropertyBean.IsFragmentQueryExist) { row.Checked = false; continue; }
                else { row.Checked = true; }

                if (IdentifiedFilter && AnnotatedFilter)
                {
                    if (row.AlignmentPropertyBean.MetaboliteName != string.Empty) { row.Checked = true; }
                    else row.Checked = false;
                }
                else if (AnnotatedFilter && (row.AlignmentPropertyBean.MetaboliteName == string.Empty || (!row.AlignmentPropertyBean.MetaboliteName.Contains("w/o") && !row.AlignmentPropertyBean.MetaboliteName.Contains("Unknown")))) { row.Checked = false; continue; }
                else if (IdentifiedFilter && (row.AlignmentPropertyBean.MetaboliteName == string.Empty || row.AlignmentPropertyBean.MetaboliteName.Contains("w/o") || row.AlignmentPropertyBean.MetaboliteName.Contains("Unknown"))) { row.Checked = false; continue; }

            }
            NumExportPeaks = source2.Count(x => x.Checked == true);
        }

    }
}
