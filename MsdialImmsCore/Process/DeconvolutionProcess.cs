using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.MsdialImmsCore.Process
{
    internal sealed class DeconvolutionProcess
    {
        private readonly IMsdialDataStorage<MsdialImmsParameter> _storage;

        public DeconvolutionProcess(IMsdialDataStorage<MsdialImmsParameter> storage) {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public Dictionary<double, List<MSDecResult>> Deconvolute(IDataProvider provider, List<ChromatogramPeakFeature> chromPeakFeatures, ChromatogramPeaksDataSummaryDto summary, Action<int> reportAction, CancellationToken token) {
            var parameter = _storage.Parameter;
            var iupacDB = _storage.IupacDatabase;
            var targetCE2MSDecResults = SpectrumDeconvolution(provider, chromPeakFeatures, summary, parameter, iupacDB, reportAction, token);
            return targetCE2MSDecResults;
        }

        private static Dictionary<double, List<MSDecResult>> SpectrumDeconvolution(
            IDataProvider provider,
            List<ChromatogramPeakFeature> chromPeakFeatures,
            ChromatogramPeaksDataSummaryDto summary,
            MsdialImmsParameter parameter,
            IupacDatabase iupac,
            Action<int> reportAction,
            CancellationToken token) {

            var targetCE2MSDecResults = new Dictionary<double, List<MSDecResult>>();
            var initial_msdec = 30.0;
            var max_msdec = 30.0;
            if (parameter.AcquisitionType == Common.Enum.AcquisitionType.AIF) {
                var ceList = provider.LoadCollisionEnergyTargets();
                for (int i = 0; i < ceList.Count; i++) {
                    var targetCE = Math.Round(ceList[i], 2); // must be rounded by 2 decimal points
                    if (targetCE <= 0) {
                        Console.WriteLine("No correct CE information in AIF-MSDEC");
                        continue;
                    }
                    var max_msdec_aif = max_msdec / ceList.Count;
                    var initial_msdec_aif = initial_msdec + max_msdec_aif * i;
                    targetCE2MSDecResults[targetCE] = new Ms2Dec(initial_msdec_aif, max_msdec_aif).GetMS2DecResults(
                        provider, chromPeakFeatures, parameter, summary, iupac, targetCE, reportAction, parameter.NumThreads, token);
                }
            }
            else {
                var targetCE = Math.Round(provider.GetMinimumCollisionEnergy(), 2);
                targetCE2MSDecResults[targetCE] = new Ms2Dec(initial_msdec, max_msdec).GetMS2DecResults(
                    provider, chromPeakFeatures, parameter, summary, iupac, -1, reportAction, parameter.NumThreads, token);
            }
            return targetCE2MSDecResults;
        }
    }
}
