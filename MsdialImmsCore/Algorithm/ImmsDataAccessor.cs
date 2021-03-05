using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm
{
    class ImmsDataAccessor : DataAccessor
    {
        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, IReadOnlyList<RawSpectrum> spectrum) {
            var detected = spot.AlignedPeakProperties.Where(prop => prop.MasterPeakID >= 0);
            var peaklist = DataAccess.GetMs1Peaklist(
                spectrum, (float)peak.Mass,
                (float)(detected.Max(x => x.Mass) - detected.Min(ChromXsComparer => ChromXsComparer.Mass)) * 1.5f,
                peak.IonMode);
            return new ChromatogramPeakInfo(
                peak.FileID, peaklist,
                peak.ChromXsTop.Drift, peak.ChromXsLeft.Drift, peak.ChromXsRight.Drift);
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            return new List<IMSScanProperty>(chromatogram);
        }
    }
}
