using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsCompoundSearchUsecase : BindableBase, ICancellableCompoundSearchUsecase<ImmsCompoundResult, PeakSpotModel>
    {
        public ImmsCompoundSearchUsecase(IReadOnlyList<CompoundSearcher> compoundSearchers) {
            CompoundSearchers = compoundSearchers;
            _selectedCompoundSearcher = CompoundSearchers.FirstOrDefault();
            _searchParameterBase = _selectedCompoundSearcher?.MsRefSearchParameter;
        }

        public IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        public CompoundSearcher? SelectedCompoundSearcher {
            get => _selectedCompoundSearcher;
            set {
                if (SetProperty(ref _selectedCompoundSearcher, value)) {
                    SearchParameter = _selectedCompoundSearcher?.MsRefSearchParameter;
                }
            }
        }
        private CompoundSearcher? _selectedCompoundSearcher;

        public IList SearchMethods => (CompoundSearchers as IList) ?? CompoundSearchers.ToArray();

        public object? SearchMethod {
            get => SelectedCompoundSearcher;
            set {
                if (SearchMethod != value && value is not null && SearchMethods.Contains(value)) {
                    SelectedCompoundSearcher = (CompoundSearcher?)value;
                    OnPropertyChanged(nameof(SearchMethod));
                }
            }
        }

        public MsRefSearchParameterBase? SearchParameter {
            get => _searchParameterBase;
            private set => SetProperty(ref _searchParameterBase, value);
        }
        private MsRefSearchParameterBase? _searchParameterBase;

        public IReadOnlyList<ImmsCompoundResult> Search(PeakSpotModel peakSpot) => Search(peakSpot, default);

        public IReadOnlyList<ImmsCompoundResult> Search(PeakSpotModel peakSpot, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            var results = SelectedCompoundSearcher?.Search(
                peakSpot.PeakSpot.MSIon,
                peakSpot.MsScan,
                new List<RawPeakElement>(),
                new IonFeatureCharacter { IsotopeWeightNumber = 0, }, // Assume this is not isotope.
                token
            );
            if (results is null) {
                return System.Array.Empty<ImmsCompoundResult>();
            }
            return results.Select(r => new ImmsCompoundResult(r)).ToArray();
        }
    }
}
