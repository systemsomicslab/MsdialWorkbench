using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;

namespace CompMs.MsdialDimsCore.Algorithm
{
    public class DimsDataAccessor : DataAccessor
    {
        readonly IComparer<IMSScanProperty> Comparer = ChromXsComparer.MzComparer;

        public override ChromatogramPeakInfo AccumulateChromatogram(
            AlignmentChromPeakFeature target,
            AlignmentSpotProperty spot,
            Ms1Spectra ms1Spectra,
            IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance) {
            var detected = spot.AlignedPeakProperties.Where(peak => peak.MasterPeakID >= 0).ToList();
            var lo = detected.Min(peak => peak.ChromXsLeft.Value);
            var hi = detected.Max(peak => peak.ChromXsRight.Value);
            var tolerance = (hi - lo) * 1.5;
            var ms1Spectrum = spectrum
                .Where(spec => spec.MsLevel == 1 && !spec.Spectrum.IsEmptyOrNull())
                .Argmax(spec => spec.Spectrum.Length);
            var peaklist = DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum, target.Mass - tolerance, target.Mass + tolerance);

            return new ChromatogramPeakInfo(
                target.FileID, peaklist,
                target.ChromXsTop.Mz,
                target.ChromXsLeft.Mz,
                target.ChromXsRight.Mz);
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialPeakSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            chromatogram.Sort(Comparer);
            return chromatogram.Cast<IMSScanProperty>().ToList();
        }
    }
}
