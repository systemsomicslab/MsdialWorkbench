using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Interfaces {
    public interface IChromatogramPeakFeature {

        // basic property
        int ChromScanIdLeft { get; set; }
        int ChromScanIdTop { get; set; }
        int ChromScanIdRight { get; set; }
        Times TimesLeft { get; set; }
        Times TimesTop { get; set; }
        Times TimesRight { get; set; }
        float PeakHeightLeft { get; set; }
        float PeakHeightTop { get; set; }
        float PeakHeightRight { get; set; }
        float PeakAreaAboveZero { get; set; }
        float PeakAreaAboveBaseline { get; set; }
    }
}
