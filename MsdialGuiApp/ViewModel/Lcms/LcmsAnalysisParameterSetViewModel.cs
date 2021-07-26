using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.View;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class LcmsAnalysisParameterSetViewModel : ViewModelBase
    {
        #region Property
        public ParameterBaseVM Param {
            get => paramVM;
            set => SetProperty(ref paramVM, value);
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

        public AnnotationProcessSettingModel AnnotationProcessSettingModel { get; }
        public AnnotationProcessSettingViewModel AnnotationProcessSettingViewModel { get; }

        #endregion

        #region Field
        protected readonly MsdialLcmsParameter param;
        ParameterBaseVM paramVM;
        string alignmentResultFileName;
        ObservableCollection<AnalysisFileBean> analysisFiles;
        ObservableCollection<MzSearchQueryVM> excludedMassList;
        ObservableCollection<AdductIonVM> searchedAdductIons;
        #endregion

        public LcmsAnalysisParameterSetViewModel(MsdialLcmsParameter parameter, IEnumerable<AnalysisFileBean> files) {
            param = parameter;
            Param = MsdialProjectParameterFactory.Create(parameter);

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

            AnnotationProcessSettingModel = new AnnotationProcessSettingModel();
            var factory = new LcmsAnnotationSettingViewModelModelFactory(parameter);
            AnnotationProcessSettingViewModel = new AnnotationProcessSettingViewModel(
                    AnnotationProcessSettingModel,
                    factory.Create)
                .AddTo(Disposables);

            if (param.TargetOmics == TargetOmics.Lipidomics) {
                string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                AnnotationProcessSettingViewModel.AddNewAnnotationCommand.Execute(null);
                var annotationMethod = AnnotationProcessSettingViewModel.Annotations.Last();
                (annotationMethod as LcmsAnnotationSettingViewModel).DataBasePath.Value = lbmFiles.First();
            }

            ContinueProcessCommand = AnnotationProcessSettingViewModel.ObserveHasErrors.Inverse()
                .ToAsyncReactiveCommand<Window>()
                .WithSubscribe(async window => await Task.Run(() => ContinueProcess(window)))
                .AddTo(Disposables);
        }

        #region Command
        public AsyncReactiveCommand<Window> ContinueProcessCommand { get; }

        private void ContinueProcess(Window window) {
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
            var result = ClosingMethod();
            message.Close();

            if (result) {
                window.DialogResult = true;
                window.Close();
            }

            Mouse.OverrideCursor = null;
        }

        protected virtual bool ClosingMethod() {
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
