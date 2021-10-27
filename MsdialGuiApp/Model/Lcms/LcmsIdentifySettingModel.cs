using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Lcms
{
    public sealed class LcmsIdentifySettingModel : IdentifySettingModel
    {
        public LcmsIdentifySettingModel(ParameterBase parameter, DataBaseStorage dataBaseStorage = null)
            : base(parameter, new LcmsAnnotatorSettingFactory(), dataBaseStorage) {

        }
    }
}
