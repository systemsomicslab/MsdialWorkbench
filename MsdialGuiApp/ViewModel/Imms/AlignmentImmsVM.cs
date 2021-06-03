using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Reactive.Bindings.Extensions;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;


namespace CompMs.App.Msdial.ViewModel.Imms
{
    class AlignmentImmsVM : AlignmentFileVM
    {
        public AlignmentImmsVM(
            ImmsAlignmentModel model,
            IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator,
            IAnnotator<AlignmentSpotProperty, MSDecResult> textDBAnnotator) {

            this.model = model;

            PlotViewModel = new Chart.AlignmentPeakPlotViewModel(model.PlotModel).AddTo(Disposables);
            Ms2SpectrumViewModel = new Chart.MsSpectrumViewModel(model.Ms2SpectrumModel).AddTo(Disposables);
            BarChartViewModel = new Chart.BarChartViewModel(model.BarChartModel).AddTo(Disposables);
            AlignmentEicViewModel = new Chart.AlignmentEicViewModel(model.AlignmentEicModel).AddTo(Disposables);

            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;

            MassLower = model.Ms1Spots.Min(spot => spot.MassCenter);
            MassUpper = model.Ms1Spots.Max(spot => spot.MassCenter);
            Ms1Spots = CollectionViewSource.GetDefaultView(model.PlotModel.Spots);
        }

        private readonly ImmsAlignmentModel model;

        public Chart.AlignmentPeakPlotViewModel PlotViewModel {
            get => plotViewModel;
            set => SetProperty(ref plotViewModel, value);
        }
        private Chart.AlignmentPeakPlotViewModel plotViewModel;

        public Chart.MsSpectrumViewModel Ms2SpectrumViewModel {
            get => ms2SpectrumViewModel;
            set => SetProperty(ref ms2SpectrumViewModel, value);
        }
        private Chart.MsSpectrumViewModel ms2SpectrumViewModel;

        public Chart.BarChartViewModel BarChartViewModel {
            get => barChartViewModel;
            set => SetProperty(ref barChartViewModel, value);
        }
        private Chart.BarChartViewModel barChartViewModel;

        public Chart.AlignmentEicViewModel AlignmentEicViewModel {
            get => alignmentEicViewModel;
            set => SetProperty(ref alignmentEicViewModel, value);
        }
        private Chart.AlignmentEicViewModel alignmentEicViewModel;

        public ICollectionView Ms1Spots {
            get => ms1Spots;
            set {
                var old = ms1Spots;
                if (SetProperty(ref ms1Spots, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Spots != null) ms1Spots.Filter += PeakFilter;
                }
            }
        }
        private ICollectionView ms1Spots;

        public double MassLower {
            get => massLower;
            set {
                if (SetProperty(ref massLower, value))
                    Ms1Spots?.Refresh();
            }
        }

        public double MassUpper {
            get => massUpper;
            set {
                if (SetProperty(ref massUpper, value))
                    Ms1Spots?.Refresh();
            }
        }
        private double massLower, massUpper;

        public AlignmentSpotPropertyModel Target {
            get => target;
            set {
                if (SetProperty(ref target, value))
                    ;// SearchCompoundCommand.RaiseCanExecuteChanged();
            }
        }
        private AlignmentSpotPropertyModel target;

        public bool RefMatchedChecked => ReadDisplayFilters(DisplayFilter.RefMatched);
        public bool SuggestedChecked => ReadDisplayFilters(DisplayFilter.Suggested);
        public bool UnknownChecked => ReadDisplayFilters(DisplayFilter.Unknown);
        public bool Ms2AcquiredChecked => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
        public bool MolecularIonChecked => ReadDisplayFilters(DisplayFilter.MolecularIon);
        public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);
        public bool CcsChecked => ReadDisplayFilters(DisplayFilter.CcsMatched);
        public bool ManuallyModifiedChecked => ReadDisplayFilters(DisplayFilter.ManuallyModified);

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set {
                if (SetProperty(ref displayFilters, value))
                    Ms1Spots?.Refresh();
            }
        }
        private DisplayFilter displayFilters = 0;

        private readonly IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator, textDBAnnotator;

        bool PeakFilter(object obj) {
            if (obj is AlignmentSpotPropertyModel spot) {
                return AnnotationFilter(spot)
                    && MzFilter(spot)
                    && (!Ms2AcquiredChecked || spot.IsMsmsAssigned)
                    && (!MolecularIonChecked || spot.IsBaseIsotopeIon)
                    && (!BlankFilterChecked || spot.IsBlankFiltered)
                    && (!ManuallyModifiedChecked || spot.innerModel.IsManuallyModifiedForAnnotation);
            }
            return false;
        }

        bool AnnotationFilter(AlignmentSpotPropertyModel spot) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && spot.IsRefMatched
                || SuggestedChecked && spot.IsSuggested
                || UnknownChecked && spot.IsUnknown;
        }

        bool MzFilter(AlignmentSpotPropertyModel spot) {
            return MassLower <= spot.MassCenter
                && spot.MassCenter <= MassUpper;
        }

        /*
        public DelegateCommand<Window> SearchCompoundCommand => searchCompoundCommand ?? (searchCompoundCommand = new DelegateCommand<Window>(SearchCompound, CanSearchCompound));
        private DelegateCommand<Window> searchCompoundCommand;

        private void SearchCompound(Window owner) {
            if (Target?.innerModel == null)
                return;

            var vm = new CompoundSearchVM<AlignmentSpotProperty>(alignmentFile, Target.innerModel, msdecResult, null, mspAnnotator);
            var window = new View.Imms.CompoundSearchWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            window.ShowDialog();
        }

        private bool CanSearchCompound(Window owner) {
            if (Target?.innerModel == null) {
                return false;
            }
            return true;
        }
        */

        /*
        public DelegateCommand<Window> ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand<Window>(ShowIonTable));
        private DelegateCommand<Window> showIonTableCommand;

        private void ShowIonTable(Window owner) {
            var window = new View.Imms.IonTableViewer
            {
                DataContext = this,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = owner,
            };

            window.Show();
        }
        */

        public void SaveProject() {
            model.SaveProject();
        }

        private bool ReadDisplayFilters(DisplayFilter flags) {
            return DisplayFilters.Read(flags);
        }
    }
}
