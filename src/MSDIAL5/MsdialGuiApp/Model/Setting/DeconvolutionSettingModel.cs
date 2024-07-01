using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class DeconvolutionSettingModel : BindableBase
    {
        private readonly ChromDecBaseParameter _parameter;

        public DeconvolutionSettingModel(ChromDecBaseParameter parameter, ProcessOption process) {
            _parameter = parameter;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;
            SigmaWindowValue = parameter.SigmaWindowValue;
            AmplitudeCutoff = parameter.AmplitudeCutoff;
            RemoveAfterPrecursor = parameter.RemoveAfterPrecursor;
            KeptIsotopeRange = parameter.KeptIsotopeRange;
            KeepOriginalPrecurosrIsotopes = parameter.KeepOriginalPrecursorIsotopes;
        }

        public bool IsReadOnly { get; }

        public float SigmaWindowValue {
            get => _sigmaWindowValue;
            set => SetProperty(ref _sigmaWindowValue, value);
        }
        private float _sigmaWindowValue;

        public float AmplitudeCutoff {
            get => _amplitudeCufoff;
            set => SetProperty(ref _amplitudeCufoff, value);
        }
        private float _amplitudeCufoff;

        public bool RemoveAfterPrecursor {
            get => _removeAfterPrecurosr;
            set => SetProperty(ref _removeAfterPrecurosr, value);
        }
        private bool _removeAfterPrecurosr;

        public float KeptIsotopeRange {
            get => _keptIsotopeRange;
            set => SetProperty(ref _keptIsotopeRange, value);
        }
        private float _keptIsotopeRange;

        public bool KeepOriginalPrecurosrIsotopes {
            get => _keepOriginalPrecurosrIsotopes;
            set => SetProperty(ref _keepOriginalPrecurosrIsotopes, value);
        }
        private bool _keepOriginalPrecurosrIsotopes;

        public void Commit() {
            if (IsReadOnly) {
                return;
            }
            _parameter.SigmaWindowValue = SigmaWindowValue;
            _parameter.AmplitudeCutoff = AmplitudeCutoff;
            _parameter.RemoveAfterPrecursor = RemoveAfterPrecursor;
            _parameter.KeptIsotopeRange = KeptIsotopeRange;
            _parameter.KeepOriginalPrecursorIsotopes = KeepOriginalPrecurosrIsotopes;
        }

        public void LoadParameter(ChromDecBaseParameter parameter) {
            if (IsReadOnly) {
                return;
            }
            SigmaWindowValue = parameter.SigmaWindowValue;
            AmplitudeCutoff = parameter.AmplitudeCutoff;
            RemoveAfterPrecursor = parameter.RemoveAfterPrecursor;
            KeptIsotopeRange = parameter.KeptIsotopeRange;
            KeepOriginalPrecurosrIsotopes = parameter.KeepOriginalPrecursorIsotopes;
        }
    }
}
