using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Process
{
    public static class FileProcess {
        public static void Run(
            AnalysisFileBean file,
            IDataProvider provider,
            IMsdialDataStorage<MsdialLcmsParameter> storage,
            IAnnotationProcess annotationProcess,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            bool isGuiProcess = false,
            Action<int> reportAction = null,
            CancellationToken token = default) {
            var param = storage.Parameter;
            var mspDB = storage.MspDB;
            var textDB = storage.TextDB;
            var annotatorContainers = storage.DataBaseMapper.MoleculeAnnotators;
            var isotopeTextDB = storage.IsotopeTextDB;
            var iupacDB = storage.IupacDatabase;
            var filepath = file.AnalysisFilePath;
            var fileID = file.AnalysisFileId;


            var spectrumList = provider.LoadMsSpectrums();

            // feature detections
            Console.WriteLine("Peak picking started");
            var chromPeakFeatures = new PeakSpotting(0, 30).Run(provider, param, token, reportAction);
            IsotopeEstimator.Process(chromPeakFeatures, param, iupacDB);
            var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(spectrumList, chromPeakFeatures, param);
            file.ChromPeakFeaturesSummary = summary;

            // chrom deconvolutions
            Console.WriteLine("Deconvolution started");
            var targetCE2MSDecResults = new Dictionary<double, List<MSDecResult>>();
            var initial_msdec = 30.0;
            var max_msdec = 30.0;
            var ceList = SpectrumParser.LoadCollisionEnergyTargets(spectrumList);
            if (param.AcquisitionType == Common.Enum.AcquisitionType.AIF) {
                for (int i = 0; i < ceList.Count; i++) {
                    var targetCE = Math.Round(ceList[i], 2); // must be rounded by 2 decimal points
                    if (targetCE <= 0) {
                        Console.WriteLine("No correct CE information in AIF-MSDEC");
                        continue;
                    }
                    var max_msdec_aif = max_msdec / ceList.Count;
                    var initial_msdec_aif = initial_msdec + max_msdec_aif * i;
                    targetCE2MSDecResults[targetCE] = new Ms2Dec(initial_msdec_aif, max_msdec_aif).GetMS2DecResults(
                        spectrumList, chromPeakFeatures, param, summary, iupacDB, reportAction, token, targetCE);
                }
            }
            else {
                var targetCE = ceList.IsEmptyOrNull() ? -1 : ceList[0];
                targetCE2MSDecResults[targetCE] = new Ms2Dec(initial_msdec, max_msdec).GetMS2DecResults(
                        spectrumList, chromPeakFeatures, param, summary, iupacDB, reportAction, token);
            }

                // annotations
                Console.WriteLine("Annotation started");
                var initial_annotation = 60.0;
                var max_annotation = 30.0;
                foreach (var (ce2msdecs, index) in targetCE2MSDecResults.WithIndex()) {
                    var targetCE = ce2msdecs.Key;
                    var msdecResults = ce2msdecs.Value;
                    var max_annotation_local = max_annotation / targetCE2MSDecResults.Count;
                    var initial_annotation_local = initial_annotation + max_annotation_local * index;
                    annotationProcess.RunAnnotation(
                        chromPeakFeatures,
                        msdecResults,
                        provider,
                        param.NumThreads,
                        token,
                        v => reportAction?.Invoke((int)(initial_annotation_local + v * max_annotation_local)));
                }

                // characterizatin
                new PeakCharacterEstimator(90, 10).Process(spectrumList, chromPeakFeatures,
                    targetCE2MSDecResults.Any() ? targetCE2MSDecResults.Argmin(kvp => kvp.Key).Value : null,
                    evaluator, param, reportAction);

            // file save
            var paifile = file.PeakAreaBeanInformationFilePath;
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(paifile, chromPeakFeatures);


            var dclfile = file.DeconvolutionFilePath;
            var dclfiles = new List<string>();
            foreach (var ce2msdecs in targetCE2MSDecResults) {
                if (targetCE2MSDecResults.Count == 1) {
                    dclfiles.Add(dclfile);
                    MsdecResultsWriter.Write(dclfile, ce2msdecs.Value);
                }
                else {
                    var suffix = Math.Round(ce2msdecs.Key * 100, 0); // CE 34.50 -> 3450
                    var dclfile_suffix = Path.Combine(Path.GetDirectoryName(dclfile), Path.GetFileNameWithoutExtension(dclfile) + "_" + suffix + ".dcl");
                    dclfiles.Add(dclfile_suffix);
                    MsdecResultsWriter.Write(dclfile_suffix, ce2msdecs.Value);
                }
            }
            reportAction?.Invoke(100);
        }
    }
}
