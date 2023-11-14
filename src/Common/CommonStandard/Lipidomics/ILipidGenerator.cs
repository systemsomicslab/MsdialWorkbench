using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface ILipidGenerator {
        bool CanGenerate(ILipid lipid);
        IEnumerable<ILipid> Generate(ILipid lipid);
        IEnumerable<ILipid> GenerateUntil(ILipid lipid, Func<ILipid, bool> predicate);
    }

    public class LipidGenerator : ILipidGenerator
    {
        public LipidGenerator(ITotalChainVariationGenerator totalChainGenerator) {
            this.totalChainGenerator = totalChainGenerator;
        }

        public LipidGenerator() : this(new TotalChainVariationGenerator(minLength: 6, begin: 3, end: 3, skip: 3)) {

        }

        private readonly ITotalChainVariationGenerator totalChainGenerator;

        public bool CanGenerate(ILipid lipid) {
            return lipid.ChainCount >= 1;
        }

        public IEnumerable<ILipid> Generate(Lipid lipid) {
            return lipid.Chains.GetCandidateSets(totalChainGenerator)
                .Select(chains => new Lipid(lipid.LipidClass, lipid.Mass, chains));
        }
    }

}
