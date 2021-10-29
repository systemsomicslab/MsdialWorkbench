using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.View;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.DataObj;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    public sealed class DimsAnalysisParameterSetViewModel : ViewModelBase
    {
        public DimsAnalysisParameterSetViewModel(DimsAnalysisParameterSetModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Parameter = new ParameterBaseVM(Model.Parameter);

            AlignmentResultFileName = Model.AlignmentResultFileName;

            AnalysisFiles = Model.AnalysisFiles;

            ExcludedMassList = new ObservableCollection<MzSearchQueryVM>(
                Model.ExcludedMassList?.Select(query => new MzSearchQueryVM { Mass = query.Mass, Tolerance = query.MassTolerance })
                         .Concat(Enumerable.Repeat<MzSearchQueryVM>(null, 200).Select(_ => new MzSearchQueryVM()))
            );

            SearchedAdductIons = new ObservableCollection<AdductIonVM>(Model.SearchedAdductIons.Select(ion => new AdductIonVM(ion)));

            DataCollectionSettingViewModel = new DimsDataCollectionSettingViewModel(Model.DataCollectionSettingModel).AddTo(Disposables);

            IdentifySettingViewModel = new IdentifySettingViewModel(Model.IdentifySettingModel, new DimsAnnotatorSettingViewModelFactory()).AddTo(Disposables);

            ContinueProcessCommand = new[]{
                IdentifySettingViewModel.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand<Window>()
            .WithSubscribe(ContinueProcess)
            .AddTo(Disposables);
        }

        public DimsAnalysisParameterSetModel Model { get; }

        public ParameterBaseVM Parameter { get; }

        [Obsolete]
        public ParameterBaseVM Param => Parameter;

        public string AlignmentResultFileName {
            get => alignmentResultFileName;
            set => SetProperty(ref alignmentResultFileName, value);
        }
        private string alignmentResultFileName = string.Empty;

        public ObservableCollection<AnalysisFileBean> AnalysisFiles { get; }

        public ObservableCollection<MzSearchQueryVM> ExcludedMassList { get; }

        public ObservableCollection<AdductIonVM> SearchedAdductIons { get; }

        public DimsDataCollectionSettingViewModel DataCollectionSettingViewModel { get; }
        public IdentifySettingViewModel IdentifySettingViewModel { get; }

        public bool TogetherWithAlignment {
            get {
                return Parameter.ProcessOption.HasFlag(ProcessOption.Alignment);
            }
            set {
                if (Parameter.ProcessOption.HasFlag(ProcessOption.Alignment) == value) {
                    return;
                }
                if (value) {
                    Parameter.ProcessOption |= ProcessOption.Alignment;
                }
                else {
                    Parameter.ProcessOption &= ~ProcessOption.Alignment;
                }
                OnPropertyChanged(nameof(TogetherWithAlignment));
            }
        }

        public ReactiveCommand<Window> ContinueProcessCommand { get; }

        private void ContinueProcess(Window window) {
            Mouse.OverrideCursor = Cursors.Wait;

            var message = new ShortMessageWindow
            {
                Owner = window,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = "Loading libraries.."
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
            if (!Model.ParameterBase.SearchedAdductIons[0].IsIncluded) {
                MessageBox.Show("M + H or M - H must be included.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            Model.ParameterBase.ExcludedMassList = ExcludedMassList
                .Where(query => query.Mass.HasValue && query.Tolerance.HasValue && query.Mass > 0 && query.Tolerance > 0)
                .Select(query => new MzSearchQuery { Mass = query.Mass.Value, MassTolerance = query.Tolerance.Value })
                .ToList();
            Model.ParameterBase.SearchedAdductIons = SearchedAdductIons.Select(vm => vm.Model).ToList();

            if (Model.ParameterBase.TogetherWithAlignment && AnalysisFiles.Count > 1) {

                if (Model.ParameterBase.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange && !AnalysisFiles.All(file => file.AnalysisFileType != AnalysisFileType.Blank)) {
                    if (MessageBox.Show("If you use blank sample filter, please set at least one file's type as Blank in file property setting. " +
                        "Do you continue this analysis without the filter option?",
                        "Messsage", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.Cancel)
                        return false;
                }
            }

            DataCollectionSettingViewModel.Commit();
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
                Parameter.IsotopeTextDBFilePath = ofd.FileName;
            }
        }
    }
}
