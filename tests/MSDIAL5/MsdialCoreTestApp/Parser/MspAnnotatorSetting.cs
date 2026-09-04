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
        TargetOmics? targetOmics = null) {
        AnnotatorId = annotatorId;
        MspFilePath = mspFilePath;
        Priority = priority;
        SearchParameter = searchParameter;
        TargetOmics = targetOmics;
    }

    public string AnnotatorId { get; }
    public string MspFilePath { get; }
    public int Priority { get; }
    public MsRefSearchParameterBase SearchParameter { get; }
    public TargetOmics? TargetOmics { get; }
}
