using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsIdentifySettingModel : IdentifySettingModel
    {
        public DimsIdentifySettingModel(ParameterBase parameter, DataBaseStorage dataBaseStorage = null)
            : base(parameter, new DimsAnnotatorSettingModelFactory(), dataBaseStorage) {

        }
    }
}
