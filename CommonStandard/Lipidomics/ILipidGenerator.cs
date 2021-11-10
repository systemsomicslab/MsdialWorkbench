using CompMs.Common.Utility;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public interface ILipidGenerator {
        bool CanGenerate(ILipid lipid);
        IEnumerable<ILipid> Generate(SubLevelLipid lipid);
        IEnumerable<ILipid> Generate(SomeAcylChainLipid lipid);
        IEnumerable<ILipid> Generate(PositionSpecificAcylChainLipid lipid);
    }

    public class LipidGenerator : ILipidGenerator
    {
        public LipidGenerator(IChainGenerator acylChainGenerator) {
            AcylChainGenerator = acylChainGenerator;
        }

        public LipidGenerator() : this(new AcylChainGenerator(minLength: 6, begin: 3, skip: 3)) {

        }

        public IChainGenerator AcylChainGenerator { get; }

        public bool CanGenerate(ILipid lipid) {
            return lipid.ChainCount >= 1;
        }

        public IEnumerable<ILipid> Generate(SubLevelLipid lipid) {
            return lipid.Chain.GetCandidateSets(AcylChainGenerator)
                .Select(set => new SomeAcylChainLipid(lipid.LipidClass, lipid.Mass, set));
        }

        public IEnumerable<ILipid> Generate(SomeAcylChainLipid lipid) {
            return SearchCollection.Permutations(lipid.Chains)
                .Select(set => new PositionSpecificAcylChainLipid(lipid.LipidClass, lipid.Mass, set));
        }

        public IEnumerable<ILipid> Generate(PositionSpecificAcylChainLipid lipid) {
            var iters = lipid.Chains.Select(c => c.GetCandidates(AcylChainGenerator)).ToArray();
            
            IEnumerable<IChain[]> recurse(int i, IChain[] set) {
                if (i == lipid.ChainCount) {
                    yield return set.ToArray();
                }
                else {
                    foreach (var acyl in iters[i]) {
                        set[i] = acyl;
                        foreach (var res in recurse(i+1, set)) {
                            yield return res;
                        }
                    }
                }
            }

            return recurse(0, new IChain[lipid.ChainCount])
                .Select(chains => new PositionSpecificAcylChainLipid(lipid.LipidClass, lipid.Mass, chains));
        }
    }

}
