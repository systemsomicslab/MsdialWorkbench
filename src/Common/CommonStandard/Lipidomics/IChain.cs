using CompMs.Common.DataStructure;
using System;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    public interface IChain : IVisitableElement, IEquatable<IChain> {
        int CarbonCount { get; }
        IDoubleBond DoubleBond { get; }
        IOxidized Oxidized { get; }
        int DoubleBondCount { get; }
        int OxidizedCount { get; }
        double Mass { get; }

        bool Includes(IChain chain);
        IEnumerable<IChain> GetCandidates(IChainGenerator generator);
    }
}
