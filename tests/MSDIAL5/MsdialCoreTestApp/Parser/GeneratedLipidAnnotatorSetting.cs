using CompMs.Common.Parameter;

namespace CompMs.App.MsdialConsole.Parser;

/// <summary>
/// How the in silico lipid library for EAD, OAD and EID spectra is built and searched.
/// </summary>
/// <remarks>
/// The GUI adds this library by itself when a lipidomics project's collision type is EAD (EIEIO),
/// OAD or EID, and the Console does the same. The library is not read from a file: its references
/// are generated, peak by peak, from the lipid an LBM or MSP library has already matched, with the
/// chain, sn-position and double-bond position variations that the fragmentation can resolve.
/// </remarks>
public sealed class GeneratedLipidAnnotatorSetting
{
    /// <summary>
    /// Whether the library is used. Null follows the project: on for a lipidomics project whose
    /// collision type is EAD, OAD or EID, and off otherwise.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// The annotator's priority. Null places it above every other annotator, which is where the
    /// GUI puts it: the generated library is listed first, and it is the more specific of the two
    /// answers when both the LBM library and the generated library match a peak.
    /// </summary>
    public int? Priority { get; set; }

    public MsRefSearchParameterBase SearchParameter { get; } = CreateDefaultSearchParameter();

    /// <summary>
    /// The GUI's defaults for a new generated-lipid annotator (LcmsEadLipidAnnotatorSettingModel).
    /// </summary>
    public static MsRefSearchParameterBase CreateDefaultSearchParameter() {
        return new MsRefSearchParameterBase {
            SimpleDotProductCutOff = 0.15F,
            WeightedDotProductCutOff = 0.15F,
            ReverseDotProductCutOff = 0.3F,
            MatchedPeaksPercentageCutOff = 0.0F,
            MinimumSpectrumMatch = 1,
        };
    }
}
