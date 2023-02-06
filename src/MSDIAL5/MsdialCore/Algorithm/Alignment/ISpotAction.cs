using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public interface ISpotAction
    {
        void Process(IEnumerable<AlignmentSpotProperty> spots);
    }
}
