using CompMs.Common.DataStructure;
using System;

namespace CompMs.Common.Lipidomics
{
    internal sealed class ChainsVisitor : IVisitor<ITotalChain, ITotalChain>
    {
        private readonly IAcyclicVisitor _chainVisitor;
        private readonly IVisitor<ITotalChain, ITotalChain> _chainsVisitor;

        public ChainsVisitor(IAcyclicVisitor chainVisitor, IVisitor<ITotalChain, ITotalChain> chainsVisitor) {
            _chainVisitor = chainVisitor ?? throw new ArgumentNullException(nameof(chainVisitor));
            _chainsVisitor = chainsVisitor ?? throw new ArgumentNullException(nameof(chainsVisitor));
        }

        ITotalChain IVisitor<ITotalChain, ITotalChain>.Visit(ITotalChain item) {
            var converted = item.Accept(_chainsVisitor, IdentityDecomposer<ITotalChain, ITotalChain>.Instance);
            return converted.Accept(_chainVisitor, new ChainsDecomposer());
        }
    }
}
