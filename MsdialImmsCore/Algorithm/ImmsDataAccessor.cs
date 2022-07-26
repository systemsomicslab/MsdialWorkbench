using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm
{
    class ImmsDataAccessor : DataAccessor
    {
        public ImmsDataAccessor(double massTolerance) {
            this.massTolerance = massTolerance;
        }

        private readonly double massTolerance;

        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance) {
            var detected = spot.AlignedPeakProperties.Where(prop => prop.MasterPeakID >= 0).ToArray();
            if (!detected.Any()) {
                throw new ArgumentException(nameof(spot));
            }
            var chromMax = detected.Max(x => x.ChromXsRight.Drift.Value);
            var chromMin = detected.Min(x => x.ChromXsLeft.Drift.Value);
            var tolerance = detected.Average(x => x.PeakWidth(ChromXType.Drift)) * 1.5f;
            var peaklist = GetMs1Peaklist(
                spectrum, (float)peak.Mass,
                massTolerance,
                peak.IonMode,
                chromMin - tolerance,
                chromMax + tolerance);
            return new ChromatogramPeakInfo(
                peak.FileID, peaklist,
                peak.ChromXsTop.Drift, peak.ChromXsLeft.Drift, peak.ChromXsRight.Drift);
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialPeakSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            return new List<IMSScanProperty>(chromatogram);
        }

        private static List<ChromatogramPeak> GetMs1Peaklist(
            IReadOnlyList<RawSpectrum> spectrumList,
            double targetMass,
            double ms1Tolerance,
            IonMode ionmode,
            double chromBegin = double.MinValue,
            double chromEnd = double.MaxValue) {

            if (spectrumList == null || spectrumList.Count == 0) return null;

            ScanPolarity scanPolarity;
            switch (ionmode) {
                case IonMode.Positive:
                    scanPolarity = ScanPolarity.Positive;
                    break;
                case IonMode.Negative:
                    scanPolarity = ScanPolarity.Negative;
                    break;
                case IonMode.Both:
                default:
                    throw new ArgumentException(nameof(ionmode));
            }

            var peaklist = new List<ChromatogramPeak>();
            foreach (var spectrum in spectrumList.Where(n => n.MsLevel <= 1 && n.ScanPolarity == scanPolarity)) {
                var chromX = spectrum.DriftTime;
                if (chromX < chromBegin) continue;
                if (chromX > chromEnd) break;
                var massSpectra = spectrum.Spectrum;
                //bin intensities for focused MZ +- ms1Tolerance
                (double basepeakMz, double basepeakIntensity, double summedIntensity) = new Spectrum(massSpectra).RetrieveBin(targetMass, ms1Tolerance);
                peaklist.Add(new ChromatogramPeak(spectrum.Index, basepeakMz, summedIntensity, new ChromXs(chromX, ChromXType.Drift, ChromXUnit.Msec)));
            }

            return peaklist;
        }
    }
}
