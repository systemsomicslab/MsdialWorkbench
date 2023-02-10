using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Algorithm;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialGcMsApi.Process
{
    public sealed class FileProcess {
        private static readonly double PROCESS_START = 0d;
        private static readonly double PROCESS_END = 100d;
        private static readonly double PEAKSPOTTING_START = 0d;
        private static readonly double PEAKSPOTTING_END = 30d;
        private static readonly double DECONVOLUTION_START = 30d;
        private static readonly double DECONVOLUTION_END = 60d;
        private static readonly double ANNOTATION_START = 60d;
        private static readonly double ANNOTATION_END = 90d;

        private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
        private readonly Dictionary<int, RiDictionaryInfo> _riDictionaryInfo;
        private readonly Action<int> _reportAction;
        private readonly PeakSpotting _peakSpotting;
        private readonly Ms1Dec _ms1Deconvolution;
        private readonly Annotation _annotation;

        public FileProcess(IDataProviderFactory<AnalysisFileBean> providerFactory, IMsdialDataStorage<MsdialGcmsParameter> storage, Action<int> reportAction) {
            if (storage is null || storage.Parameter is null) {
                throw new ArgumentNullException(nameof(storage));
            }

            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _riDictionaryInfo = storage.Parameter.FileIdRiInfoDictionary;
            _reportAction = reportAction;
            _peakSpotting = new PeakSpotting(storage.IupacDatabase, storage.Parameter);
            _ms1Deconvolution = new Ms1Dec(storage.Parameter);
            _annotation = new Annotation(storage.MspDB, storage.Parameter);
        }

        public async Task RunAsync(AnalysisFileObject analysisFileObject, CancellationToken token = default) {
            _reportAction?.Invoke((int)PROCESS_START);

            Console.WriteLine("Loading spectral information");
            var provider = analysisFileObject.ParseData(_providerFactory);
            token.ThrowIfCancellationRequested();

            // feature detections
            Console.WriteLine("Peak picking started");
            var reportSpotting = ReportProgress.FromRange(_reportAction, PEAKSPOTTING_START, PEAKSPOTTING_END);
            var chromPeakFeatures = _peakSpotting.Run(provider, reportSpotting, token);
            await analysisFileObject.SetChromatogramPeakFeaturesSummaryAsync(provider, chromPeakFeatures, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            // chrom deconvolutions
            Console.WriteLine("Deconvolution started");
            var reportDeconvolution = ReportProgress.FromRange(_reportAction, DECONVOLUTION_START, DECONVOLUTION_END);
            var spectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            var msdecResults = _ms1Deconvolution.GetMSDecResults(spectra, chromPeakFeatures, reportDeconvolution);
            token.ThrowIfCancellationRequested();

            // annotations
            Console.WriteLine("Annotation started");
            var reportAnnotation = ReportProgress.FromRange(_reportAction, ANNOTATION_START, ANNOTATION_END);
            var carbon2RtDict = analysisFileObject.GetRiDictionary(_riDictionaryInfo);
            var spectrumFeatures = _annotation.MainProcess(msdecResults, carbon2RtDict, reportAnnotation);
            token.ThrowIfCancellationRequested();

            // save
            analysisFileObject.SaveChromatogramPeakFeatures(chromPeakFeatures);
            analysisFileObject.SaveMsdecResultWithAnnotationInfo(msdecResults);

            _reportAction?.Invoke((int)PROCESS_END);
        }

        public static void Run(AnalysisFileBean file, IMsdialDataStorage<MsdialGcmsParameter> container, bool isGuiProcess = false, Action<int> reportAction = null, CancellationToken token = default) {
            var providerFactory = new StandardDataProviderFactory(isGuiProcess: isGuiProcess);
            new FileProcess(providerFactory, container, reportAction).RunAsync(new AnalysisFileObject(file), token).Wait();
        }
    }
}
