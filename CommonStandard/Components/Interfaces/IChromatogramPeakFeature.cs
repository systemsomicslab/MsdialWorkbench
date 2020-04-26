using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Interfaces {
    public interface IChromatogramPeakFeature {

        // basic property
        long ChromScanIdLeft { get; set; }
        long ChromScanIdTop { get; set; }
        long ChromScanIdRight { get; set; }
        Times TimesLeft { get; set; }
        Times TimesTop { get; set; }
        Times TimesRight { get; set; }
        float PeakHeightLeft { get; set; }
        float PeakHeightTop { get; set; }
        float PeakHeightRight { get; set; }
        float PeakAreaAboveZero { get; set; }
        float PeakAreaAboveBaseline { get; set; }
        float EstimatedNoise { get; set; }
        float SignalToNoise { get; set; }

        // peak feature
        float PeakPureValue { get; set; }
        float ShapenessValue { get; set; }
        float GaussianSimilarityValue { get; set; }
        float IdealSlopeValue { get; set; }
        float BasePeakValue { get; set; }
        float SymmetryValue { get; set; }

        // link to raw data
        long MS1RawSpectrumID { get; set; }
        List<long> MS2RawSpectrumIDs { get; set; } 

    }
}
