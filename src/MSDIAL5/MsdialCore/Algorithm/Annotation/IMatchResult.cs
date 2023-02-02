using CompMs.Common.DataObj.Result;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMatchResult
    {
        IEnumerable<double> Scores { get; }
        void Assign(MsScanMatchResult result);
    }
}
