using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsAnalysisParameterSetModel : AnalysisParameterSetModelBase
    {
        public DimsAnalysisParameterSetModel(MsdialDimsParameter parameter, IEnumerable<AnalysisFileBean> files)
            : base(parameter, files) {
            Parameter = parameter;

            DataCollectionSettingModel = new DimsDataCollectionSettingModel(parameter.ProcessBaseParam, parameter.PeakPickBaseParam, parameter.ProviderFactoryParameter);
            IdentifySettingModel = new IdentifySettingModel(parameter, new DimsAnnotatorSettingModelFactory(), Parameter.ProcessOption);

            if (Parameter.TargetOmics == TargetOmics.Lipidomics) {
                string mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var lbmFiles = Directory.GetFiles(mainDirectory, $"*.{SaveFileFormat.lbm}?", SearchOption.TopDirectoryOnly);
                if (lbmFiles.Length > 0) {
                    IdentifySettingModel.AddDataBaseZZZ();
                    var databaseModel = IdentifySettingModel.DataBaseModels.Last();
                    databaseModel.DataBasePath = lbmFiles.First();
                }
            }
            else if (Parameter.TargetOmics == TargetOmics.Proteomics) {
                Parameter.MaxChargeNumber = 6;
                Parameter.MinimumAmplitude = 100000;
                Parameter.AmplitudeCutoff = 1000;
            }
        }

        public MsdialDimsParameter Parameter { get; }

        public DimsDataCollectionSettingModel DataCollectionSettingModel { get; }
        public IdentifySettingModel IdentifySettingModel { get; private set; }
    }
}
