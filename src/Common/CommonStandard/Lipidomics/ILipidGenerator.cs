using System;
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

        private readonly ITotalChainVariationGenerator totalChainGenerator;

        public bool CanGenerate(ILipid lipid) {
            return lipid.ChainCount >= 1;
        }

        public IEnumerable<ILipid> Generate(ILipid lipid) {
            return GenerateCore(lipid);
        }

        private IEnumerable<ILipid> GenerateCore(ILipid lipid) {
            return lipid.Chains.GetCandidateSets(totalChainGenerator)
            .Select(chains => new Lipid(lipid.LipidClass, lipid.Mass, chains));
        }

        public IEnumerable<ILipid> GenerateUntil(ILipid lipid, Func<ILipid, bool> predicate) {
            return GenerateLipid(lipid, predicate);
        }

        private IEnumerable<ILipid> GenerateLipid(ILipid lipid, Func<ILipid, bool> predicate) {
            if (!predicate.Invoke(lipid)) {
                yield break;
            }
            foreach (var lipid_ in GenerateCore(lipid)) {
                yield return lipid_;
                foreach (var lipid__ in GenerateLipid(lipid_, predicate)) {
                    yield return lipid__;
                }
            }
        }
    }

}
