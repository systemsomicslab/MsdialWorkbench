using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
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
        public LcmsAnalysisParameterSetModel(MsdialLcmsParameter parameter, IEnumerable<AnalysisFileBean> files)
            : base(parameter, files) {

            Parameter = parameter;

            IdentitySettingModel = new LcmsIdentitySettingModel(parameter);

            if (Parameter.TargetOmics == TargetOmics.Lipidomics) {
                string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var lbmFiles = Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "?", SearchOption.TopDirectoryOnly);
                IdentitySettingModel.AddDataBase();
                var databaseModel = IdentitySettingModel.DataBaseModels.Last();
                databaseModel.DataBasePath = lbmFiles.First();
            }
        }

        public MsdialLcmsParameter Parameter { get; }
        public LcmsIdentitySettingModel IdentitySettingModel { get; }
    }
}
