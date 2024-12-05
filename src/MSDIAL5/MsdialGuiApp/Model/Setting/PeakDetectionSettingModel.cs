using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IPeakDetectionSettingModel {
        bool IsReadOnly { get; }
        void LoadParameter(PeakPickBaseParameter parameter);
        void Commit();
    }

    public sealed class PeakDetectionSettingModel : BindableBase, IPeakDetectionSettingModel
    {

        public PeakDetectionSettingModel(PeakPickBaseParameterModel peakPickBaseParameterModel, ProcessOption process) {
            PeakPickSettingModel = peakPickBaseParameterModel;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;
        }

        public PeakPickBaseParameterModel PeakPickSettingModel { get; }

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
