using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export.Internal;
using System;

namespace CompMs.MsdialCore.Export;

public static class AlignmentMetadataAccessorExtensions {
    public static IMetadataAccessor Insert(this IMetadataAccessor accessor, string additionalColumn, int insertPosition, Func<AlignmentSpotProperty, IMSScanProperty, string> getter) {
        return new InsertAlignmentMetadataAccessorDecorator(accessor, additionalColumn, insertPosition, getter);
    }
}
