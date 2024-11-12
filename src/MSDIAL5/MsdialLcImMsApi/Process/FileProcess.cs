using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Algorithm;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcImMsApi.Process;

public sealed class FileProcess : IFileProcessor
{
    private readonly IDataProviderFactory<RawMeasurement> spectrumProviderFactory;
    private readonly IDataProviderFactory<RawMeasurement> accSpectrumProviderFactory;
    private readonly IAnnotationProcess annotationProcess;
    private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;
    private readonly IMsdialDataStorage<MsdialLcImMsParameter> storage;
    private readonly bool isGuiProcess;

    public FileProcess(
        IDataProviderFactory<RawMeasurement> spectrumProviderFactory,
        IDataProviderFactory<RawMeasurement> accSpectrumProviderFactory,
        IAnnotationProcess annotationProcess,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        IMsdialDataStorage<MsdialLcImMsParameter> storage,
        bool isGuiProcess) {
        this.spectrumProviderFactory = spectrumProviderFactory;
        this.accSpectrumProviderFactory = accSpectrumProviderFactory;
        this.annotationProcess = annotationProcess;
        this.evaluator = evaluator;
        this.storage = storage;
        this.isGuiProcess = isGuiProcess;
    }

    public async Task RunAsync(AnalysisFileBean file, ProcessOption option, IProgress<int>? reporter = null, CancellationToken token = default) {
        if (!option.HasFlag(ProcessOption.PeakSpotting) && !option.HasFlag(ProcessOption.Identification)) {
            return;
        }

        var rawObj = file.LoadRawMeasurement(isImagingMsData: false, isGuiProcess: isGuiProcess, retry: 5, sleepMilliSeconds: 500);
        var spectrumProvider = new Lazy<IDataProvider>(() => spectrumProviderFactory.Create(rawObj));
        var accSpectrumProvider = accSpectrumProviderFactory.Create(rawObj);

        var (chromPeakFeatures, mSDecResultCollections) = option.HasFlag(ProcessOption.PeakSpotting)
            ? FindPeakAndScans(file, spectrumProvider.Value, accSpectrumProvider, reporter, token)
            : await LoadPeakAndScans(file, token).ConfigureAwait(false);

        if (option.HasFlag(ProcessOption.Identification)) {
            // annotations
            Console.WriteLine("Annotation started");
            await PeakAnnotationAsync(annotationProcess, spectrumProvider.Value, chromPeakFeatures.Items, mSDecResultCollections, storage.Parameter, reporter, token).ConfigureAwait(false);
        }

        // characterizatin
        PeakCharacterization(file, mSDecResultCollections, accSpectrumProvider, chromPeakFeatures.Items, evaluator, storage.Parameter, reporter);

        // file save
        await SaveToFileAsync(file, chromPeakFeatures, mSDecResultCollections).ConfigureAwait(false);

        reporter?.Report(100);
    }

    private async Task<(ChromatogramPeakFeatureCollection, MSDecResultCollection[])> LoadPeakAndScans(AnalysisFileBean file, CancellationToken token) {
        var peakTask = file.LoadChromatogramPeakFeatureCollectionAsync(token);
        var resultsTask = Task.WhenAll(MSDecResultCollection.DeserializeAsync(file, token));

        token.ThrowIfCancellationRequested();
        var chromPeakFeatures = await peakTask.ConfigureAwait(false);
        chromPeakFeatures.ClearMatchResultProperties();
        var mSDecResultCollections = await resultsTask.ConfigureAwait(false);
        return (chromPeakFeatures, mSDecResultCollections);
    }

    private (ChromatogramPeakFeatureCollection, MSDecResultCollection[]) FindPeakAndScans(AnalysisFileBean file, IDataProvider spectrumProvider, IDataProvider accSpectrumProvider, IProgress<int>? progress, CancellationToken token) {
        var reportAction = progress is not null ? progress.Report : (Action<int>)null;
        Console.WriteLine("Peak picking started");
        var chromPeakFeatures_ = PeakSpotting(file, spectrumProvider, accSpectrumProvider, progress, token);
        var chromPeakFeatures = new ChromatogramPeakFeatureCollection(chromPeakFeatures_);

        var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(spectrumProvider, chromPeakFeatures.Items);
        file.ChromPeakFeaturesSummary = summary;

        Console.WriteLine("Deconvolution started");
        var targetCE2MSDecResults = SpectrumDeconvolution(file, spectrumProvider, chromPeakFeatures_, summary, storage.Parameter, storage.IupacDatabase, progress, token);
        var mSDecResultCollections = targetCE2MSDecResults.Select(pair => new MSDecResultCollection(pair.Value, pair.Key)).ToArray();

        return (chromPeakFeatures, mSDecResultCollections);
    }

    private List<ChromatogramPeakFeature> PeakSpotting(
        AnalysisFileBean file,
        IDataProvider spectrumProvider,
        IDataProvider accSpectrumProvider,
        IProgress<int>? progress,
        CancellationToken token) {

        var parameter = storage.Parameter;
        if (!parameter.FileID2CcsCoefficients.TryGetValue(file.AnalysisFileId, out var coeff)) {
            coeff = null;
        }

        var chromPeakFeatures = new PeakSpotting(0, 30, parameter).Execute4DFeatureDetection(file, spectrumProvider,
            accSpectrumProvider, parameter.NumThreads, progress, token);
        var iupacDB = storage.IupacDatabase;
        IsotopeEstimator.Process(chromPeakFeatures, parameter, iupacDB);
        CopyIsotopeInformation2DriftFeatures(chromPeakFeatures);

        CcsEstimator.Process(chromPeakFeatures, parameter, parameter.IonMobilityType, coeff, parameter.IsAllCalibrantDataImported);
        return chromPeakFeatures;
    }

    private void CopyIsotopeInformation2DriftFeatures(List<ChromatogramPeakFeature> features) {
        foreach (var feature in features) {
            foreach(var dFeature in feature.DriftChromFeatures) {
                dFeature.PeakCharacter.IsotopeWeightNumber = feature.PeakCharacter.IsotopeWeightNumber;
                dFeature.PeakCharacter.Charge = feature.PeakCharacter.Charge;
                dFeature.PeakCharacter.IsotopeParentPeakID = feature.PeakCharacter.IsotopeParentPeakID;
            }
        }
    }

    private static Dictionary<double, List<MSDecResult>> SpectrumDeconvolution(
        AnalysisFileBean file,
        IDataProvider provider,
        List<ChromatogramPeakFeature> chromPeakFeatures,
        ChromatogramPeaksDataSummaryDto summary,
        MsdialLcImMsParameter parameter,
        IupacDatabase iupac,
        IProgress<int>? progress, CancellationToken token) {

        var targetCE2MSDecResults = new Dictionary<double, List<MSDecResult>>();
        var initial_msdec = 30.0;
        var max_msdec = 30.0;
        var ceList = provider.LoadCollisionEnergyTargets();
        Ms2Dec ms2Dec = new Ms2Dec(file);
        if (file.AcquisitionType == AcquisitionType.AIF) {
            for (int i = 0; i < ceList.Count; i++) {
                var targetCE = Math.Round(ceList[i], 2); // must be rounded by 2 decimal points
                if (targetCE <= 0) {
                    Console.WriteLine("No correct CE information in AIF-MSDEC");
                    continue;
                }
                var max_msdec_aif = max_msdec / ceList.Count;
                var initial_msdec_aif = initial_msdec + max_msdec_aif * i;
                ReportProgress reporter = ReportProgress.FromLength(progress, initial_msdec_aif, max_msdec_aif);
                targetCE2MSDecResults[targetCE] = ms2Dec.GetMS2DecResults(provider, chromPeakFeatures, parameter, summary, iupac, reporter, token, targetCE);
            }
        }
        else {
            var targetCE = ceList.IsEmptyOrNull() ? -1 : Math.Round(ceList[0], 2);
            ReportProgress reporter = ReportProgress.FromLength(progress, initial_msdec, max_msdec);
            targetCE2MSDecResults[targetCE] = ms2Dec.GetMS2DecResults(provider, chromPeakFeatures, parameter, summary, iupac, reporter, token, targetCE);
        }
        return targetCE2MSDecResults;
    }

    private static async Task PeakAnnotationAsync(
        IAnnotationProcess annotationProcess,
        IDataProvider provider,
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        MSDecResultCollection[] mSDecResultCollections,
        MsdialLcImMsParameter parameter,
        IProgress<int>? progress,
        CancellationToken token) {

        var initial_annotation = 60.0;
        var max_annotation = 30.0;
        var max_annotation_local = max_annotation / mSDecResultCollections.Length;
        foreach (var (mSDecResultCollection, index) in mSDecResultCollections.WithIndex()) {
            var initial_annotation_local = initial_annotation + max_annotation_local * index;
            var reporter = ReportProgress.FromLength(progress, initial_annotation_local, max_annotation_local);
            await annotationProcess.RunAnnotationAsync(chromPeakFeatures, mSDecResultCollection, provider, parameter.NumThreads - 1, reporter.Report, token).ConfigureAwait(false);
        }
    }

    private static void PeakCharacterization(
        AnalysisFileBean file,
        MSDecResultCollection[] mSDecResultCollections,
        IDataProvider provider,
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        MsdialLcImMsParameter parameter,
        IProgress<int>? progress) {

        new PeakCharacterEstimator(90, 10).Process(file, provider, chromPeakFeatures, mSDecResultCollections.Any() ? mSDecResultCollections.Argmin(kvp => kvp.CollisionEnergy).MSDecResults : null, evaluator, parameter, progress);
    }

    private static void SaveToFile(
        AnalysisFileBean file,
        List<ChromatogramPeakFeature> chromPeakFeatures,
        Dictionary<double, List<MSDecResult>> targetCE2MSDecResults) {

        var paifile = file.PeakAreaBeanInformationFilePath;
        MsdialPeakSerializer.SaveChromatogramPeakFeatures(paifile, chromPeakFeatures);

        var dclfile = file.DeconvolutionFilePath;
        var dclfiles = new List<string>();
        if (targetCE2MSDecResults.Count == 1) {
            dclfiles.Add(dclfile);
            MsdecResultsWriter.Write(dclfile, targetCE2MSDecResults.Single().Value);
        }
        else {
            var dclDirectory = Path.GetDirectoryName(dclfile);
            var dclName = Path.GetFileNameWithoutExtension(dclfile);
            foreach (var ce2msdecs in targetCE2MSDecResults) {
                var suffix = Math.Round(ce2msdecs.Key * 100, 0); // CE 34.50 -> 3450
                var dclfile_suffix = Path.Combine(dclDirectory,  dclName + "_" + suffix + ".dcl");
                dclfiles.Add(dclfile_suffix);
                MsdecResultsWriter.Write(dclfile_suffix, ce2msdecs.Value);
            }
        }
        file.DeconvolutionFilePathList = dclfiles;
    }

    private static Task SaveToFileAsync(AnalysisFileBean file, ChromatogramPeakFeatureCollection chromPeakFeatures, IReadOnlyList<MSDecResultCollection> mSDecResultCollections) {
        Task t1, t2;

        t1 = chromPeakFeatures.SerializeAsync(file);

        if (mSDecResultCollections.Count == 1) {
            t2 = mSDecResultCollections[0].SerializeAsync(file);
        }
        else {
            file.DeconvolutionFilePathList.Clear();
            t2 = Task.WhenAll(mSDecResultCollections.Select(mSDecResultCollection => mSDecResultCollection.SerializeWithCEAsync(file)));
        }

        return Task.WhenAll(t1, t2);
    }
}
