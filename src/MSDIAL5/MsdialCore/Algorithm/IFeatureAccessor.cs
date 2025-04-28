using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm;

public interface IFeatureAccessor<TScan> {
    List<TScan> GetMSScanProperties(AnalysisFileBean analysisFile);
    ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance);
}
