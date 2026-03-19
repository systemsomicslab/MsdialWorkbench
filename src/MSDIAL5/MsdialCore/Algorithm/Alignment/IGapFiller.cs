using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Alignment;

public interface IGapFiller
{
    bool NeedsGapFill(AlignmentSpotProperty spot, AnalysisFileBean analysisFile);
    void GapFill(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectra, AlignmentSpotProperty spot, int fileID);
}
