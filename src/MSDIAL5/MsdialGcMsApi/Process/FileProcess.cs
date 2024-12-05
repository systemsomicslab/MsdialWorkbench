using CompMs.Common.DataObj;
using CompMs.Common.Enum;
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
using System.Collections.ObjectModel;
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

        public FileProcess(IDataProviderFactory<AnalysisFileBean> providerFactory, IMsdialDataStorage<MsdialGcmsParameter> storage, CalculateMatchScore calculateMatchScore) {
            if (storage is null || storage.Parameter is null) {
                throw new ArgumentNullException(nameof(storage));
            }
            _riCompoundType = storage.Parameter.RiCompoundType;
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _riDictionaryInfo = storage.Parameter.FileIdRiInfoDictionary;
            _peakSpotting = new PeakSpotting(storage.IupacDatabase, storage.Parameter);
            _ms1Deconvolution = new Ms1Dec(storage.Parameter);
            _annotation = new Annotation(calculateMatchScore, storage.Parameter);
        }

        public async Task RunAsync(AnalysisFileBean analysisFile, ProcessOption option, IProgress<int>? progress, CancellationToken token = default) {
            if (!option.HasFlag(ProcessOption.PeakSpotting) && !option.HasFlag(ProcessOption.Identification)) {
                return;
            }

            var report = progress is null ? (Action<int>?)null : progress.Report;

            progress?.Report((int)PROCESS_START);

            var carbon2RtDict = analysisFile.GetRiDictionary(_riDictionaryInfo);
            var riHandler = carbon2RtDict is null ? null : new RetentionIndexHandler(_riCompoundType, carbon2RtDict);

            Console.WriteLine("Loading spectral information");
            var provider = _providerFactory.Create(analysisFile);
            token.ThrowIfCancellationRequested();

            var getTask = option.HasFlag(ProcessOption.PeakSpotting)
                ? DetectScans(analysisFile, riHandler, provider, progress, token)
                : LoadScans(analysisFile, riHandler, provider, token);
            var (chromPeakFeatures, msdecResults, spectra) = await getTask.ConfigureAwait(false);

            token.ThrowIfCancellationRequested();
            var annotatedMSDecResults = Identification(msdecResults, option, progress);
            var spectrumFeatureCollection = _ms1Deconvolution.GetSpectrumFeaturesByQuantMassInformation(analysisFile, spectra, annotatedMSDecResults);
            SetRetentionIndex(spectrumFeatureCollection, riHandler);

            // save
            await chromPeakFeatures.SerializeAsync(analysisFile, token);
            analysisFile.SaveMsdecResultWithAnnotationInfo(spectrumFeatureCollection.Items.Select(s => s.AnnotatedMSDecResult.MSDecResult).ToList());
            analysisFile.SaveSpectrumFeatures(spectrumFeatureCollection);

            progress?.Report((int)PROCESS_END);
        }

        private async Task<(ChromatogramPeakFeatureCollection, List<MSDecResult>, ReadOnlyCollection<RawSpectrum>)> LoadScans(AnalysisFileBean analysisFile, RetentionIndexHandler riHandler, IDataProvider provider, CancellationToken token) {
            var spectraTask = provider.LoadMsSpectrumsAsync(token);
            var chromPeakFeatures = await analysisFile.LoadChromatogramPeakFeatureCollectionAsync();
            var msdecResults = analysisFile.LoadMsdecResultWithAnnotationInfo();

            SetRetentionIndex(chromPeakFeatures.Items, riHandler);
            SetRetentionIndex(msdecResults, riHandler);

            var spectra = await spectraTask.ConfigureAwait(false);
            return (chromPeakFeatures, msdecResults, spectra);
        }

        private async Task<(ChromatogramPeakFeatureCollection, List<MSDecResult>, ReadOnlyCollection<RawSpectrum>)> DetectScans(AnalysisFileBean analysisFile, RetentionIndexHandler riHandler, IDataProvider provider, IProgress<int>? progress, CancellationToken token) {
            // feature detections
            Console.WriteLine("Peak picking started");
            var reportSpotting = ReportProgress.FromRange(progress, PEAKSPOTTING_START, PEAKSPOTTING_END);
            var chromPeakFeatures_ = _peakSpotting.Run(analysisFile, provider, reportSpotting, token);
            var chromPeakFeatures = new ChromatogramPeakFeatureCollection(chromPeakFeatures_);
            SetRetentionIndex(chromPeakFeatures_, riHandler);
            await analysisFile.SetChromatogramPeakFeaturesSummaryAsync(provider, chromPeakFeatures_, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            // chrom deconvolutions
            Console.WriteLine("Deconvolution started");
            var reportDeconvolution = ReportProgress.FromRange(progress, DECONVOLUTION_START, DECONVOLUTION_END);
            var spectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            var msdecResults = _ms1Deconvolution.GetMSDecResults(spectra, chromPeakFeatures_, reportDeconvolution);
            SetRetentionIndex(msdecResults, riHandler);
            token.ThrowIfCancellationRequested();

            return (chromPeakFeatures, msdecResults, spectra);
        }

        private AnnotatedMSDecResult[] Identification(List<MSDecResult> msdecResults, ProcessOption option, IProgress<int> progress) {
            if (option.HasFlag(ProcessOption.Identification)) {
                // annotations
                Console.WriteLine("Annotation started");
                var reportAnnotation = ReportProgress.FromRange(progress, ANNOTATION_START, ANNOTATION_END);
                return _annotation.MainProcess(msdecResults, reportAnnotation);
            }
            else {
                return msdecResults.Select(r => new AnnotatedMSDecResult(r, new())).ToArray();
            }
        }

        public static void Run(AnalysisFileBean file, IMsdialDataStorage<MsdialGcmsParameter> container, bool isGuiProcess = false, IProgress<int> reportAction = null, CancellationToken token = default) {
            var providerFactory = new StandardDataProviderFactory(isGuiProcess: isGuiProcess);
            FileProcess processor = new(providerFactory, container, new CalculateMatchScore(container.DataBases.MetabolomicsDataBases.FirstOrDefault(), container.Parameter.MspSearchParam, container.Parameter.RetentionType));
            processor.RunAsync(file, ProcessOption.PeakSpotting | ProcessOption.Identification, reportAction, token).Wait();
        }

        private void SetRetentionIndex(IReadOnlyList<IChromatogramPeakFeature> peaks, RetentionIndexHandler riHandler) {
            if (riHandler is null) {
                return;
            }
            foreach (var peak in peaks) {
                peak.ChromXsLeft.RI = riHandler.Convert(peak.ChromXsLeft.RT);
                peak.ChromXsTop.RI = riHandler.Convert(peak.ChromXsTop.RT);
                peak.ChromXsRight.RI = riHandler.Convert(peak.ChromXsRight.RT);
            }
        }

        private void SetRetentionIndex(IReadOnlyList<MSDecResult> results, RetentionIndexHandler riHandler) {
            if (riHandler is null) {
                return;
            }
            foreach (var result in results) {
                result.ChromXs.RI = riHandler.Convert(result.ChromXs.RT);
                foreach (var chrom in result.ModelPeakChromatogram.Select(p => p.ChromXs)) {
                    chrom.RI = riHandler.Convert(chrom.RT);
                }
            }
        }

        private void SetRetentionIndex(SpectrumFeatureCollection spectrumFeatures, RetentionIndexHandler riHandler) {
            if (riHandler is null) {
                return;
            }
            foreach (var spectrumFeature in spectrumFeatures.Items) {
                var peakFeature = spectrumFeature.QuantifiedChromatogramPeak.PeakFeature;
                peakFeature.ChromXsLeft.RI = riHandler.Convert(peakFeature.ChromXsLeft.RT);
                peakFeature.ChromXsTop.RI = riHandler.Convert(peakFeature.ChromXsTop.RT);
                peakFeature.ChromXsRight.RI = riHandler.Convert(peakFeature.ChromXsRight.RT);
            }
        }
    }
}
