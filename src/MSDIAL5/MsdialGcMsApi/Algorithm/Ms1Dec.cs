using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm
{
    public sealed class Ms1Dec {
        private readonly MsdialGcmsParameter _parameter;

        public Ms1Dec(MsdialGcmsParameter parameter) {
            _parameter = parameter;
        }

        public List<MSDecResult> GetMSDecResults(IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures, ReportProgress reporter) {
            return MSDecHandler.GetMSDecResults(spectrumList, chromPeakFeatures, _parameter, reporter);
        }

        public SpectrumFeatureCollection GetSpectrumFeaturesByQuantMassInformation(AnalysisFileBean file, IReadOnlyList<RawSpectrum> spectra, IReadOnlyList<AnnotatedMSDecResult> msdecResults) {
            var rawSpectra = new RawSpectra(spectra, _parameter.IonMode, file.AcquisitionType);

            var spectrumFeatures = new List<SpectrumFeature>(msdecResults.Count);
            foreach (var annotatedMSDecResult in msdecResults) {
                var quantifiedChromatogramPeak = MSDecHandler.GetChromatogramQuantInformation(rawSpectra, annotatedMSDecResult.MSDecResult, annotatedMSDecResult.QuantMass, _parameter);
                if (quantifiedChromatogramPeak is null) {
                    continue;
                }
                spectrumFeatures.Add(new SpectrumFeature(annotatedMSDecResult, quantifiedChromatogramPeak));
            }
            var order = 0;
            foreach (var peak in spectrumFeatures.OrderBy(s => s.QuantifiedChromatogramPeak.PeakFeature.PeakHeightTop)) {
                peak.QuantifiedChromatogramPeak.PeakShape.AmplitudeScoreValue = order / (float)(spectrumFeatures.Count - 1);
                peak.QuantifiedChromatogramPeak.PeakShape.AmplitudeOrderValue = order++;
            }
            return new SpectrumFeatureCollection(spectrumFeatures);
        }
    }
}
