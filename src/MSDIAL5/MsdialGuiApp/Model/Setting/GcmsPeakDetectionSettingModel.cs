using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class GcmsPeakDetectionSettingModel : BindableBase, IPeakDetectionSettingModel
    {
        private readonly ChromDecBaseParameter _chromDecBaseParameter;

        public GcmsPeakDetectionSettingModel(PeakPickBaseParameter peakPickBaseParameter, ChromDecBaseParameter chromDecBaseParameter, ProcessOption process) {
            _chromDecBaseParameter = chromDecBaseParameter;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;
            PeakPickSettingModel = new PeakPickSettingModel(peakPickBaseParameter);
            AccuracyType = chromDecBaseParameter.AccuracyType;
        }

        public PeakPickSettingModel PeakPickSettingModel { get; }

        public bool IsReadOnly { get; }

        public AccuracyType AccuracyType {
            get => _accuracyType;
            set => SetProperty(ref _accuracyType, value);
        }
        private AccuracyType _accuracyType;

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
        }

        public void LoadParameter(PeakPickBaseParameter parameter) {
            if (IsReadOnly) {
                return;
            }
            PeakPickSettingModel.LoadParameter(parameter);
            AccuracyType = _chromDecBaseParameter.AccuracyType;
        }
    }
}
