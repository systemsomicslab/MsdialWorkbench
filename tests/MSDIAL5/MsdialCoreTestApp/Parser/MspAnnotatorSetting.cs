using CompMs.Common.Parameter;

namespace CompMs.App.MsdialConsole.Parser;

public sealed class MspAnnotatorSetting
{
    public MspAnnotatorSetting(string annotatorId, string mspFilePath, int priority, MsRefSearchParameterBase searchParameter) {
        AnnotatorId = annotatorId;
        MspFilePath = mspFilePath;
        Priority = priority;
        SearchParameter = searchParameter;
    }

    public string AnnotatorId { get; }
    public string MspFilePath { get; }
    public int Priority { get; }
    public MsRefSearchParameterBase SearchParameter { get; }
}
