using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CompMs.App.Msdial.Model.Lcms
{
    public class LcmsAnalysisParameterSetModel : AnalysisParameterSetModelBase
    {
        public LcmsAnalysisParameterSetModel(MsdialLcmsParameter parameter, IEnumerable<AnalysisFileBean> files, DataBaseStorage dataBaseStorages)
            : base(parameter, files) {

            Parameter = parameter;

            IdentitySettingModel = new IdentifySettingModel(parameter, new LcmsAnnotatorSettingFactory(), dataBaseStorages);
            if (files.Count() <= 1) {
                Parameter.TogetherWithAlignment = false;
            }

            if (Parameter.TargetOmics == TargetOmics.Lipidomics) {
                string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                if (lbmFiles.Length > 0) {
                    IdentitySettingModel.AddDataBaseZZZ();
                    var databaseModel = IdentitySettingModel.DataBaseModels.Last();
                    databaseModel.DataBasePath = lbmFiles.First();
                }
            }
            else if (Parameter.TargetOmics == TargetOmics.Proteomics) {
                Parameter.MaxChargeNumber = 6;
                Parameter.RemoveAfterPrecursor = false;
                //Parameter.MinimumAmplitude = 500000;
                //Parameter.AmplitudeCutoff = 1000;

                //Parameter.RetentionTimeBegin = 10;
                //Parameter.RetentionTimeEnd = 30;
                //Parameter.MassRangeBegin = 400;
                //Parameter.MassRangeEnd = 900;

                Parameter.MspSearchParam.SimpleDotProductCutOff = 0.0F;
                Parameter.MspSearchParam.WeightedDotProductCutOff = 0.0F;
                Parameter.MspSearchParam.ReverseDotProductCutOff = 0.0F;
                Parameter.MspSearchParam.MatchedPeaksPercentageCutOff = 0.0F;
                Parameter.MspSearchParam.MinimumSpectrumMatch = 0.0F;
                Parameter.MspSearchParam.TotalScoreCutoff = 0.0F;
                Parameter.MspSearchParam.AndromedaScoreCutOff = 0.0F;
            }
        }

        public MsdialLcmsParameter Parameter { get; }
        public IdentifySettingModel IdentitySettingModel { get; }
    }
}
