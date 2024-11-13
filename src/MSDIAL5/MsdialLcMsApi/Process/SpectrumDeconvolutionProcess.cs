using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Process
{
    internal sealed class SpectrumDeconvolutionProcess
    {
        private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;

        public SpectrumDeconvolutionProcess(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public List<MSDecResultCollection> Deconvolute(IDataProvider provider, IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, AnalysisFileBean analysisFile, ChromatogramPeaksDataSummaryDto summaryDto, IProgress<int>? progress, CancellationToken token) {
            var mSDecREsultCollections = new List<MSDecResultCollection>();
            var initial_msdec = 30.0;
            var max_msdec = 30.0;
            var ceList = provider.LoadCollisionEnergyTargets();
            var summary = ChromatogramPeaksDataSummary.ConvertFromDto(summaryDto);
            if (analysisFile.AcquisitionType == Common.Enum.AcquisitionType.AIF) {
                for (int i = 0; i < ceList.Count; i++) {
                    var targetCE = Math.Round(ceList[i], 2); // must be rounded by 2 decimal points
                    if (targetCE <= 0) {
                        Console.WriteLine("No correct CE information in AIF-MSDEC");
                        continue;
                    }
                    var max_msdec_aif = max_msdec / ceList.Count;
                    var initial_msdec_aif = initial_msdec + max_msdec_aif * i;
                    var results = new Ms2Dec(initial_msdec_aif, max_msdec_aif).GetMS2DecResults(analysisFile, provider, chromPeakFeatures, _storage.Parameter, summary, _storage.IupacDatabase, progress, token, targetCE);
                    mSDecREsultCollections.Add(new MSDecResultCollection(results, targetCE));
                }
            }
            else {
                var targetCE = ceList.IsEmptyOrNull() ? -1 : ceList[0];
                var results = new Ms2Dec(initial_msdec, max_msdec).GetMS2DecResults(analysisFile, provider, chromPeakFeatures, _storage.Parameter, summary, _storage.IupacDatabase, progress, token);
                mSDecREsultCollections.Add(new MSDecResultCollection(results, targetCE));
            }
            return mSDecREsultCollections;
        }
    }
}
