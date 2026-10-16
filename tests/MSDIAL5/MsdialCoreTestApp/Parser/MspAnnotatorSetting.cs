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
        DataBaseSource dataBaseSource = DataBaseSource.Msp) {
        AnnotatorId = annotatorId;
        MspFilePath = mspFilePath;
        Priority = priority;
        SearchParameter = searchParameter;
        TargetOmics = targetOmics;
        DataBaseSource = dataBaseSource;
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
}
