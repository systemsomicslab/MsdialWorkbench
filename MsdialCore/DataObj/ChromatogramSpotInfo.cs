using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CompMs.Common.Components;

namespace CompMs.MsdialCore.DataObj
{
    public class ChromatogramSpotInfo
    {
        public ReadOnlyCollection<ChromatogramPeakInfo> PeakInfos { get; }
        public ChromXs ChromXs { get; }

        public ChromatogramSpotInfo(IEnumerable<ChromatogramPeakInfo> peakinfos, ChromXs chromXs) {
            PeakInfos = new ReadOnlyCollection<ChromatogramPeakInfo>(peakinfos.ToList());
            ChromXs = chromXs;
        }
    }
}
