using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
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
using System.Text;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Process {
    public sealed class FileProcess {
        private FileProcess() { }

        public static void Run(AnalysisFileBean file, MsdialDataStorage container, bool isGuiProcess = false, 
            Action<int> reportAction = null, CancellationToken token = new CancellationToken()) {
            var param = (MsdialLcmsParameter)container.ParameterBase;
            var mspDB = container.MspDB;
            var textDB = container.TextDB;
            var isotopeTextDB = container.IsotopeTextDB;
            var iupacDB = container.IupacDatabase;
            var filepath = file.AnalysisFilePath;
            var fileID = file.AnalysisFileId;
            using (var access = new RawDataAccess(filepath, 0, isGuiProcess, file.RetentionTimeCorrectionBean.PredictedRt)) {

                // parse raw data
                Console.WriteLine("Loading spectral information");
                Common.DataObj.RawMeasurement rawObj = null;
                foreach (var i in Enumerable.Range(0, 5)) {
                    rawObj = DataAccess.GetRawDataMeasurement(access);
                    if (rawObj != null) break;
                    Thread.Sleep(2000);
                }
                if (rawObj == null) {
                    throw new FileLoadException($"Loading {filepath} failed.");
                }

                var spectrumList = rawObj.SpectrumList;

                // feature detections
                Console.WriteLine("Peak picking started");
                var chromPeakFeatures = new PeakSpotting(0, 30).Run(rawObj, param, reportAction);
                IsotopeEstimator.Process(chromPeakFeatures, param, iupacDB);
                var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(spectrumList, chromPeakFeatures, param);
                file.ChromPeakFeaturesSummary = summary;

                // chrom deconvolutions
                Console.WriteLine("Deconvolution started");
                var targetCE2MSDecResults = new Dictionary<double, List<MSDecResult>>();
                var initial_msdec = 30.0;
                var max_msdec = 30.0;
                if (param.AcquisitionType == Common.Enum.AcquisitionType.AIF) {
                    var ceList = rawObj.CollisionEnergyTargets;
                    for (int i = 0; i < ceList.Count; i++) {
                        var targetCE = Math.Round(ceList[i], 2); // must be rounded by 2 decimal points
                        if (targetCE <= 0) {
                            Console.WriteLine("No correct CE information in AIF-MSDEC");
                            continue;
                        }
                        var max_msdec_aif = max_msdec / ceList.Count;
                        var initial_msdec_aif = initial_msdec + max_msdec_aif * i;
                        targetCE2MSDecResults[targetCE] = new Ms2Dec(initial_msdec_aif, max_msdec_aif).GetMS2DecResults(
                            spectrumList, chromPeakFeatures, param, summary, reportAction, token, targetCE);
                    }
                }
                else {
                    var targetCE = rawObj.CollisionEnergyTargets.IsEmptyOrNull() ? -1 : Math.Round(rawObj.CollisionEnergyTargets[0], 2);
                    targetCE2MSDecResults[targetCE] = new Ms2Dec(initial_msdec, max_msdec).GetMS2DecResults(
                           spectrumList, chromPeakFeatures, param, summary, reportAction, token);
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
                    new Annotation(initial_annotation_local, max_annotation_local).MainProcess(spectrumList, chromPeakFeatures, msdecResults, mspDB, textDB, param, reportAction);
                }

                // characterizatin
                new PeakCharacterEstimator(90, 10).Process(spectrumList, chromPeakFeatures, 
                    targetCE2MSDecResults.Any() ? targetCE2MSDecResults.Argmin(kvp => kvp.Key).Value : null, 
                    param, reportAction);

                // file save
                var paifile = file.PeakAreaBeanInformationFilePath;
                MsdialSerializer.SaveChromatogramPeakFeatures(paifile, chromPeakFeatures);

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
}
