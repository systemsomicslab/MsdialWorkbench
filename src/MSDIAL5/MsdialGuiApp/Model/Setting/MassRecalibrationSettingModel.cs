using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    /// <summary>
    /// The "Mass recalibration" step of an LC-MS project's processing settings: whether each file's MS1 and MS2 m/z are
    /// recalibrated from a first annotation before its peaks are picked for good, and with which window and minimum.
    /// </summary>
    /// <remarks>
    /// The values are the project's <see cref="ParameterBase.MassRecalibrationParam"/>, the same the Console reads from
    /// its method file. They take effect when the peaks are picked, so they are read-only unless processing starts from
    /// peak picking; the libraries searched are those of the Identification step, checked when processing starts.
    /// </remarks>
    public sealed class MassRecalibrationSettingModel : BindableBase
    {
        private readonly ParameterBase _parameter;

        public MassRecalibrationSettingModel(ParameterBase parameter, ProcessOption process) {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;
            Load(parameter.MassRecalibrationParam);
        }

        /// <summary>True when processing does not pick peaks, so that these settings would not be used.</summary>
        public bool IsReadOnly { get; }

        /// <summary>Whether each file's m/z are recalibrated; off by default.</summary>
        public bool Recalibrate {
            get => _recalibrate;
            set => SetProperty(ref _recalibrate, value);
        }
        private bool _recalibrate;

        /// <summary>The half width, in ppm, of the m/z windows of the first annotation.</summary>
        public double SearchPpm {
            get => _searchPpm;
            set => SetProperty(ref _searchPpm, value);
        }
        private double _searchPpm;

        /// <summary>The smallest modelled error, in ppm, that is applied.</summary>
        public double MinimumCorrectionPpm {
            get => _minimumCorrectionPpm;
            set => SetProperty(ref _minimumCorrectionPpm, value);
        }
        private double _minimumCorrectionPpm;

        /// <summary>Why the values cannot run, or null when they can.</summary>
        public string? Problem() => ToParameter().Problem();

        /// <summary>
        /// Writes the values into the project's parameter; false, writing nothing, when they cannot run. Read-only, it
        /// writes nothing and does not stop processing.
        /// </summary>
        public bool TryCommit() {
            if (IsReadOnly) {
                return true;
            }
            var parameter = ToParameter();
            if (parameter.Problem() is not null) {
                return false;
            }
            _parameter.MassRecalibrationParam = parameter;
            return true;
        }

        /// <summary>Shows the values of a parameter file being loaded.</summary>
        public void LoadParameter(ParameterBase parameter) {
            if (IsReadOnly || parameter is null) {
                return;
            }
            Load(parameter.MassRecalibrationParam);
        }

        private void Load(MassRecalibrationParameter parameter) {
            Recalibrate = parameter.IsEnabled;
            SearchPpm = parameter.SearchPpm;
            MinimumCorrectionPpm = parameter.MinimumCorrectionPpm;
        }

        private MassRecalibrationParameter ToParameter() {
            return new MassRecalibrationParameter
            {
                IsEnabled = Recalibrate,
                SearchPpm = SearchPpm,
                MinimumCorrectionPpm = MinimumCorrectionPpm,
            };
        }
    }
}
