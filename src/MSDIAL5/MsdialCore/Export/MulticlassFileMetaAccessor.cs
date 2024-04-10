using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export;

/// <summary>
/// Accesses and constructs metadata for multiclass analysis files, allowing dynamic inclusion of class-specific headers.
/// </summary>
/// <remarks>
/// This accessor facilitates the handling of files that pertain to multiple classes or parameters,
/// dynamically adjusting the metadata headers and content based on specified or discovered class information.
/// </remarks>
public sealed class MulticlassFileMetaAccessor
{
    private static readonly string[] _aboveFields = [
        "Class",
    ];
    private static readonly string[] _belowFields = [
        "File type",
        "Injection order",
        "Batch ID",
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="MulticlassFileMetaAccessor"/> with predefined additional classes.
    /// </summary>
    /// <param name="classes">An array of strings representing the classes to be included in the metadata.</param>
    public MulticlassFileMetaAccessor(string[] classes)
    {
        AdditionalClasses = classes;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MulticlassFileMetaAccessor"/> with a specified number of placeholder classes.
    /// </summary>
    /// <param name="numClasses">The number of classes to generate, named as Parameter1, Parameter2, etc.</param>
    public MulticlassFileMetaAccessor(int numClasses) : this(Enumerable.Range(1, numClasses).Select(i => $"Parameter{i}").ToArray()) { }

    /// <summary>
    /// Gets or sets the additional classes that are included in the metadata headers.
    /// </summary>
    public string[] AdditionalClasses { get; set; }

    /// <summary>
    /// Gets the metadata headers, dynamically including the additional classes between predefined fields.
    /// </summary>
    /// <returns>A read-only list of headers combining predefined and additional class headers.</returns>
    public IReadOnlyList<string> GetHeaders() => [.. _aboveFields, .. AdditionalClasses, .. _belowFields];

    /// <summary>
    /// Constructs the metadata content for a single analysis file, including dynamic class information.
    /// </summary>
    /// <param name="file">The analysis file bean containing the file's metadata.</param>
    /// <returns>An array of metadata content values for the file.</returns>
    public string[] GetContent(AnalysisFileBean file) {
        var classes = file.AnalysisFileClass.Split(['_'], AdditionalClasses.Length);
        if (classes.Length < AdditionalClasses.Length) {
            classes = [.. classes, .. Enumerable.Repeat("NA", AdditionalClasses.Length - classes.Length)];
        }
        return [
            file.AnalysisFileClass,
            .. classes,
            file.AnalysisFileType.ToString(),
            file.AnalysisFileAnalyticalOrder.ToString(),
            file.AnalysisBatch.ToString(),
        ];
    }

    /// <summary>
    /// Constructs the metadata contents for multiple analysis files, including dynamic class information.
    /// </summary>
    /// <param name="files">A collection of analysis file beans.</param>
    /// <returns>A two-dimensional array of metadata content values for the files.</returns>
    public string[][] GetContents(IEnumerable<AnalysisFileBean> files) {
        return files.Select(GetContent).ToArray();
    }
}
