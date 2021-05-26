using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
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
            var peaklist = GetMs1Peaklist(
                spectrum, (float)peak.Mass,
                (float)(detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f,
                peak.IonMode);
            return new ChromatogramPeakInfo(
                peak.FileID, peaklist,
                peak.ChromXsTop.Drift, peak.ChromXsLeft.Drift, peak.ChromXsRight.Drift);
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
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
                DataAccess.RetrieveBinnedMzIntensity(massSpectra, targetMass, ms1Tolerance, out double basepeakMz, out double basepeakIntensity, out double summedIntensity);
                peaklist.Add(new ChromatogramPeak() { ID = spectrum.Index, ChromXs = new ChromXs(chromX, ChromXType.Drift, ChromXUnit.Msec), Mass = basepeakMz, Intensity = summedIntensity });
            }

            return peaklist;
        }
    }
}
