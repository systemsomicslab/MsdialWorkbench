using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export;

/// <summary>
/// Provides functionality to access metadata for GNPS file classes.
/// This class implements the <see cref="IFileClassMetaAccessor"/> interface
/// to retrieve headers and content information for analysis files.
/// </summary>
public class GnpsFileClassMetaAccessor : IFileClassMetaAccessor
{
    /// <summary>
    /// Gets the headers for the GNPS file class metadata.
    /// </summary>
    /// <returns>A read-only list of header strings.</returns>
    public IReadOnlyList<string> GetHeaders() => [ "Class", "File type", "Injection order", ];

    /// <summary>
    /// Retrieves the metadata content for a specific analysis file.
    /// </summary>
    /// <param name="file">The <see cref="AnalysisFileBean"/> object representing the analysis file.</param>
    /// <returns>An array of strings containing the metadata content.</returns>
    public string[] GetContent(AnalysisFileBean file) {
        return [
            file.AnalysisFileClass,
            file.AnalysisFileType.ToString(),
            file.AnalysisFileAnalyticalOrder.ToString(),
        ];
    }

    /// <summary>
    /// Retrieves the metadata content for a collection of analysis files.
    /// </summary>
    /// <param name="files">An enumerable collection of <see cref="AnalysisFileBean"/> objects.</param>
    /// <returns>A jagged array of strings containing the metadata content for each file.</returns>
    public string[][] GetContents(IEnumerable<AnalysisFileBean> files) {
        return files.Select(GetContent).ToArray();
    }
}
