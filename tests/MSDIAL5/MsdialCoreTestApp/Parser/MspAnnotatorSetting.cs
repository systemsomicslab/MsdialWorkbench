using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Enum;

namespace CompMs.App.MsdialConsole.Parser;

public sealed class MspAnnotatorSetting
{
    public MspAnnotatorSetting(
        string annotatorId,
        string mspFilePath,
        int priority,
        MsRefSearchParameterBase searchParameter,
        TargetOmics? targetOmics = null,
        DataBaseSource dataBaseSource = DataBaseSource.Msp,
        bool allowsSuggestions = true) {
        AnnotatorId = annotatorId;
        MspFilePath = mspFilePath;
        Priority = priority;
        SearchParameter = searchParameter;
        TargetOmics = targetOmics;
        DataBaseSource = dataBaseSource;
        AllowsSuggestions = allowsSuggestions;
    }

    public string AnnotatorId { get; }
    public string MspFilePath { get; }
    public int Priority { get; }
    public MsRefSearchParameterBase SearchParameter { get; }
    public TargetOmics? TargetOmics { get; }

    /// <summary>
    /// Whether this library's spectra were acquired or computed.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="DataBaseSource.Msp"/>, which is what every library was assumed to be
    /// before the question could be asked, so a settings file that says nothing behaves as it did.
    /// The GUI asks this through the database-kind dropdown; this is the same question for a run
    /// that has no GUI, which is every run the reanalysis pipeline makes.
    /// </remarks>
    public DataBaseSource DataBaseSource { get; }

    /// <summary>
    /// Whether this annotator may keep a candidate it did not accept, as a suggestion.
    /// </summary>
    /// <remarks>
    /// Defaults to true, which is what every annotator did before the question could be asked, so a
    /// settings file that says nothing behaves as it did. False keeps only the reference matches:
    /// no precursor-only candidate for a feature without a product-ion spectrum, and no candidate
    /// whose spectrum was compared and fell short. That is what a large in-silico library needs,
    /// where a mass window holds hundreds of generated records and every feature would otherwise
    /// carry a suggestion named after one of them.
    /// </remarks>
    public bool AllowsSuggestions { get; }
}
