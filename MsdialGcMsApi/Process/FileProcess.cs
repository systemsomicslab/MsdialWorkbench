using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Algorithm;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace CompMs.MsdialGcMsApi.Process {
    public sealed class FileProcess {
        private FileProcess() { }

        public static void Run(AnalysisFileBean file, MsdialDataStorage container, bool isGuiProcess = false, 
            Action<int> reportAction = null, CancellationToken token = new CancellationToken()) {
            var param = (MsdialGcmsParameter)container.ParameterBase;
            var mspDB = container.MspDB;
            var iupacDB = container.IupacDatabase;
            var filepath = file.AnalysisFilePath;
            var fileID = file.AnalysisFileId;
            using (var access = new RawDataAccess(filepath, 0, false, isGuiProcess, file.RetentionTimeCorrectionBean.PredictedRt)) {

                // parse raw data
                Console.WriteLine("Loading spectral information");
                var rawObj = DataAccess.GetRawDataMeasurement(access);
                var spectrumList = rawObj.SpectrumList;

                // feature detections
                Console.WriteLine("Peak picking started");
                var chromPeakFeatures = new PeakSpotting(0, 30).Run(rawObj, param, reportAction);
                IsotopeEstimator.Process(chromPeakFeatures, param, iupacDB);
                var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(spectrumList, chromPeakFeatures, param);
                file.ChromPeakFeaturesSummary = summary;

                // chrom deconvolutions
                Console.WriteLine("Deconvolution started");
                var initial_msdec = 30.0;
                var max_msdec = 30.0;
                var msdecResults = new Ms1Dec(initial_msdec, max_msdec).GetMSDecResults(spectrumList, chromPeakFeatures, param, summary, reportAction, token);
                
                // annotations
                Console.WriteLine("Annotation started");
                var initial_annotation = 60.0;
                var max_annotation = 30.0;

                Dictionary<int, float> carbon2RtDict = null;
                if (!param.FileIdRiInfoDictionary.IsEmptyOrNull() && param.FileIdRiInfoDictionary.ContainsKey(fileID))
                    carbon2RtDict = param.FileIdRiInfoDictionary[fileID].RiDictionary;
                new Annotation(initial_annotation, max_annotation).MainProcess(msdecResults, mspDB, param, carbon2RtDict, reportAction);

                // file save
                var paifile = file.PeakAreaBeanInformationFilePath;
                MsdialSerializer.SaveChromatogramPeakFeatures(paifile, chromPeakFeatures);

                var dclfile = file.DeconvolutionFilePath;
                MsdecResultsWriter.Write(dclfile, msdecResults, true);
            }
        }
    }
}
