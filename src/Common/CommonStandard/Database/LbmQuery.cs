using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
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
        ST,
        DGTA,LDGTA,
        NATau,NAPhe,
        PT,
        DMEDFAHFA, DMEDFA, DMEDOxFA,
        CE_d7,Cer_NS_d7, PC_d5, PE_d5,PG_d5,PS_d5, PI_d5, SM_d9,TG_d5, DG_d5, LPC_d5, LPE_d5, LPG_d5, LPS_d5, LPI_d5,
        ASHexCer,
        NATryA, NA5HT, WE, BisMeLPA, NALeu, NASer, NAAla, NAGln, NAVal,
        bmPC, Cer_ABP,
        // new version
        //CerP, SM,
        //CE, FA,
        //PA, PC, PE, PG, PI, PS,
        //MG, DG, TG,
        //MGDG, DGDG, SQDG, DGTS, 
        //Cer_ADS, Cer_AS, Cer_BDS, Cer_BS, Cer_EODS, Cer_EOS,
        //Cer_NDS, Cer_NS, Cer_NP, Cer_AP, GlcCer_AP,
        //BMP, FAHFA, 
        //DGGA, ADGGA, CL, EtherPC, EtherPE,
        //OxPA, OxPC, OxPS, OxPE, OxPG, OxPI, OxFA, EtherOxPC, EtherOxPE,
        //CAR, LPC, LPE, LPA, LPS, LPI, LPG, LDGTS, PMeOH, PEtOH, PBtOH, SHexCer, GM3,
        //EtherLPC, EtherLPE, Sph, DHSph, PhytoSph,
        //LNAPE, LNAPS, Ac2PIM1, Ac2PIM2, Ac3PIM2, Ac4PIM2, LipidA, DLCL, Cer_OS,
        //Hex3Cer, Hex2Cer, Cer_EBDS, MLCL, AHexCer, ASM, HBMP, Undefined,
        //HexCer_NS, HexCer_NDS, HexCer_AP, Cholesterol,
        //CholesterolSulfate, EtherMGDG, EtherDGDG, EtherTG, HexCer_EOS, EtherDG, LDGCC, DGCC,
        //EtherPI, EtherPS, PE_Cer, PI_Cer, EtherPG, SL, NAGly, EtherLPG, CoQ,
        //DCAE, GDCAE, GLCAE, TDCAE, TLCAE, NAE, Vitamin_E, BileAcid, NAGlySer, EtherLPS, EtherLPI,
        //NAOrn, VAE, BRSE, CASE, SISE, STSE, AHexBRS, AHexCAS, AHexCS, AHexSIS, AHexSTS,
        //Cer_HS, HexCer_HS, Cer_NDOS,
        //SPE, SHex, SPEHex, SPGHex, CSLPHex, BRSLPHex, CASLPHex, SISLPHex, STSLPHex,
        //CSPHex, BRSPHex, CASPHex, SISPHex, STSPHex, Cer_HDS, HexCer_HDS,
        //SSulfate, BAHex, BASulfate,
        //Others, Unknown, SPLASH, EtherSMGDG, SMGDG, Vitamin_D,
        //LCAE, KLCAE, KDCAE

    }

    /// <summary>
    /// This is the storage of lipid query to pick up LipidBlast MS/MS that the user wants to see.
    /// 1. first the LbmQueryParcer.cs will pick up all queries of LipidBlast from the LbmQueries.txt of Resources floder of MS-DIAL assembry.
    /// 2. The users can select what the user wants to see in LbmDbSetWin.xaml and the isSelected property will be changed to True.
    /// 3. The LipidBlast MS/MS of the true queries will be picked up by LbmFileParcer.cs. 
    /// </summary>
    [DataContract]
    [MessagePackObject]
    public class LbmQuery
    {
        [DataMember]
        private LbmClass lbmClass;
        [DataMember]
        private AdductIon adductIon;
        [DataMember]
        private IonMode ionMode;
        [DataMember]
        private bool isSelected;

        [Key(0)]
        public LbmClass LbmClass
        {
            get { return lbmClass; }
            set { lbmClass = value; }
        }

        [Key(1)]
        public AdductIon AdductIon
        {
            get { return adductIon; }
            set { adductIon = value; }
        }

        [Key(2)]
        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        [Key(3)]
        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; }
        }
    }
}
