using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export;

/// <summary>
/// Provides a decorator over an existing <see cref="IMetadataAccessor"/> to include ion mode information in metadata.
/// </summary>
/// <param name="accessor">The underlying metadata accessor to decorate.</param>
/// <remarks>
/// This class modifies the behavior of an existing metadata accessor by adding an "Ion mode" column to the content and headers.
/// </remarks>
public sealed class IonModeAlignmentMetdataAccessorDecorator(IMetadataAccessor accessor) : IMetadataAccessor
{
    private static readonly string AdditionalColumn = "Ion mode";

    /// <summary>
    /// Retrieves content from the underlying accessor and adds "Ion mode" information.
    /// </summary>
    /// <param name="spot">The alignment spot property, providing context for ion mode.</param>
    /// <param name="msdec">The MS scan property, used by the underlying accessor to retrieve content.</param>
    /// <returns>A read-only dictionary containing the original content with an added "Ion mode" entry.</returns>
    public IReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec) {
        var contents = accessor.GetContent(spot, msdec);
        if (contents is not IDictionary<string, string> writable) {
            var dict = contents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            contents = dict;
            writable = dict;
        }
        writable[AdditionalColumn] = $"{spot.IonMode}";
        return contents;
    }

    /// <summary>
    /// Retrieves headers from the underlying accessor and adds an "Ion mode" column.
    /// </summary>
    /// <returns>An array of headers with an added "Ion mode" entry.</returns>
    public string[] GetHeaders() {
        var header = accessor.GetHeaders();
        var idx = Array.IndexOf(header, "MSMS spectrum");
        if (idx == -1) {
            idx = header.Length;
        }
        var result = new string[header.Length + 1];
        Array.Copy(header, 0, result, 0, idx);
        result[idx] = AdditionalColumn;
        Array.Copy(header, idx, result, idx + 1, header.Length - idx);
        return result;
    }
}
