using CompMs.Common.Parameter;

namespace CompMs.App.MsdialConsole.Parser;

public sealed class TextAnnotatorSetting
{
    public TextAnnotatorSetting(string annotatorId, string textDbFilePath, int priority, MsRefSearchParameterBase searchParameter) {
        AnnotatorId = annotatorId;
        TextDbFilePath = textDbFilePath;
        Priority = priority;
        SearchParameter = searchParameter;
    }

    public string AnnotatorId { get; }
    public string TextDbFilePath { get; }
    public int Priority { get; }
    public MsRefSearchParameterBase SearchParameter { get; }
}
