using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Export;

public interface IFileClassMetaAccessor
{
    string[] GetContent(AnalysisFileBean file);
    string[][] GetContents(IEnumerable<AnalysisFileBean> files);
    IReadOnlyList<string> GetHeaders();
}