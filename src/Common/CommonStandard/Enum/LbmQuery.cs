using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CompMs.Common.DataObj.Property;
using MessagePack;

namespace CompMs.Common.Enum
{
    public enum LbmClass
    {
        Undefined, Others, Unknown, SPLASH,

        FA, FAHFA, OxFA, CAR, NAE, NAGly, NAGlySer, NAOrn,

        PA, PC, PE, PG, PI, PS,
        BMP, HBMP, EtherPC, EtherPE, EtherPI, EtherPS, EtherPG,
        OxPA, OxPC, OxPS, OxPE, OxPG, OxPI, EtherOxPC, EtherOxPE,
        LPC, LPE, LPA, LPS, LPI, LPG,
        PMeOH, PEtOH, PBtOH, EtherLPC, EtherLPE, EtherLPG, EtherLPS, EtherLPI,
        LNAPE, LNAPS, MLCL, DLCL, CL,

        MG, DG, TG, MGDG, DGDG, SQDG, DGTS, DGGA, ADGGA, LDGTS, LDGCC, DGCC,
        EtherMGDG, EtherDGDG, EtherTG, EtherDG, EtherSMGDG, SMGDG,

        Sph, DHSph, PhytoSph, SM, CerP,
        Cer_ADS, Cer_AS, Cer_BDS, Cer_BS, Cer_OS, Cer_EODS, Cer_EOS,
        Cer_NDS, Cer_NS, Cer_NP, Cer_AP, Cer_EBDS, Cer_HS, Cer_HDS, Cer_NDOS,
        HexCer_NS, HexCer_NDS, HexCer_AP, HexCer_HS, HexCer_HDS, HexCer_EOS,
        Hex2Cer, Hex3Cer, SHexCer, GM3, AHexCer, ASM,
        PE_Cer, PI_Cer, SL,

        Ac2PIM1, Ac2PIM2, Ac3PIM2, Ac4PIM2, LipidA,

        Vitamin_E, Vitamin_D, CoQ,

        CE, DCAE, GDCAE, GLCAE, TDCAE, TLCAE, BileAcid,
        VAE, BRSE, CASE, SISE, STSE, AHexBRS, AHexCAS, AHexCS, AHexSIS, AHexSTS,
        SPE, SHex, SPEHex, SPGHex, CSLPHex, BRSLPHex, CASLPHex, SISLPHex, STSLPHex,
        CSPHex, BRSPHex, CASPHex, SISPHex, STSPHex,
        SSulfate, BAHex, BASulfate,
        LCAE, KLCAE, KDCAE,
        MMPE, DMPE, MIPC, EGSE, DEGSE, DSMSE,
        OxTG, TG_EST,
        GPNAE, DGMG, MGMG,
        GD1a, GD1b, GD2, GD3, GM1, GQ1b, GT1b, NGcGM3,
        ST,
        DGTA, LDGTA,
        NATau, NAPhe,
        PT,
        DMEDFAHFA, DMEDFA, DMEDOxFA,
        CE_d7, Cer_NS_d7, PC_d5, PE_d5, PG_d5, PS_d5, PI_d5, SM_d9, TG_d5, DG_d5, LPC_d5, LPE_d5, LPG_d5, LPS_d5, LPI_d5,
        NATryA, NA5HT, WE, BisMeLPA, NALeu, NASer, NAAla, NAGln, NAVal,
        bmPC,
        ASHexCer,
        Cer_ABP
    }

    /// <summary>
    /// This is the storage of lipid query to pick up LipidBlast MS/MS that the user wants to see.
    /// 1. first the LbmQueryParcer.cs will pick up all queries of LipidBlast from the LbmQueries.txt of Resources floder of MS-DIAL assembry.
    /// 2. The users can select what the user wants to see in LbmDbSetWin.xaml and the isSelected property will be changed to True.
    /// 3. The LipidBlast MS/MS of the true queries will be picked up by LbmFileParcer.cs. 
    /// </summary>
    [MessagePackObject]
    public class LbmQuery
    {
        [Key(0)]
        public LbmClass LbmClass { get; set; }

        [Key(1)]
        public AdductIon AdductType { get; set; }

        [Key(2)]
        public IonMode IonMode { get; set; }

        [Key(3)]
        public bool IsSelected { get; set; }
    }
}
