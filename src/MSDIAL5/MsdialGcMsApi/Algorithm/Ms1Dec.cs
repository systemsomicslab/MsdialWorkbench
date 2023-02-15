using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialGcMsApi.Algorithm {
    public sealed class Ms1Dec {
        private readonly MsdialGcmsParameter _parameter;

        public Ms1Dec(MsdialGcmsParameter parameter) {
            _parameter = parameter;
        }

        public List<MSDecResult> GetMSDecResults(IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures, ReportProgress reporter) {
            return MSDecHandler.GetMSDecResults(spectrumList, chromPeakFeatures, _parameter, reporter.ReportAction, reporter.InitialProgress, reporter.ProgressMax);
        }

        public List<PeakDetectionResult> GetPeakDetectionResultsByQuantMassInformation(
            IReadOnlyList<RawSpectrum> spectra, IReadOnlyList<AnnotatedMSDecResult> features, IReadOnlyList<MSDecResult> msdecResults) {
            var rawSpectra = new RawSpectra(spectra, _parameter.IonMode, _parameter.AcquisitionType);

            var results = new List<PeakDetectionResult>();
            for (int i = 0; i < msdecResults.Count; i++) {
                var result = MSDecHandler.GetChromatogramQuantInformation(rawSpectra, msdecResults[i], features[i].QuantMass, _parameter);
                results.Add(result);
            }
            return results;
        }
    }
}
