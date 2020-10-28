using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace MsdialDimsCoreUiTestApp.Model
{
    internal class Ms2Info {
        public ChromatogramPeakFeature ChromatogramPeakFeature { get; set; }
        public int PeakID { get; set; }
        public double Mass { get; set; }
        public double Intensity { get; set; }
        public List<SpectrumPeak> Centroids { get; set; }
        public MoleculeMsReference MspMatch {get;set;}
        public bool RefMatched { get; set; }
        public bool Suggested { get; set; }
        public bool Unknown { get; set; }
        public bool Ms2Acquired { get; set; }
    }
}
