using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Dims
{
    internal sealed class DimsCompoundSearchService : BindableBase
    {
        public DimsCompoundSearchService(IReadOnlyList<CompoundSearcher> compoundSearchers) {
            CompoundSearchers = compoundSearchers;
        }

        public IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        public CompoundSearcher SelectedCompoundSearcher {
            get => _selectedCompoundSearcher;
            set => SetProperty(ref _selectedCompoundSearcher, value);
        }
        private CompoundSearcher _selectedCompoundSearcher;

        public DimsCompoundResult[] Search(PeakSpotModel peakSpot) {
            var results = SelectedCompoundSearcher.Search(
                peakSpot.PeakSpot.MSIon,
                peakSpot.MSDecResult,
                new List<RawPeakElement>(),
                new IonFeatureCharacter { IsotopeWeightNumber = 0, } // Assume this is not isotope.
            );
            if (results is null) {
                return System.Array.Empty<DimsCompoundResult>();
            }
            return results.Select(r => new DimsCompoundResult(r)).ToArray();
        }
    }
}
