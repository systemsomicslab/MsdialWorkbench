using CompMs.App.Msdial.Model.Imms;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm;
using Reactive.Bindings.Extensions;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsMethodVM : MethodVM
    {
        static ImmsMethodVM() {
            serializer = new MsdialImmsCore.Parser.MsdialImmsSerializer();
        }
        private static readonly MsdialSerializer serializer;

        public ImmsMethodVM(
            MsdialDataStorage storage,
            IWindowService<CompoundSearchVM> compoundSearchService)
            : base(serializer) {

            model = new ImmsMethodModel(storage, new ImmsAverageDataProviderFactory(0.001, 0.002, retry: 5, isGuiProcess: true)).AddTo(Disposables);
            this.compoundSearchService = compoundSearchService ?? throw new System.ArgumentNullException(nameof(compoundSearchService));

            AnalysisFilesView = CollectionViewSource.GetDefaultView(model.AnalysisFiles);
            AlignmentFilesView = CollectionViewSource.GetDefaultView(model.AlignmentFiles);

            PropertyChanged += OnDisplayFiltersChanged;
        }

        private readonly ImmsMethodModel model;

        private readonly IWindowService<CompoundSearchVM> compoundSearchService;

        public ICollectionView AnalysisFilesView {
            get => analysisFilesView;
            set => SetProperty(ref analysisFilesView, value);
        }
        private ICollectionView analysisFilesView;

        public ICollectionView AlignmentFilesView {
            get => alignmentFilesView;
            set => SetProperty(ref alignmentFilesView, value);
        }
        private ICollectionView alignmentFilesView;

        public AnalysisImmsVM AnalysisVM {
            get => analysisVM;
            set {
                var old = analysisVM;
                if (SetProperty(ref analysisVM, value)) {
                    old?.Dispose();
                }
            }
        }
        private AnalysisImmsVM analysisVM;

        public AlignmentImmsVM AlignmentVM {
            get => alignmentVM;
            set {
                var old = alignmentVM;
                if (SetProperty(ref alignmentVM, value)) {
                    old?.Dispose();
                }
            }
        }
        private AlignmentImmsVM alignmentVM;

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

        private bool ReadDisplayFilter(DisplayFilter flag) {
            return displayFilters.Read(flag);
        }

        private void WriteDisplayFilter(DisplayFilter flag, bool set) {
            displayFilters.Write(flag, set);
            OnPropertyChanged(nameof(displayFilters));
        }

        public override int InitializeNewProject(Window window) {
            model.InitializeNewProject(window);

            LoadAnalysisFile(model.AnalysisFiles.FirstOrDefault());

            return 0;
        }

        public override void LoadProject() {
            model.LoadAnnotator();

            LoadSelectedAnalysisFile();
        }

        public DelegateCommand LoadAnalysisFileCommand {
            get => loadAnalysisFileCommand ?? (loadAnalysisFileCommand = new DelegateCommand(LoadSelectedAnalysisFile));
        }
        private DelegateCommand loadAnalysisFileCommand;

        private void LoadSelectedAnalysisFile() {
            if (analysisFilesView.CurrentItem is AnalysisFileBean analysis) {
                LoadAnalysisFile(analysis);
            }
        }

        public DelegateCommand LoadAlignmentFileCommand {
            get => loadAlignmentFileCommand ?? (loadAlignmentFileCommand = new DelegateCommand(LoadSelectedAlignmentFile));
        }
        private DelegateCommand loadAlignmentFileCommand;
        private void LoadSelectedAlignmentFile() {
            if (alignmentFilesView.CurrentItem is AlignmentFileBean alignment) {
                LoadAlignmentFile(alignment);
            }
        }

        private void LoadAnalysisFile(AnalysisFileBean analysis) {
            model.LoadAnalysisFile(analysis);

            if (AnalysisVM != null) {
                AnalysisVM.Dispose();
                Disposables.Remove(AnalysisVM);
            }
            AnalysisVM = new AnalysisImmsVM(
                model.AnalysisModel,
                analysis,
                model.MspChromatogramAnnotator,
                model.TextDBChromatogramAnnotator,
                compoundSearchService)
            {
                DisplayFilters = displayFilters
            };
            Disposables.Add(AnalysisVM);
        }

        private void LoadAlignmentFile(AlignmentFileBean alignment) {
            model.LoadAlignmentFile(alignment);

            if (AlignmentVM != null) {
                AlignmentVM?.Dispose();
                Disposables.Remove(AlignmentVM);
            }
            AlignmentVM = new AlignmentImmsVM(
                model.AlignmentModel,
                model.MspAlignmentAnnotator,
                model.TextDBAlignmentAnnotator,
                compoundSearchService)
            {
                DisplayFilters = displayFilters
            };
            Disposables.Add(AlignmentVM);
        }

        public override void SaveProject() {
            AlignmentVM?.SaveProject();
        }

        public DelegateCommand<Window> ExportAnalysisResultCommand => exportAnalysisResultCommand ?? (exportAnalysisResultCommand = new DelegateCommand<Window>(model.ExportAnalysis));
        private DelegateCommand<Window> exportAnalysisResultCommand;
        

        public DelegateCommand<Window> ExportAlignmentResultCommand => exportAlignmentResultCommand ?? (exportAlignmentResultCommand = new DelegateCommand<Window>(model.ExportAlignment));
        private DelegateCommand<Window> exportAlignmentResultCommand;
    }
}
