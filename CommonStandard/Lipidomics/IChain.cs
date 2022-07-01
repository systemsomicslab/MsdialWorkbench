using CompMs.Common.DataStructure;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    public interface IChain : IVisitableElement<IChain> {
        int CarbonCount { get; }
        IDoubleBond DoubleBond { get; }
        IOxidized Oxidized { get; }
        int DoubleBondCount { get; }
        int OxidizedCount { get; }
        double Mass { get; }

        IEnumerable<IChain> GetCandidates(IChainGenerator generator);
    }
}
