using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
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

        private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
        private readonly Dictionary<int, RiDictionaryInfo> _riDictionaryInfo;
        private readonly PeakSpotting _peakSpotting;
        private readonly Ms1Dec _ms1Deconvolution;
        private readonly Annotation _annotation;

        public FileProcess(IDataProviderFactory<AnalysisFileBean> providerFactory, IMsdialDataStorage<MsdialGcmsParameter> storage) {
            if (storage is null || storage.Parameter is null) {
                throw new ArgumentNullException(nameof(storage));
            }
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _riDictionaryInfo = storage.Parameter.FileIdRiInfoDictionary;
            _peakSpotting = new PeakSpotting(storage.IupacDatabase, storage.Parameter);
            _ms1Deconvolution = new Ms1Dec(storage.Parameter);
            _annotation = new Annotation(storage.DataBases.MetabolomicsDataBases.FirstOrDefault()?.DataBase.Database, storage.Parameter);
        }

        public async Task RunAsync(AnalysisFileBean analysisFile, Action<int> reportAction, CancellationToken token = default) {
            reportAction?.Invoke((int)PROCESS_START);

            Console.WriteLine("Loading spectral information");
            var provider = _providerFactory.Create(analysisFile);
            token.ThrowIfCancellationRequested();

            // feature detections
            Console.WriteLine("Peak picking started");
            var reportSpotting = ReportProgress.FromRange(reportAction, PEAKSPOTTING_START, PEAKSPOTTING_END);
            var chromPeakFeatures = _peakSpotting.Run(analysisFile, provider, reportSpotting, token);
            await analysisFile.SetChromatogramPeakFeaturesSummaryAsync(provider, chromPeakFeatures, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            // chrom deconvolutions
            Console.WriteLine("Deconvolution started");
            var reportDeconvolution = ReportProgress.FromRange(reportAction, DECONVOLUTION_START, DECONVOLUTION_END);
            var spectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            var msdecResults = _ms1Deconvolution.GetMSDecResults(spectra, chromPeakFeatures, reportDeconvolution);
            token.ThrowIfCancellationRequested();

            // annotations
            Console.WriteLine("Annotation started");
            var reportAnnotation = ReportProgress.FromRange(reportAction, ANNOTATION_START, ANNOTATION_END);
            var carbon2RtDict = analysisFile.GetRiDictionary(_riDictionaryInfo);
            var annotatedMSDecResults = _annotation.MainProcess(msdecResults, carbon2RtDict, reportAnnotation);
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
            var carbon2RtDict = analysisFile.GetRiDictionary(_riDictionaryInfo);
            var annotatedMSDecResults = _annotation.MainProcess(mSDecResults, carbon2RtDict, reportAnnotation);
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
    }
}
