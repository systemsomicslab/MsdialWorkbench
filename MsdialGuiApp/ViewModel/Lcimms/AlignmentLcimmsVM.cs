using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;


namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    public class AlignmentLcimmsVM : AlignmentFileVM
    {
        public AlignmentLcimmsVM(LcimmsAlignmentModel model) {
            this.model = model;
            Ms1Spots = CollectionViewSource.GetDefaultView(model.Ms1Spots);
            MassLower = model.MassMin;
            MassUpper = model.MassMax;

            WeakEventManager<LcimmsAlignmentModel, PropertyChangedEventArgs>.AddHandler(model, "PropertyChanged", (s, e) => SearchCompoundCommand.RaiseCanExecuteChanged());
        }

        public LcimmsAlignmentModel Model => model;
        private readonly LcimmsAlignmentModel model;

        public AlignmentPeakPlotVM RtPlotVM => rtPlotVM;
        private AlignmentPeakPlotVM rtPlotVM;

        public AlignmentPeakPlotVM DriftPlotVM => driftPlotVM;
        private AlignmentPeakPlotVM driftPlotVM;

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

        public bool RefMatchedChecked => ReadDisplayFilters(DisplayFilter.RefMatched);
        public bool SuggestedChecked => ReadDisplayFilters(DisplayFilter.Suggested);
        public bool UnknownChecked => ReadDisplayFilters(DisplayFilter.Unknown);
        public bool Ms2AcquiredChecked => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
        public bool MolecularIonChecked => ReadDisplayFilters(DisplayFilter.MolecularIon);
        public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);
        public bool CcsChecked => ReadDisplayFilters(DisplayFilter.CcsMatched);

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set {
                if (SetProperty(ref displayFilters, value))
                    Ms1Spots?.Refresh();
            }
        }
        private DisplayFilter displayFilters = 0;

        bool PeakFilter(object obj) {
            if (obj is AlignmentSpotPropertyModel spot) {
                return AnnotationFilter(spot)
                    && MzFilter(spot)
                    && (!Ms2AcquiredChecked || spot.IsMsmsAssigned)
                    && (!MolecularIonChecked || spot.IsBaseIsotopeIon)
                    && (!BlankFilterChecked || spot.IsBlankFiltered);
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

        public DelegateCommand<Window> SearchCompoundCommand => searchCompoundCommand ?? (searchCompoundCommand = new DelegateCommand<Window>(SearchCompound, CanSearchCompound));
        private DelegateCommand<Window> searchCompoundCommand;

        private void SearchCompound(Window owner) {
            if (model.Target?.innerModel == null)
                return;

            using (var vm = new CompoundSearchVM<AlignmentSpotProperty>(model.AlignmentFile, model.Target.innerModel, model.MsdecResult, null, model.MspAnnotator)) {
                var window = new View.CompoundSearchWindow
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                window.ShowDialog();
            }
        }

        private bool CanSearchCompound(Window owner) {
            if (model.Target?.innerModel == null) {
                return false;
            }
            return true;
        }

        /*
        public DelegateCommand<Window> ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand<Window>(ShowIonTable));
        private DelegateCommand<Window> showIonTableCommand;

        public void ShowIonTable(Window owner) {
            var window = new View.IonTableViewer
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