using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class GcmsPeakDetectionSettingModel : BindableBase, IPeakDetectionSettingModel
    {
        private readonly ChromDecBaseParameter _chromDecBaseParameter;
        private readonly MsdialGcmsParameter _gcmsParameter;

        public GcmsPeakDetectionSettingModel(PeakPickBaseParameterModel peakPickBaseParameterModel, ChromDecBaseParameter chromDecBaseParameter, ProcessOption process, MsdialGcmsParameter gcmsParameter) {
            _chromDecBaseParameter = chromDecBaseParameter;
            _gcmsParameter = gcmsParameter;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;
            PeakPickSettingModel = peakPickBaseParameterModel;
            AccuracyType = chromDecBaseParameter.AccuracyType;
            ModulationTimeInSeconds = gcmsParameter.ModulationTime * 60d;
            IsGcxgcProcess = gcmsParameter.MachineCategory == MachineCategory.GCGCMS;
        }

        public PeakPickBaseParameterModel PeakPickSettingModel { get; }

        public bool IsReadOnly { get; }

        public AccuracyType AccuracyType {
            get => _accuracyType;
            set => SetProperty(ref _accuracyType, value);
        }
        private AccuracyType _accuracyType;

        public double ModulationTimeInSeconds {
            get => _modulationTimeInSeconds;
            set => SetProperty(ref _modulationTimeInSeconds, value);
        }
        private double _modulationTimeInSeconds;

        public bool IsGcxgcProcess { get; }

        public void Commit() {
            if (IsReadOnly) {
                return;
            }
            _chromDecBaseParameter.AccuracyType = AccuracyType;
            if (AccuracyType == AccuracyType.IsNominal) {
                PeakPickSettingModel.MassSliceWidth = .5f;
                PeakPickSettingModel.CentroidMs1Tolerance = .5f;
            }
            PeakPickSettingModel.Commit();
            _gcmsParameter.ModulationTime = ModulationTimeInSeconds / 60d;
        }

        public void LoadParameter(PeakPickBaseParameter parameter) {
            if (IsReadOnly) {
                return;
            }
            PeakPickSettingModel.LoadParameter(parameter);
            AccuracyType = _chromDecBaseParameter.AccuracyType;
        }

        public void LoadParameter(ParameterBase parameter) {
            LoadParameter(parameter.PeakPickBaseParam);

            if (_gcmsParameter.MachineCategory == MachineCategory.GCGCMS && parameter is MsdialGcmsParameter gcmsParameter) {
                ModulationTimeInSeconds = gcmsParameter.ModulationTime * 60d;
            }
        }
    }
}
