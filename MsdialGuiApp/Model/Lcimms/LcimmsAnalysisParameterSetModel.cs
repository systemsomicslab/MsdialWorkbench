using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel;
using CompMs.Common.DataObj;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Lcimms
{
    public class LcimmsAnalysisParameterSetModel : AnalysisParameterSetModelBase
    {
        public LcimmsAnalysisParameterSetModel(MsdialLcImMsParameter parameter, IEnumerable<AnalysisFileBean> files)
            : base(parameter, files) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            Parameter = parameter;

            if (parameter.FileID2CcsCoefficients is null) {
                parameter.FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>();
            }
            FileID2CcsCoefficients = parameter.FileID2CcsCoefficients;
            foreach (var file in files) {
                if (!FileID2CcsCoefficients.ContainsKey(file.AnalysisFileId)) {
                    var calinfo = DataAccess.ReadIonMobilityCalibrationInfo(file.AnalysisFilePath) ?? new RawCalibrationInfo();
                    FileID2CcsCoefficients[file.AnalysisFileId] = new CoefficientsForCcsCalculation(calinfo);
                }
            }
        }

        public MsdialLcImMsParameter Parameter { get; }

        public Dictionary<int, CoefficientsForCcsCalculation> FileID2CcsCoefficients { get; } 
    }
}
