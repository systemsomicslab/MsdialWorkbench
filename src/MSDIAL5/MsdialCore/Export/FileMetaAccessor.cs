using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export;

public sealed class FileMetaAccessor
{
    private static readonly string[] _fields = [
        "Class",
        "File type",
        "Injection order",
        "Batch ID",
    ];

    public IReadOnlyList<string> GetHeaders() => _fields;

    public string[] GetContent(AnalysisFileBean file) =>
    [
        file.AnalysisFileClass,
        file.AnalysisFileType.ToString(),
        file.AnalysisFileAnalyticalOrder.ToString(),
        file.AnalysisBatch.ToString(),
    ];

    public string[][] GetContents(IEnumerable<AnalysisFileBean> files) => files.Select(GetContent).ToArray();
}
