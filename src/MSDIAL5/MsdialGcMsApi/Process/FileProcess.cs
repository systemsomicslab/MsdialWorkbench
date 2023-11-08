using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Algorithm;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialGcMsApi.Process
{
    public sealed class FileProcess : IFileProcessor {
        private static readonly double PROCESS_START = 0d;
        private static readonly double PROCESS_END = 100d;
        private static readonly double PEAKSPOTTING_START = 0d;
        private static readonly double PEAKSPOTTING_END = 30d;
        private static readonly double DECONVOLUTION_START = 30d;
        private static readonly double DECONVOLUTION_END = 60d;
        private static readonly double ANNOTATION_START = 60d;
        private static readonly double ANNOTATION_END = 90d;

        private readonly RiCompoundType _riCompoundType;
        private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
        private readonly Dictionary<int, RiDictionaryInfo> _riDictionaryInfo;
        private readonly PeakSpotting _peakSpotting;
        private readonly Ms1Dec _ms1Deconvolution;
        private readonly Annotation _annotation;

        public FileProcess(IDataProviderFactory<AnalysisFileBean> providerFactory, IMsdialDataStorage<MsdialGcmsParameter> storage) {
            if (storage is null || storage.Parameter is null) {
                throw new ArgumentNullException(nameof(storage));
            }
            _riCompoundType = storage.Parameter.RiCompoundType;
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _riDictionaryInfo = storage.Parameter.FileIdRiInfoDictionary;
            _peakSpotting = new PeakSpotting(storage.IupacDatabase, storage.Parameter);
            _ms1Deconvolution = new Ms1Dec(storage.Parameter);
            _annotation = new Annotation(storage.DataBases.MetabolomicsDataBases.FirstOrDefault(), storage.Parameter);
        }

        public async Task RunAsync(AnalysisFileBean analysisFile, Action<int> reportAction, CancellationToken token = default) {
            reportAction?.Invoke((int)PROCESS_START);
            var carbon2RtDict = analysisFile.GetRiDictionary(_riDictionaryInfo);

            Console.WriteLine("Loading spectral information");
            var provider = _providerFactory.Create(analysisFile);
            token.ThrowIfCancellationRequested();

            // feature detections
            Console.WriteLine("Peak picking started");
            var reportSpotting = ReportProgress.FromRange(reportAction, PEAKSPOTTING_START, PEAKSPOTTING_END);
            var chromPeakFeatures = _peakSpotting.Run(analysisFile, provider, reportSpotting, token);
            SetRetentionIndexForChromatogramPeakFeature(chromPeakFeatures, carbon2RtDict);
            await analysisFile.SetChromatogramPeakFeaturesSummaryAsync(provider, chromPeakFeatures, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            // chrom deconvolutions
            Console.WriteLine("Deconvolution started");
            var reportDeconvolution = ReportProgress.FromRange(reportAction, DECONVOLUTION_START, DECONVOLUTION_END);
            var spectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            var msdecResults = _ms1Deconvolution.GetMSDecResults(spectra, chromPeakFeatures, reportDeconvolution);
            SetRetentionIndexForMSDecResult(msdecResults, carbon2RtDict);
            token.ThrowIfCancellationRequested();

            // annotations
            Console.WriteLine("Annotation started");
            var reportAnnotation = ReportProgress.FromRange(reportAction, ANNOTATION_START, ANNOTATION_END);
            var annotatedMSDecResults = _annotation.MainProcess(msdecResults, reportAnnotation);
            token.ThrowIfCancellationRequested();

            var spectrumFeatureCollection = _ms1Deconvolution.GetSpectrumFeaturesByQuantMassInformation(analysisFile, spectra, annotatedMSDecResults);

            // save
            analysisFile.SaveChromatogramPeakFeatures(chromPeakFeatures);
            analysisFile.SaveMsdecResultWithAnnotationInfo(msdecResults);
            analysisFile.SaveSpectrumFeatures(spectrumFeatureCollection);

            reportAction?.Invoke((int)PROCESS_END);
        }

        public async Task AnnotateAsync(AnalysisFileBean analysisFile, Action<int> reportAction, CancellationToken token = default) {
            reportAction?.Invoke((int)PROCESS_START);
            Console.WriteLine("Loading spectral information");
            var provider = _providerFactory.Create(analysisFile);
            token.ThrowIfCancellationRequested();
            var spectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            var mSDecResults = analysisFile.LoadMsdecResultWithAnnotationInfo();

            // annotations
            Console.WriteLine("Annotation started");
            var reportAnnotation = ReportProgress.FromRange(reportAction, ANNOTATION_START, ANNOTATION_END);
            var annotatedMSDecResults = _annotation.MainProcess(mSDecResults, reportAnnotation);
            token.ThrowIfCancellationRequested();

            var spectrumFeatureCollection = _ms1Deconvolution.GetSpectrumFeaturesByQuantMassInformation(analysisFile, spectra, annotatedMSDecResults);

            // save
            analysisFile.SaveMsdecResultWithAnnotationInfo(mSDecResults);
            analysisFile.SaveSpectrumFeatures(spectrumFeatureCollection);
            reportAction?.Invoke((int)PROCESS_END);
        }

        public static void Run(AnalysisFileBean file, IMsdialDataStorage<MsdialGcmsParameter> container, bool isGuiProcess = false, Action<int> reportAction = null, CancellationToken token = default) {
            var providerFactory = new StandardDataProviderFactory(isGuiProcess: isGuiProcess);
            new FileProcess(providerFactory, container).RunAsync(file, reportAction, token).Wait();
        }

        private void SetRetentionIndexForChromatogramPeakFeature(IReadOnlyList<IChromatogramPeakFeature> peaks, Dictionary<int, float> carbon2RtDict) {
            if (carbon2RtDict.IsEmptyOrNull()) {
                return;
            }
            if (_riCompoundType == RiCompoundType.Alkanes)
                ExecuteForKovats(carbon2RtDict, peaks.SelectMany(p => new[] { p.ChromXsLeft, p.ChromXsTop, p.ChromXsRight }));
            else {
                ExecuteForFiehnFames(carbon2RtDict, peaks.SelectMany(p => new[] { p.ChromXsLeft, p.ChromXsTop, p.ChromXsRight }));
            }
        }

        private void SetRetentionIndexForMSDecResult(IReadOnlyList<MSDecResult> results, Dictionary<int, float> carbon2RtDict) {
            if (carbon2RtDict.IsEmptyOrNull()) {
                return;
            }
            if (_riCompoundType == RiCompoundType.Alkanes)
                ExecuteForKovats(carbon2RtDict, results.SelectMany(r => r.ModelPeakChromatogram.Select(p => p.ChromXs).Append(r.ChromXs)));
            else {
                ExecuteForFiehnFames(carbon2RtDict, results.SelectMany(r => r.ModelPeakChromatogram.Select(p => p.ChromXs).Append(r.ChromXs)));
            }
        }

        private void ExecuteForKovats(Dictionary<int, float> carbon2RtDict, IEnumerable<ChromXs> chroms) {
            foreach (var chrom in chroms) {
                chrom.RI = new RetentionIndex(RetentionIndexHandler.GetRetentionIndexByAlkanes(carbon2RtDict, (float)chrom.RT.Value));
            }
        }

        private void ExecuteForFiehnFames(Dictionary<int, float> famesRtDict, IEnumerable<ChromXs> chroms) {
            var fiehnRiDict = RetentionIndexHandler.GetFiehnFamesDictionary();
            var fiehnRiCoeff = RetentionIndexHandler.GetFiehnRiCoefficient(fiehnRiDict, famesRtDict);
            foreach (var chrom in chroms) {
                chrom.RI = new RetentionIndex(Math.Round(RetentionIndexHandler.CalculateFiehnRi(fiehnRiCoeff, chrom.RT.Value), 1));
            }
        }
    }
}
