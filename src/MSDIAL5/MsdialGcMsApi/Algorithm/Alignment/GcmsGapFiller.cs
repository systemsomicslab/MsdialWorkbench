using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment;

public abstract class GcmsGapFiller : GapFiller
{
    private readonly List<AnalysisFileBean> _files;
    private readonly MsdialGcmsParameter _param;
    protected readonly AlignmentIndexType indexType;
    private List<MoleculeMsReference> _mspDB;
    private readonly bool _isRepresentativeQuantMassBasedOnBasePeakMz;

    private bool IsReplaceMode => !_mspDB.IsEmptyOrNull();
    private int Bin => _param?.AccuracyType == AccuracyType.IsAccurate ? 2 : 0;

    public GcmsGapFiller(List<AnalysisFileBean> files, List<MoleculeMsReference> mspDB, MsdialGcmsParameter param) : base(param) {
        _files = files;
        _mspDB = mspDB;
        _param = param;
        _isRepresentativeQuantMassBasedOnBasePeakMz = param.IsRepresentativeQuantMassBasedOnBasePeakMz;

        indexType = param.AlignmentIndexType;
    }

    public override bool NeedsGapFill(AlignmentSpotProperty spot, AnalysisFileBean analysisFile) {
        var peak = spot.AlignedPeakProperties.First(p => p.FileID == analysisFile.AnalysisFileId);
        return peak.Mass != spot.QuantMass;
    }

    protected static SpectrumPeak GetBasePeak(List<SpectrumPeak> spectrum) {
        return spectrum.Argmax(peak => peak.Intensity);
    }
}

public class GcmsRTGapFiller : GcmsGapFiller
{
    private readonly MsdialGcmsParameter _parameter;


    private readonly double rtTol;
    protected override double AxTol => rtTol;

    public GcmsRTGapFiller(List<AnalysisFileBean> files, List<MoleculeMsReference> mspDB, MsdialGcmsParameter parameter) : base(files, mspDB, parameter) {
        _parameter = parameter;
        rtTol = parameter.RetentionTimeAlignmentTolerance;
    }

    protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
        var peaklist = peaks as AlignmentChromPeakFeature[] ?? peaks.ToArray();
        return new ChromXs(peaklist.Average(peak => peak.ChromXsTop.RT.Value), ChromXType.RT, ChromXUnit.Min)
        {
            RI = new RetentionIndex(peaklist.Average(peak => peak.ChromXsTop.RI.Value)),
            Mz = new MzValue(spot.QuantMass),
        };
    }

    protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
        return peaks.Max(peak => peak.PeakWidth(ChromXType.RT));
    }

    /// <summary>
    /// peak width is RT range
    /// </summary>
    /// <param name="center"></param>
    /// <param name="peakWidth"></param>
    /// <param name="spectrumList"></param>
    /// <param name="fileID"></param>
    /// <returns></returns>
    protected override List<ChromatogramPeak> GetPeaks(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
        var centralMz = center.Mz.Value;
        // RT conversion
        var centralRT = center.RT;
        var maxRt = centralRT + peakWidth * 2d; // temp
        var minRt = centralRT - peakWidth * 2d; // temp

        var range = ChromatogramRange.FromTimes(minRt, maxRt);
        using (var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(centralMz, _parameter.PeakPickBaseParam.CentroidMs1Tolerance), range))
        using (Chromatogram smoothed = chromatogram.ChromatogramSmoothing(smoothingMethod, smoothingLevel)) {
            return smoothed.AsPeakArray();
        }
    }
}

public class GcmsRIGapFiller : GcmsGapFiller
{
    private readonly MsdialGcmsParameter _parameter;

    private readonly double _riTol;
    private readonly Dictionary<int, RetentionIndexHandler> _fileIdToHandler;

    protected override double AxTol => _riTol;

    public GcmsRIGapFiller(List<AnalysisFileBean> files, List<MoleculeMsReference> mspDB, MsdialGcmsParameter parameter) : base(files, mspDB, parameter) {
        _parameter = parameter;
        _riTol = parameter.RetentionIndexAlignmentTolerance;
        _fileIdToHandler = parameter.GetRIHandlers();
    }

    protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
        var peaklist = peaks as AlignmentChromPeakFeature[] ?? peaks.ToArray();
        return new ChromXs(peaklist.Average(peak => peak.ChromXsTop.RI.Value), ChromXType.RI, ChromXUnit.None)
        {
            RT = new RetentionTime(peaklist.Average(peak => peak.ChromXsTop.RT.Value)),
            Mz = new MzValue(spot.QuantMass),
        };
    }

    protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
        return peaks.Max(peak => peak.PeakWidth(ChromXType.RI));
    }

    /// <summary>
    /// peak width is RI range
    /// </summary>
    /// <param name="ms1Spectra"></param>
    /// <param name="rawSpectra"></param>
    /// <param name="spectrum"></param>
    /// <param name="center"></param>
    /// <param name="peakWidth"></param>
    /// <param name="fileID"></param>
    /// <param name="smoothingMethod"></param>
    /// <param name="smoothingLevel"></param>
    /// <returns></returns>
    protected override List<ChromatogramPeak> GetPeaks(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
        var centralMz = center.Mz.Value;
        // RT conversion
        var riHandler = _fileIdToHandler[fileID];
        var centralRT = riHandler.ConvertBack(center.RI);
        var maxRt = riHandler.ConvertBack(center.RI + peakWidth * 2d); // temp
        var minRt = riHandler.ConvertBack(center.RI - peakWidth * 2d); // temp
        var rtTol = maxRt.Value - minRt.Value;

        var range = ChromatogramRange.FromTimes(minRt, maxRt);
        using (var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(centralMz, _parameter.PeakPickBaseParam.CentroidMs1Tolerance), range))
        using (Chromatogram smoothed = chromatogram.ChromatogramSmoothing(smoothingMethod, smoothingLevel)) {
            var peaks = smoothed.AsPeakArray();
            foreach (var peak in peaks) {
                peak.ChromXs.RI = riHandler.Convert(peak.ChromXs.RT);
                peak.ChromXs.MainType = ChromXType.RI;
            }
            return peaks;
        }
    }
}
