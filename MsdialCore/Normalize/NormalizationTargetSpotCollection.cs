using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.Normalize
{
    internal class NormalizationTargetSpotCollection
    {
        private readonly IReadOnlyList<AlignmentSpotProperty> _spots;
        private readonly List<AlignmentSpotProperty> _orderedSpots;

        public NormalizationTargetSpotCollection(IReadOnlyList<AlignmentSpotProperty> spots) {
            _spots = spots ?? throw new ArgumentNullException(nameof(spots));
            _orderedSpots = _spots
                .SelectMany(spot => spot.AlignmentDriftSpotFeatures is null ? IEnumerableExtension.Return(spot) : spot.AlignmentDriftSpotFeatures.Prepend(spot))
                .OrderBy(spot => spot.MasterAlignmentID)
                .ToList();
            TargetSpots = _spots.Select(spot => new NormalizationTargetSpot(spot)).ToList().AsReadOnly();
        }

        public IReadOnlyList<AlignmentSpotProperty> Spots => _spots;

        public ReadOnlyCollection<NormalizationTargetSpot> TargetSpots { get; }

        public void Initialize() {
            foreach (var spot in _spots) {
                spot.InternalStandardAlignmentID = -1;
                foreach (var prop in spot.AlignedPeakProperties) {
                    prop.NormalizedPeakHeight = -1d;
                    prop.NormalizedPeakAreaAboveBaseline = -1d;
                    prop.NormalizedPeakAreaAboveZero = -1d;
                }
            }
        }

        public void NormalizeInternalStandardSpot(StandardCompound compound, IonAbundanceUnit unit) {
            var index = _orderedSpots.LowerBound(compound.PeakID, (spot, id) => spot.MasterAlignmentID.CompareTo(id));
            if (index >= _orderedSpots.Count || _orderedSpots[index].MasterAlignmentID != compound.PeakID) {
                return;
            }
            var targetSpot = _orderedSpots[index];
            targetSpot.IonAbundanceUnit = unit;
            targetSpot.InternalStandardAlignmentID = compound.PeakID;
            foreach (var prop in targetSpot.AlignedPeakProperties) {
                prop.NormalizedPeakHeight = compound.Concentration;
                prop.NormalizedPeakAreaAboveBaseline = compound.Concentration;
                prop.NormalizedPeakAreaAboveZero = compound.Concentration;
            }
        }

        public AlignmentSpotProperty FindSpot(int spotId) {
            var index = _orderedSpots.LowerBound(spotId, (spot, id) => spot.MasterAlignmentID.CompareTo(id));
            if (index >= 0 && index < _orderedSpots.Count) {
                return _orderedSpots[index];
            }
            return null;
        }

        public double LowessSpanTune(IReadOnlyList<AnalysisFileBean> files) {
            return 0d;
        }
    }
}
