using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm;

internal sealed class GcmsDataAccessor(MsdialGcmsParameter parameter) : DataAccessor, IFeatureAccessor<MSDecResult>, IFeatureAccessor<SpectrumFeature>
{
    private readonly IGcmsDataAccessor _accessorImpl = parameter.AlignmentIndexType switch
    {
        AlignmentIndexType.RI => new RiGcmsDataAccessorImpl(parameter),
        AlignmentIndexType.RT => new RtGcmsDataAccessorImpl(parameter),
        _ => new RtGcmsDataAccessorImpl(parameter),
    };

    public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
        return new List<IMSScanProperty>(_accessorImpl.GetMSScanProperties(analysisFile));
    }

    List<MSDecResult> IFeatureAccessor<MSDecResult>.GetMSScanProperties(AnalysisFileBean analysisFile) {
        return _accessorImpl.GetMSScanProperties(analysisFile);
    }

    List<SpectrumFeature> IFeatureAccessor<SpectrumFeature>.GetMSScanProperties(AnalysisFileBean analysisFile) {
        var collection = analysisFile.LoadSpectrumFeatures();
        return new List<SpectrumFeature>(collection.Items);
    }

    public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance) {
        return _accessorImpl.AccumulateChromatogram(peak, spot, ms1Spectra, spectrum, ms1MassTolerance);
    }

    interface IGcmsDataAccessor {
        List<MSDecResult> GetMSScanProperties(AnalysisFileBean analysisFile);
        ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance);
    }

    class RtGcmsDataAccessorImpl(MsdialGcmsParameter parameter) : IGcmsDataAccessor {
        private readonly MsdialGcmsParameter _parameter = parameter;
        private static readonly IComparer<IMSScanProperty> _comparer = ChromXsComparer.RTComparer;

        public List<MSDecResult> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var msdecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out _, out _);
            msdecResults.Sort(_comparer);
            return msdecResults;
        }

        public ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance) {
            var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0).ToArray();
            var rtMin = detected.Min(x => x.ChromXsTop.RT.Value);
            var rtMax = detected.Max(x => x.ChromXsTop.RT.Value);
            var rtPeakWidth = detected.Average(x => x.PeakWidth(ChromXType.RT));
            var rtLeft = rtMin - rtPeakWidth * 2F;
            var rtRight = rtMax + rtPeakWidth * 2F;
            if (rtRight - rtLeft > 5 && _parameter.AlignmentBaseParam.RetentionTimeAlignmentTolerance <= 2.5) {
                rtLeft = spot.TimesCenter.Value - 2.5;
                rtRight = spot.TimesCenter.Value + 2.5;
            }
            var chromatogramRange = new ChromatogramRange(rtLeft, rtRight, ChromXType.RT, ChromXUnit.Min);
            var chromatogram = ms1Spectra.GetMs1ExtractedChromatogram(spot.QuantMass, ms1MassTolerance, chromatogramRange);
            return new ChromatogramPeakInfo(
                peak.FileID, chromatogram.ChromatogramSmoothing(_parameter.PeakPickBaseParam.SmoothingMethod, _parameter.PeakPickBaseParam.SmoothingLevel).AsPeakArray(),
                (float)peak.ChromXsTop.RT.Value, (float)peak.ChromXsLeft.RT.Value, (float)peak.ChromXsRight.RT.Value);
        }
    }

    class RiGcmsDataAccessorImpl(MsdialGcmsParameter parameter) : IGcmsDataAccessor {
        private readonly MsdialGcmsParameter _parameter = parameter;
        private static readonly IComparer<IMSScanProperty> _comparer = ChromXsComparer.RIComparer;
        private readonly Dictionary<int, RetentionIndexHandler> _fileIdToHandler = parameter.GetRIHandlers();

        public List<MSDecResult> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var msdecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out _, out _);
            msdecResults.Sort(_comparer);
            return msdecResults;
        }

        public ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance) {
            var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0).ToArray();
            var riMin = detected.Min(x => x.ChromXsTop.RI);
            var riMax = detected.Max(x => x.ChromXsTop.RI);
            var riPeakWidth = detected.Average(x => x.PeakWidth(ChromXType.RI));
            var riLeft = riMin - riPeakWidth * 2F;
            var riRight = riMax + riPeakWidth * 2F;
            var handler = _fileIdToHandler[peak.FileID];
            
            var chromatogramRange = ChromatogramRange.FromTimes(handler.ConvertBack(riLeft), handler.ConvertBack(riRight));
            var chromatogram = ms1Spectra.GetMs1ExtractedChromatogram(spot.QuantMass, ms1MassTolerance, chromatogramRange);
            var smoothedChromatogram = chromatogram.ChromatogramSmoothing(_parameter.PeakPickBaseParam.SmoothingMethod, _parameter.PeakPickBaseParam.SmoothingLevel).AsPeakArray();
            foreach (var p in smoothedChromatogram) {
                p.ChromXs.RI = handler.Convert(p.ChromXs.RT);
                p.ChromXs.MainType = ChromXType.RI;
            }
            var peakInfo = new ChromatogramPeakInfo(peak.FileID, smoothedChromatogram, (float)peak.ChromXsTop.RI.Value, (float)peak.ChromXsLeft.RI.Value, (float)peak.ChromXsRight.RI.Value);
            return peakInfo;
        }
    }
}
