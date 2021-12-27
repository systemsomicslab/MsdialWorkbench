using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class FacadeLipidSpectrumGenerator : ILipidSpectrumGenerator
    {
        private readonly Dictionary<LbmClass, List<ILipidSpectrumGenerator>> map = new Dictionary<LbmClass, List<ILipidSpectrumGenerator>>();

        public bool CanGenerate(ILipid lipid, AdductIon adduct) {
            if (map.TryGetValue(lipid.LipidClass, out var generators)) {
                return generators.Any(gen => gen.CanGenerate(lipid, adduct));
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
            if (map.TryGetValue(lipid.LipidClass, out var generators)) {
                var generator = generators.FirstOrDefault(gen => gen.CanGenerate(lipid, adduct));
                return generator?.Generate(lipid, adduct, molecule);
            }
            return null;
        }

        public void Add(LbmClass lipidClass, ILipidSpectrumGenerator generator) {
            if (!map.ContainsKey(lipidClass)) {
                map.Add(lipidClass, new List<ILipidSpectrumGenerator>());
            }
            map[lipidClass].Add(generator);
        }

        public void Remove(LbmClass lipidClass, ILipidSpectrumGenerator generator) {
            if (map.ContainsKey(lipidClass)) {
                map[lipidClass].Remove(generator);
            }
        }
    }
}
