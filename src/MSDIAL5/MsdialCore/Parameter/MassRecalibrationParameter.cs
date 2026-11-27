using CompMs.MsdialCore.Algorithm.MassCalibration;
using MessagePack;
using System.Collections.Generic;
using System.Globalization;

namespace CompMs.MsdialCore.Parameter
{
    /// <summary>
    /// Whether, and how, an LC-MS project recalibrates each file's MS1 and MS2 m/z before its peaks are picked for good:
    /// a first annotation with wide m/z windows measures the error, a model of it is chosen and judged, and a model worth
    /// applying is recorded with the file (<see cref="DataObj.AnalysisFileBean.MassRecalibration"/>).
    /// </summary>
    /// <remarks>
    /// The GUI's "Mass recalibration" step and the Console's method-file keys set the same values, and
    /// <see cref="ParametersAsText"/> writes them under those keys, so a project's parameter text can be read back as a
    /// method file.
    /// </remarks>
    [MessagePackObject]
    public sealed class MassRecalibrationParameter
    {
        /// <summary>The method-file and parameter-text names of the settings.</summary>
        public const string IsEnabledKey = "Mass recalibration";
        public const string SearchPpmKey = "Mass recalibration search window (ppm)";
        public const string MinimumCorrectionPpmKey = "Mass recalibration minimum correction (ppm)";

        /// <summary>Whether the files are recalibrated; off by default, which reads every file's m/z as recorded.</summary>
        [Key(0)]
        public bool IsEnabled { get; set; }

        /// <summary>The half width, in ppm, of the m/z windows of the first annotation.</summary>
        [Key(1)]
        public double SearchPpm { get; set; } = AnnotationCalibrantCollector.DefaultSearchPpm;

        /// <summary>The smallest modelled error, in ppm, that is applied (with three standard errors).</summary>
        [Key(2)]
        public double MinimumCorrectionPpm { get; set; } = MassCalibrationDecision.DefaultMinimumCorrectionPpm;

        /// <summary>Why these values cannot run, or null when they can.</summary>
        public string? Problem() {
            if (!(SearchPpm > 0d) || double.IsInfinity(SearchPpm)) {
                return $"The mass recalibration search window must be a positive number of ppm, not {SearchPpm.ToString(CultureInfo.InvariantCulture)}.";
            }
            if (!(MinimumCorrectionPpm >= 0d) || double.IsInfinity(MinimumCorrectionPpm)) {
                return $"The mass recalibration minimum correction must be a number of ppm, zero or more, not {MinimumCorrectionPpm.ToString(CultureInfo.InvariantCulture)}.";
            }
            return null;
        }

        /// <summary>The settings as parameter-text lines, which the Console method file reads back.</summary>
        public List<string> ParametersAsText() {
            var invariant = CultureInfo.InvariantCulture;
            return new List<string>
            {
                IsEnabledKey + ": " + IsEnabled.ToString(),
                SearchPpmKey + ": " + SearchPpm.ToString(invariant),
                MinimumCorrectionPpmKey + ": " + MinimumCorrectionPpm.ToString(invariant),
            };
        }
    }
}
