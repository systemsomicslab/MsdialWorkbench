using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;

namespace CompMs.App.Msdial.Model.Search
{
    public sealed class PeakFilterModel : BindableBase
    {
        public PeakFilterModel(DisplayFilter enabledFilter) {
            EnabledFilter = enabledFilter;
        }

        public DisplayFilter EnabledFilter { get; }

        public DisplayFilter CheckedFilter {
            get => _checkedFilter;
            set => SetProperty(ref _checkedFilter, value & EnabledFilter);
        }
        private DisplayFilter _checkedFilter;

        public bool PeakFilter(IFilterable filterable, IMatchResultEvaluator<IFilterable> evaluator) {
            var checkedFilter = CheckedFilter;
            return AnnotationFilter(filterable, evaluator)
                && (!checkedFilter.All(DisplayFilter.Ms2Acquired) || filterable.IsMsmsAssigned)
                && (!checkedFilter.All(DisplayFilter.MolecularIon) || filterable.IsBaseIsotopeIon)
                && (!checkedFilter.All(DisplayFilter.Blank) || !filterable.IsBlankFiltered)
                && (!checkedFilter.All(DisplayFilter.UniqueIons) || filterable.IsFragmentQueryExisted)
                && (!checkedFilter.All(DisplayFilter.ManuallyModified) || filterable.IsManuallyModifiedForAnnotation)
                && (!checkedFilter.All(DisplayFilter.MscleanrBlank) || !filterable.IsBlankFilteredByPostCurator)
                && (!checkedFilter.All(DisplayFilter.MscleanrBlankGhost) || !filterable.IsBlankGhostFilteredByPostCurator)
                && (!checkedFilter.All(DisplayFilter.MscleanrIncorrectMass) || !filterable.IsMzFilteredByPostCurator)
                && (!checkedFilter.All(DisplayFilter.MscleanrRsd) || !filterable.IsRsdFilteredByPostCurator)
                && (!checkedFilter.All(DisplayFilter.MscleanrRmd) || !filterable.IsRmdFilteredByPostCurator);
        }

        private bool AnnotationFilter(IFilterable spot, IMatchResultEvaluator<IFilterable> evaluator) {
            var checkedFilter = CheckedFilter;
            if (!checkedFilter.Any(DisplayFilter.Annotates)) return true;
            return checkedFilter.All(DisplayFilter.RefMatched) && evaluator.IsReferenceMatched(spot)
                || checkedFilter.All(DisplayFilter.Suggested) && evaluator.IsAnnotationSuggested(spot)
                || checkedFilter.All(DisplayFilter.Unknown) && spot.MatchResults.IsUnknown;
        }
    }
}
