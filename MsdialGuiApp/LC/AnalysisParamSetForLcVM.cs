using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Lipidomics;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Parameter;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.LC
{
    class AnalysisParamSetForLcVM : ViewModelBase {
        #region Property
        public MsdialLcmsParameterVM Param {
            get => param;
            set => SetProperty(ref param, value);
        }

        public MsRefSearchParameterBaseVM MspSearchParam {
            get => mspSearchParam;
            set => SetProperty(ref mspSearchParam, value);
        }

        public MsRefSearchParameterBaseVM TextDbSearchParam {
            get => textDbSearchParam;
            set => SetProperty(ref textDbSearchParam, value);
        }

        public string AlignmentResultFileName {
            get => alignmentResultFileName;
            set => SetProperty(ref alignmentResultFileName, value);
        }

        public ObservableCollection<AnalysisFileBean> AnalysisFiles {
            get => analysisFiles;
            set => SetProperty(ref analysisFiles, value);
        }

        public ObservableCollection<MzSearchQueryVM> ExcludedMassList {
            get => excludedMassList;
            set => SetProperty(ref excludedMassList, value);
        }

        public ObservableCollection<AdductIonVM> SearchedAdductIons {
            get => searchedAdductIons;
            set => SetProperty(ref searchedAdductIons, value);
        }

        #endregion

        #region Field
        MsdialLcmsParameter source;
        MsdialLcmsParameterVM param;
        MsRefSearchParameterBaseVM mspSearchParam, textDbSearchParam;
        string alignmentResultFileName;
        ObservableCollection<AnalysisFileBean> analysisFiles;
        ObservableCollection<MzSearchQueryVM> excludedMassList;
        ObservableCollection<AdductIonVM> searchedAdductIons;
        #endregion

        public AnalysisParamSetForLcVM(MsdialLcmsParameter parameter, IEnumerable<AnalysisFileBean> files) {
            source = parameter;
            Param = new MsdialLcmsParameterVM(parameter);
            MspSearchParam = new MsRefSearchParameterBaseVM(parameter.MspSearchParam);
            TextDbSearchParam = new MsRefSearchParameterBaseVM(parameter.TextDbSearchParam);

            var dt = DateTime.Now;
            AlignmentResultFileName = "AlignmentResult" + dt.ToString("_yyyy_MM_dd_hh_mm_ss");

            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(files);

            ExcludedMassList = new ObservableCollection<MzSearchQueryVM>(
                parameter.ExcludedMassList?.Select(query => new MzSearchQueryVM { Mass = query.Mass, Tolerance = query.MassTolerance })
                         .Concat(Enumerable.Repeat<MzSearchQueryVM>(null, 200).Select(_ => new MzSearchQueryVM()))
            );
            SearchedAdductIons = new ObservableCollection<AdductIonVM>(parameter.SearchedAdductIons?.Select(ion => new AdductIonVM(ion)));
        }

        #region Command
        public DelegateCommand<Window> ContinueProcessCommand {
            get => continueProcessCommand ?? (continueProcessCommand = new DelegateCommand<Window>(ContinueProcess, ValidateAnalysisFilePropertySetWindow));
        }
        private DelegateCommand<Window> continueProcessCommand;

        private void ContinueProcess(Window window) {
            if (ClosingMethod()) {
                window.DialogResult = true;
                window.Close();
            }
        }

        private bool ClosingMethod() {
            source.ExcludedMassList = ExcludedMassList.Where(query => query.Mass.HasValue && query.Tolerance.HasValue && query.Mass > 0 && query.Tolerance > 0)
                                                      .Select(query => new MzSearchQuery { Mass = query.Mass.Value, MassTolerance = query.Tolerance.Value })
                                                      .ToList();
            return true;
        }

        private bool ValidateAnalysisFilePropertySetWindow(Window window) {
            return true;
        }

        public DelegateCommand<Window> CancelProcessCommand {
            get => cancelProcessCommand ?? (cancelProcessCommand = new DelegateCommand<Window>(CancelProcess));
        }
        private DelegateCommand<Window> cancelProcessCommand;

        private void CancelProcess(Window window) {
            window.DialogResult = false;
            window.Close();
        }

        public DelegateCommand<Window> AddAdductIonCommand {
            get => addAdductIonCommand ?? (addAdductIonCommand = new DelegateCommand<Window>(AddAdductIon));
        }
        private DelegateCommand<Window> addAdductIonCommand;

        private void AddAdductIon(Window owner) {
            var vm = new UserDefinedAdductSetVM();
            var window = new UserDefinedAdductSetWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            if (window.ShowDialog() == true) {
                if (source.SearchedAdductIons == null)
                    source.SearchedAdductIons = new List<AdductIon>();
                source.SearchedAdductIons.Add(vm.AdductIon);
                SearchedAdductIons.Add(new AdductIonVM(vm.AdductIon));
            }
        }

        public ICommand MspCommand {
            get {
                if (source.TargetOmics == CompMs.Common.Enum.TargetOmics.Lipidomics)
                    return LipidDBSetCommand;
                return MspFileSelectCommand;
            }
        }

        public DelegateCommand<Window> LipidDBSetCommand {
            get => lipidDBSetCommand ?? (lipidDBSetCommand = new DelegateCommand<Window>(LipidDBSet));
        }
        private DelegateCommand<Window> lipidDBSetCommand;

        private void LipidDBSet(Window owner) {
            var mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "*", SearchOption.TopDirectoryOnly).Length != 1)
            {
                MessageBox.Show("There is no LBM file or several LBM files are existed in this application folder. Please see the tutorial.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var vm = new LipidDbSetVM(source.LipidQueryContainer, source.IonMode);
            var window = new LipidDbSetWindow
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = vm,
            };
            window.ShowDialog();
        }

        public DelegateCommand MspFileSelectCommand {
            get => mspFileSelectCommand ?? (mspFileSelectCommand = new DelegateCommand(MspFileSelect));
        }
        private DelegateCommand mspFileSelectCommand;

        private void MspFileSelect() {
            var ofd = new OpenFileDialog
            {
                Title = "Import a library file",
                Filter = "MSP file(*.msp)|*.msp*",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true) {
                Param.MspFilePath = ofd.FileName;
            }
        }

        public DelegateCommand TextDBFileSelectCommand {
            get => textDBFileSelectCommand ?? (textDBFileSelectCommand = new DelegateCommand(TextDBFileSelect));
        }
        private DelegateCommand textDBFileSelectCommand;

        private void TextDBFileSelect() {
            var ofd = new OpenFileDialog
            {
                Title = "Import a library file for post identification",
                Filter = "Text file(*.txt)|*.txt;",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true) {
                Param.TextDBFilePath = ofd.FileName;
            }
        }

        public DelegateCommand IsotopeTextDBFileSelectCommand {
            get => isotopeTextDBFileSelectCommand ?? (isotopeTextDBFileSelectCommand = new DelegateCommand(IsotopeTextDBFileSelect));
        }
        private DelegateCommand isotopeTextDBFileSelectCommand;

        private void IsotopeTextDBFileSelect() {
            var ofd = new OpenFileDialog
            {
                Title = "Import target formulas library",
                Filter = "Text file(*.txt)|*.txt;",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true) {
                Param.IsotopeTextDBFilePath = ofd.FileName;
            }
        }
        #endregion
    }

    class MsdialLcmsParameterVM : DynamicViewModelBase<MsdialLcmsParameter>
    {
        public MsdialLcmsParameterVM(MsdialLcmsParameter innerModel) : base(innerModel) { }

        public string MspFilePath {
            get => InnerModel.MspFilePath;
            set {
                if (InnerModel.MspFilePath == value) return;
                InnerModel.MspFilePath = value;
                OnPropertyChanged(nameof(MspFilePath));
            }
        }
        public string TextDBFilePath {
            get => InnerModel.TextDBFilePath;
            set {
                if (InnerModel.TextDBFilePath == value) return;
                InnerModel.TextDBFilePath = value;
                OnPropertyChanged(nameof(TextDBFilePath));
            }
        }
        public string IsotopeTextDBFilePath {
            get => InnerModel.IsotopeTextDBFilePath;
            set {
                if (InnerModel.IsotopeTextDBFilePath == value) return;
                InnerModel.IsotopeTextDBFilePath = value;
                OnPropertyChanged(nameof(IsotopeTextDBFilePath));
            }
        }
    }

    class MsRefSearchParameterBaseVM : DynamicViewModelBase<MsRefSearchParameterBase>
    {
        public MsRefSearchParameterBaseVM(MsRefSearchParameterBase innerModel) : base(innerModel) { }
    }

    public class MzSearchQueryVM : ViewModelBase
    {
        public double? Mass {
            get => mass;
            set => SetProperty(ref mass, value);
        }
        public double? Tolerance {
            get => tolerance;
            set => SetProperty(ref tolerance, value);
        }

        private double? mass, tolerance;
    }

    class AdductIonVM : DynamicViewModelBase<AdductIon>
    {
        public AdductIonVM(AdductIon innerModel) : base(innerModel) { }
    }
}
