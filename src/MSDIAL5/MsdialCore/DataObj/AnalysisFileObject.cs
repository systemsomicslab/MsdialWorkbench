using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class AnalysisFileObject
    {
        private readonly AnalysisFileBean _instance;

        public AnalysisFileObject(AnalysisFileBean instance) {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public IDataProvider ParseData(IDataProviderFactory<AnalysisFileBean> providerFactory) {
            return providerFactory.Create(_instance);
        }

        public async Task SetChromatogramPeakFeaturesSummaryAsync(IDataProvider provider, List<ChromatogramPeakFeature> chromPeakFeatures, CancellationToken token) {
            var spectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            _instance.ChromPeakFeaturesSummary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures);
        }

        public Dictionary<int, float> GetRiDictionary(Dictionary<int, RiDictionaryInfo> riDictionaries) {
            return riDictionaries?.TryGetValue(_instance.AnalysisFileId, out var dictionary) == true ? dictionary.RiDictionary : null;
        }

        public void SaveChromatogramPeakFeatures(List<ChromatogramPeakFeature> chromPeakFeatures) {
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(_instance.PeakAreaBeanInformationFilePath, chromPeakFeatures);
        }

        public void SaveMsdecResultWithAnnotationInfo(List<MSDecResult> msdecResults) {
            MsdecResultsWriter.Write(_instance.DeconvolutionFilePath, msdecResults, true);
        }

        public AnalysisFileBean GetAnalysisFileBean { get => _instance; }
    }
}
