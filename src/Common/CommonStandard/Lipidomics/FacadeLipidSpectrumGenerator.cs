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

        public IMSScanProperty? Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null) {
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
                    generator.Add(LbmClass.CL, new CLSpectrumGenerator());
                    generator.Add(LbmClass.BMP, new BMPSpectrumGenerator());
                    generator.Add(LbmClass.HBMP, new HBMPSpectrumGenerator());

                    //generator.Add(LbmClass.MG, new MGSpectrumGenerator());
                    generator.Add(LbmClass.DGTS, new DGTSSpectrumGenerator());
                    generator.Add(LbmClass.LDGTA, new LDGTASpectrumGenerator());
                    generator.Add(LbmClass.LDGTS, new LDGTSSpectrumGenerator());
                    generator.Add(LbmClass.DGTA, new DGTASpectrumGenerator());
                    generator.Add(LbmClass.DMEDFAHFA, new DMEDFAHFASpectrumGenerator());
                    generator.Add(LbmClass.DMEDFA, new DMEDFASpectrumGenerator());
                    generator.Add(LbmClass.DMEDOxFA, new DMEDFASpectrumGenerator());
                    generator.Add(LbmClass.CAR, new CARSpectrumGenerator());
                    generator.Add(LbmClass.DG, new DGSpectrumGenerator());
                    generator.Add(LbmClass.TG, new TGSpectrumGenerator());

                    generator.Add(LbmClass.SM, new SMSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NDS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NP, new CeramidePhytoSphSpectrumGenerator());
                    generator.Add(LbmClass.Cer_AS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_ADS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_AP, new CeramidePhytoSphSpectrumGenerator());
                    generator.Add(LbmClass.Cer_BS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_BDS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_ABP, new CeramidePhytoSphSpectrumGenerator());
                    //generator.Add(LbmClass.Cer_HS, new CeramideSpectrumGenerator());
                    //generator.Add(LbmClass.Cer_HDS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.HexCer_NS, new HexCerSpectrumGenerator());
                    generator.Add(LbmClass.HexCer_NDS, new HexCerSpectrumGenerator());
                    //generator.Add(LbmClass.Hex2Cer, new Hex2CerSpectrumGenerator());
                    generator.Add(LbmClass.GM3, new GM3SpectrumGenerator());
                    generator.Add(LbmClass.SHexCer, new SHexCerSpectrumGenerator());
                   
                    //generator.Add(LbmClass.CE, new CESpectrumGenerator());
                    generator.Add(LbmClass.PC_d5, new PCd5SpectrumGenerator());

                    generator.Add(LbmClass.PE_d5, new PEd5SpectrumGenerator());
                    generator.Add(LbmClass.PG_d5, new PGd5SpectrumGenerator());
                    generator.Add(LbmClass.PI_d5, new PId5SpectrumGenerator());
                    generator.Add(LbmClass.PS_d5, new PSd5SpectrumGenerator());
                    generator.Add(LbmClass.LPC_d5, new LPCd5SpectrumGenerator());
                    generator.Add(LbmClass.LPE_d5, new LPEd5SpectrumGenerator());
                    generator.Add(LbmClass.LPG_d5, new LPGd5SpectrumGenerator());
                    generator.Add(LbmClass.LPI_d5, new LPId5SpectrumGenerator());
                    generator.Add(LbmClass.LPS_d5, new LPSd5SpectrumGenerator());
                    generator.Add(LbmClass.DG_d5, new DGd5SpectrumGenerator());

                    generator.Add(LbmClass.TG_d5, new TGd5SpectrumGenerator());
                    generator.Add(LbmClass.CE_d7, new CEd7SpectrumGenerator());
                    generator.Add(LbmClass.SM_d9, new SMd9SpectrumGenerator());
                    generator.Add(LbmClass.Cer_NS_d7, new CerNSd7SpectrumGenerator());

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
                    generator.Add(LbmClass.PC, new PCOadSpectrumGenerator());
                    generator.Add(LbmClass.LPC, new LPCOadSpectrumGenerator());
                    generator.Add(LbmClass.EtherPC, new EtherPCOadSpectrumGenerator());
                    generator.Add(LbmClass.EtherLPC, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.PE, new PEOadSpectrumGenerator());
                    generator.Add(LbmClass.LPE, new LPEOadSpectrumGenerator());
                    generator.Add(LbmClass.EtherPE, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.EtherLPE, new OadDefaultSpectrumGenerator());
                    generator.Add(LbmClass.PG, new PGOadSpectrumGenerator());
                    generator.Add(LbmClass.LPG, new LPGOadSpectrumGenerator());
                    generator.Add(LbmClass.PI, new PIOadSpectrumGenerator());
                    generator.Add(LbmClass.LPI, new LPIOadSpectrumGenerator());
                    generator.Add(LbmClass.PS, new PSOadSpectrumGenerator());
                    generator.Add(LbmClass.LPS, new LPSOadSpectrumGenerator());
                    generator.Add(LbmClass.TG, new TGOadSpectrumGenerator());
                    generator.Add(LbmClass.DG, new DGOadSpectrumGenerator());
                    generator.Add(LbmClass.SM, new SMOadSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NDS, new CeramideOadSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NS, new CeramideOadSpectrumGenerator());
                    generator.Add(LbmClass.DMEDFAHFA, new DMEDFAHFAOadSpectrumGenerator());

                    generator.Add(LbmClass.PC_d5, new PCd5OadSpectrumGenerator());
                    generator.Add(LbmClass.PE_d5, new PEd5OadSpectrumGenerator());
                    generator.Add(LbmClass.PG_d5, new PGd5OadSpectrumGenerator());
                    generator.Add(LbmClass.PI_d5, new PId5OadSpectrumGenerator());
                    generator.Add(LbmClass.PS_d5, new PSd5OadSpectrumGenerator());
                    generator.Add(LbmClass.LPC_d5, new LPCd5OadSpectrumGenerator());
                    generator.Add(LbmClass.LPE_d5, new LPEd5OadSpectrumGenerator());
                    generator.Add(LbmClass.LPG_d5, new LPGd5OadSpectrumGenerator());
                    generator.Add(LbmClass.LPI_d5, new LPId5OadSpectrumGenerator());
                    generator.Add(LbmClass.LPS_d5, new LPSd5OadSpectrumGenerator());
                    generator.Add(LbmClass.DG_d5, new DGd5OadSpectrumGenerator());

                    generator.Add(LbmClass.TG_d5, new TGd5OadSpectrumGenerator());
                    generator.Add(LbmClass.CE_d7, new CEd7OadSpectrumGenerator());
                    generator.Add(LbmClass.SM_d9, new SMd9OadSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NS_d7, new CerNSd7OadSpectrumGenerator());
                    @oadlipidgenerator = generator;
                }
                return @oadlipidgenerator;
            }
        }
        private static ILipidSpectrumGenerator @oadlipidgenerator;
        public static ILipidSpectrumGenerator EidLipidGenerator
        {
            get
            {
                if (@eidlipidgenerator is null)
                {
                    var generator = new FacadeLipidSpectrumGenerator();
                    generator.Add(LbmClass.PC, new PCEidSpectrumGenerator());
                    generator.Add(LbmClass.LPC, new LPCEidSpectrumGenerator());
                    generator.Add(LbmClass.EtherPC, new EtherPCEidSpectrumGenerator());
                    generator.Add(LbmClass.PE, new PEEidSpectrumGenerator());
                    generator.Add(LbmClass.LPE, new LPEEidSpectrumGenerator());
                    generator.Add(LbmClass.EtherPE, new EtherPEEidSpectrumGenerator());
                    generator.Add(LbmClass.PG, new PGEidSpectrumGenerator());
                    generator.Add(LbmClass.PI, new PIEidSpectrumGenerator());
                    generator.Add(LbmClass.PS, new PSEidSpectrumGenerator());
                    generator.Add(LbmClass.PA, new PAEidSpectrumGenerator());
                    generator.Add(LbmClass.LPG, new LPGEidSpectrumGenerator());
                    generator.Add(LbmClass.LPI, new LPIEidSpectrumGenerator());
                    generator.Add(LbmClass.LPS, new LPSEidSpectrumGenerator());
                    generator.Add(LbmClass.CL, new CLEidSpectrumGenerator());
                    generator.Add(LbmClass.MG, new MGEidSpectrumGenerator());
                    generator.Add(LbmClass.DG, new DGEidSpectrumGenerator());
                    generator.Add(LbmClass.TG, new TGEidSpectrumGenerator());
                    generator.Add(LbmClass.BMP, new BMPEidSpectrumGenerator());
                    generator.Add(LbmClass.HBMP, new HBMPEidSpectrumGenerator());
                    // below here are EID not implemented
                    generator.Add(LbmClass.SM, new SMSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NDS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_NP, new CeramidePhytoSphSpectrumGenerator());
                    generator.Add(LbmClass.Cer_AS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_ADS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_AP, new CeramidePhytoSphSpectrumGenerator());
                    generator.Add(LbmClass.Cer_BS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.Cer_BDS, new CeramideSpectrumGenerator());
                    //generator.Add(LbmClass.Cer_HS, new CeramideSpectrumGenerator());
                    //generator.Add(LbmClass.Cer_HDS, new CeramideSpectrumGenerator());
                    generator.Add(LbmClass.HexCer_NS, new HexCerSpectrumGenerator());
                    generator.Add(LbmClass.DGTA, new DGTASpectrumGenerator());
                    generator.Add(LbmClass.DGTS, new DGTSSpectrumGenerator());
                    generator.Add(LbmClass.LDGTA, new LDGTASpectrumGenerator());
                    generator.Add(LbmClass.LDGTS, new LDGTSSpectrumGenerator());
                    generator.Add(LbmClass.GM3, new GM3SpectrumGenerator());
                    generator.Add(LbmClass.SHexCer, new SHexCerSpectrumGenerator());
                    generator.Add(LbmClass.CAR, new CARSpectrumGenerator());
                    @eidlipidgenerator = generator;
                }
                return @eidlipidgenerator;
            }
        }
        private static ILipidSpectrumGenerator @eidlipidgenerator;

    }
}
