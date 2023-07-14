using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;

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

        public bool PeakFilter<T>(T filterable, IMatchResultEvaluator<T> evaluator) where T: IFilterable, IAnnotatedObject {
            return AnnotationFilter(filterable, evaluator) && OtherFilters(filterable) && MscleanrFilter(filterable);
        }

        public bool AnnotationFilter<T>(T spot, IMatchResultEvaluator<T> evaluator) where T: IAnnotatedObject {
            var checkedFilter = CheckedFilter;
            if (!checkedFilter.Any(DisplayFilter.Annotates)) return true;
            return checkedFilter.All(DisplayFilter.RefMatched) && evaluator.IsReferenceMatched(spot)
                || checkedFilter.All(DisplayFilter.Suggested) && evaluator.IsAnnotationSuggested(spot)
                || checkedFilter.All(DisplayFilter.Unknown) && spot.MatchResults.IsUnknown;
        }

        public bool OtherFilters<T>(T filterable) where T: IFilterable {
            var checkedFilter = CheckedFilter;
            return (!checkedFilter.All(DisplayFilter.Ms2Acquired) || filterable.IsMsmsAssigned)
                && (!checkedFilter.All(DisplayFilter.MolecularIon) || filterable.IsMonoIsotopicIon)
                && (!checkedFilter.All(DisplayFilter.Blank) || !filterable.IsBlankFiltered)
                && (!checkedFilter.All(DisplayFilter.UniqueIons) || filterable.IsFragmentQueryExisted)
                && (!checkedFilter.All(DisplayFilter.ManuallyModified) || filterable.IsManuallyModifiedForAnnotation);
        }

        public bool MscleanrFilter<T>(T filterable) where T: IFilterable {
            var checkedFilter = CheckedFilter;
            return (!checkedFilter.All(DisplayFilter.MscleanrBlank) || !filterable.IsBlankFilteredByPostCurator)
                && (!checkedFilter.All(DisplayFilter.MscleanrBlankGhost) || !filterable.IsBlankGhostFilteredByPostCurator)
                && (!checkedFilter.All(DisplayFilter.MscleanrIncorrectMass) || !filterable.IsMzFilteredByPostCurator)
                && (!checkedFilter.All(DisplayFilter.MscleanrRsd) || !filterable.IsRsdFilteredByPostCurator)
                && (!checkedFilter.All(DisplayFilter.MscleanrRmd) || !filterable.IsRmdFilteredByPostCurator);
        }
    }
}
