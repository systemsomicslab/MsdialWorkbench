using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm;
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
            IDataProvider provider,
            AnalysisFileBean file,
            MsdialImmsParameter parameter,
            IupacDatabase iupacDB,
            List<MoleculeMsReference> mspDB,
            List<MoleculeMsReference> textDB,
            Action<int> reportAction = null,
            CancellationToken token = default) {

            var rawObj = provider.LoadMeasurement();
            var spectrumList = provider.LoadRawSpectrum();

            Console.WriteLine("Peak picking started");
            var chromPeakFeatures = new PeakSpotting(0, 30).Run(rawObj, parameter, reportAction);

            IsotopeEstimator.Process(chromPeakFeatures, parameter, iupacDB);

            var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(spectrumList, chromPeakFeatures, parameter);
            file.ChromPeakFeaturesSummary = summary;

            Console.WriteLine("Deconvolution started");
            var targetCE2MSDecResults = new Dictionary<double, List<MSDecResult>>();
            var initial_msdec = 30.0;
            var max_msdec = 30.0;
            if (parameter.AcquisitionType == Common.Enum.AcquisitionType.AIF) {
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
                        spectrumList, chromPeakFeatures, parameter, summary, targetCE, reportAction, token);
                }
            }
            else {
                var targetCE = rawObj.CollisionEnergyTargets.IsEmptyOrNull() ? -1 : Math.Round(rawObj.CollisionEnergyTargets[0], 2);
                targetCE2MSDecResults[targetCE] = new Ms2Dec(initial_msdec, max_msdec).GetMS2DecResults(
                       spectrumList, chromPeakFeatures, parameter, summary, -1, reportAction, token);
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
                new Annotation(initial_annotation_local, max_annotation_local).MainProcess(spectrumList, chromPeakFeatures, msdecResults, mspDB, textDB, parameter, reportAction);
            }

            // characterizatin
            new PeakCharacterEstimator(90, 10).Process(spectrumList, chromPeakFeatures, 
                targetCE2MSDecResults.Any() ? targetCE2MSDecResults.Argmin(kvp => kvp.Key).Value : null, 
                parameter, reportAction);

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
