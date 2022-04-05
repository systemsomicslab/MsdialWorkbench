using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Search
{
    public class PeakFilterModel : BindableBase
    {
        public PeakFilterModel(DisplayFilter enabledFilter) {
            EnabledFilter = enabledFilter;
        }

        public DisplayFilter EnabledFilter { get; }

        public DisplayFilter CheckedFilter {
            get => checkedFilter;
            set => SetProperty(ref checkedFilter, value & EnabledFilter);
        }
        private DisplayFilter checkedFilter;

        public bool PeakFilter(IFilterable filterable, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var checkedFilter = CheckedFilter;
            return AnnotationFilter(filterable, evaluator)
                && (!checkedFilter.All(DisplayFilter.Ms2Acquired) || filterable.IsMsmsAssigned)
                && (!checkedFilter.All(DisplayFilter.MolecularIon) || filterable.IsBaseIsotopeIon)
                && (!checkedFilter.All(DisplayFilter.Blank) || !filterable.IsBlankFiltered)
                && (!checkedFilter.All(DisplayFilter.UniqueIons) || filterable.IsFragmentQueryExisted)
                && (!checkedFilter.All(DisplayFilter.ManuallyModified) || filterable.IsManuallyModifiedForAnnotation);
        }

        private bool AnnotationFilter(IAnnotatedObject spot, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var checkedFilter = CheckedFilter;
            if (!checkedFilter.Any(DisplayFilter.Annotates)) return true;
            return checkedFilter.All(DisplayFilter.RefMatched) && spot.MatchResults.IsReferenceMatched(evaluator)
                || checkedFilter.All(DisplayFilter.Suggested) && spot.MatchResults.IsAnnotationSuggested(evaluator)
                || checkedFilter.All(DisplayFilter.Unknown) && spot.MatchResults.IsUnknown;
        }
    }
}
