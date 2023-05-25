using CompMs.Common.DataStructure;

namespace CompMs.Common.Lipidomics
{
    internal sealed class LipidAnnotationLevelConverter : IVisitor<Lipid, ILipid>
    {
        private readonly IVisitor<ITotalChain, ITotalChain> _chainsVisitor;

        public LipidAnnotationLevelConverter(IVisitor<ITotalChain, ITotalChain> chainsVisitor) {
            _chainsVisitor = chainsVisitor ?? throw new System.ArgumentNullException(nameof(chainsVisitor));   
        }

        Lipid IVisitor<Lipid, ILipid>.Visit(ILipid item) {
            var converted = item.Chains.Accept(_chainsVisitor, IdentityDecomposer<ITotalChain, ITotalChain>.Instance);
            if (item.Chains == converted && item is Lipid lipid) {
                return lipid;
            }
            return new Lipid(item.LipidClass, item.Mass, converted);
        }
    }
}
