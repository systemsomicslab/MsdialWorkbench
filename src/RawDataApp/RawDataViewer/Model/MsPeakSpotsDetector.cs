using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public class MsPeakSpotsDetector
    {
        public async Task<(MsPeakSpotsSummary, MsSnDistribution)> DetectAsync(AnalysisDataModel dataModel, CancellationToken token = default) {
            var ionMode = dataModel.IonMode;
            switch (dataModel.MachineCategory) {
                case MachineCategory.LCMS: {
                        var provider = await dataModel.CreateDataProvider(token).ConfigureAwait(false);
                        var peaks = await (new MsdialLcMsApi.Algorithm.PeakSpotting(dataModel.AnalysisFile, 0, 100)).RunAsync(provider, new MsdialLcmsApi.Parameter.MsdialLcmsParameter { MinimumAmplitude = 0d, IonMode = ionMode, }, null, token).ConfigureAwait(false);
                        return (PeaksToSummary(peaks), new MsSnDistribution(peaks));
                    }
                case MachineCategory.IMMS: {
                        var providerFactory = new MsdialImmsCore.Algorithm.ImmsAverageDataProviderFactory<RawMeasurement>(new StandardDataProviderFactory(), mzTolerance: 0.001, driftTolerance: 0.002);
                        var provider = await dataModel.CreateDataProviderByFactory(providerFactory, token).ConfigureAwait(false);
                        var peaks = await (new MsdialImmsCore.Algorithm.PeakSpotting(new MsdialImmsCore.Parameter.MsdialImmsParameter { MinimumAmplitude = 0d, IonMode = ionMode, })).RunAsync(dataModel.AnalysisFile, provider, null, token).ConfigureAwait(false);
                        return (PeaksToSummary(peaks.Items), new MsSnDistribution(peaks.Items));
                    }
                case MachineCategory.IFMS: {
                        var provider = await dataModel.CreateDataProvider(token).ConfigureAwait(false);
                        var ms1Spectrum = provider.LoadMs1Spectrums().Argmax(spec => spec.Spectrum.Length);
                        var chromPeaks = DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum);
                        var parameter = new ParameterBase();
                        var sChromPeaks = new Chromatogram(chromPeaks, ChromXType.Mz, ChromXUnit.Mz).ChromatogramSmoothing(parameter.SmoothingMethod, parameter.SmoothingLevel).AsPeakArray();

                        var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, parameter.MinimumDatapoints, parameter.MinimumAmplitude);
                        if (peakPickResults.IsEmptyOrNull()) {
                            return (new MsPeakSpotsSummary(new DataPoint[0]), new MsSnDistribution(Array.Empty<ChromatogramPeakFeature>()));
                        }
                        var peaks = MsdialDimsCore.ProcessFile.ConvertPeaksToPeakFeatures(peakPickResults, ms1Spectrum, provider, dataModel.AnalysisFile.AcquisitionType);
                        return (PeaksToSummary(peaks), new MsSnDistribution(peaks));
                    }
                case MachineCategory.LCIMMS: {
                        var providers = await Task.WhenAll(new[]
                        {
                            dataModel.CreateDataProvider(token),
                            dataModel.CreateAccumulatedDataProvider(token),
                        }).ConfigureAwait(false);
                        var provider = providers[0];
                        var accProvider = providers[1];
                        var parameter = new MsdialLcImMsApi.Parameter.MsdialLcImMsParameter { MinimumAmplitude = 0d, IonMode = ionMode, };
                        var peaks = await (new MsdialLcImMsApi.Algorithm.PeakSpotting(0, 100, parameter)).Execute4DFeatureDetectionAsync(dataModel.AnalysisFile, provider, accProvider, parameter.NumThreads, null, token).ConfigureAwait(false);
                        var flatten = peaks.SelectMany(peak => peak.DriftChromFeatures).ToList();
                        return (PeaksToSummary(flatten), new MsSnDistribution(peaks));
                    }
                case MachineCategory.GCMS:
                default:
                    return (new MsPeakSpotsSummary(new DataPoint[0]), new MsSnDistribution(Array.Empty<ChromatogramPeakFeature>()));
            }
        }

        private static MsPeakSpotsSummary PeaksToSummary(IReadOnlyList<ChromatogramPeakFeature> peaks) {
            return new MsPeakSpotsSummary(
                peaks.GroupBy(peak => Math.Floor(Math.Log(peak.PeakFeature.PeakHeightTop, 2)), (key, values) => (key, count: values.Count()))
                    .Select(kvp => new DataPoint { X = Math.Pow(2, kvp.key), Y = kvp.count, })
                    .OrderBy(p => p.X)
                    .ToArray());
        }
    }
}
