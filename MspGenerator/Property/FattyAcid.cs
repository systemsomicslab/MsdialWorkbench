using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MspGenerator

{
    public enum LbmClass
    {
        // old version
        Cer, CerP, G1Cer, SM,
        CE, FA,
        lysoDGTS, lysoPC, lysoPE,
        PA, PC, PE, PG, PI, PS,
        plasmenylPC, plasmenylPE,
        MG, DG, TG,
        MGDG, DGDG, SQDG, DGTS, AcylCarnitine,
        Cer_ADS, Cer_AS, Cer_BDS, Cer_BS, Cer_EODS, Cer_EOS,
        Cer_NDS, Cer_NS, Cer_NP, GlcCer_NS, GlcCer_NDS, Cer_AP, GlcCer_AP,
        BMP, FAHFA, lysoPG, lysoPI, lysoPS, lysoPA,
        HemiBMP, DGGA, ADGGA, CL, EtherPC, EtherPE,
        OxPA, OxPC, OxPS, OxPE, OxPG, OxPI, OxFA, EtherOxPC, EtherOxPE,
        CAR, LPC, LPE, LPA, LPS, LPI, LPG, LDGTS, PMeOH, PEtOH, PBtOH, SHexCer, GM3,
        EtherLPC, EtherLPE, Sph, DHSph, PhytoSph,
        LNAPE, LNAPS, Ac2PIM1, Ac2PIM2, Ac3PIM2, Ac4PIM2, LipidA, DLCL, Cer_OS,
        Hex3Cer, Hex2Cer, Cer_EBDS, MLCL, AHexCer, ASM, HBMP, Undefined,
        HexCer_NS, HexCer_NDS, HexCer_AP, Cholesterol,
        CholesterolSulfate, EtherMGDG, EtherDGDG, EtherTG, HexCer_EOS, EtherDG, LDGCC, DGCC,
        EtherPI, EtherPS, PE_Cer, PI_Cer, EtherPG, SL, NAGly, EtherLPG, CoQ,
        DCAE, GDCAE, GLCAE, TDCAE, TLCAE, NAE, Vitamin_E, BileAcid, NAGlySer, EtherLPS, EtherLPI,
        NAOrn, VAE, BRSE, CASE, SISE, STSE, AHexBRS, AHexCAS, AHexCS, AHexSIS, AHexSTS,
        Cer_HS, HexCer_HS, Cer_NDOS,
        SPE, SHex, SPEHex, SPGHex, CSLPHex, BRSLPHex, CASLPHex, SISLPHex, STSLPHex,
        CSPHex, BRSPHex, CASPHex, SISPHex, STSPHex, Cer_HDS, HexCer_HDS,
        SSulfate, BAHex, BASulfate,
        Others, Unknown, SPLASH, EtherSMGDG, SMGDG, Vitamin_D,
        LCAE, KLCAE, KDCAE,
        MMPE, DMPE, MIPC, EGSE, DEGSE, DSMSE,
        OxTG, TG_EST,
        GPNAE, DGMG, MGMG,
        GD1a, GD1b, GD2, GD3, GM1, GT1b, GQ1b, NGcGM3,
        ST
    }

    public class FattyAcidChain
    {
        private string chainString;
        private int chainLength;
        private int bondCount;
        private int oxCount;
        private string smiles;

        public FattyAcidChain()
        {
            chainString = string.Empty;
            chainLength = 0;
            bondCount = 0;
            oxCount = 0;
            smiles = string.Empty;
        }

        public string ChainString
        {
            get { return chainString; }
            set { chainString = value; }
        }

        public int ChainLength
        {
            get { return chainLength; }
            set { chainLength = value; }
        }

        public int BondCount
        {
            get { return bondCount; }
            set { bondCount = value; }
        }

        public int OxCount
        {
            get { return oxCount; }
            set { oxCount = value; }
        }

        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

    }
}
