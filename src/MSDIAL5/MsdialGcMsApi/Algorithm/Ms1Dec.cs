using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task<SpectrumFeatureCollection> GetSpectrumFeaturesByQuantMassInformationAsync(AnalysisFileBean file, IReadOnlyList<RawSpectrum> spectra, IReadOnlyList<AnnotatedMSDecResult> msdecResults, IDataProvider spectraProvider, CancellationToken token = default) {
            var rawSpectra = new RawSpectra(spectra, _parameter.IonMode, file.AcquisitionType, spectraProvider);

            var spectrumFeatures = new List<SpectrumFeature>(msdecResults.Count);
            foreach (var annotatedMSDecResult in msdecResults) {
                token.ThrowIfCancellationRequested();
                var quantifiedChromatogramPeak = await MSDecHandler.GetChromatogramQuantInformationAsync(rawSpectra, annotatedMSDecResult.MSDecResult, annotatedMSDecResult.QuantMass, _parameter, token).ConfigureAwait(false);
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
