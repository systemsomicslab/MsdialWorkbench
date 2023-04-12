using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class GcmsPeakDetectionSettingModel : BindableBase
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
            set {
                if (SetProperty(ref _accuracyType, value)) {
                    OnPropertyChanged(nameof(IsAccurateMS));
                }
            }
        }
        private AccuracyType _accuracyType;

        public bool IsAccurateMS {
            get {
                return AccuracyType == AccuracyType.IsAccurate;
            }
            set {
                if (value) {
                    AccuracyType = AccuracyType.IsAccurate;
                }
                else {
                    AccuracyType = AccuracyType.IsNominal;
                }
            }
        }

        public void Commit() {
            if (IsReadOnly) {
                return;
            }
            PeakPickSettingModel.Commit();
            _chromDecBaseParameter.AccuracyType = AccuracyType;
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
