using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CompMs.App.Msdial.Model.Lcimms
{
    public class LcimmsAnalysisParameterSetModel : AnalysisParameterSetModelBase
    {
        public LcimmsAnalysisParameterSetModel(MsdialLcImMsParameter parameter, IReadOnlyCollection<AnalysisFileBean> files, DataBaseStorage dataBaseStorage)
            : base(parameter, files) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }
            Parameter = parameter;

            IdentifySettingModel = new IdentifySettingModel(Parameter, new LcimmsAnnotatorSettingFactory(), Parameter.ProcessOption, dataBaseStorage);
            if (files.Count <= 1) {
                Parameter.ProcessOption &= ~ProcessOption.Alignment;
            }

            if (Parameter.TargetOmics == TargetOmics.Lipidomics) {
                var mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var lbmFiles = Directory.GetFiles(mainDirectory, $"*.{SaveFileFormat.lbm}?", SearchOption.TopDirectoryOnly);
                if (lbmFiles.FirstOrDefault() is string lbmFile) {
                    var databaseModel = IdentifySettingModel.AddDataBaseZZZ();
                    databaseModel.DataBasePath = lbmFile;
                }
            }

            if (Parameter.FileID2CcsCoefficients is null) {
                Parameter.FileID2CcsCoefficients = new Dictionary<int, CoefficientsForCcsCalculation>();
            }
            FileID2CcsCoefficients = Parameter.FileID2CcsCoefficients;
            foreach (var file in files) {
                if (!FileID2CcsCoefficients.ContainsKey(file.AnalysisFileId)) {
                    var calinfo = DataAccess.ReadIonMobilityCalibrationInfo(file.AnalysisFilePath) ?? new RawCalibrationInfo();
                    FileID2CcsCoefficients[file.AnalysisFileId] = new CoefficientsForCcsCalculation(calinfo);
                }
            }
        }

        public MsdialLcImMsParameter Parameter { get; }

        public IdentifySettingModel IdentifySettingModel { get; }

        public Dictionary<int, CoefficientsForCcsCalculation> FileID2CcsCoefficients { get; } 
    }
}
