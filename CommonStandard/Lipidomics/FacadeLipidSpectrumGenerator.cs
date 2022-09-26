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

        public static ILipidSpectrumGenerator Default {
            get {
                if (@default is null) {
                    var generator = new FacadeLipidSpectrumGenerator();
                    generator.Add(LbmClass.EtherPC, new EtherPCSpectrumGenerator());
                    generator.Add(LbmClass.EtherPE, new EtherPESpectrumGenerator());
                    generator.Add(LbmClass.LPC, new LPCSpectrumGenerator());
                    generator.Add(LbmClass.LPE, new LPESpectrumGenerator());
                    generator.Add(LbmClass.LPG, new LPGSpectrumGenerator());
                    generator.Add(LbmClass.LPI, new LPISpectrumGenerator());
                    generator.Add(LbmClass.LPS, new LPSSpectrumGenerator());
                    generator.Add(LbmClass.PA, new PASpectrumGenerator());
                    generator.Add(LbmClass.PC, new PCSpectrumGenerator());
                    generator.Add(LbmClass.PE, new PESpectrumGenerator());
                    generator.Add(LbmClass.PG, new PGSpectrumGenerator());
                    generator.Add(LbmClass.PI, new PISpectrumGenerator());
                    generator.Add(LbmClass.PS, new PSSpectrumGenerator());
                    generator.Add(LbmClass.MG, new MGSpectrumGenerator());
                    generator.Add(LbmClass.DG, new DGSpectrumGenerator());
                    generator.Add(LbmClass.TG, new TGSpectrumGenerator());
                    generator.Add(LbmClass.BMP, new BMPSpectrumGenerator());
                    generator.Add(LbmClass.SM, new SMSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.HexCer_NS, new HexCerSpectrumGenerator());
                    generator.Add(LbmClass.DGTA, new DGTASpectrumGenerator());
                    generator.Add(LbmClass.DGTS, new DGTSSpectrumGenerator());
                    generator.Add(LbmClass.LDGTA, new LDGTASpectrumGenerator());
                    generator.Add(LbmClass.LDGTS, new LDGTSSpectrumGenerator());
                    generator.Add(LbmClass.HBMP, new HBMPSpectrumGenerator());
                    generator.Add(LbmClass.GM3, new GM3SpectrumGenerator());
                    generator.Add(LbmClass.SHexCer, new SHexCerSpectrumGenerator());
                    generator.Add(LbmClass.CAR, new CARSpectrumGenerator());
                    generator.Add(LbmClass.CL, new CLSpectrumGenerator());

                    @default = generator;
                }
                return @default;
            }
        }
        private static ILipidSpectrumGenerator @default;

        public static ILipidSpectrumGenerator OadLipidGenerator {
            get {
                if (@oadlipidgenerator is null) {
                    var generator = new FacadeLipidSpectrumGenerator();
                    generator.Add(LbmClass.EtherPC, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.EtherPE, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.LPC, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.LPE, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.LPG, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.LPI, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.LPS, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.PA, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.PC, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.PE, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.PG, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.PI, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.PS, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.MG, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.DG, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.TG, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.BMP, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.SM, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NS, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.HexCer_NS, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.DGTA, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.DGTS, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.LDGTA, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.LDGTS, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.HBMP, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.GM3, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.SHexCer, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.CAR, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.CL, new OadDefaultSpectrumGenerator());

                    @oadlipidgenerator = generator;
                }
                return @oadlipidgenerator;
            }
        }
        private static ILipidSpectrumGenerator @oadlipidgenerator;

    }
}
