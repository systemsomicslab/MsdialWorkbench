using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public interface ISpotFilter
    {
        IEnumerable<AlignmentSpotProperty> Filter(IEnumerable<AlignmentSpotProperty> spots);
    }
}
