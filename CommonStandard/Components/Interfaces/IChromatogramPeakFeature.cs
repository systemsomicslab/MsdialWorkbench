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
        ChromXs ChromXsLeft { get; set; }
        ChromXs ChromXsTop { get; set; }
        ChromXs ChromXsRight { get; set; }
        double PeakHeightLeft { get; set; }
        double PeakHeightTop { get; set; }
        double PeakHeightRight { get; set; }
        double PeakAreaAboveZero { get; set; }
        double PeakAreaAboveBaseline { get; set; }
        double Mass { get; set; }
        double PeakWidth(ChromXType type);
    }
}
