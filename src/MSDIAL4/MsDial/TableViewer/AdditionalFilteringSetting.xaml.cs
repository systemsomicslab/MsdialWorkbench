using System;
using System.Collections.Generic;
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

namespace Rfx.Riken.OsakaUniv.TableViewer
{
    /// <summary>
    /// AdditionalFilteringSetting.xaml の相互作用ロジック
    /// </summary>
    public partial class AdditionalFilteringSetting : Window
    {
        public AdditionalFilteringSettingVM VM {get;set;}
        public AdditionalFilteringSetting(FilterSettings settings)
        {
            InitializeComponent();
            VM = new AdditionalFilteringSettingVM(settings);
            this.DataContext = VM;
        }
        public AdditionalFilteringSetting(AlignmentSpotTableViewerVM alignmentSpotTableViewerVM)
        {
            InitializeComponent();
            VM = new AdditionalFilteringSettingVM(alignmentSpotTableViewerVM);
            this.DataContext = VM;
        }
    }

    public class AdditionalFilteringSettingVM : ViewModelBase
    {
        public AdditionalFilteringSettingVM(FilterSettings settings)
        {
            this.Settings = settings;
        }
        public AdditionalFilteringSettingVM(AlignmentSpotTableViewerVM alignmentSpotTableViewerVM)
        {
            this.Settings = alignmentSpotTableViewerVM.Settings;
            this.AlignmentSpotTableViewerVM = alignmentSpotTableViewerVM;
        }

        public FilterSettings Settings { get; set; }
        public AlignmentSpotTableViewerVM AlignmentSpotTableViewerVM { get; set; }

        public bool IsAccurateMassChecked {
            get { return Settings.IsAccurateMassSimilarityCutOff; }
            set {
                Settings.IsAccurateMassSimilarityCutOff = value;
                OnPropertyChanged(nameof(IsAccurateMassChecked));
            }
        }

        public bool IsRtChecked {
            get { return Settings.IsRetentionTimeSimilarityCutOff; }
            set {
                Settings.IsRetentionTimeSimilarityCutOff = value;
                OnPropertyChanged(nameof(IsRtChecked));
            }
        }

        public bool IsDotProductSimilarityCutOff {
            get { return Settings.IsDotProductSimilarityCutOff; }
            set {
                Settings.IsDotProductSimilarityCutOff = value;
                OnPropertyChanged(nameof(IsDotProductSimilarityCutOff));
            }
        }

        public bool IsReverseDotProductSimilarityCutOff {
            get { return Settings.IsReverseDotProductSimilarityCutOff; }
            set {
                Settings.IsReverseDotProductSimilarityCutOff = value;
                OnPropertyChanged(nameof(IsReverseDotProductSimilarityCutOff));
            }
        }

        public bool IsFragmentPresenceSimilarityCutOff {
            get { return Settings.IsFragmentPresenceSimilarityCutOff; }
            set {
                Settings.IsFragmentPresenceSimilarityCutOff = value;
                OnPropertyChanged(nameof(IsFragmentPresenceSimilarityCutOff));
            }
        }

        public bool IsSimpleDotProductChecked {
            get { return Settings.IsSimpleDotProductSimilarityCutOff; }
            set {
                Settings.IsSimpleDotProductSimilarityCutOff = value;
                OnPropertyChanged(nameof(IsSimpleDotProductChecked));
            }
        }

        public bool IsCalcCorrelationsCHecked
        {
            get { return this.AlignmentSpotTableViewerVM.IsCalcCorrelation; }
            set
            {
                this.AlignmentSpotTableViewerVM.IsCalcCorrelation = value;
                OnPropertyChanged(nameof(IsCalcCorrelationsCHecked));
            }
        }


        public float AccurateMassCutOffMin {
            get {
                return Settings.AccurateMassSimilarityCutOffMin;
            }
            set {
                if (Settings.AccurateMassSimilarityCutOffMin == value) return;
                Settings.AccurateMassSimilarityCutOffMin = GetTargetValues(value);
                OnPropertyChanged(nameof(AccurateMassCutOffMin));
            }
        }

        public float AccurateMassCutOffMax {
            get {
                return Settings.AccurateMassSimilarityCutOffMax;
            }
            set {
                if (Settings.AccurateMassSimilarityCutOffMax == value) return;
                Settings.AccurateMassSimilarityCutOffMax = GetTargetValues(value);
                OnPropertyChanged(nameof(AccurateMassCutOffMax));
            }
        }

        public float RtCutOffMin {
            get {
                return Settings.RetentionTimeSimilarityCutOffMin;
            }
            set {
                if (Settings.RetentionTimeSimilarityCutOffMin == value) return;
                Settings.RetentionTimeSimilarityCutOffMin = GetTargetValues(value);
                OnPropertyChanged(nameof(RtCutOffMin));
            }
        }

        public float RtCutOffMax {
            get {
                return Settings.RetentionTimeSimilarityCutOffMax;
            }
            set {
                if (Settings.RetentionTimeSimilarityCutOffMax == value) return;
                Settings.RetentionTimeSimilarityCutOffMax = GetTargetValues(value);
                OnPropertyChanged(nameof(RtCutOffMax));
            }
        }

        public float DotProductSimilarityCutOffMin {
            get {
                return Settings.DotProductSimilarityCutOffMin;
            }
            set {
                if (Settings.DotProductSimilarityCutOffMin == value) return;
                Settings.DotProductSimilarityCutOffMin = GetTargetValues(value);
                OnPropertyChanged(nameof(DotProductSimilarityCutOffMin));
            }
        }

        public float DotProductSimilarityCutOffMax {
            get {
                return Settings.DotProductSimilarityCutOffMax;
            }
            set {
                if (Settings.DotProductSimilarityCutOffMax == value) return;
                Settings.DotProductSimilarityCutOffMax = GetTargetValues(value);
                OnPropertyChanged(nameof(DotProductSimilarityCutOffMax));
            }
        }

        public float ReverseDotProductSimilarityCutOffMin {
            get {
                return Settings.ReverseDotProductSimilarityCutOffMin;
            }
            set {
                if (Settings.ReverseDotProductSimilarityCutOffMin == value) return;
                Settings.ReverseDotProductSimilarityCutOffMin = GetTargetValues(value);
                OnPropertyChanged(nameof(ReverseDotProductSimilarityCutOffMin));
            }
        }

        public float ReverseDotProductSimilarityCutOffMax {
            get {
                return Settings.ReverseDotProductSimilarityCutOffMax;
            }
            set {
                if (Settings.ReverseDotProductSimilarityCutOffMax == value) return;
                Settings.ReverseDotProductSimilarityCutOffMax = GetTargetValues(value);
                OnPropertyChanged(nameof(ReverseDotProductSimilarityCutOffMax));
            }
        }


        public float FragmentPresenceSimilarityCutOffMin {
            get {
                return Settings.FragmentPresenceSimilarityCutOffMin;
            }
            set {
                if (Settings.FragmentPresenceSimilarityCutOffMin == value) return;
                Settings.FragmentPresenceSimilarityCutOffMin = GetTargetValues(value);
                OnPropertyChanged(nameof(FragmentPresenceSimilarityCutOffMin));
            }
        }

        public float FragmentPresenceSimilarityCutOffMax {
            get {
                return Settings.FragmentPresenceSimilarityCutOffMax;
            }
            set {
                if (Settings.FragmentPresenceSimilarityCutOffMax == value) return;
                Settings.FragmentPresenceSimilarityCutOffMax = GetTargetValues(value);
                OnPropertyChanged(nameof(FragmentPresenceSimilarityCutOffMax));
            }
        }
        public float SimpleDotProductSimilarityCutOffMin {
            get {
                return Settings.SimpleDotProductSimilarityCutOffMin;
            }
            set {
                if (Settings.SimpleDotProductSimilarityCutOffMin == value) return;
                Settings.SimpleDotProductSimilarityCutOffMin = GetTargetValues(value);
                OnPropertyChanged(nameof(SimpleDotProductSimilarityCutOffMin));
            }
        }

        public float SimpleDotProductSimilarityCutOffMax {
            get {
                return Settings.SimpleDotProductSimilarityCutOffMax;
            }
            set {
                if (Settings.SimpleDotProductSimilarityCutOffMax == value) return;
                Settings.SimpleDotProductSimilarityCutOffMax = GetTargetValues(value);
                OnPropertyChanged(nameof(SimpleDotProductSimilarityCutOffMax));
            }
        }

        public float RetentionTimeTolForCorrelation
        {
            get
            {
                return this.AlignmentSpotTableViewerVM.RetentionTimeTolForCorrelation;
            }
            set
            {
                if (AlignmentSpotTableViewerVM.RetentionTimeTolForCorrelation == value) return;
                AlignmentSpotTableViewerVM.RetentionTimeTolForCorrelation = GetRtValues(value);
                OnPropertyChanged(nameof(RetentionTimeTolForCorrelation));
            }
        }


        private float GetTargetValues(float val)
        {
            if(val < -1)
            {
                return -1;
            }
            else if(val > 1000)
            {
                return 1000;
            }
            else
            {
                return val;
            }
        }

        private float GetRtValues(float val)
        {
            if (val < 0)
            {
                return 0;
            }
            else if (val > 200)
            {
                return 2000;
            }
            else
            {
                return val;
            }
        }


        private DelegateCommand finishCommand;
        public DelegateCommand FinishCommand {
            get {
                return finishCommand ?? (finishCommand = new DelegateCommand(obj => {
                    this.Settings.Update();
                    ((AdditionalFilteringSetting)obj).Close();
                }, x => true));
            }
        }
    }
}
