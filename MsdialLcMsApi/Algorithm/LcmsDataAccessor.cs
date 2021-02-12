using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm
{
    class LcmsDataAccessor : DataAccessor
    {
        static readonly IComparer<IMSScanProperty> Comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);

        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, IReadOnlyList<RawSpectrum> spectrum) {
            var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
            var peaklist = DataAccess.GetMs1Peaklist(
                spectrum, (float)peak.Mass,
                (float)(detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f,
                peak.IonMode);
            return new ChromatogramPeakInfo(
                peak.FileID, peaklist,
                (float)peak.ChromXsTop.RT.Value, (float)peak.ChromXsLeft.RT.Value, (float)peak.ChromXsRight.RT.Value);
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            chromatogram.Sort(Comparer);
            return new List<IMSScanProperty>(chromatogram);
        }
    }
}
