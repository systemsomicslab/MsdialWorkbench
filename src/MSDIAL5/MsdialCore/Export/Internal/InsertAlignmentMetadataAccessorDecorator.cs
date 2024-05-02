using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export.Internal;

internal sealed class InsertAlignmentMetadataAccessorDecorator : IMetadataAccessor
{
    public InsertAlignmentMetadataAccessorDecorator(IMetadataAccessor accessor, string additionalColumn, int insertPosition, Func<AlignmentSpotProperty, IMSScanProperty, string> getter)
    {
        Accessor = accessor;
        AdditionalColumn = additionalColumn;
        InsertPosition = insertPosition;
        Getter = getter;
    }

    public IMetadataAccessor Accessor { get; }
    public string AdditionalColumn { get; }
    public int InsertPosition { get; }
    public Func<AlignmentSpotProperty, IMSScanProperty, string> Getter { get; }

    public IReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec) {
        var contents = Accessor.GetContent(spot, msdec);
        if (contents is not IDictionary<string, string> writable) {
            var dict = contents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            contents = dict;
            writable = dict;
        }
        writable[AdditionalColumn] = Getter(spot, msdec);
        return contents;
    }

    public string[] GetHeaders() {
        var header = Accessor.GetHeaders();
        var idx = InsertPosition;
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