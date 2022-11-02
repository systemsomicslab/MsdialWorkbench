using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialImmsCore.Process
{
    public sealed class FileProcess
    {
        private readonly PeakPickProcess _peakPickProcess;
        private readonly DeconvolutionProcess _deconvolutionProcess;
        private readonly PeakAnnotationProcess _peakAnnotationProcess;

        public FileProcess(
            IMsdialDataStorage<MsdialImmsParameter> storage,
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> mspAnnotator,
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> textDBAnnotator,
            IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            if (storage is null) {
                throw new ArgumentNullException(nameof(storage));
            }

            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }

            _peakPickProcess = new PeakPickProcess(storage);
            _deconvolutionProcess = new DeconvolutionProcess(storage);
            _peakAnnotationProcess = new PeakAnnotationProcess(storage, evaluator, mspAnnotator, textDBAnnotator);
        }

        public void Run(AnalysisFileBean file, IDataProvider provider, Action<int> reportAction = null, CancellationToken token = default) {
            Console.WriteLine("Peak picking started");
            var chromPeakFeatures = _peakPickProcess.Pick(file, provider, reportAction);

            var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures);
            file.ChromPeakFeaturesSummary = summary;

            Console.WriteLine("Deconvolution started");
            var targetCE2MSDecResults = _deconvolutionProcess.Deconvolute(provider, chromPeakFeatures, summary, reportAction, token);

            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(provider, chromPeakFeatures, targetCE2MSDecResults, reportAction, token);

            // file save
            SaveToFile(file, new ChromatogramPeakFeatureCollection(chromPeakFeatures), targetCE2MSDecResults);

            reportAction?.Invoke(100);
        }

        public void Annotate(AnalysisFileBean file, IDataProvider provider, Action<int> reportAction = null, CancellationToken token = default) {
            var (chromPeakFeatures, targetCE2MSDecResults) = LoadFromFile(file);

            // annotations
            Console.WriteLine("Annotation started");
            _peakAnnotationProcess.Annotate(provider, chromPeakFeatures.Items, targetCE2MSDecResults, reportAction, token);

            // file save
            SaveToFile(file, chromPeakFeatures, targetCE2MSDecResults);

            reportAction?.Invoke(100);
        }

        public static void Run(
            AnalysisFileBean file,
            IMsdialDataStorage<MsdialImmsParameter> storage,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            bool isGuiProcess = false,
            Action<int> reportAction = null, CancellationToken token = default) {

            var mspAnnotator = new ImmsMspAnnotator(new MoleculeDataBase(storage.MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB), storage.Parameter.MspSearchParam, storage.Parameter.TargetOmics, "MspDB", -1);
            var textDBAnnotator = new ImmsTextDBAnnotator(new MoleculeDataBase(storage.TextDB, "TextDB", DataBaseSource.Text, SourceType.TextDB), storage.Parameter.TextDbSearchParam, "TextDB", -1);
            var provider = new ImmsAverageDataProvider(file, 0.001, 0.002, retry: 5, isGuiProcess: isGuiProcess);
            Run(file, storage, mspAnnotator, textDBAnnotator, provider, evaluator, reportAction, token);
        }

        public static void Run(
            AnalysisFileBean file,
            IMsdialDataStorage<MsdialImmsParameter> storage,
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> mspAnnotator,
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> textDBAnnotator,
            IDataProvider provider,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            Action<int> reportAction = null,
            CancellationToken token = default) {

            var processor = new FileProcess(storage, mspAnnotator, textDBAnnotator, evaluator);
            processor.Run(file, provider, reportAction, token);
        }

        private static (ChromatogramPeakFeatureCollection, Dictionary<double, List<MSDecResult>>) LoadFromFile(AnalysisFileBean file) {
            var peaks = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).Result;
            var decResults = new Dictionary<double, List<MSDecResult>>();
            if (file.DeconvolutionFilePathList.Count == 1) {
                var dclfile = file.DeconvolutionFilePath;
                decResults[-1] = MsdecResultsReader.ReadMSDecResults(dclfile, out var _, out var _);
            }
            else {
                foreach (var dclfile in file.DeconvolutionFilePathList) {
                    var fileName = Path.GetFileNameWithoutExtension(dclfile);
                    var suffix = fileName.Split('_').Last();
                    var ce = double.TryParse(suffix, out var ce_) ? ce_ / 100 : -1d; // 3450 -> CE 34.50
                    decResults[ce] = MsdecResultsReader.ReadMSDecResults(dclfile, out var _, out var _);
                }
            }
            return (peaks, decResults);
        }

        private static void SaveToFile(
            AnalysisFileBean file,
            ChromatogramPeakFeatureCollection chromPeakFeatures,
            Dictionary<double, List<MSDecResult>> targetCE2MSDecResults) {

            chromPeakFeatures.SerializeAsync(file).Wait();

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
