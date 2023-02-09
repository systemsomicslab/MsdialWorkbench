using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public class MsSpectrumIntensitySummarizer
    {
        public async Task<MsSpectrumSummary[]> SummarizeAsync(Task<IDataProvider> providerTask) {
            var provider = await providerTask.ConfigureAwait(false);
            var spectra = await provider.LoadMsSpectrumsAsync(default).ConfigureAwait(false);
            (var ms1, var ms2) = new RawDataAccess().ParseRawdataForStatistics(spectra);
            return new[]
            {
                new MsSpectrumSummary(ConvertFreqToDataPoint(ms1), "Histogram of ms1 spectrum intensity"),
                new MsSpectrumSummary(ConvertFreqToDataPoint(ms2), "Histogram of ms2 spectrum intensity"),
            };
        }

        private static DataPoint[] ConvertFreqToDataPoint(Dictionary<int, int> binToFreq) {
            return binToFreq.OrderBy(kvp => kvp.Key)
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => new DataPoint { X = Math.Pow(2, kvp.Key), Y = kvp.Value, })
                .ToArray();
        }
    }
}
