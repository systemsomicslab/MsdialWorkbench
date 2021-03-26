using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Lipidomics;
using CompMs.App.Msdial.View;
using CompMs.App.Msdial.ViewModel;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel
{
    class AnalysisParamSetVM<T> : ViewModelBase where T : ParameterBase
    {
        #region Property
        public ParameterBaseVM Param {
            get => paramVM;
            set => SetProperty(ref paramVM, value);
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

        public bool TogetherWithAlignment {
            get => (Param.ProcessOption.HasFlag(ProcessOption.Alignment));
            set {
                if (Param.ProcessOption.HasFlag(ProcessOption.Alignment) == value)
                    return;
                if (value) {
                    Param.ProcessOption |= ProcessOption.Alignment;
                }
                else {
                    Param.ProcessOption &= ~ProcessOption.Alignment;
                }
                OnPropertyChanged(nameof(TogetherWithAlignment));
            }
        }

        public List<MoleculeMsReference> MspDB { get; set; } = new List<MoleculeMsReference>();
        public List<MoleculeMsReference> TextDB { get; set; } = new List<MoleculeMsReference>();

        #endregion

        #region Field
        protected readonly T param;
        ParameterBaseVM paramVM;
        MsRefSearchParameterBaseVM mspSearchParam, textDbSearchParam;
        string alignmentResultFileName;
        ObservableCollection<AnalysisFileBean> analysisFiles;
        ObservableCollection<MzSearchQueryVM> excludedMassList;
        ObservableCollection<AdductIonVM> searchedAdductIons;
        #endregion

        public AnalysisParamSetVM(T parameter, IEnumerable<AnalysisFileBean> files) {
            param = parameter;
            Param = MsdialProjectParameterFactory.Create(parameter);
            MspSearchParam = new MsRefSearchParameterBaseVM(parameter.MspSearchParam);
            TextDbSearchParam = new MsRefSearchParameterBaseVM(parameter.TextDbSearchParam);

            var dt = DateTime.Now;
            AlignmentResultFileName = "AlignmentResult" + dt.ToString("_yyyy_MM_dd_hh_mm_ss");

            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(files);

            ExcludedMassList = new ObservableCollection<MzSearchQueryVM>(
                parameter.ExcludedMassList?.Select(query => new MzSearchQueryVM { Mass = query.Mass, Tolerance = query.MassTolerance })
                         .Concat(Enumerable.Repeat<MzSearchQueryVM>(null, 200).Select(_ => new MzSearchQueryVM()))
            );

            if (parameter.SearchedAdductIons.IsEmptyOrNull())
                parameter.SearchedAdductIons = AdductResourceParser.GetAdductIonInformationList(parameter.IonMode);
            parameter.SearchedAdductIons[0].IsIncluded = true;
            SearchedAdductIons = new ObservableCollection<AdductIonVM>(parameter.SearchedAdductIons.Select(ion => new AdductIonVM(ion)));

            parameter.QcAtLeastFilter = false;

        }

        #region Command
        public DelegateCommand<Window> ContinueProcessCommand {
            get => continueProcessCommand ?? (continueProcessCommand = new DelegateCommand<Window>(ContinueProcess, ValidateAnalysisParamSetWindow));
        }
        private DelegateCommand<Window> continueProcessCommand;
        private bool canExecuteCommand = true;

        private async void ContinueProcess(Window window) {
            canExecuteCommand = false;
            ContinueProcessCommand.RaiseCanExecuteChanged();
            Mouse.OverrideCursor = Cursors.Wait;

            var message = new ShortMessageWindow
            {
                Owner = window,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection
                        ? "RT correction viewer will be opened\nafter libraries are loaded."
                        : "Loading libraries.."
            };
            message.Show();
            var result = await ClosingMethod();
            message.Close();

            if (result) {
                window.DialogResult = true;
                window.Close();
            }

            Mouse.OverrideCursor = null;
            canExecuteCommand = true;
            ContinueProcessCommand.RaiseCanExecuteChanged();
        }

        protected virtual async Task<bool> ClosingMethod() {
            if (!param.SearchedAdductIons[0].IsIncluded) {
                MessageBox.Show("M + H or M - H must be included.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (param.MaxChargeNumber <= 0)
                param.MaxChargeNumber = 2;

            param.ExcludedMassList = ExcludedMassList
                .Where(query => query.Mass.HasValue && query.Tolerance.HasValue && query.Mass > 0 && query.Tolerance > 0)
                .Select(query => new MzSearchQuery { Mass = query.Mass.Value, MassTolerance = query.Tolerance.Value })
                .ToList();

            if (!string.IsNullOrEmpty(param.MspFilePath) && param.TargetOmics == TargetOmics.Metabolomics) {
                var ext = Path.GetExtension(param.MspFilePath);
                if (ext == ".msp" || ext == ".msp2") {
                    MspDB = LibraryHandler.ReadMspLibrary(param.MspFilePath).OrderBy(msp => msp.PrecursorMz).ToList();
                }
                else {
                    MspDB = new List<MoleculeMsReference>();
                }
            }
            else if (string.IsNullOrEmpty(param.MspFilePath) && param.TargetOmics == TargetOmics.Metabolomics) {
                MspDB = new List<MoleculeMsReference>();
            }
            else if (param.TargetOmics == TargetOmics.Lipidomics) {
                string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var files = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                if (files.Length == 1) {
                    param.MspFilePath = files.First();
                    MspDB = await Task.Run(() => LibraryHandler.ReadLipidMsLibrary(param.MspFilePath, param).OrderBy(msp => msp.PrecursorMz).ToList());
                }
            }
            var counter = 0;
            foreach (var msp in MspDB)
                msp.ScanID = counter++;

            if (!string.IsNullOrEmpty(param.TextDBFilePath)) {
                if (File.Exists(param.TextDBFilePath)) {
                    MessageBox.Show($"{param.TextDBFilePath} does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                TextDB = TextLibraryParser.TextLibraryReader(param.TextDBFilePath, out string error);
                if (TextDB.IsEmptyOrNull()) {
                    MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            if (param.TogetherWithAlignment && AnalysisFiles.Count > 1) {
                param.QcAtLeastFilter = false;

                if (param.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange && !AnalysisFiles.All(file => file.AnalysisFileType != AnalysisFileType.Blank)) {
                    if (MessageBox.Show("If you use blank sample filter, please set at least one file's type as Blank in file property setting. " +
                        "Do you continue this analysis without the filter option?",
                        "Messsage", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.Cancel)
                        return false;
                }
            }

            return true;
        }

        private bool ValidateAnalysisParamSetWindow(Window window) {
            return canExecuteCommand;
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
                if (param.SearchedAdductIons == null)
                    param.SearchedAdductIons = new List<AdductIon>();
                param.SearchedAdductIons.Add(vm.AdductIon);
                SearchedAdductIons.Add(new AdductIonVM(vm.AdductIon));
            }
        }

        public ICommand MspCommand {
            get {
                if (param.TargetOmics == TargetOmics.Lipidomics)
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
            if (Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly).Length != 1) {
                MessageBox.Show("There is no LBM file or several LBM files are existed in this application folder. Please see the tutorial.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var vm = new LipidDbSetVM(param.LipidQueryContainer, param.IonMode);
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
}
