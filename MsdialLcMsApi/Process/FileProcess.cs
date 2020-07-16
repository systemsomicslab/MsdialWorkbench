using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Process {
    public sealed class FileProcess {
        private FileProcess() { }

        public static void Run(AnalysisFileBean file, MsdialDataStorage container, bool isGuiProcess = false, Action<int> reportAction = null, CancellationToken token = new CancellationToken()) {
            var param = (MsdialLcmsParameter)container.ParameterBase;
            var mspDB = container.MspDB;
            var textDB = container.TextDB;
            var isotopeTextDB = container.IsotopeTextDB;
            var iupacDB = container.IupacDatabase;
            var filepath = file.AnalysisFilePath;
            var fileID = file.AnalysisFileId;
            using (var access = new RawDataAccess(filepath, fileID, isGuiProcess, file.RetentionTimeCorrectionBean.PredictedRt)) {
                var rawObj = DataAccess.GetRawDataMeasurement(access);
                var spectrumList = rawObj.SpectrumList;
                var chromPeakFeatures = new PeakSpotting(0, 20).Run(spectrumList, param, reportAction);
                IsotopeEstimator.Process(chromPeakFeatures, param, iupacDB);
                
                file.ChromPeakFeaturesSummary = ChromFeatureSummarizer.GetChromFeaturesSummary(spectrumList, chromPeakFeatures, param);
                if (param.AcquisitionType == Common.Enum.AcquisitionType.AIF) {
                }
                else {

                }
            }
        }
    }
}
