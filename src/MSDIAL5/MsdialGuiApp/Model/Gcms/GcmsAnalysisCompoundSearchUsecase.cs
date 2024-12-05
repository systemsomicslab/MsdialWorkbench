using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialGcMsApi.Algorithm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAnalysisCompoundSearchUsecase : BindableBase, ICompoundSearchUsecase<GcmsCompoundResult, Ms1BasedSpectrumFeature>
    {
        private readonly CalculateMatchScore? _calculateMatchScore;

        public GcmsAnalysisCompoundSearchUsecase(CalculateMatchScore? calculateMatchScore)
        {
            SearchParameter = calculateMatchScore?.CopySearchParameter();
            _calculateMatchScore = SearchParameter is { }
                ? calculateMatchScore?.With(SearchParameter)
                : default;
        }

        public IList SearchMethods => Array.Empty<object>();

        public object? SearchMethod {
            get => _searchMethod;
            set {
                if (SearchMethod != value && SearchMethods.Contains(value)) {
                    _searchMethod = value;
                    OnPropertyChanged(nameof(SearchMethod));
                }
            }
        }
        private object? _searchMethod = null;

        public MsRefSearchParameterBase? SearchParameter { get; }

        public IReadOnlyList<GcmsCompoundResult> Search(Ms1BasedSpectrumFeature target) {
            if (_calculateMatchScore is null) {
                return Array.Empty<GcmsCompoundResult>();
            }
            var compounds = _calculateMatchScore.CalculateMatches(target.Scan)
                    .OrderByDescending(r => r.TotalScore)
                    .Select(result => new GcmsCompoundResult(_calculateMatchScore.Reference(result), result))
                    .ToArray();
            foreach (var compound in compounds) {
                ((ICompoundResult)compound).MatchResult.Source |= SourceType.Manual;
            }
            return compounds;
        }
    }
}
