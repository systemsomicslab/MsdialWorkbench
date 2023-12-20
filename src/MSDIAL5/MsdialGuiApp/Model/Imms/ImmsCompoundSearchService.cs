using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsCompoundSearchService : BindableBase
    {
        public ImmsCompoundSearchService(IReadOnlyList<CompoundSearcher> compoundSearchers) {
            CompoundSearchers = compoundSearchers;
        }

        public IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        public CompoundSearcher SelectedCompoundSearcher {
            get => _compoundSearcher;
            set => SetProperty(ref _compoundSearcher, value);
        }
        private CompoundSearcher _compoundSearcher;

        public ImmsCompoundResult[] Search(PeakSpotModel peakSpot) {
            var results = SelectedCompoundSearcher?.Search(
                peakSpot.PeakSpot.MSIon,
                peakSpot.MSDecResult,
                new List<RawPeakElement>(),
                new IonFeatureCharacter { IsotopeWeightNumber = 0, } // Assume this is not isotope.
            );
            if (results is null) {
                return System.Array.Empty<ImmsCompoundResult>();
            }
            return results.Select(r => new ImmsCompoundResult(r)).ToArray();
        }
    }
}
