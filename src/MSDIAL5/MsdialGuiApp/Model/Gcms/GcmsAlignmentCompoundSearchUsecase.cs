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
using System.Threading;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAlignmentCompoundSearchUsecase : BindableBase, ICancellableCompoundSearchUsecase<GcmsCompoundResult, PeakSpotModel>
    {
        private readonly CalculateMatchScore? _calculateMatchScore;
        private readonly GcmsCompoundSearchCache? _cache;

        public GcmsAlignmentCompoundSearchUsecase(CalculateMatchScore? calculateMatchScore)
        {
            _cache = calculateMatchScore is null ? null : GcmsCompoundSearchCache.For(calculateMatchScore);
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

        public IReadOnlyList<GcmsCompoundResult> Search(PeakSpotModel target) => Search(target, default);

        public IReadOnlyList<GcmsCompoundResult> Search(PeakSpotModel target, CancellationToken token) {
            if (_calculateMatchScore is null) {
                return Array.Empty<GcmsCompoundResult>();
            }
            return _cache!.Search(target.MsScan, SearchParameter!, (scan, parameter) => {
                var calculator = _calculateMatchScore.With(parameter);
                var compounds = calculator.CalculateMatches(scan, token)
                    .OrderByDescending(r => r.TotalScore)
                    .Select(result => new GcmsCompoundResult(calculator.Reference(result), result))
                    .ToArray();
                foreach (var compound in compounds) {
                    ((ICompoundResult)compound).MatchResult.Source |= SourceType.Manual;
                }
                return compounds;
            }, token);
        }
    }
}
