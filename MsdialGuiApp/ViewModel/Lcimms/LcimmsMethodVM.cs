using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Lcimms;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    class LcimmsMethodVM : MethodVM
    {
        public LcimmsMethodVM(MsdialDataStorage storage, List<AnalysisFileBean> analysisFiles, List<AlignmentFileBean> alignmentFiles)
            : this(new LcimmsMethodModel(storage, analysisFiles, alignmentFiles)) {
            
        }

        public LcimmsMethodVM(LcimmsMethodModel model) : base(model.Serializer) {
            this.model = model;
            AnalysisFilesView = CollectionViewSource.GetDefaultView(model.AnalysisFiles);
            AlignmentFilesView = CollectionViewSource.GetDefaultView(model.AlignmentFiles);

            PropertyChanged += OnDisplayFiltersChanged;
        }

        private readonly LcimmsMethodModel model;
        public ICollectionView AnalysisFilesView { get; }
        public ICollectionView AlignmentFilesView { get; }

        public AnalysisLcimmsVM AnalysisVM {
            get => analysisVM;
            set => SetProperty(ref analysisVM, value);
        }
        private AnalysisLcimmsVM analysisVM;

        public AlignmentLcimmsVM AlignmentVM {
            get => alignmentVM;
            set => SetProperty(ref alignmentVM, value);
        }
        private AlignmentLcimmsVM alignmentVM;

        public bool RefMatchedChecked {
            get => ReadDisplayFilter(DisplayFilter.RefMatched);
            set => WriteDisplayFilter(DisplayFilter.RefMatched, value);
        }
        public bool SuggestedChecked {
            get => ReadDisplayFilter(DisplayFilter.Suggested);
            set => WriteDisplayFilter(DisplayFilter.Suggested, value);
        }
        public bool UnknownChecked {
            get => ReadDisplayFilter(DisplayFilter.Unknown);
            set => WriteDisplayFilter(DisplayFilter.Unknown, value);
        }
        public bool CcsChecked {
            get => ReadDisplayFilter(DisplayFilter.CcsMatched);
            set => WriteDisplayFilter(DisplayFilter.CcsMatched, value);
        }
        public bool Ms2AcquiredChecked {
            get => ReadDisplayFilter(DisplayFilter.Ms2Acquired);
            set => WriteDisplayFilter(DisplayFilter.Ms2Acquired, value);
        }
        public bool MolecularIonChecked {
            get => ReadDisplayFilter(DisplayFilter.MolecularIon);
            set => WriteDisplayFilter(DisplayFilter.MolecularIon, value);
        }
        public bool BlankFilterChecked {
            get => ReadDisplayFilter(DisplayFilter.Blank);
            set => WriteDisplayFilter(DisplayFilter.Blank, value);
        }
        public bool UniqueIonsChecked {
            get => ReadDisplayFilter(DisplayFilter.UniqueIons);
            set => WriteDisplayFilter(DisplayFilter.UniqueIons, value);
        }
        public bool ManuallyModifiedChecked {
            get => ReadDisplayFilter(DisplayFilter.ManuallyModified);
            set => WriteDisplayFilter(DisplayFilter.ManuallyModified, value);
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        void OnDisplayFiltersChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(displayFilters)) {
                if (AnalysisVM != null)
                    AnalysisVM.DisplayFilters = displayFilters;
                if (AlignmentVM != null)
                    AlignmentVM.DisplayFilters = displayFilters;
            }
        }

        public override int InitializeNewProject(Window window) {
            // Set analysis param
            if (!ProcessSetAnalysisParameter(window))
                return -1;

            var processOption = model.Storage.ParameterBase.ProcessOption;
            // Run Identification
            if (processOption.HasFlag(CompMs.Common.Enum.ProcessOption.Identification) || processOption.HasFlag(CompMs.Common.Enum.ProcessOption.PeakSpotting)) {
                if (!ProcessAnnotaion(window, model.Storage))
                    return -1;
            }

            // Run Alignment
            if (processOption.HasFlag(CompMs.Common.Enum.ProcessOption.Alignment)) {
                if (!ProcessAlignment(window))
                    return -1;
            }

            LoadAnalysisFile(model.Storage.AnalysisFiles.FirstOrDefault());
            return 0;
        }

        private bool ProcessSetAnalysisParameter(Window owner) {
            var analysisParamSetVM = new AnalysisParamSetVM<MsdialLcImMsParameter>(model.Storage.ParameterBase as MsdialLcImMsParameter, model.Storage.AnalysisFiles);
            var apsw = new AnalysisParamSetForLcimmsWindow
            {
                DataContext = analysisParamSetVM,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            if (apsw.ShowDialog() != true)
                return false;

            model.SetStorageContent(analysisParamSetVM.AlignmentResultFileName, analysisParamSetVM.MspDB, analysisParamSetVM.TextDB);
            return true;
        }

        private bool ProcessAnnotaion(Window owner, MsdialDataStorage storage) {
            var vm = new ProgressBarMultiContainerVM
            {
                MaxValue = storage.AnalysisFiles.Count,
                CurrentValue = 0,
                ProgressBarVMs = new ObservableCollection<ProgressBarVM>(
                        storage.AnalysisFiles.Select(file => new ProgressBarVM { Label = file.AnalysisFileName })
                    ),
            };
            var pbmcw = new ProgressBarMultiContainerWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            pbmcw.Loaded += async (s, e) => {
                foreach ((var analysisfile, var pbvm) in storage.AnalysisFiles.Zip(vm.ProgressBarVMs)) {
                    await model.RunAnnotationProcess(analysisfile, v => pbvm.CurrentValue = v);
                    vm.CurrentValue++;
                }
                pbmcw.Close();
            };
            pbmcw.ShowDialog();

            return true;
        }

        private bool ProcessAlignment(Window owner) {
            var vm = new ProgressBarVM
            {
                IsIndeterminate = true,
                Label = "Process alignment..",
            };
            var pbw = new ProgressBarWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            pbw.Show();

            model.RunAlignmentProcess();
            pbw.Close();

            return true;
        }

        public override void LoadProject() {
            LoadSelectedAnalysisFile();
        }

        public DelegateCommand LoadAnalysisFileCommand {
            get => loadAnalysisFileCommand ?? (loadAnalysisFileCommand = new DelegateCommand(LoadSelectedAnalysisFile));
        }
        private DelegateCommand loadAnalysisFileCommand;

        private AnalysisFileBean cacheAnalysisFile;
        private void LoadSelectedAnalysisFile() {
            if (AnalysisFilesView.CurrentItem is AnalysisFileBean analysis) {
                LoadAnalysisFile(analysis);
            }
        }

        private void LoadAnalysisFile(AnalysisFileBean analysis) {
            if (cacheAnalysisFile == analysis || analysis == null) {
                return;
            }

            cacheAnalysisFile = analysis;
            model.AnalysisFile = analysis;
            AnalysisVM = new AnalysisLcimmsVM(model.AnalysisModel);
        }

        public DelegateCommand LoadAlignmentFileCommand {
            get => loadAlignmentFileCommand ?? (loadAlignmentFileCommand = new DelegateCommand(LoadSelectedAlignmentFile));
        }
        private DelegateCommand loadAlignmentFileCommand;

        private AlignmentFileBean cacheAlignmentFile;
        private void LoadSelectedAlignmentFile() {
            if (AlignmentFilesView.CurrentItem is AlignmentFileBean alignment) {
                LoadAlignmentFile(alignment);
            }
        }

        private void LoadAlignmentFile(AlignmentFileBean alignment) {
            if (cacheAlignmentFile == alignment || alignment == null) {
                return;
            }

            cacheAlignmentFile = alignment;
            model.AlignmentFile = alignment;
            AlignmentVM = new AlignmentLcimmsVM(model.AlignmentModel);
        }

        public override void SaveProject() {
            model.SaveProject();
        }

        public DelegateCommand<Window> ExportAlignmentResultCommand => exportAlignmentResultCommand ?? (exportAlignmentResultCommand = new DelegateCommand<Window>(ExportAlignment));
        private DelegateCommand<Window> exportAlignmentResultCommand;

        private void ExportAlignment(Window owner) {
            var vm = new AlignmentResultExportVM(model.AlignmentFile, model.Storage.AlignmentFiles, model.Storage);
            var dialog = new AlignmentResultExportWin
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            dialog.ShowDialog();
        }

        private bool ReadDisplayFilter(DisplayFilter flag) {
            return displayFilters.Read(flag);
        }

        private void WriteDisplayFilter(DisplayFilter flag, bool set) {
            displayFilters.Write(flag, set);
            OnPropertyChanged(nameof(displayFilters));
        }
    }
}
