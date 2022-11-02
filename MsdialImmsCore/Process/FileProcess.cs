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
    public static class FileProcess
    {
        public static void Run(
            AnalysisFileBean file,
            IMsdialDataStorage<MsdialImmsParameter> storage,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            bool isGuiProcess = false,
            Action<int> reportAction = null, CancellationToken token = default) {

            var mspAnnotator = new ImmsMspAnnotator(new MoleculeDataBase(storage.MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB), storage.Parameter.MspSearchParam, storage.Parameter.TargetOmics, "MspDB", -1);
            var textDBAnnotator = new ImmsTextDBAnnotator(new MoleculeDataBase(storage.TextDB, "TextDB", DataBaseSource.Text, SourceType.TextDB), storage.Parameter.TextDbSearchParam, "TextDB", -1);

            Run(file, storage, mspAnnotator, textDBAnnotator, new ImmsAverageDataProvider(file, 0.001, 0.002, retry: 5, isGuiProcess: isGuiProcess), evaluator, reportAction, token);
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

            Console.WriteLine("Peak picking started");
            var peakPicker = new PeakPickProcess(storage);
            var chromPeakFeatures = peakPicker.Pick(file, provider, reportAction);

            var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, chromPeakFeatures);
            file.ChromPeakFeaturesSummary = summary;

            Console.WriteLine("Deconvolution started");
            var deconvolute = new DeconvolutionProcess(storage);
            var targetCE2MSDecResults = deconvolute.Deconvolute(provider, chromPeakFeatures, summary, reportAction, token);

            // annotations
            Console.WriteLine("Annotation started");
            new PeakAnnotationProcess().Annotate(storage, provider, chromPeakFeatures, targetCE2MSDecResults, mspAnnotator, textDBAnnotator, evaluator, reportAction, token);

            // file save
            SaveToFile(file, chromPeakFeatures, targetCE2MSDecResults);

            reportAction?.Invoke(100);
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
