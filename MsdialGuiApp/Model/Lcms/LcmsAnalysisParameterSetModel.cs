using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Result;
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
                Parameter.ProcessOption &= ~ProcessOption.Alignment;
            }

            if (Parameter.TargetOmics == TargetOmics.Lipidomics) {
                if (IdentitySettingModel.DataBaseModels.Count == 0) {
                    if (Parameter.CollistionType == CollisionType.EIEIO
                        && IdentitySettingModel.DataBaseModels.All(m => m.DBSource != DataBaseSource.EadLipid)) {
                        var databaseModel = IdentitySettingModel.AddDataBaseZZZ();
                        databaseModel.DBSource = DataBaseSource.EadLipid;
                    }

                    string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                    var lbmFile = lbmFiles.FirstOrDefault();
                    if (!(lbmFile is null)
                        && IdentitySettingModel.DataBaseModels.All(m => m.DBSource != DataBaseSource.Msp)) {
                        var databaseModel = IdentitySettingModel.AddDataBaseZZZ();
                        databaseModel.DataBasePath = lbmFile;
                    }
                }
            }
            else if (Parameter.TargetOmics == TargetOmics.Proteomics) {
                Parameter.MaxChargeNumber = 8;
                Parameter.RemoveAfterPrecursor = false;
            }
        }

        public MsdialLcmsParameter Parameter { get; }
        public IdentifySettingModel IdentitySettingModel { get; }
    }
}
