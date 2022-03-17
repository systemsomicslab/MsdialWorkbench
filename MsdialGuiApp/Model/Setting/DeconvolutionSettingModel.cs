using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Setting
{
    public class DeconvolutionSettingModel : BindableBase
    {
        private readonly ChromDecBaseParameter parameter;

        public DeconvolutionSettingModel(ChromDecBaseParameter parameter, ProcessOption process) {
            this.parameter = parameter;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;
            SigmaWindowValue = parameter.SigmaWindowValue;
            AmplitudeCutoff = parameter.AmplitudeCutoff;
            RemoveAfterPrecursor = parameter.RemoveAfterPrecursor;
            KeptIsotopeRange = parameter.KeptIsotopeRange;
            KeepOriginalPrecurosrIsotopes = parameter.KeepOriginalPrecursorIsotopes;
        }

        public bool IsReadOnly { get; }

        public float SigmaWindowValue {
            get => sigmaWindowValue;
            set => SetProperty(ref sigmaWindowValue, value);
        }
        private float sigmaWindowValue;

        public float AmplitudeCutoff {
            get => amplitudeCufoff;
            set => SetProperty(ref amplitudeCufoff, value);
        }
        private float amplitudeCufoff;

        public bool RemoveAfterPrecursor {
            get => removeAfterPrecurosr;
            set => SetProperty(ref removeAfterPrecurosr, value);
        }
        private bool removeAfterPrecurosr;

        public float KeptIsotopeRange {
            get => keptIsotopeRange;
            set => SetProperty(ref keptIsotopeRange, value);
        }
        private float keptIsotopeRange;

        public bool KeepOriginalPrecurosrIsotopes {
            get => keepOriginalPrecurosrIsotopes;
            set => SetProperty(ref keepOriginalPrecurosrIsotopes, value);
        }
        private bool keepOriginalPrecurosrIsotopes;

        public void Commit() {
            if (IsReadOnly) {
                return;
            }
            parameter.SigmaWindowValue = SigmaWindowValue;
            parameter.AmplitudeCutoff = AmplitudeCutoff;
            parameter.RemoveAfterPrecursor = RemoveAfterPrecursor;
            parameter.KeptIsotopeRange = KeptIsotopeRange;
            parameter.KeepOriginalPrecursorIsotopes = KeepOriginalPrecurosrIsotopes;
        }
    }
}
