namespace CompMs.Common.Algorithm.PeakPick;

/// <summary>
/// Represents the parameters required to estimate the noise level in chromatographic data analysis.
/// </summary>
/// <remarks>
/// This class encapsulates various settings used in noise estimation for chromatographic data.
/// It includes parameters for configuring bin sizes, minimum window size for valid analysis, a minimum noise level to return in edge cases, and a noise factor to adjust sensitivity.
/// These settings are utilized in methods like <see cref="GetMinimumNoiseLevel"/> to control how the noise estimation is performed and to handle scenarios where insufficient data is available for accurate estimation.
/// </remarks>
public sealed class NoiseEstimateParameter {
    /// <summary>
    /// Gets or sets the bin size for grouping peaks in the noise calculation.
    /// Larger bins provide a broader analysis but may smooth over smaller variations in noise.
    /// </summary>
    public int NoiseEstimateBin { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of bins required for the analysis to be considered valid.
    /// If the number of valid bins is below this value, a default or specified minimum noise level is returned.
    /// </summary>
    public int MinimumNoiseWindowSize { get; set; }

    /// <summary>
    /// Gets or sets the minimum noise level to return if the calculated noise level is below this value or if there are not enough bins to make a valid estimation.
    /// This acts as a safety threshold to avoid underestimating noise in sparse data.
    /// </summary>
    public double MinimumNoiseLevel { get; set; }

    /// <summary>
    /// Gets or sets the noise factor that can be used to adjust the sensitivity of the noise estimation.
    /// This parameter can help fine-tune the noise estimation algorithm to be more or less sensitive to intensity variations.
    /// </summary>
    public double NoiseFactor { get; set; }

    internal readonly static NoiseEstimateParameter GlobalParameter = new() {
        MinimumNoiseLevel = 0d,
        MinimumNoiseWindowSize = 10,
        NoiseEstimateBin = 50,
        NoiseFactor = 3d,
    };
}
