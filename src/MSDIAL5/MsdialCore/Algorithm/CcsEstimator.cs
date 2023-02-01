using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm {
    public static class CcsEstimator {
        public static void Process(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            ParameterBase param, IonMobilityType type, CoefficientsForCcsCalculation calinfo, bool isAllCalibrantDataImported = false) {

            foreach (var chromPeak in chromPeakFeatures) { // to deal with both IM-MS and LC-IM-MS
                var chromXs = chromPeak.ChromXs;
                if (chromXs.Drift.Value > 0) {
                    chromPeak.CollisionCrossSection = IonMobilityUtility.MobilityToCrossSection(type, chromXs.Drift.Value, 
                        Math.Abs(chromPeak.PeakCharacter.Charge), chromPeak.Mass, calinfo, isAllCalibrantDataImported);
                }
                foreach (var driftPeak in chromPeak.DriftChromFeatures.OrEmptyIfNull()) { // for LC-IM-MS
                    if (driftPeak.ChromXs.Drift.Value > 0) {
                        driftPeak.CollisionCrossSection = IonMobilityUtility.MobilityToCrossSection(type, driftPeak.ChromXs.Drift.Value,
                            Math.Abs(chromPeak.PeakCharacter.Charge), chromPeak.Mass, calinfo, isAllCalibrantDataImported);
                    }
                }
            }
        }
    }
}
