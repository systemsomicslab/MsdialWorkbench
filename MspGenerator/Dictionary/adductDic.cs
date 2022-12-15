using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MspGenerator
{
    public sealed class adductDic
    {
        private adductDic() { }

        public static Dictionary<string, AdductIon>
        adductIonDic = new Dictionary<string, AdductIon>()
        {
            { "[M]+",new AdductIon(){ AdductIonName= "[M]+", AdductIonMass = - MassDictionary.Electron ,AdductSurfix ="non", IonMode ="Positive"} },
            { "[M+H]+",new AdductIon(){ AdductIonName= "[M+H]+", AdductIonMass = MassDictionary.Proton ,AdductSurfix ="H", IonMode ="Positive"} },
            { "[M+NH4]+",new AdductIon(){ AdductIonName= "[M+NH4]+", AdductIonMass = MassDictionary.NH4Adduct ,AdductSurfix ="NH4", IonMode ="Positive"} },
            { "[M+Na]+",new AdductIon(){ AdductIonName= "[M+Na]+", AdductIonMass = MassDictionary.NaAdduct ,AdductSurfix ="Na", IonMode ="Positive"} },
            { "[M-H]-",new AdductIon(){ AdductIonName= "[M-H]-", AdductIonMass = -MassDictionary.Proton ,AdductSurfix ="H", IonMode ="Negative"} },
            { "[M+HCOO]-",new AdductIon(){ AdductIonName= "[M+HCOO]-", AdductIonMass = MassDictionary.HCOOadduct ,AdductSurfix ="FA", IonMode ="Negative"} },
            { "[M+CH3COO]-",new AdductIon(){ AdductIonName= "[M+CH3COO]-", AdductIonMass = MassDictionary.CH3COOadduct ,AdductSurfix ="Hac", IonMode ="Negative"} },
            { "[M-2H]2-",new AdductIon(){ AdductIonName= "[M-2H]2-", AdductIonMass = -MassDictionary.Proton*2 ,AdductSurfix ="2H-", IonMode ="Negative"} },
            { "[M+H-H2O]+",new AdductIon(){ AdductIonName= "[M+H-H2O]+", AdductIonMass = MassDictionary.Proton-MassDictionary.H2OMass ,AdductSurfix ="H-H2O", IonMode ="Positive"} },
            { "[M+2H]2+",new AdductIon(){ AdductIonName= "[M+2H]2+", AdductIonMass = MassDictionary.Proton*2 ,AdductSurfix ="2H+", IonMode ="Positive"} },
            { "[M+2NH4]2+",new AdductIon(){ AdductIonName= "[M+2NH4]2+", AdductIonMass = MassDictionary.NH4Adduct*2 ,AdductSurfix ="2NH4", IonMode ="Positive"} },

        };

        public static Dictionary<string, string>
        adductIonSurfixDic = new Dictionary<string, string>()
        {
                    { "non_Pos" , "[M]+"},
                    { "H_Pos","[M+H]+"},
                    { "NH4_Pos","[M+NH4]+" },
                    { "Na_Pos","[M+Na]+" },
                    { "H_Neg","[M-H]-" },
                    { "FA_Neg","[M+HCOO]-" },
                    { "Hac_Neg","[M+CH3COO]-"},
                    { "2H-_Neg","[M-2H]2-"},
                    { "H-H2O_Pos","[M+H-H2O]+"},
                    { "2H+_Pos","[M+2H]2+"},
                    { "2NH4_Pos","[M+2NH4]2+" },
        };


        public static Dictionary<string, List<string>>
        lipidClassAdductDic = new Dictionary<string, List<string>>()
        {
            // (GP)glycerophosphoLipids 2 eqally acyl chains
                {   "PC" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+Na]+"}    },
                {   "PE" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+Na]+"}    },
                {   "PG" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"}    },
                {   "PI" ,    new List<string>(){ "[M-H]-", "[M+Na]+", "[M+NH4]+"}    },
                {   "PS" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+Na]+"}    },
                {   "PT" ,    new List<string>(){ "[M-H]-", "[M+H]+"}    },
                {   "PA" ,    new List<string>(){ "[M-H]-" }    },
                {   "PMeOH" ,    new List<string>(){ "[M-H]-", "[M+NH4]+" }    }, 
                {   "PEtOH" ,    new List<string>(){ "[M-H]-", "[M+NH4]+" }   },
                {   "MMPE" ,    new List<string>(){ "[M-H]-", "[M+H]+"}    },
                {   "DMPE" ,    new List<string>(){ "[M-H]-", "[M+H]+"}    },


            // (GP)glycerophosphoLipids mono acyl(ester) chains
                {   "LPC" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+Na]+" , }    },
                {   "LPCSN1" ,    new List<string>(){ "[M+H]+" }    },
                {   "LPE" ,    new List<string>(){ "[M-H]-", "[M+H]+"}    },
                {   "LPG" ,    new List<string>(){ "[M-H]-" }    },
                {   "LPI" ,    new List<string>(){ "[M-H]-" }    },
                {   "LPS" ,    new List<string>(){ "[M-H]-" }    },
                {   "LPA" ,    new List<string>(){ "[M-H]-" }    },
                {   "EtherLPC" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-"}    },
                {   "EtherLPE" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },
                {   "EtherLPE_P" ,    new List<string>(){"[M-H]-" }    },
                {   "EtherLPG" ,    new List<string>(){ "[M-H]-"}    },
                {   "LDGTS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+H]+","[M+Na]+" }    },
                {   "LDGCC" ,    new List<string>(){ "[M+H]+"  }    },
                {   "GPNAE" ,    new List<string>(){ "[M-H]-", "[M+H]+"}    },


            // (GL)glyceroLipids 2 eqally acyl chains
                {   "DG" ,    new List<string>(){ "[M+Na]+", "[M+NH4]+"  , }    },
                {   "BMP" ,    new List<string>(){ "[M+NH4]+"  }    },
                {   "DGDG" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
                {   "MGDG" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
                {   "SQDG" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
                {   "DGTS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+" }    },
                {   "DGGA" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
                {   "DLCL" ,    new List<string>(){ "[M-H]-"  }    },
                {   "SMGDG" ,    new List<string>(){ "[M-H]-"  }    },
                {   "DGCC" ,    new List<string>(){ "[M+H]+"  }    },

            // (GL)glyceroLipids mono acyl(ester) chains
                {   "MG" ,    new List<string>(){ "[M+NH4]+"  }    },
                {   "DGMG" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
                {   "MGMG" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },


            // (GP)glycerophosphoLipids 2 not eqally acyl chains
                {   "OxPC" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-"}    },
                {   "OxPE" ,    new List<string>(){ "[M-H]-" }    },
                {   "OxPG" ,    new List<string>(){ "[M-H]-" }    },
                {   "OxPI" ,    new List<string>(){ "[M-H]-" }    },
                {   "OxPS" ,    new List<string>(){ "[M-H]-" }    },
                {   "LNAPE" ,    new List<string>(){ "[M-H]-"  }    },
                {   "LNAPS" ,    new List<string>(){ "[M-H]-"  }    },

            // (GP)glycerophosphoLipids 2 not eqally ether and acyl chains
                {   "EtherPC" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-"}    },
                {   "EtherPE_O" ,    new List<string>(){ "[M+H]+"}    },
                {   "EtherPE" ,    new List<string>(){ "[M-H]-", "[M+H]+"}    },
                {   "EtherPG" ,    new List<string>(){ "[M-H]-"}    },
                {   "EtherPI" ,    new List<string>(){ "[M-H]-"}    },
                {   "EtherPS" ,    new List<string>(){ "[M-H]-"}    },
                {   "EtherDG" ,    new List<string>(){ "[M+NH4]+"  }    },
                {   "EtherDGDG" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+Na]+", "[M+NH4]+" , }    },
                {   "EtherMGDG" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+Na]+", "[M+NH4]+" , }    },
                {   "EtherSMGDG" ,    new List<string>(){ "[M-H]-", "[M+NH4]+" }    },

                {   "EtherOxPC" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-"}    },
                {   "EtherOxPE" ,    new List<string>(){ "[M-H]-"}    },

            // (GL)(GP) two eqally acyl chains and one independent chain
                {   "ADGGA" ,    new List<string>(){ "[M+NH4]+", "[M-H]-" }    },
                {   "MLCL" ,    new List<string>(){ "[M-H]-"  }    },
                {   "HBMP" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
                {   "EtherTG" ,    new List<string>(){ "[M+Na]+", "[M+NH4]+"  , }    },
                {   "OxTG" ,    new List<string>(){ "[M+NH4]+" }    },

            // (GL)(GP) three eqally acyl chains
                {   "TG" ,    new List<string>(){ "[M+Na]+", "[M+NH4]+"  , }    },

            // (GL) four acyl chains
                {   "CL" ,    new List<string>(){ "[M-H]-", "[M+NH4]+", "[M-2H]2-", }    },
                {   "TG_EST" ,    new List<string>(){  "[M+NH4]+"  }    },


            //cer 2chains
                {   "Cer_ADS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-"}    },
                {   "Cer_AP" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
                {   "Cer_AS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-"}    },
                {   "Cer_BDS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-"}    },
                {   "Cer_BS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-" }    },
                {   "Cer_NDS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
                {   "Cer_NP" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+" }    },
                {   "Cer_NS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+H]+", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
                {   "Cer_HS" ,    new List<string>(){ "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
                {   "Cer_HDS" ,    new List<string>(){ "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
                {   "HexCer_AP" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
                {   "HexCer_NDS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
                {   "HexCer_NS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
                {   "HexCer_HS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+", "[M-H]-" }    },
                {   "HexCer_HDS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+", "[M-H]-" }    },
                {   "GM3" ,    new List<string>(){ "[M+NH4]+", "[M-H]-", "[M-2H]2-" }    },
                {   "GD1a" ,    new List<string>(){ "[M-H]-", "[M-2H]2-", "[M+2NH4]2+", "[M+2H]2+" }    },
                {   "GD1b" ,    new List<string>(){ "[M-H]-", "[M-2H]2-", "[M+2NH4]2+", "[M+2H]2+" }    },
                {   "GD2" ,    new List<string>(){ "[M-H]-", "[M-2H]2-" }    },
                {   "GD3" ,    new List<string>(){ "[M-H]-", "[M-2H]2-" }    },
                {   "GM1" ,    new List<string>(){ "[M-H]-", "[M-2H]2-" , "[M+NH4]+", "[M+2NH4]2+", "[M+2H]2+" }    },
                {   "GT1b" ,    new List<string>(){ "[M-H]-", "[M-2H]2-", "[M+2NH4]2+", "[M+2H]2+" }    },
                {   "GQ1b" ,    new List<string>(){ "[M-H]-", "[M-2H]2-", "[M+2NH4]2+" }    },
                {   "NGcGM3" ,    new List<string>(){ "[M-H]-", "[M-2H]2-", "[M+H]+", "[M+NH4]+" }    },
                {   "CerP" ,    new List<string>(){ "[M-H]-", "[M+H]+"}    },
                {   "Hex2Cer" ,    new List<string>(){ "[M+H]+" , "[M+HCOO]-", "[M+CH3COO]-" }    },
                {   "Hex3Cer" ,    new List<string>(){ "[M+H]+" , "[M+HCOO]-", "[M+CH3COO]-" }    },

                {   "MIPC" ,    new List<string>(){ "[M+H]+" , "[M-H]-" }    },


            //cer 2chains and chain conbination
                {   "SM" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+Na]+"  }    },
                {   "SM+O" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-" }    },
                {   "SL" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
                {   "SL+O" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
                {   "PE_Cer_d" ,    new List<string>(){ "[M-H]-" }    },
                {   "PE_Cer_d+O" ,    new List<string>(){ "[M-H]-" }    },
                {   "PI_Cer_d+O" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },
                {   "PI_Cer_d" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },
                {   "SHexCer" ,    new List<string>(){ "[M+H]+", "[M-H]-" }    },
                {   "SHexCer+O" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },

            //cer 3chains and chain conbination
                {   "ASM" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-"}    },
                {   "Cer_EODS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-"}    },
                {   "Cer_EOS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+H]+", "[M+CH3COO]-", "[M+H-H2O]+" }    },
                {   "HexCer_EOS" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+H-H2O]+", "[M-H]-" }    },
                {   "Cer_EBDS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M-H]-" }    },  // "[M+H-H2O]+" is not use
                {   "AHexCer" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+H-H2O]+", "[M-H]-" }    },

            //cer 1 chains
                {   "Sph" ,    new List<string>(){ "[M+H]+"  }    },
                {   "DHSph" ,    new List<string>(){ "[M+H]+"  }    },
                {   "PhytoSph" ,    new List<string>(){ "[M+H]+"  }    },

            //FA
                {   "FA" ,    new List<string>(){ "[M-H]-" }    },
                {   "OxFA" ,    new List<string>(){ "[M-H]-" }    },

                {   "FAHFA" ,    new List<string>(){ "[M-H]-"  }    },
                {   "AAHFA" ,    new List<string>(){ "[M-H]-"  }    },
                {   "NAGly_FAHFA" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
                {   "NAGlySer_FAHFA" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
                {   "NAOrn_FAHFA" ,    new List<string>(){ "[M+H]+"  }    },
            //single chain
                {   "CAR" ,    new List<string>(){ "[M+H]+" }    },
                {   "VAE" ,    new List<string>(){ "[M+H]+", "[M+Na]+"  , }    },
                {   "NAE" ,    new List<string>(){ "[M+H]+" , "[M+HCOO]-", "[M+CH3COO]-" }    },
                {   "NAGly_OxFA" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },
                {   "NAGlySer_OxFA" ,    new List<string>(){ "[M-H]-", "[M+H]+"  , }    },
                {   "NAOrn_OxFA" ,    new List<string>(){ "[M+H]+"  }    },

                {   "NATau_FA" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
                {   "NATau_OxFA" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
                {   "NAPhe_FA" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },
                {   "NAPhe_OxFA" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },

                {   "NAGly_FA" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
                {   "NAOrn_FA" ,    new List<string>(){ "[M+H]+"  }    },



            //steroidal
                {   "CE" ,    new List<string>(){ "[M+Na]+", "[M+NH4]+"  , }    },

                {   "BRSE" ,    new List<string>(){ "[M+NH4]+"  }    },
                {   "CASE" ,    new List<string>(){ "[M+NH4]+"  }    },
                {   "SISE" ,    new List<string>(){ "[M+NH4]+"  }    },
                {   "STSE" ,    new List<string>(){ "[M+NH4]+"  }    },

                {   "AHexBRS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
                {   "AHexCAS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
                {   "AHexCS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
                {   "AHexSIS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
                {   "AHexSTS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },

                {   "DCAE",  new List<string>(){ "[M-H]-", "[M+NH4]+" }    },
                {   "GDCAE", new List<string>(){ "[M-H]-", "[M+NH4]+" }    },
                {   "GLCAE",  new List<string>(){ "[M-H]-", "[M+NH4]+" }    },
                {   "TDCAE",  new List<string>(){ "[M-H]-", "[M+NH4]+"}    },
                {   "TLCAE",  new List<string>(){ "[M-H]-", "[M+NH4]+"}    },
                {   "KLCAE",  new List<string>(){ "[M-H]-", "[M+NH4]+"}    },
                {   "KDCAE",  new List<string>(){ "[M-H]-", "[M+NH4]+"}    },
                {   "LCAE",   new List<string>(){"[M-H]-", "[M+NH4]+"}    },

                {   "CSLPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+", }    },
                {   "BRSLPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+", }    },
                {   "CASLPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+", }    },
                {   "SISLPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+", }    },
                {   "STSLPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+", }    },

                {   "CSPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
                {   "BRSPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
                {   "CASPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
                {   "SISPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
                {   "STSPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },

                {   "BAHex" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+", }    },
                {   "BASulfate" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
                {   "SHex" ,    new List<string>(){ "[M+HCOO]-", "[M+NH4]+", "[M+CH3COO]-", }    },
                {   "SPEHex" ,    new List<string>(){ "[M+NH4]+" }    },
                {   "SPGHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+", }    },
                {   "SPE" ,    new List<string>(){ "[M-H]-", "[M+H]+"  , }    },
                {   "SSulfate" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },

                {   "CoQ" ,    new List<string>(){ "[M+H]+","[M+Na]+", "[M+NH4]+" }    },
                {   "Vitamin_D" ,    new List<string>(){ "[M+H]+"  }    },
                {   "VitaminE" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-" , }    },
                {   "BileAcid" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },

                //bacterial lipid
                {"Ac2PIM1", new List<string>(){ "[M-H]-" } },
                {"Ac2PIM2", new List<string>(){ "[M-H]-" } },
                {"Ac3PIM2", new List<string>(){ "[M-H]-" } },
                {"Ac4PIM2", new List<string>(){ "[M-H]-" } },

                {"LipidA", new List<string>(){ "[M-H]-" } },


                //added steroid
                {"EGSE",  new List<string>(){ "[M+H]+", "[M+NH4]+","[M+Na]+" } },
                {"DEGSE",  new List<string>(){ "[M+H]+", "[M+NH4]+","[M+Na]+" } },
                {"DSMSE",  new List<string>(){ "[M+H]+", "[M+NH4]+","[M+Na]+" } },

                {"ST",  new List<string>(){ "[M+H]+", "[M+NH4]+", "[M+Na]+", "[M+H-H2O]+" } },


                //FAHFA-DMED 20221122
                {"DMEDFAHFA",  new List<string>(){ "[M+H]+" } },
                {"DMEDFAHFAHFA",  new List<string>(){ "[M+H]+" } },


    };




        //public static Dictionary<string, double>
        //    adductMassDic = new Dictionary<string, double>()
        //    {
        //        {   "[M+NH4]+" ,    MassDictionary.NH4Adduct    },
        //        {   "[M-H]-"   ,    -MassDictionary.Proton      },

        //    };

        //public static Dictionary<string, string>
        //    adductIonmodeDic = new Dictionary<string, string>()
        //    {
        //        {   "[M+NH4]+" ,   "Positive" },
        //        {   "[M-H]-"   ,   "Negative" },
        //    };

        //public static Dictionary<string, string>
        //    adductSurfixDic = new Dictionary<string, string>()
        //    {
        //        {   "[M+NH4]+"  , "NH4" },
        //        {   "[M-H]-"    , "H"   },

        //    };



    }


}

