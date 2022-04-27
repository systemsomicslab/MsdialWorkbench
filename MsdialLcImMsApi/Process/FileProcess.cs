using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Algorithm;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcImMsApi.Process
{
    public sealed class FileProcess
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

        public Task RunAsync(AnalysisFileBean file, Action<int> reportAction = null, CancellationToken token = default) {

            var parameter = storage.Parameter;
            var iupacDB = storage.IupacDatabase;

            var rawObj = LoadMeasurement(file, isGuiProcess);

            var spectrumProvider = spectrumProviderFactory.Create(rawObj);
            var accSpectrumProvider = accSpectrumProviderFactory.Create(rawObj);

            Console.WriteLine("Peak picking started");
            var chromPeakFeatures = PeakSpotting(file, spectrumProvider, accSpectrumProvider, token, reportAction);

            var spectrumList = spectrumProvider.LoadMsSpectrums();
            var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(spectrumList, chromPeakFeatures, parameter);
            file.ChromPeakFeaturesSummary = summary;

            Console.WriteLine("Deconvolution started");
            var targetCE2MSDecResults = SpectrumDeconvolution(spectrumList, chromPeakFeatures, summary, parameter, iupacDB, reportAction, token);

            // annotations
            Console.WriteLine("Annotation started");
            PeakAnnotation(annotationProcess, accSpectrumProvider, chromPeakFeatures, targetCE2MSDecResults, parameter, reportAction, token);

            // characterizatin
            PeakCharacterization(targetCE2MSDecResults, spectrumList, chromPeakFeatures, evaluator, parameter, reportAction);

            // file save
            SaveToFile(file, chromPeakFeatures, targetCE2MSDecResults);

            reportAction?.Invoke(100);

            return Task.CompletedTask;
        }

        public static void Run(
            AnalysisFileBean file,
            IDataProviderFactory<RawMeasurement> spectrumProviderFactory,
            IDataProviderFactory<RawMeasurement> accSpectrumProviderFactory,
            IAnnotationProcess annotationProcess,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            IMsdialDataStorage<MsdialLcImMsParameter> storage,
            bool isGuiProcess = false,
            Action<int> reportAction = null,
            CancellationToken token = default) {

            new FileProcess(
                spectrumProviderFactory,
                accSpectrumProviderFactory,
                annotationProcess,
                evaluator,
                storage,
                isGuiProcess)
                .RunAsync(file, reportAction, token).Wait();
        }

        private static RawMeasurement LoadMeasurement(AnalysisFileBean file, bool isGuiProcess) {
            using (var access = new RawDataAccess(file.AnalysisFilePath, 0, false, isGuiProcess)) {
                for (var i = 0; i < 5; i++) {
                    var rawObj = DataAccess.GetRawDataMeasurement(access);
                    if (rawObj != null)
                        return rawObj;
                    Thread.Sleep(5000);
                }
                throw new FileLoadException($"Loading {file.AnalysisFilePath} failed.");
            }
        }

        private List<ChromatogramPeakFeature> PeakSpotting(
            AnalysisFileBean file,
            IDataProvider spectrumProvider,
            IDataProvider accSpectrumProvider,
            CancellationToken token,
            Action<int> reportAction) {

            var parameter = storage.Parameter;
            if (!parameter.FileID2CcsCoefficients.TryGetValue(file.AnalysisFileId, out var coeff)) {
                coeff = null;
            }

            var chromPeakFeatures = new PeakSpotting(0, 30).Execute4DFeatureDetection(spectrumProvider, accSpectrumProvider, 
                parameter, parameter.NumThreads, token, reportAction);
            var iupacDB = storage.IupacDatabase;
            IsotopeEstimator.Process(chromPeakFeatures, parameter, iupacDB);
            CcsEstimator.Process(chromPeakFeatures, parameter, parameter.IonMobilityType, coeff, parameter.IsAllCalibrantDataImported);
            return chromPeakFeatures;
        }

        private static Dictionary<double, List<MSDecResult>> SpectrumDeconvolution(
            IReadOnlyList<RawSpectrum> spectrumList,
            List<ChromatogramPeakFeature> chromPeakFeatures,
            ChromatogramPeaksDataSummary summary,
            MsdialLcImMsParameter parameter,
            IupacDatabase iupac,
            Action<int> reportAction,
            CancellationToken token) {

            var targetCE2MSDecResults = new Dictionary<double, List<MSDecResult>>();
            var initial_msdec = 30.0;
            var max_msdec = 30.0;
            var ceList = SpectrumParser.LoadCollisionEnergyTargets(spectrumList);
            if (parameter.AcquisitionType == Common.Enum.AcquisitionType.AIF) {
                for (int i = 0; i < ceList.Count; i++) {
                    var targetCE = Math.Round(ceList[i], 2); // must be rounded by 2 decimal points
                    if (targetCE <= 0) {
                        Console.WriteLine("No correct CE information in AIF-MSDEC");
                        continue;
                    }
                    var max_msdec_aif = max_msdec / ceList.Count;
                    var initial_msdec_aif = initial_msdec + max_msdec_aif * i;
                    targetCE2MSDecResults[targetCE] = new Ms2Dec(initial_msdec_aif, max_msdec_aif).GetMS2DecResults(
                        spectrumList, chromPeakFeatures, parameter, summary, iupac, reportAction, token, targetCE);
                }
            }
            else {
                var targetCE = ceList.IsEmptyOrNull() ? -1 : Math.Round(ceList[0], 2);
                targetCE2MSDecResults[targetCE] = new Ms2Dec(initial_msdec, max_msdec).GetMS2DecResults(
                       spectrumList, chromPeakFeatures, parameter, summary, iupac, reportAction, token, targetCE);
            }
            return targetCE2MSDecResults;
        }

        private static void PeakAnnotation(
            IAnnotationProcess annotationProcess,
            IDataProvider accSpectrumProvider,
            List<ChromatogramPeakFeature> chromPeakFeatures,
            Dictionary<double, List<MSDecResult>> targetCE2MSDecResults,
            MsdialLcImMsParameter parameter,
            Action<int> reportAction,
            CancellationToken token) {

            var initial_annotation = 60.0;
            var max_annotation = 30.0;
            var max_annotation_local = max_annotation / targetCE2MSDecResults.Count;
            foreach (var (ce2msdecs, index) in targetCE2MSDecResults.WithIndex()) {
                var msdecResults = ce2msdecs.Value;
                var initial_annotation_local = initial_annotation + max_annotation_local * index;
                annotationProcess.RunAnnotation(chromPeakFeatures, msdecResults, accSpectrumProvider, parameter.NumThreads - 1, token, v => ReportProgress.Show(initial_annotation_local, max_annotation_local, v, chromPeakFeatures.Count, reportAction));
            }
        }

        private static void PeakCharacterization(
            Dictionary<double, List<MSDecResult>> targetCE2MSDecResults,
            IReadOnlyList<RawSpectrum> spectrumList,
            List<ChromatogramPeakFeature> chromPeakFeatures,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            MsdialLcImMsParameter parameter,
            Action<int> reportAction) {

            new PeakCharacterEstimator(90, 10).Process(spectrumList, chromPeakFeatures,
                targetCE2MSDecResults.Any() ? targetCE2MSDecResults.Argmin(kvp => kvp.Key).Value : null,
                evaluator, parameter, reportAction);
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
    }
}
