using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface ILipidGenerator {
        bool CanGenerate(ILipid lipid);
        IEnumerable<ILipid> Generate(Lipid lipid);
    }

    public class LipidGenerator : ILipidGenerator
    {
        public LipidGenerator(IChainGenerator acylChainGenerator) {
            AcylChainGenerator = acylChainGenerator;
        }

        public LipidGenerator() : this(new AcylChainGenerator(minLength: 6, begin: 3, end: 3, skip: 3)) {

        }

        public IChainGenerator AcylChainGenerator { get; }

        public bool CanGenerate(ILipid lipid) {
            return lipid.ChainCount >= 1;
        }

        public IEnumerable<ILipid> Generate(Lipid lipid) {
            return lipid.Chains.GetCandidateSets(AcylChainGenerator)
                .Select(chains => new Lipid(lipid.LipidClass, lipid.Mass, chains));
        }
    }

}
