using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class PeakDetectionSettingModel : BindableBase
    {
        public PeakDetectionSettingModel(PeakPickBaseParameter parameter, ProcessOption process) {
            PeakPickSettingModel = new PeakPickSettingModel(parameter);
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;
        }

        public PeakPickSettingModel PeakPickSettingModel { get; }

        public bool IsReadOnly { get; }

        public void Commit() {
            if (IsReadOnly) {
                return;
            }
            PeakPickSettingModel.Commit();
        }

        public void LoadParameter(PeakPickBaseParameter parameter) {
            if (IsReadOnly) {
                return;
            }
            PeakPickSettingModel.LoadParameter(parameter);
        }
    }
}
