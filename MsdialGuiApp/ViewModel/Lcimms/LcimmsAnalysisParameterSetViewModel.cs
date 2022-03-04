using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.View;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    class LcimmsAnalysisParameterSetViewModel : ViewModelBase
    {
        public LcimmsAnalysisParameterSetViewModel(LcimmsAnalysisParameterSetModel model) {
            Model = model;
            Parameter = new MsdialLcimmsParameterViewModel(Model.Parameter);

            AlignmentResultFileName = Model.AlignmentResultFileName;

            AnalysisFiles = Model.AnalysisFiles;

            ExcludedMassList = new ObservableCollection<MzSearchQueryVM>(
                Model.ParameterBase.ExcludedMassList?.Select(query => new MzSearchQueryVM { Mass = query.Mass, Tolerance = query.MassTolerance })
                    .Concat(Enumerable.Repeat<MzSearchQueryVM>(null, 200).Select(_ => new MzSearchQueryVM())));

            SearchedAdductIons = new ObservableCollection<AdductIonVM>(Model.SearchedAdductIons.Select(ion => new AdductIonVM(ion)));

            IdentifySettingViewModel = new IdentifySettingViewModel(Model.IdentifySettingModel, new LcimmsAnnotatorSettingViewModelFactory(), Observable.Return(true)).AddTo(Disposables);

            var rep = Model.FileID2CcsCoefficients.Values.FirstOrDefault();
            if (rep != null) {
                if (rep.IsAgilentIM) {
                    Parameter.IonMobilityType.Value = IonMobilityType.Dtims;
                }
                else if (rep.IsWatersIM) {
                    Parameter.IonMobilityType.Value = IonMobilityType.Twims;
                }
                else if (rep.IsBrukerIM) {
                    Parameter.IonMobilityType.Value = IonMobilityType.Tims;
                }
            }

            ccsCalibrationInfoVSs = new ObservableCollection<CcsCalibrationInfoVS>();
            foreach (var file in Model.AnalysisFiles) {
                ccsCalibrationInfoVSs.Add(new CcsCalibrationInfoVS(file, Model.FileID2CcsCoefficients[file.AnalysisFileId]));
            }

            CcsCalibrationInfoVSs = new ReadOnlyObservableCollection<CcsCalibrationInfoVS>(ccsCalibrationInfoVSs);
            var TimsInfoImported = Parameter.IsTIMS;
            var DtimsInfoImported = new[]
            {
                Parameter.IsDTIMS,
                CcsCalibrationInfoVSs.ObserveElementProperty(m => m.AgilentBeta)
                    .Select(_ => CcsCalibrationInfoVSs.All(info => info.AgilentBeta > -1)),
                CcsCalibrationInfoVSs.ObserveElementProperty(m => m.AgilentTFix)
                    .Select(_ => CcsCalibrationInfoVSs.All(info => info.AgilentTFix > -1)),
            }.CombineLatestValuesAreAllTrue();
            var TwimsInfoImported = new[]
            {
                Parameter.IsTWIMS,
                CcsCalibrationInfoVSs.ObserveElementProperty(m => m.WatersCoefficient)
                    .Select(_ => CcsCalibrationInfoVSs.All(info => info.WatersCoefficient > -1)),
                CcsCalibrationInfoVSs.ObserveElementProperty(m => m.WatersT0)
                    .Select(_ => CcsCalibrationInfoVSs.All(info => info.WatersT0 > -1)),
                CcsCalibrationInfoVSs.ObserveElementProperty(m => m.WatersExponent)
                    .Select(_ => CcsCalibrationInfoVSs.All(info => info.WatersExponent > -1)),
            }.CombineLatestValuesAreAllTrue();
            IsAllCalibrantDataImported = new[]
            {
                TimsInfoImported,
                DtimsInfoImported,
                TwimsInfoImported,
            }.CombineLatest(xs => xs.Any(x => x))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            IsAllCalibrantDataImported.Subscribe(x => Parameter.IsAllCalibrantDataImported.Value = x)
                .AddTo(Disposables);

            ContinueProcessCommand = new[]
            {
                IdentifySettingViewModel.ObserveHasErrors,
                Parameter.ObserveProperty(m => m.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange)
                    .Select(v => v && AnalysisFiles.All(file => file.AnalysisFileType != AnalysisFileType.Blank)),
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand<Window>()
            .WithSubscribe(ContinueProcess)
            .AddTo(Disposables);
        }

        public LcimmsAnalysisParameterSetModel Model { get; }

        public IdentifySettingViewModel IdentifySettingViewModel { get; }

        public MsdialLcimmsParameterViewModel Parameter { get; }

        [Obsolete]
        public MsdialLcimmsParameterViewModel Param => Parameter;

        public string AlignmentResultFileName {
            get => alignmentResultFileName;
            set => SetProperty(ref alignmentResultFileName, value);
        }
        private string alignmentResultFileName;

        public ObservableCollection<AnalysisFileBean> AnalysisFiles {
            get => analysisFiles;
            set => SetProperty(ref analysisFiles, value);
        }
        private ObservableCollection<AnalysisFileBean> analysisFiles;

        public ObservableCollection<MzSearchQueryVM> ExcludedMassList {
            get => excludedMassList;
            set => SetProperty(ref excludedMassList, value);
        }
        private ObservableCollection<MzSearchQueryVM> excludedMassList;

        public ObservableCollection<AdductIonVM> SearchedAdductIons {
            get => searchedAdductIons;
            set => SetProperty(ref searchedAdductIons, value);
        }
        private ObservableCollection<AdductIonVM> searchedAdductIons;

        public bool TogetherWithAlignment {
            get => Param.ProcessOption.HasFlag(ProcessOption.Alignment);
            set {
                if (Param.ProcessOption.HasFlag(ProcessOption.Alignment) == value) {
                    return;
                }
                if (value) {
                    Param.ProcessOption |= ProcessOption.Alignment;
                }
                else {
                    Param.ProcessOption &= ~ProcessOption.Alignment;
                }
                OnPropertyChanged(nameof(TogetherWithAlignment));
            }
        }

        private readonly ObservableCollection<CcsCalibrationInfoVS> ccsCalibrationInfoVSs;

        public ReactiveCommand<Window> ContinueProcessCommand { get; }

        private void ContinueProcess(Window window) {
            Mouse.OverrideCursor = Cursors.Wait;

            var message = new ShortMessageWindow
            {
                Owner = window,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = Model.Parameter.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection
                    ? "RT correction viewer will be opened\nafter libraries are loaded."
                    : "Loading libraries..."
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

        private bool ClosingMethod() {
            if (!Model.Parameter.SearchedAdductIons[0].IsIncluded) {
                MessageBox.Show("M + H or M - H must be included.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            Parameter.Commit();

            Model.Parameter.ExcludedMassList = ExcludedMassList
                .Where(query => query.Mass.HasValue && query.Tolerance.HasValue && query.Mass > 0 && query.Tolerance > 0)
                .Select(query => new MzSearchQuery { Mass = query.Mass.Value, MassTolerance = query.Tolerance.Value })
                .ToList();

            Model.ClosingMethod();

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
                if (Model.Parameter.SearchedAdductIons == null) {
                    Model.Parameter.SearchedAdductIons = new List<AdductIon>();
                }
                Model.Parameter.SearchedAdductIons.Add(vm.AdductIon);
                SearchedAdductIons.Add(new AdductIonVM(vm.AdductIon));
            }
        }

        public DelegateCommand IsotopeTextDBFileSelectCommand {
            get => isotopeTextDBFileSelectCommand ?? (isotopeTextDBFileSelectCommand = new DelegateCommand(IsotopeTextDBFileSelect));
        }
        public ReadOnlyObservableCollection<CcsCalibrationInfoVS> CcsCalibrationInfoVSs { get; }
        public ReadOnlyReactivePropertySlim<bool> IsAllCalibrantDataImported { get; }

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
    }
}
