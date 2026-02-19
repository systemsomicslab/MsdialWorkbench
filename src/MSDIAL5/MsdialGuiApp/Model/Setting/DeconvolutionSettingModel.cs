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
            RelativeAmplitudeCutoff = parameter.RelativeAmplitudeCutoff;
            RemoveAfterPrecursor = parameter.RemoveAfterPrecursor;
            KeptIsotopeRange = parameter.KeptIsotopeRange;
            KeepOriginalPrecurosrIsotopes = parameter.KeepOriginalPrecursorIsotopes;
            ExecuteQ1Deconvolution = parameter.ExecuteQ1Deconvolution;
            ExecuteChromDeconvolution = parameter.ExecuteChromDeconvolution;
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

        public float RelativeAmplitudeCutoff {
            get => _relativeAmplitudeCutoff;
            set => SetProperty(ref _relativeAmplitudeCutoff, value);
        }
        private float _relativeAmplitudeCutoff;

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

        public bool ExecuteChromDeconvolution {
            get => _executeChromDeconvolution;
            set => SetProperty(ref _executeChromDeconvolution, value);
        }
        private bool _executeChromDeconvolution;

        public bool ExecuteQ1Deconvolution {
            get => _executeQ1Deconvolution;
            set => SetProperty(ref _executeQ1Deconvolution, value);
        }
        private bool _executeQ1Deconvolution;

        public int Q1DeconvolutionMs2AcquisitionRadius {
            get => _q1DeconvolutionMs2AcquisitionRadius;
            set => SetProperty(ref _q1DeconvolutionMs2AcquisitionRadius, value);
        }
        private int _q1DeconvolutionMs2AcquisitionRadius;

        public void Commit() {
            if (IsReadOnly) {
                return;
            }
            _parameter.SigmaWindowValue = SigmaWindowValue;
            _parameter.AmplitudeCutoff = AmplitudeCutoff;
            _parameter.RelativeAmplitudeCutoff = RelativeAmplitudeCutoff;
            _parameter.RemoveAfterPrecursor = RemoveAfterPrecursor;
            _parameter.KeptIsotopeRange = KeptIsotopeRange;
            _parameter.KeepOriginalPrecursorIsotopes = KeepOriginalPrecurosrIsotopes;
            _parameter.ExecuteChromDeconvolution = ExecuteChromDeconvolution;
            _parameter.ExecuteQ1Deconvolution = ExecuteQ1Deconvolution;
            _parameter.Q1DeconvolutionMs2AcquisitionRadius = Q1DeconvolutionMs2AcquisitionRadius;
        }

        public void LoadParameter(ChromDecBaseParameter parameter) {
            if (IsReadOnly) {
                return;
            }
            SigmaWindowValue = parameter.SigmaWindowValue;
            AmplitudeCutoff = parameter.AmplitudeCutoff;
            RelativeAmplitudeCutoff = parameter.RelativeAmplitudeCutoff;
            RemoveAfterPrecursor = parameter.RemoveAfterPrecursor;
            KeptIsotopeRange = parameter.KeptIsotopeRange;
            KeepOriginalPrecurosrIsotopes = parameter.KeepOriginalPrecursorIsotopes;
            ExecuteChromDeconvolution = parameter.ExecuteChromDeconvolution;
            ExecuteQ1Deconvolution = parameter.ExecuteQ1Deconvolution;
            Q1DeconvolutionMs2AcquisitionRadius = parameter.Q1DeconvolutionMs2AcquisitionRadius;
        }
    }
}
