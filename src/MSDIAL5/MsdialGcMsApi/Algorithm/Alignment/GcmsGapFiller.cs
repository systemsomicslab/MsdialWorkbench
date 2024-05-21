using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment
{
    public abstract class GcmsGapFiller : GapFiller
    {
        private readonly List<AnalysisFileBean> files;
        private readonly MsdialGcmsParameter param;
        protected readonly AlignmentIndexType indexType;
        private List<MoleculeMsReference> mspDB;
        private readonly bool isRepresentativeQuantMassBasedOnBasePeakMz;

        private bool IsReplaceMode => !mspDB.IsEmptyOrNull();
        private int Bin => param?.AccuracyType == AccuracyType.IsAccurate ? 2 : 0;

        public GcmsGapFiller(List<AnalysisFileBean> files, List<MoleculeMsReference> mspDB, MsdialGcmsParameter param) : base(param) {
            this.files = files;
            this.mspDB = mspDB;
            this.param = param;
            this.isRepresentativeQuantMassBasedOnBasePeakMz = param.IsRepresentativeQuantMassBasedOnBasePeakMz;

            indexType = param.AlignmentIndexType;
        }

        protected double GetQuantmass(AlignmentChromPeakFeature[] peaks) {
            var repFileID = DataObjConverter.GetRepresentativeFileID(peaks);
            var repPeak = peaks.FirstOrDefault(peak => peak.FileID == repFileID);
            var mspId = repPeak.MspID();
            
            if (IsReplaceMode && repFileID >= 0 && mspId >= 0) {
                var refQuantMass = mspDB[mspId].QuantMass;
                if (param.MassRangeBegin <= refQuantMass && refQuantMass <= param.MassRangeEnd) {
                    return refQuantMass;
                }
            }

            var dclFile = files[repFileID].DeconvolutionFilePath;
            var msdecResult = MsdecResultsReader.ReadMSDecResult(dclFile, repPeak.SeekPointToDCLFile);
            var spectrum = msdecResult.Spectrum;
            var basepeak = GetBasePeak(spectrum);
            if (isRepresentativeQuantMassBasedOnBasePeakMz) {
                return basepeak.Mass;
            }

            var bin = Bin;
            var maxPeakHeight = peaks.Max(peak => peak.PeakHeightTop);
            var quantMassGroup = peaks.Where(peak => peak.PeakHeightTop > maxPeakHeight * 0.1)
                                      .GroupBy(peak => Math.Round(peak.Mass, bin));
            var quantMassCandidate = quantMassGroup.Argmax(group => group.Count()).Average(peak => peak.Mass);
            var isQuantMassExist = SuitableQuantMassExists(quantMassCandidate, basepeak.Intensity, spectrum, param.CentroidMs1Tolerance, 10.0F * 0.01);
            if (isQuantMassExist) {
                return quantMassCandidate;
            }

            var repQuantMass = repPeak.Mass;
            var isSuitableQuantMassExist = SuitableQuantMassExists(repQuantMass, basepeak.Intensity, spectrum, bin, 10.0 * 0.01);
            if (isSuitableQuantMassExist)
                return repQuantMass;

            return basepeak.Mass;
        }

        protected static SpectrumPeak GetBasePeak(List<SpectrumPeak> spectrum) {
            return spectrum.Argmax(peak => peak.Intensity);
        }

        // spectrum should be ordered by m/z value
        protected static bool SuitableQuantMassExists(double mass, double intensity, List<SpectrumPeak> spectrum, double bin, double threshold) {
            return spectrum.Where(peak => mass - bin <= peak.Mass)
                           .TakeWhile(peak => peak.Mass <= mass + bin)
                           .Any(peak => Math.Abs(peak.Mass - mass) <= bin
                                     && peak.Intensity > intensity * threshold);
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

        protected override ChromXs GetCenter(IEnumerable<AlignmentChromPeakFeature> peaks) {
            var peaklist = peaks as AlignmentChromPeakFeature[] ?? peaks.ToArray();
            return new ChromXs(peaklist.Average(peak => peak.ChromXsTop.RT.Value), ChromXType.RT, ChromXUnit.Min)
            {
                RI = new RetentionIndex(peaklist.Average(peak => peak.ChromXsTop.RI.Value)),
                Mz = new MzValue(GetQuantmass(peaklist)),
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
            var maxRt = centralRT + peakWidth * 0.5; // temp
            var minRt = centralRT - peakWidth * 0.5; // temp
            var rtTol = maxRt.Value - minRt.Value;

            var range = new ChromatogramRange(centralRT.Value, centralRT.Value, ChromXType.RT, ChromXUnit.Min).ExtendWith(rtTol * 3);
            using (var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(centralMz, _parameter.CentroidMs1Tolerance), range))
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
            _fileIdToHandler = parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary.ToDictionary(kvp => kvp.Key, kvp => new RetentionIndexHandler(parameter.RiCompoundType, kvp.Value.RiDictionary));
        }

        protected override ChromXs GetCenter(IEnumerable<AlignmentChromPeakFeature> peaks) {
            var peaklist = peaks as AlignmentChromPeakFeature[] ?? peaks.ToArray();
            return new ChromXs(peaklist.Average(peak => peak.ChromXsTop.RI.Value), ChromXType.RI, ChromXUnit.None)
            {
                RT = new RetentionTime(peaklist.Average(peak => peak.ChromXsTop.RT.Value)),
                Mz = new MzValue(GetQuantmass(peaklist)),
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
            var maxRt = riHandler.ConvertBack(center.RI + peakWidth * 0.5); // temp
            var minRt = riHandler.ConvertBack(center.RI - peakWidth * 0.5); // temp
            var rtTol = maxRt.Value - minRt.Value;

            var range = new ChromatogramRange(centralRT.Value, centralRT.Value, ChromXType.RT, ChromXUnit.Min).ExtendWith(rtTol * 3);
            using (var chromatogram = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(centralMz, _parameter.CentroidMs1Tolerance), range))
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
}
