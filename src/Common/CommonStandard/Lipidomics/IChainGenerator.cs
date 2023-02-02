using System.Collections.Generic;

namespace CompMs.Common.Lipidomics
{
    public interface IChainGenerator
    {
        IEnumerable<IChain> Generate(AcylChain chain);

        IEnumerable<IChain> Generate(AlkylChain chain);

        IEnumerable<IChain> Generate(SphingoChain chain);

        bool CarbonIsValid(int carbon);

        bool DoubleBondIsValid(int carbon, int doubleBond);
    }
}
