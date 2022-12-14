using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MspGenerator
{
    public class GlycerolipidFragmentation
    {

        public static void pcFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "PC" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+Na]+"}},
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass - MassDictionary.Proton - 12 - MassDictionary.HydrogenMass * 2;
                var fra02int = 999;
                var fra02comment = "[M-CH3]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 7 + MassDictionary.HydrogenMass * 15 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra03int = 100;
                var fra03comment = "C7H15NO5P";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain1Mass + MassDictionary.OxygenMass;
                var fra04int = 50;
                var fra04comment = "M-CH3-SN1(RCOOH)";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - chain2Mass + MassDictionary.OxygenMass;
                var fra05int = 50;
                var fra05comment = "M-CH3-SN2(RCOOH)";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra04mass - MassDictionary.H2OMass;
                var fra06int = 50;
                var fra06comment = "M-CH3-SN1(RCH=C=O)";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra05mass - MassDictionary.H2OMass;
                var fra07int = 50;
                var fra07comment = "M-CH3-SN2(RCH=C=O)";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra08int = 600;
                var fra08comment = "SN1";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra09int = 600;
                var fra09comment = "SN2";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M+Na]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 3 + MassDictionary.HydrogenMass * 9 + MassDictionary.NitrogenMass);
                var fra02int = 500;
                var fra02comment = "NL of C3H9N";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - (12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra03int = 600;
                var fra03comment = "NL of C5H14NO4P";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass + adductDic.adductIonDic["[M+H]+"].AdductIonMass - adductDic.adductIonDic[adduct].AdductIonMass;
                var fra04int = 400;
                var fra04comment = "NL of C5H14NO4PNa";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 5 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra05int = 100;
                var fra05comment = "C5H12N+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra06int = 400;
                var fra06comment = "C2H5O4PNa+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 300;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra02int = 50;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "SN1-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra04int = 50;
                var fra04comment = "SN2 acyl loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 50;
                var fra05comment = "SN2-H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra06int = 999;
                var fra06comment = "C5H15NO4P";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }

        }
        public static void peFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "PE" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+Na]+"}    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 700;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra02int = 50;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "SN1-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra04int = 50;
                var fra04comment = "SN2 acyl loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 50;
                var fra05comment = "SN2-H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra06int = 999;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra07int = 999;
                var fra07comment = "SN2";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 5 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra08int = 100;
                var fra08comment = "C5H11NO5P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);


            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = chain1Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra02int = 50;
                var fra02comment = "SN1-H2O";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain2Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra03int = 50;
                var fra03comment = "SN2-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - (12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra04int = 999;
                var fra04comment = "NL of C2H8NO4P";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 70;
                var fra01comment = "[M+Na]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - 12 * 2 - MassDictionary.HydrogenMass * 5 - MassDictionary.NitrogenMass;
                var fra02int = 400;
                var fra02comment = "NL of C2H5N";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - 12 * 2 - MassDictionary.HydrogenMass * 8 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass * 4 - MassDictionary.PhosphorusMass;
                var fra03int = 500;
                var fra03comment = "NL of C2H8NO4P";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = exactMass + MassDictionary.Proton - 12 * 2 - MassDictionary.HydrogenMass * 8 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass * 4 - MassDictionary.PhosphorusMass;
                var fra04int = 999;
                var fra04comment = "NL of C2H7NO4PNa";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.NaAdduct;
                var fra05int = 300;
                var fra05comment = "C2H8NO4PNa";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }

        }

        public static void mmpeFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{ "MMPE" ,    new List<string>() { "[M-H]-", "[M+H]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 150;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra02int = 10;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 10;
                var fra03comment = "SN1 acyl loss -H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra04int = 10;
                var fra04comment = "SN2 acyl loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 10;
                var fra05comment = "SN2 acyl loss -H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra06int = 999;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra07int = 999;
                var fra07comment = "SN2";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 6 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra08int = 10;
                var fra08comment = "C6H13NO5P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 3 + MassDictionary.HydrogenMass * 10 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra09int = 10;
                var fra09comment = "C3H10NO4P-H";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 10;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = chain1Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra02int = 50;
                var fra02comment = "SN1-H2O";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain2Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra03int = 50;
                var fra03comment = "SN2-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = fra02mass - MassDictionary.H2OMass;
                var fra05int = 10;
                var fra05comment = "SN1-H2O-H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra03mass - MassDictionary.H2OMass;
                var fra06int = 10;
                var fra06comment = "SN2-H2O-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra04mass = fra01mass - (12 * 3 + MassDictionary.HydrogenMass * 10 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra04int = 999;
                var fra04comment = "NL of C3H10NO4P";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);



            }
        }

        public static void dmpeFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{ "DMPE" ,    new List<string>() { "[M-H]-", "[M+H]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 150;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra02int = 10;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 10;
                var fra03comment = "SN1 acyl loss -H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra04int = 10;
                var fra04comment = "SN2 acyl loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 10;
                var fra05comment = "SN2 acyl loss -H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra06int = 999;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra07int = 999;
                var fra07comment = "SN2";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 7 + MassDictionary.HydrogenMass * 16 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra08int = 5;
                var fra08comment = "C7H15NO5P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 4 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra09int = 5;
                var fra09comment = "C4H12NO4P-H";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = chain1Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra02int = 50;
                var fra02comment = "SN1-H2O";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain2Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra03int = 50;
                var fra03comment = "SN2-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = fra02mass - MassDictionary.H2OMass;
                var fra05int = 10;
                var fra05comment = "SN1-H2O-H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra03mass - MassDictionary.H2OMass;
                var fra06int = 10;
                var fra06comment = "SN2-H2O-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra04mass = fra01mass - (12 * 4 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra04int = 999;
                var fra04comment = "NL of C4H12NO4P";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra07mass = 12 * 4 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra07int = 200;
                var fra07comment = "C4H12NO4P + H";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
        }

        public static void pgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "PG" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"}    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra02int = 100;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 100;
                var fra03comment = "SN1-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra04int = 100;
                var fra04comment = "SN2 acyl loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "NL of SN2+H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra06int = 999;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra07int = 999;
                var fra07comment = "SN2";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = "[M+NH4]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton - (12 * 3 + MassDictionary.HydrogenMass * 9 + MassDictionary.OxygenMass * 6 + MassDictionary.PhosphorusMass);
                var fra02int = 999;
                var fra02comment = "[M-C3H8O6P]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + MassDictionary.Proton + MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass;
                var fra03int = 50;
                var fra03comment = "[SN1+C3H5O]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain2Mass + MassDictionary.Proton + MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass;
                var fra04int = 50;
                var fra04comment = "[SN2+C3H5O]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }

        }
        public static void piFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "PI" ,    new List<string>(){ "[M-H]-", "[M+Na]+", "[M+NH4]+"}    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra02int = 100;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 100;
                var fra03comment = "SN1-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra04int = 100;
                var fra04comment = "SN2 acyl loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "SN2 loss -H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra06int = 200;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra07int = 200;
                var fra07comment = "SN2";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 6 + MassDictionary.HydrogenMass * 11 + MassDictionary.OxygenMass * 8 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra08int = 200;
                var fra08comment = "C6H10O8P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 9 + MassDictionary.HydrogenMass * 15 + MassDictionary.OxygenMass * 9 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra09int = 200;
                var fra09comment = "C9H14O9P";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 800;
                var fra01comment = "[M+Na]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 6 + MassDictionary.HydrogenMass * 13 + MassDictionary.OxygenMass * 9 + MassDictionary.PhosphorusMass);
                var fra02int = 100;
                var fra02comment = "NL of C6H13O9P";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass + MassDictionary.Proton - MassDictionary.NaAdduct;
                var fra03int = 300;
                var fra03comment = "NL of C6H12O9PNa";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = (12 * 6 + MassDictionary.HydrogenMass * 13 + MassDictionary.OxygenMass * 9 + MassDictionary.PhosphorusMass) + MassDictionary.NaAdduct;
                var fra04int = 999;
                var fra04comment = "C6H13O9PNa";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = "[M+NH4]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton - (12 * 6 + MassDictionary.HydrogenMass * 13 + MassDictionary.OxygenMass * 9 + MassDictionary.PhosphorusMass);
                var fra02int = 999;
                var fra02comment = "NL of C6H12O9P+NH4";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }

        }
        public static void ptFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "PT" ,    new List<string>(){ "[M-H]-", "[M+H]+"}    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - 12 * 4 - MassDictionary.HydrogenMass * 7 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass * 2;
                var fra02int = 50;
                var fra02comment = "[M-H]- C4H7NO2";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass + MassDictionary.OxygenMass;
                var fra03int = 100;
                var fra03comment = "precursor -C4H7NO2 -SN1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 100;
                var fra04comment = "precursor -C4H7NO2 -SN1 -H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - chain2Mass + MassDictionary.OxygenMass;
                var fra05int = 100;
                var fra05comment = "precursor -C4H7NO2 -SN2 -H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 100;
                var fra06comment = "precursor -C4H7NO2 -SN2 -H2OO";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra07int = 100;
                var fra07comment = "SN1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra08int = 100;
                var fra08comment = "SN2";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.PhosphorusMass + MassDictionary.OxygenMass * 5 + MassDictionary.Electron;
                var fra09int = 50;
                var fra09comment = "C3H6O5P-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 4 + MassDictionary.HydrogenMass * 10 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 6 + MassDictionary.PhosphorusMass);
                var fra02int = 999;
                var fra02comment = "Precursor -C4H10NO6P";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass + MassDictionary.OxygenMass;
                var fra03int = 100;
                var fra03comment = "Header and SN1 acyl loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra04int = 10;
                var fra04comment = "SN1 acyl loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra041mass = fra04mass - MassDictionary.H2OMass;
                var fra041int = 10;
                var fra041comment = "SN1 acyl loss - H2O";
                fragmentList.Add(fra041mass + "\t" + fra041int + "\t" + fra041comment);

                var fra05mass = fra02mass - chain2Mass + MassDictionary.OxygenMass;
                var fra05int = 100;
                var fra05comment = "Header and SN2 acyl loss";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra06int = 10;
                var fra06comment = "SN2 acyl loss";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra061mass = fra06mass - MassDictionary.H2OMass;
                var fra061int = 10;
                var fra061comment = "SN2 acyl loss - H2O";
                fragmentList.Add(fra061mass + "\t" + fra061int + "\t" + fra061comment);

                var fra07mass = 12 * 4 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 - MassDictionary.Electron;
                var fra07int = 100;
                var fra07comment = "C4H8NO2+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }

        }
        public static void psFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "PS" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+Na]+"}    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - 12 * 3 - MassDictionary.HydrogenMass * 5 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass * 2;
                var fra02int = 600;
                var fra02comment = "[M-H]- C3H5NO2";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass + MassDictionary.OxygenMass;
                var fra03int = 200;
                var fra03comment = "M- C3H5NO2-SN1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 200;
                var fra04comment = "SN1+H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - chain2Mass + MassDictionary.OxygenMass;
                var fra05int = 200;
                var fra05comment = "M- C3H5NO2-SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 200;
                var fra06comment = "SN2+H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra07int = 999;
                var fra07comment = "SN1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra08int = 999;
                var fra08comment = "SN2";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);


            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 3 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 6 + MassDictionary.PhosphorusMass);
                var fra02int = 999;
                var fra02comment = "fragment -C3H8NO6P";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra03int = 10;
                var fra03comment = "SN1 acyl loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 10;
                var fra04comment = "SN1-H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra05int = 10;
                var fra05comment = "SN2 acyl loss";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 10;
                var fra06comment = "SN2-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 800;
                var fra01comment = "[M+Na]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 3 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2);
                var fra02int = 300;
                var fra02comment = "NL of C3H5NO2";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - (12 * 3 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 6 + MassDictionary.PhosphorusMass);
                var fra03int = 700;
                var fra03comment = "NL of C3H8NO6P";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass + MassDictionary.Proton - MassDictionary.NaAdduct;
                var fra04int = 300;
                var fra04comment = "NL of C3H7NO6PNa";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = (12 * 3 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 6 + MassDictionary.PhosphorusMass) + MassDictionary.NaAdduct;
                var fra05int = 999;
                var fra05comment = "C3H8NO6PNa";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }

        }
        public static void paFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "PA" ,    new List<string>(){ "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 400;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra02int = 100;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 100;
                var fra03comment = "SN1-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra04int = 100;
                var fra04comment = "SN2 acyl loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "SN2-H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra06int = 999;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra07int = 999;
                var fra07comment = "SN2";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra08int = 300;
                var fra08comment = "C3H6O5P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);


            }

        }

        public static void pmeohFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "PMeOH" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"}    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 150;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra02int = 50;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra03int = 50;
                var fra03comment = "SN2 acyl loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra04int = 999;
                var fra04comment = "SN1";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra05int = 999;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 4 + MassDictionary.HydrogenMass * 9 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra06int = 10;
                var fra06comment = "[C4H9O5P-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = 12 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra07int = 10;
                var fra07comment = "[CH4O4P]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 0;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 0;
                //var fra02comment = "[M+H]+ Precursor Mass";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - (12 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra03int = 999;
                var fra03comment = "[M-CH5O4P+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain1Mass - MassDictionary.H2OMass - MassDictionary.HydrogenMass;
                var fra04int = 30;
                var fra04comment = "SN1 acyl loss -H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra01mass - chain2Mass - MassDictionary.H2OMass - MassDictionary.HydrogenMass;
                var fra05int = 30;
                var fra05comment = "SN2 acyl loss -H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra04mass - (12 + MassDictionary.HydrogenMass * 3 + MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass);
                var fra06int = 60;
                var fra06comment = "[M-SN1-113+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra05mass - (12 + MassDictionary.HydrogenMass * 3 + MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass);
                var fra07int = 60;
                var fra07comment = "[M-SN2-113+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain1Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra08int = 120;
                var fra08comment = "SN1-16";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = chain2Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra09int = 120;
                var fra09comment = "SN2-16";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = 12 * 4 + MassDictionary.HydrogenMass * 9 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra10int = 60;
                var fra10comment = "[C4H9O5P+H]+";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = 12 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra11int = 10;
                var fra11comment = "[CH6O4P]+";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);
            }
        }
        public static void petohFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "PMeOH" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"}    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 150;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass + MassDictionary.OxygenMass;
                var fra02int = 50;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - chain2Mass + MassDictionary.OxygenMass;
                var fra03int = 50;
                var fra03comment = "SN2 acyl loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra04int = 999;
                var fra04comment = "SN1";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra05int = 999;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 5 + MassDictionary.HydrogenMass * 11 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra06int = 10;
                var fra06comment = "[C5H11O5P-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = 12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra07int = 10;
                var fra07comment = "[C2H7O4P-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 0;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 0;
                //var fra02comment = "[M+H]+ Precursor Mass";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - (12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra03int = 999;
                var fra03comment = "[M-C2H7O4P+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain1Mass - MassDictionary.H2OMass - MassDictionary.HydrogenMass;
                var fra04int = 30;
                var fra04comment = "SN1 acyl loss -H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra01mass - chain2Mass - MassDictionary.H2OMass - MassDictionary.HydrogenMass;
                var fra05int = 30;
                var fra05comment = "SN2 acyl loss -H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra04mass - (12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass);
                var fra06int = 60;
                var fra06comment = "[M-SN1-127+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra05mass - (12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass);
                var fra07int = 60;
                var fra07comment = "[M-SN2-127+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain1Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra08int = 120;
                var fra08comment = "SN1-16";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = chain2Mass + MassDictionary.Proton - MassDictionary.OxygenMass;
                var fra09int = 120;
                var fra09comment = "SN2-16";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = 12 * 5 + MassDictionary.HydrogenMass * 11 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra10int = 60;
                var fra10comment = "[C5H11O5P+H]+";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = 12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra11int = 10;
                var fra11comment = "[C2H7O4P+H]+";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);
            }
        }

        public static void lpcFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, string lipidClass)
        {
            //{ "LPC" ,    new List<string>() { "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+Na]+", }    },
            //{ "LPCSN1" ,    new List<string>() { "[M+H]+" }    },

            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0) + MassDictionary.OxygenMass;

            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 10;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass - MassDictionary.Proton - 12 - MassDictionary.HydrogenMass * 2;
                var fra02int = 400;
                var fra02comment = "[M-CH3]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 7 + MassDictionary.HydrogenMass * 15 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra03int = 100;
                var fra03comment = "C7H15NO5P";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra08mass = chain1Mass + MassDictionary.HydrogenMass + MassDictionary.Electron;
                var fra08int = 999;
                var fra08comment = "SN1";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = "[M+Na]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 3 + MassDictionary.HydrogenMass * 9 + MassDictionary.NitrogenMass);
                var fra02int = 999;
                var fra02comment = "NL of C3H9N";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = (12 * 5 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass) + MassDictionary.Proton;
                var fra03int = 200;
                var fra03comment = " C5H14NO4P";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = exactMass + MassDictionary.Proton - (12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra04int = 100;
                var fra04comment = "MAG-H2O+H";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 5 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra05int = 100;
                var fra05comment = "C5H12N+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 400;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 150;
                var fra02comment = "[M+H-H2O]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra06mass = 12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra06int = 999;
                var fra06comment = "C5H15NO4P";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (lipidClass == "LPCSN1")
                {
                    var fra07mass = 12 * 5 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass + MassDictionary.Proton;
                    var fra07int = 300;
                    var fra07comment = "C5H14NO+";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                }

            }

        }

        public static void lpeFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "LPE" ,    new List<string>() { "[M-H]-", "[M+H]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass;
                var fra02int = 100;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 100;
                var fra03comment = "SN1-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra05int = 50;
                var fra05comment = "PO3-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra06int = 999;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra08mass = 12 * 5 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra08int = 100;
                var fra08comment = "C5H11NO5P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);


            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 10;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                //var fra02mass = fra01mass - MassDictionary.H2OMass;
                //var fra02int = 10;
                //var fra02comment = "[M+H]+ -H2O";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra04mass = fra01mass - (12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra04int = 999;
                var fra04comment = "NL of C2H8NO4P";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }

        }

        public static void lpgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "LPG" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 300;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass;
                var fra02int = 100;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 100;
                var fra03comment = "SN1-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra06mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra06int = 999;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra07int = 150;
                var fra07comment = "[C3H8O6P-H2O-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
        }

        public static void lpiFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "LPI" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass;
                //var fra02int = 100;
                //var fra02comment = "SN1 acyl loss";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 450;
                var fra03comment = "NL of SN1+H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra06mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra06int = 999;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra08mass = 12 * 6 + MassDictionary.HydrogenMass * 11 + MassDictionary.OxygenMass * 8 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra08int = 300;
                var fra08comment = "C6H10O8P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra09int = 500;
                var fra09comment = "C3H6O5P-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra07mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra07int = 50;
                var fra07comment = "PO3-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
        }

        public static void lpsFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "LPS" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 10;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - 12 * 3 - MassDictionary.HydrogenMass * 5 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass * 2;
                var fra02int = 500;
                var fra02comment = "[M-H]- C3H5NO2";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra03int = 200;
                var fra03comment = "SN1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra04int = 999;
                var fra04comment = "[C3H8O6P-H2O-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra07mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra07int = 500;
                var fra07comment = "PO3-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
        }

        public static void lpaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "LPA" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 400;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra04int = 999;
                var fra04comment = "[C3H8O6P-H2O-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra07mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra07int = 50;
                var fra07comment = "PO3-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }

        public static void etherLpcFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "EtherLPC" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-"}    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 10;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass - MassDictionary.Proton - 12 - MassDictionary.HydrogenMass * 2;
                var fra02int = 999;
                var fra02comment = "[M-CH3]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra04mass = exactMass - MassDictionary.Proton - (12 * 5 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass);
                var fra04int = 150;
                var fra04comment = "[M-C5H14NO]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra03mass = 12 * 4 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra03int = 100;
                var fra03comment = "C4H11NO4P-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra07mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra07int = 130;
                var fra07comment = "PO3-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = 12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra05int = 500;
                var fra05comment = "C5H15NO4P+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra06int = 300;
                var fra06comment = "C2H5O4P+H+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = 12 * 5 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass + MassDictionary.Proton;
                var fra07int = 999;
                var fra07comment = "C5H14NO+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 5 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra08int = 300;
                var fra08comment = "C5H12N+";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

            }

        }
        public static void etherLpeFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "EtherLPE" ,    new List<string>() { "[M-H]-" , "[M+H]+"}    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass);
                var fra02int = 250;
                var fra02comment = "[M-C2H8NO]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra03int = 250;
                var fra03comment = "C2H7NO4P-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra07mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra07int = 600;
                var fra07comment = "PO3-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.Proton - (12 * 3 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra02int = 350;
                var fra02comment = "[M-C3H8NO4P]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "[M-C3H10NO5P]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

            }

        }
        public static void etherLpePFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "EtherLPE_P" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 5 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass);
                var fra02int = 999;
                var fra02comment = "[M-C5H13NO5P]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 5 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra03int = 500;
                var fra03comment = "C5H11NO5P-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra04int = 300;
                var fra04comment = "C2H7NO4P-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra07mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra07int = 600;
                var fra07comment = "PO3-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }

        public static void etherLpgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "EtherLPG" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 700;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra06mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra06int = 300;
                var fra06comment = "SN1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra03mass = 12 * 3 + MassDictionary.HydrogenMass * 9 + MassDictionary.OxygenMass * 6 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra03int = 50;
                var fra03comment = "C3H8O6P-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra07mass = 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra07int = 999;
                var fra07comment = "[C3H8O6P-H2O-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra08int = 500;
                var fra08comment = "PO3-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }
        }

        public static void tgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double)
        {
            //{ "TG" ,    new List<string>() { "[M+Na]+", "[M+NH4]+", }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = acylChainMass(chain3Carbon, chain3Double, 0);
            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 50;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "NL of SN1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "NL of SN2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - chain3Mass - MassDictionary.H2OMass;
                var fra05int = 999;
                var fra05comment = "NL of SN3";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - chain1Mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "NL of SN1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 50;
                var fra04comment = "NL of SN2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra01mass - chain3Mass - MassDictionary.H2OMass;
                var fra05int = 50;
                var fra05comment = "NL of SN3";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }

        }

        public static void dgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "DG" ,    new List<string>(){ "[M+Na]+", "[M+NH4]+"  , }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+NH4]+")
            {
                var fra00mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;

                var fra01mass = exactMass + MassDictionary.Proton;
                var fra01int = 50;
                var fra01comment = "[M+H]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 400;
                var fra02comment = "[M-H2O+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass;
                var fra03int = 999;
                var fra03comment = "NL of SN1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain2Mass;
                var fra04int = 999;
                var fra04comment = "NL of SN2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                //var fra03mass = fra01mass - chain1Mass - MassDictionary.H2OMass;
                //var fra03int = 999;
                //var fra03comment = "NL of SN1";
                //fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                //var fra04mass = fra01mass - chain2Mass - MassDictionary.H2OMass;
                //var fra04int = 999;
                //var fra04comment = "NL of SN2";
                //fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void mgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{    "MG" ,    new List<string>(){ "[M+NH4]+"  },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 50;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "[M-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
        }

        public static void bmpFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{    "BMP" ,    new List<string>(){ "[M+NH4]+"  },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 50;
                //var fra02comment = "[M+H]+";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 12 * 3 - MassDictionary.HydrogenMass * 9 - MassDictionary.OxygenMass * 6 - MassDictionary.PhosphorusMass;
                var fra03int = 50;
                var fra03comment = "[M-C3H8O6P]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass + MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2;
                var fra04int = 999;
                var fra04comment = "[SN1+C3H5O]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass + MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2;
                var fra05int = 999;
                var fra05comment = "[SN2+C3H5O]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
        }

        public static void dgdgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{ "DGDG" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 100;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 500;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass;
                var fra03int = 300;
                var fra03comment = "NL of SN1+HCOOH";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain2Mass;
                var fra04int = 300;
                var fra04comment = "NL of SN2+HCOOH";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass - MassDictionary.H2OMass;
                var fra05int = 300;
                var fra05comment = "NL of SN1+HCOOH+H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra04mass - MassDictionary.H2OMass;
                var fra06int = 300;
                var fra06comment = "NL of SN2+HCOOH+H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra07int = 600;
                var fra07comment = "sn1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain2Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra08int = 600;
                var fra08comment = "sn2";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 15 + MassDictionary.HydrogenMass * 24 + MassDictionary.OxygenMass * 11 - MassDictionary.Proton;
                var fra09int = 500;
                var fra09comment = "C15H23O11";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = fra09mass + MassDictionary.H2OMass;
                var fra10int = 999;
                var fra10comment = "C15H25O12";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = fra10mass + MassDictionary.H2OMass;
                var fra11int = 200;
                var fra11comment = "C15H27O13";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = "M+NH4";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 0;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - (12 * 12 + MassDictionary.HydrogenMass * 20 + MassDictionary.OxygenMass * 10);
                var fra03int = 999;
                var fra03comment = "NL of C12H20O10+NH3";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "NL of C12H22O11+NH3";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass - chain1Mass - MassDictionary.H2OMass;
                var fra05int = 999;
                var fra05comment = "NL of C12H20O10+NH3+SN1";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra03mass - chain2Mass - MassDictionary.H2OMass;
                var fra06int = 999;
                var fra06comment = "NL of C12H20O10+NH3+SN2";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }

        public static void mgdgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "MGDG" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 300;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 100;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass;
                var fra03int = 50;
                var fra03comment = "NL of SN1+HCOOH";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain2Mass;
                var fra04int = 50;
                var fra04comment = "NL of SN2+HCOOH";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra07mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra07int = 999;
                var fra07comment = "sn1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain2Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra08int = 999;
                var fra08comment = "sn2";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 9 + MassDictionary.HydrogenMass * 18 + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra09int = 50;
                var fra09comment = "C9H17O8";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = "M+NH4";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 0;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.C6H10O5 - MassDictionary.H2OMass;
                var fra03int = 600;
                var fra03comment = "NL of C12H20O10+NH3";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = fra03mass - chain1Mass;
                var fra05int = 999;
                var fra05comment = "NL of C12H20O10+NH3+SN1";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra03mass - chain2Mass;
                var fra06int = 999;
                var fra06comment = "NL of C12H20O10+NH3+SN2";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }

        public static void sqdgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "SQDG" ,    new List<string>() { "[M-H]-", "[M+NH4]+", }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - chain1Mass - MassDictionary.H2OMass;
                var fra03int = 100;
                var fra03comment = "NL of SN1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 100;
                var fra04comment = "NL of SN2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra07mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra07int = 100;
                var fra07comment = "sn1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain2Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra08int = 100;
                var fra08comment = "sn2";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 6 + MassDictionary.HydrogenMass * 10 + MassDictionary.OxygenMass * 7 + MassDictionary.SulfurMass - MassDictionary.Proton;
                var fra09int = 200;
                var fra09comment = "C6H9O7S";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);


            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 0;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass - MassDictionary.H2OMass;
                var fra03int = 100;
                var fra03comment = "[M-SN1+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 100;
                var fra04comment = "[M-SN2+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - MassDictionary.C6H10O5 - MassDictionary.OxygenMass * 2 - MassDictionary.SulfurMass;
                var fra05int = 100;
                var fra05comment = "[M-C6H10O7S+H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 200;
                var fra06comment = "[M-C6H10O7S-H2O+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain1Mass + MassDictionary.Proton;
                var fra07int = 100;
                var fra07comment = "sn1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain2Mass + MassDictionary.Proton;
                var fra08int = 100;
                var fra08comment = "sn2";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra05mass - chain1Mass - MassDictionary.H2OMass;
                var fra09int = 999;
                var fra09comment = "[M-C6H10O7S-SN1+H]+";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = fra05mass - chain2Mass - MassDictionary.H2OMass;
                var fra10int = 999;
                var fra10comment = "[M-C6H10O7S-SN2+H]+";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);
            }
        }

        public static void dgmgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{   "DGMG" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 150;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 150;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                //var fra03mass = MassDictionary.C6H10O5 + MassDictionary.H2OMass - MassDictionary.Proton;
                //var fra03int = 400;
                //var fra03comment = "sugar-";
                //fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra07mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra07int = 999;
                var fra07comment = "sn1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra10mass = 12 * 15 + MassDictionary.HydrogenMass * 24 + MassDictionary.OxygenMass * 11 - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra10int = 800;
                var fra10comment = "C15H25O12";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = fra10mass + MassDictionary.H2OMass;
                var fra11int = 200;
                var fra11comment = "C15H27O13";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = "M+NH4";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 0;
                //var fra02comment = "[M+H]+";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - (12 * 12 + MassDictionary.HydrogenMass * 20 + MassDictionary.OxygenMass * 10);
                var fra03int = 500;
                var fra03comment = "[M+H-2sugars]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "[M+H-2sugars]+ -H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain1Mass + MassDictionary.Proton;
                var fra05int = 700;
                var fra05comment = "SN1(acyl)+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 500;
                var fra06comment = "SN1(acyl)+ -H2O";
                if (chain1Double != 0)
                {
                    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
                }

            }
        }

        public static void mgmgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //    { "MGMG" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 200;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 10;
                var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra07mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra07int = 999;
                var fra07comment = "sn1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = "M+NH4";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 0;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.C6H10O5 - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "NL of C6H10O5 and H2O";

                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
                var fra05mass = chain1Mass + MassDictionary.Proton;
                var fra05int = 500;
                var fra05comment = "SN1(acyl)+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 500;
                var fra06comment = "SN1(acyl)+ -H2O";
                if (chain1Double != 0)
                {
                    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
                }
            }
        }

        public static void dgtsFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "DGTS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-","[M+H]+"  }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);

            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - chain1Mass;
                var fra03int = 300;
                var fra03comment = "[M-SN1+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass;
                var fra04int = 300;
                var fra04comment = "[M-SN2+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass - MassDictionary.H2OMass;
                var fra05int = 300;
                var fra05comment = "NL of SN1+H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra04mass - MassDictionary.H2OMass;
                var fra06int = 300;
                var fra06comment = "NL of SN2+H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra09mass = 12 * 10 + MassDictionary.HydrogenMass * 21 + MassDictionary.OxygenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra09int = 400;
                var fra09comment = "C10H21NO5H";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = 12 * 7 + MassDictionary.HydrogenMass * 13 + MassDictionary.OxygenMass * 2 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra10int = 50;
                var fra10comment = "C7H14NO2";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 700;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 50;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - (12 * 3 + MassDictionary.HydrogenMass * 9 + MassDictionary.NitrogenMass);
                var fra03int = 100;
                var fra03comment = "[M+Na]+ -C3H9N";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra07mass = fra01mass - (12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2);
                var fra07int = 200;
                var fra07comment = "[M+Na]+ -C2H2O3";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra04mass = fra03mass - (12 + MassDictionary.OxygenMass);
                var fra04int = 999;
                var fra04comment = "[M+Na]+ -C3H9N -CO";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - chain1Mass - MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "[M+Na]+ -C3H9N -C2H4 -sn1";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra04mass - chain2Mass - MassDictionary.H2OMass;
                var fra06int = 100;
                var fra06comment = "[M+Na]+ -C3H9N -C2H4 -sn2";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra09mass = 12 * 10 + MassDictionary.HydrogenMass * 21 + MassDictionary.OxygenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra09int = 50;
                var fra09comment = "C10H21NO5H";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = 12 * 7 + MassDictionary.HydrogenMass * 13 + MassDictionary.OxygenMass * 2 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra10int = 10;
                var fra10comment = "C7H14NO2";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

            }

            else if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 10;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }

                var fra02mass = exactMass - MassDictionary.Proton - 12 - MassDictionary.HydrogenMass;
                var fra02int = 100;
                var fra02comment = "[M-CH2]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = exactMass - MassDictionary.Proton - 12 * 3 - MassDictionary.HydrogenMass * 5;
                var fra03int = 999;
                var fra03comment = "[M-C3H5]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 10;
                var fra04comment = "[M-C3H5-H2O]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass - chain1Mass;
                var fra05int = 50;
                var fra05comment = "[M-C3H5-SN1]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra03mass - chain2Mass;
                var fra06int = 50;
                var fra06comment = "[M-C3H5-SN2]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra07int = 50;
                var fra07comment = "SN1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra08int = 50;
                var fra08comment = "SN2";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }
        }

        public static void dggaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //   {   "DGGA" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - chain1Mass;
                var fra03int = 50;
                var fra03comment = "[M-SN1-H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass;
                var fra04int = 50;
                var fra04comment = "[M-SN2-H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 500;
                var fra05comment = "[SN1-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra06int = 500;
                var fra06comment = "[SN2-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton - MassDictionary.C6H10O5 - MassDictionary.OxygenMass * 2;
                var fra02int = 500;
                var fra02comment = "[M-C6H10O7+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2;
                var fra03int = 999;
                var fra03comment = "[SN1+C3H4O2+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain2Mass + MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2;
                var fra04int = 999;
                var fra04comment = "[SN2+C3H4O2+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void dlclFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //  {   "DLCL" ,    new List<string>(){ "[M-H]-"  }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra02int = 500;
                var fra02comment = "C3H6O5P-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass - MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.H2OMass + MassDictionary.PhosphorusMass;
                var fra03int = 250;
                var fra03comment = "[FA1+C3H6O5P]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain2Mass - MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.H2OMass + MassDictionary.PhosphorusMass;
                var fra04int = 250;
                var fra04comment = "[FA2+C3H6O5P]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 50;
                var fra05comment = "[SN1-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra06int = 50;
                var fra06comment = "[SN2-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }

        public static void smgdgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //  {  "SMGDG" ,    new List<string>(){ "[M-H]-"  }   },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 6 + MassDictionary.HydrogenMass * 10 + MassDictionary.OxygenMass * 8 + MassDictionary.SulfurMass - MassDictionary.Proton;
                var fra02int = 10;
                var fra02comment = "[C6H9O8S]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - chain1Mass - MassDictionary.H2OMass;
                var fra03int = 10;
                var fra03comment = "[M-SN1-H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 10;
                var fra04comment = "[M-SN2-H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 10;
                var fra05comment = "[SN1-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra06int = 10;
                var fra06comment = "[SN2-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = MassDictionary.HydrogenMass * 2 + MassDictionary.OxygenMass * 4 + MassDictionary.SulfurMass - MassDictionary.Proton;
                var fra07int = 10;
                var fra07comment = "[H2SO4-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }

        public static void dgccFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //  {  "DGCC" ,    new List<string>(){ "[M+H]+"  }   },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - chain1Mass;
                var fra03int = 50;
                var fra03comment = "[NL of SN1]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass;
                var fra04int = 50;
                var fra04comment = "[NL of SN2]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass - MassDictionary.H2OMass;
                var fra05int = 50;
                var fra05comment = "[NL of SN1+H2O]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra04mass - MassDictionary.H2OMass;
                var fra06int = 50;
                var fra06comment = "[NL of SN2+H2O]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra02mass = 12 * 6 + MassDictionary.HydrogenMass * 13 + MassDictionary.OxygenMass * 2 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra02int = 100;
                var fra02comment = "C6H14NO2+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra07mass = fra02mass - 12 - MassDictionary.OxygenMass;
                var fra07int = 200;
                var fra07comment = "C5H14NO+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }

        public static void etherPcFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{ "EtherPC" ,    new List<string>() { "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 300;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }

                var fra02mass = exactMass - MassDictionary.Proton - 12 - MassDictionary.HydrogenMass * 2;
                var fra02int = 800;
                var fra02comment = "[M-CH3]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 7 + MassDictionary.HydrogenMass * 16 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra03int = 100;
                var fra03comment = "C7H15NO5P";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain2Mass;
                var fra04int = 200;
                var fra04comment = "M-CH3-SN2(RCOOH)";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 999;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra04mass - MassDictionary.H2OMass;
                var fra06int = 200;
                var fra06comment = "M-CH3-SN2(RCH=C=O)";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra02int = 20;
                var fra02comment = "loss C5H14NO4P";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = (12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass) + MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "C5H14NO4P";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass;
                var fra04int = 20;
                var fra04comment = "acyl loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void etherPePFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "EtherPE" ,    new List<string>() { "[M-H]-", "[M+H]+" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain2Mass;
                var fra02int = 250;
                var fra02comment = "SN2 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "SN2";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra02int = 200;
                var fra02comment = "PreCursor -C2H8NO4P";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + MassDictionary.OxygenMass + MassDictionary.Proton + (12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass);
                var fra03int = 500;
                var fra03comment = "Sn1ether+C2H8NO3P ";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - (MassDictionary.HydrogenMass * 3 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra04int = 200;
                var fra04comment = "SN1 ether +C2H8NO3P-H3PO4";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - chain1Mass;
                var fra05int = 999;
                var fra05comment = "NL of C2H8NO4P+SN1";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }

        }
        public static void etherPeOFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "EtherPEe" ,    new List<string>() { "[M+H]+" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra02int = 999;
                var fra02comment = "PreCursor -141";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain2Mass + MassDictionary.Proton;
                var fra03int = 1;
                var fra03comment = "SN2 acyl";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

            }

        }
        public static void etherPgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "EtherPG" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
            {
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = (12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass) - MassDictionary.Proton;
                var fra03int = 50;
                var fra03comment = "C3H8O5P-H";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass + MassDictionary.OxygenMass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra04int = 250;
                var fra04comment = "[SN1+O-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 900;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
        }

        public static void etherPiFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "EtherPI" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain2Mass - MassDictionary.C6H10O5 - MassDictionary.H2OMass;
                var fra02int = 200;
                var fra02comment = "M-SN2-C6H12O6";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - chain2Mass;
                var fra03int = 100;
                var fra03comment = "SN2 acyl loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 100;
                var fra04comment = "SN2 loss -H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 150;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = MassDictionary.C6H10O5 + MassDictionary.PhosphorusMass + MassDictionary.OxygenMass * 3 + MassDictionary.Electron;
                var fra06int = 200;
                var fra06comment = "C6H10O8P";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }

        public static void etherPsFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "EtherPS" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - 12 * 3 - MassDictionary.HydrogenMass * 5 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass * 2;
                var fra02int = 600;
                var fra02comment = "[M-H]- C3H5NO2";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain2Mass;
                var fra03int = 999;
                var fra03comment = "M-87-SN2";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 200;
                var fra04comment = "SN2+H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }
        }

        public static void etherDgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "EtherDAG" ,    new List<string>() { "[M+NH4]+" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 50;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra02int = 50;
                var fra02comment = "[M-H2O+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass;
                var fra03int = 999;
                var fra03comment = "[NL of SN1]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

            }

        }
        public static void etherDgdgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "EtherDGDG" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-", "[M+Na]+", "[M+NH4]+", }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 100;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 500;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain2Mass;
                var fra03int = 300;
                var fra03comment = "NL of SN2";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 300;
                var fra04comment = "NL of SN2+H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra05int = 600;
                var fra05comment = "sn2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain2Mass - MassDictionary.H2OMass;
                var fra02int = 150;
                var fra02comment = "M-SN2";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.C6H10O5;
                var fra03int = 150;
                var fra03comment = "M-SN2-C6H10O5";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton - MassDictionary.C6H10O5 * 2;
                var fra02int = 999;
                var fra02comment = "NL of C12H20O10+NH3";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "NL of C12H22O11+NH3";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = fra02mass - chain2Mass - MassDictionary.H2OMass;
                var fra05int = 999;
                var fra05comment = "NL of C12H20O10+NH3+SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }

        }
        public static void etherMgdgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            //    { "EtherMGDG" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-", "[M+Na]+", "[M+NH4]+", }    },
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 0;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 200;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain2Mass;
                var fra03int = 50;
                var fra03comment = "NL of SN2";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = chain2Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra05int = 999;
                var fra05comment = "sn2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain2Mass - MassDictionary.H2OMass;
                var fra02int = 150;
                var fra02comment = "M-SN2";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton - MassDictionary.C6H10O5 - MassDictionary.H2OMass;
                var fra02int = 600;
                var fra02comment = "NL of C6H12O6";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra04mass = fra02mass - chain1Mass;
                var fra04int = 999;
                var fra04comment = "NL of C6H12O6+SN1";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }

        }
        public static void etherSmgdgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "EtherSMGDG" ,    new List<string>() {  "[M-H]-", "[M+NH4]+"  }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass; ;
                var fra01int = 999;
                var fra01comment = "";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain2Mass - MassDictionary.H2OMass;
                var fra02int = 5;
                var fra02comment = "NL of (SN2 and H2O)";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 6 + MassDictionary.HydrogenMass * 9 + MassDictionary.OxygenMass * 8 + MassDictionary.SulfurMass + MassDictionary.Electron;
                var fra03int = 5;
                var fra03comment = "[C6H9O8S]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = MassDictionary.HydrogenMass * 2 + MassDictionary.SulfurMass + MassDictionary.OxygenMass * 4 - MassDictionary.Proton;
                var fra04int = 5;
                var fra04comment = "[H2SO4-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton - (12 * 6 + MassDictionary.HydrogenMass * 10 + MassDictionary.OxygenMass * 8 + MassDictionary.SulfurMass);
                var fra02int = 100;
                var fra02comment = "[M-SHex]+ ";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 700;
                var fra03comment = "[M-SHex-H2O]+ ";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain2Mass + MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass + MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "[SN2+C3H5O]+ ";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass + MassDictionary.Proton;
                var fra05int = 50;
                var fra05comment = "SN2-H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain1Mass + MassDictionary.Proton + 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2;
                var fra06int = 50;
                var fra06comment = "[SN1+C3H5O]+ ";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }

        public static void oxPcFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain2Ox)
        {
            //{   "OxPC" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-"}    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, chain2Ox);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 200;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                //var fra02int = 999;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = exactMass - MassDictionary.Proton - 12 - MassDictionary.HydrogenMass * 2;
                var fra03int = 999;
                var fra03comment = "[M-CH3]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 600;
                var fra04comment = "SN1";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 100;
                var fra06comment = "SN2-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (chain2Ox > 1)
                {
                    var fra07mass = fra06mass - MassDictionary.H2OMass;
                    var fra07int = 100;
                    var fra07comment = "SN2-2H2O";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                }

                var fra08mass = 12 * 7 + MassDictionary.HydrogenMass * 15 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra08int = 100;
                var fra08comment = "C7H15NO5P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }
        }
        public static void oxPeFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain2Ox)
        {
            //{   "OxPE" ,    new List<string>(){ "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, chain2Ox);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 700;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra04mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "SN1";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 100;
                var fra06comment = "SN2-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (chain2Ox > 1)
                {
                    var fra07mass = fra06mass - MassDictionary.H2OMass;
                    var fra07int = 100;
                    var fra07comment = "SN2-2H2O";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                }

                var fra08mass = 12 * 5 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra08int = 100;
                var fra08comment = "C5H11NO5P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

            }
        }
        public static void oxPgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain2Ox)
        {
            //{   "OxPG" ,    new List<string>(){ "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, chain2Ox);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra04mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "SN1";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 100;
                var fra06comment = "SN2-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (chain2Ox > 1)
                {
                    var fra07mass = fra06mass - MassDictionary.H2OMass;
                    var fra07int = 100;
                    var fra07comment = "SN2-2H2O";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                }
            }
        }
        public static void oxPiFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain2Ox)
        {
            //{   "OxPI" ,    new List<string>(){ "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, chain2Ox);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra04mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 200;
                var fra04comment = "SN1";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 200;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 50;
                var fra06comment = "SN2-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (chain2Ox > 1)
                {
                    var fra07mass = fra06mass - MassDictionary.H2OMass;
                    var fra07int = 50;
                    var fra07comment = "SN2-2H2O";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                }

                var fra08mass = 12 * 9 + MassDictionary.HydrogenMass * 14 + MassDictionary.OxygenMass * 9 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra08int = 200;
                var fra08comment = "C9H14O9P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 6 + MassDictionary.HydrogenMass * 10 + MassDictionary.OxygenMass * 8 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra09int = 200;
                var fra09comment = "C6H10O8P";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);
            }
        }

        public static void oxPsFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain2Ox)
        {
            //{   "OxPS" ,    new List<string>(){ "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, chain2Ox);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - 12 * 3 - MassDictionary.HydrogenMass * 5 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass * 2;
                var fra03int = 600;
                var fra03comment = "NL of C3H5NO2";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "SN1";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 100;
                var fra06comment = "SN2-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (chain2Ox > 1)
                {
                    var fra07mass = fra06mass - MassDictionary.H2OMass;
                    var fra07int = 100;
                    var fra07comment = "SN2-2H2O";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                }
            }
        }

        public static void adggaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double)
        {
            // {   "ADGGA" ,    new List<string>(){ "[M+NH4]+", "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = acylChainMass(chain3Carbon, chain3Double, 0);

            if (adduct == "[M+NH4]+")
            {
                //var fragmentList = new List<string>();

                var fra01mass = exactMass + MassDictionary.NH4Adduct;
                var fra01int = 300;
                var fra01comment = "[M+NH4]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass - 194.042652622 - chain3Mass + MassDictionary.Proton;
                var fra02int = 100;
                var fra02comment = "[M-SN3-C6H10O7+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 194.042652622 + chain3Mass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[C6H10O7+SN3+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass + MassDictionary.CarbonMass * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                var fra04int = 300;
                var fra04comment = "[SN1+C3H4O2+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass + MassDictionary.CarbonMass * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                var fra05int = 300;
                var fra05comment = "[SN2+C3H4O2+H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain3Mass + MassDictionary.Proton;
                var fra06int = 300;
                var fra06comment = "[SN3-2H+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }
            //(neg H)
            if (adduct == "[M-H]-")
            {
                //var fragmentList = new List<string>();

                var fra01mass = exactMass - MassDictionary.Proton;
                var fra01int = 999;
                var fra01comment = "[M-H]-";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra02int = 300;
                var fra02comment = "[SN1]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain2Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra03int = 300;
                var fra03comment = "[SN2]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain3Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra04int = 300;
                var fra04comment = "[SN3]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void etherOxPcFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain2Ox)
        {
            //{ "EtherOxPC" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, chain2Ox);

            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 300;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass - MassDictionary.Proton - 12 - MassDictionary.HydrogenMass * 2;
                var fra02int = 800;
                var fra02comment = "[M-CH3]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain2Mass;
                var fra03int = 200;
                var fra03comment = "NL of SN2+CH3";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 200;
                var fra04comment = "NL of SN2+CH3+H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 999;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 50;
                var fra06comment = "SN2-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (chain2Ox > 1)
                {
                    var fra07mass = fra06mass - MassDictionary.H2OMass;
                    var fra07int = 50;
                    var fra07comment = "SN2-2H2O";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                }

                var fra08mass = 12 * 7 + MassDictionary.HydrogenMass * 15 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra08int = 100;
                var fra08comment = "C7H15NO5P";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }
        }
        public static void etherOxPeFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain2Ox)
        {
            //    { "EtherOxPE" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = etherChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, chain2Ox);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra04mass = fra01mass - chain2Mass;
                var fra04int = 250;
                var fra04comment = "[M-H]-SN2 acyl chain";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 999;
                var fra05comment = "SN2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 50;
                var fra06comment = "SN2-H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (chain2Ox > 1)
                {
                    var fra07mass = fra06mass - MassDictionary.H2OMass;
                    var fra07int = 50;
                    var fra07comment = "SN2-2H2O";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                }
            }
        }

        public static void lnapeFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{ "LNAPE" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 700;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass;
                var fra02int = 100;
                var fra02comment = "SN1 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 100;
                var fra03comment = "SN1andH2O loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "SN1";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra05int = 50;
                var fra05comment = "C3H6O5P";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }

        }
        public static void lnapsFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "LNAPS" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 700;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain2Mass - (12 * 3 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass * 2 + MassDictionary.NitrogenMass);
                var fra02int = 999;
                var fra02comment = "NL of C3H5NO2+SN2+H2O";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra03int = 700;
                var fra03comment = "C3H6O5P";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }

        }

        public static void ldgccFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "LDGCC" ,    new List<string>() { "[M+H]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 6 + MassDictionary.HydrogenMass * 13 + MassDictionary.OxygenMass * 2 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra02int = 100;
                var fra02comment = "C6H14NO2+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra07mass = fra02mass - 12 - MassDictionary.OxygenMass;
                var fra07int = 200;
                var fra07comment = "C5H14NO+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }

        public static void ldgtsFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //    { "LDGTS" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-","[M+H]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 400;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - MassDictionary.H2OMass;
                var fra03int = 200;
                var fra03comment = "[M+H-H2O]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = fra01mass - chain1Mass - MassDictionary.H2OMass;
                var fra05int = 50;
                var fra05comment = "NL of SN1+H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra09mass = 12 * 10 + MassDictionary.HydrogenMass * 21 + MassDictionary.OxygenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra09int = 400;
                var fra09comment = "C10H21NO5H";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = 12 * 7 + MassDictionary.HydrogenMass * 13 + MassDictionary.OxygenMass * 2 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra10int = 50;
                var fra10comment = "C7H14NO2";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 300;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - (12 * 3 + MassDictionary.HydrogenMass * 9 + MassDictionary.NitrogenMass);
                //var fra03int = 200;
                //var fra03comment = "[M+Na]+ -C3H9N";
                //fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - (12 + MassDictionary.OxygenMass);
                var fra04int = 999;
                var fra04comment = "[M+Na]+ -C3H9N -CO";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass - (12 + MassDictionary.OxygenMass * 2);
                var fra05int = 200;
                var fra05comment = "[M+Na]+ -C3H9N -CO2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra09mass = 12 * 10 + MassDictionary.HydrogenMass * 21 + MassDictionary.OxygenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra09int = 100;
                var fra09comment = "C10H21NO5H";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);
            }

            else if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 1;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }

                var fra03mass = exactMass - MassDictionary.Proton - 12 * 3 - MassDictionary.HydrogenMass * 5;
                var fra03int = 999;
                var fra03comment = "[M-C3H5]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra07mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra07int = 500;
                var fra07comment = "SN1";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }

        public static void mlclFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double)
        {
            //{ "MLCL" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = acylChainMass(chain3Carbon, chain3Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra06mass = 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra06int = 300;
                var fra06comment = "C3H6O5P";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra02mass = chain1Mass + chain2Mass + fra06mass + MassDictionary.H2OMass;
                var fra02int = 300;
                var fra02comment = "[FA2+FA3+C3H4O4P]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain3Mass + fra06mass + MassDictionary.H2OMass;
                var fra03int = 300;
                var fra03comment = "[FA1+C3H6O5P]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 100;
                var fra04comment = "[FA1-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 100;
                var fra05comment = "[FA2-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra07mass = chain3Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra07int = 100;
                var fra07comment = "[FA3-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
        }

        public static void hbmpFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double)
        {
            //    { "HBMP" ,    new List<string>() { "[M-H]-", "[M+NH4]+", }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = acylChainMass(chain3Carbon, chain3Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain3Mass - MassDictionary.H2OMass;
                var fra02int = 50;
                var fra02comment = "[M-SN3-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain3Mass + 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra03int = 50;
                var fra03comment = "[SN3+C3H6O4P-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain2Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 300;
                var fra04comment = "[FA2-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain3Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra05int = 300;
                var fra05comment = "[FA3-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain1Mass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra06int = 300;
                var fra06comment = "[FA1-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = chain1Mass + MassDictionary.Proton + (12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2);
                var fra03int = 100;
                var fra03comment = "[SN1+C3H4O2+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain2Mass + MassDictionary.Proton + (12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2);
                var fra04int = 100;
                var fra04comment = "[SN2+C3H4O2+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = chain3Mass + MassDictionary.Proton + (12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2);
                var fra05int = 100;
                var fra05comment = "[SN3+C3H4O2+H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra02mass = exactMass - fra05mass - (MassDictionary.HydrogenMass * 3 + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass) + 2 * MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "[M-SN3-H3O4P]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
        }

        public static void oxTgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double, int chain3Ox)
        {
            //{ "OxTG" ,    new List<string>(){ "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = acylChainMass(chain3Carbon, chain3Double, chain3Ox);
            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 50;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "NL of SN1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "NL of SN2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - chain3Mass - MassDictionary.H2OMass;
                var fra05int = 999;
                var fra05comment = "NL of SN3";// chain3 = OxFA = SN3
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra02mass - chain1Mass - 2 * MassDictionary.H2OMass;
                var fra06int = 500;
                var fra06comment = "NL of SN1 and H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra02mass - chain2Mass - 2 * MassDictionary.H2OMass;
                var fra07int = 500;
                var fra07comment = "NL of SN2 and H2O";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra02mass - MassDictionary.H2OMass;
                var fra08int = 500;
                var fra08comment = "[M-H2O+H]+";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = chain1Mass + MassDictionary.Proton + (12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2);
                var fra09int = 100;
                var fra09comment = "SN1+C3H6O2";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = chain2Mass + MassDictionary.Proton + (12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 2);
                var fra10int = 100;
                var fra10comment = "SN2+C3H6O2";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = chain3Mass + MassDictionary.Proton;
                var fra11int = 200;
                var fra11comment = "SN3+";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

            }

        }

        public static void etherTgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double)
        {
            //{ "EtherTAG" ,    new List<string>() { "[M+Na]+", "[M+NH4]+", }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = etherChainMass(chain3Carbon, chain3Double, 0);

            if (adduct == "[M+NH4]+") // chain3 = ether = SN1
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 50;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "NL of SN2";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "NL of SN3";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - chain3Mass - MassDictionary.H2OMass;
                var fra05int = 999;
                var fra05comment = "NL of SN1";// chain3 = ether = SN1
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }
            else if (adduct == "[M+Na]+") // chain3 = ether = SN1
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - chain1Mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "NL of SN2";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 50;
                var fra04comment = "NL of SN3";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }

        }

        public static void cardiolipinFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double, int chain4Carbon, int chain4Double)
        {
            //{ "CL" ,    new List<string>() { "[M-H]-", "[M+NH4]+", }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = acylChainMass(chain3Carbon, chain3Double, 0);
            var chain4Mass = acylChainMass(chain4Carbon, chain4Double, 0);
            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 300;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + chain2Mass + 2 * MassDictionary.H2OMass + 12 * 3 + MassDictionary.HydrogenMass * 2 + MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[SN1+SN2+C3H3]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain3Mass + chain4Mass + 2 * MassDictionary.H2OMass + 12 * 3 + MassDictionary.HydrogenMass * 2 + MassDictionary.Proton;
                var fra04int = 999;
                var fra04comment = "[SN3+SN4+C3H3]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }
            else if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra12mass = 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra12int = 300;
                var fra12comment = "[C3H6O5P]-";
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                var fra02mass = fra12mass + chain1Mass + chain2Mass + MassDictionary.H2OMass;
                var fra02int = 500;
                var fra02comment = "[SN1+SN2+C3H6O4P]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra12mass + chain3Mass + chain4Mass + MassDictionary.H2OMass;
                var fra03int = 500;
                var fra03comment = "[SN3+SN4+C3H6O4P]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra12mass + chain1Mass;
                var fra04int = 700;
                var fra04comment = "[SN1+C3H6O4P]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra12mass + chain2Mass;
                var fra05int = 700;
                var fra05comment = "[SN2+C3H6O4P]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra12mass + chain3Mass;
                var fra06int = 700;
                var fra06comment = "[SN3+C3H6O4P]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra12mass + chain4Mass;
                var fra07int = 700;
                var fra07comment = "[SN4+C3H6O4P]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra08int = 500;
                var fra08comment = "[SN1-H]-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = chain2Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra09int = 500;
                var fra09comment = "[SN2-H]-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = chain3Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra10int = 500;
                var fra10comment = "[SN3-H]-";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = chain4Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra11int = 500;
                var fra11comment = "[SN4-H]-";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 500;
                var fra01comment = "[M-2H]2- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra12mass = 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra12int = 500;
                var fra12comment = "[C3H6O5P]-";
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                var fra04mass = fra12mass + chain1Mass;
                var fra04int = 300;
                var fra04comment = "[SN1+C3H6O4P]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra12mass + chain2Mass;
                var fra05int = 300;
                var fra05comment = "[SN2+C3H6O4P]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra12mass + chain3Mass;
                var fra06int = 300;
                var fra06comment = "[SN3+C3H6O4P]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra12mass + chain4Mass;
                var fra07int = 300;
                var fra07comment = "[SN4+C3H6O4P]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain1Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra08int = 999;
                var fra08comment = "[SN1-H]-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = chain2Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra09int = 999;
                var fra09comment = "[SN2-H]-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = chain3Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra10int = 999;
                var fra10comment = "[SN3-H]-";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = chain4Mass + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra11int = 999;
                var fra11comment = "[SN4-H]-";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);
            }
        }

        public static void fahfaTgFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double, int chain4Carbon, int chain4Double)
        {
            //{ "TG_EST" ,    new List<string>() { "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = acylChainMass(chain3Carbon, chain3Double, 0) + MassDictionary.OxygenMass;
            var chain4Mass = acylChainMass(chain4Carbon, chain4Double, 0);

            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 50;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass - MassDictionary.H2OMass;
                var fra03int = 800;
                var fra03comment = "NL of SN1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain2Mass - MassDictionary.H2OMass;
                var fra04int = 800;
                var fra04comment = "NL of SN2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - chain4Mass - MassDictionary.H2OMass;
                var fra05int = 250;
                var fra05comment = "NL of SN4";// chain4 = Extra FA
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - chain3Mass;
                var fra06int = 999;
                var fra06comment = "NL of SN3 and SN4";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra05mass - chain1Mass - MassDictionary.H2OMass;
                var fra07int = 100;
                var fra07comment = "NL of SN1 and SN4";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra05mass - chain2Mass - MassDictionary.H2OMass;
                var fra08int = 100;
                var fra08comment = "NL of SN2 and SN4";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }
        }

        public static void Ac2PIM1Fragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{ "Ac2PIM1", new List<string>() { "[M-H]-" } },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.C6H10O5;
                var fra02int = 200;
                var fra02comment = "[M-H]-C6H10O5";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - chain1Mass - MassDictionary.OxygenMass;
                var fra03int = 999;
                var fra03comment = "[M-H]-sn1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass - MassDictionary.OxygenMass;
                var fra04int = 300;
                var fra04comment = "[M-H]-sn2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - MassDictionary.C6H10O5;
                var fra05int = 600;
                var fra05comment = "[M-H]-C12H20O10";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - chain1Mass - MassDictionary.OxygenMass;
                var fra06int = 400;
                var fra06comment = "[M-H]-C12H20O10-sn1";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra05mass - chain2Mass - MassDictionary.OxygenMass;
                var fra07int = 400;
                var fra07comment = "[M-H]-C12H20O10-sn2";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = chain1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra08int = 400;
                var fra08comment = "[SN1-H]-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = chain2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra09int = 400;
                var fra09comment = "[SN2-H]-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);
            }
        }

        public static void Ac2PIM2Fragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "Ac2PIM2", new List<string>() { "[M-H]-" } },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.C6H10O5;
                var fra02int = 50;
                var fra02comment = "[M-H]-C6H10O5";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - chain1Mass - MassDictionary.OxygenMass;
                var fra03int = 999;
                var fra03comment = "[M-H]-sn1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass - MassDictionary.OxygenMass;
                var fra04int = 999;
                var fra04comment = "[M-H]-sn2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass - chain2Mass - MassDictionary.OxygenMass;
                var fra05int = 200;
                var fra05comment = "[M-H]-sn1-sn2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra08mass = chain1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra08int = 400;
                var fra08comment = "[SN1-H]-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = chain2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra09int = 400;
                var fra09comment = "[SN2-H]-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);
            }
        }

        public static void Ac3PIM2Fragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double)
        {
            //    { "Ac3PIM2", new List<string>() { "[M-H]-" } },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = acylChainMass(chain3Carbon, chain3Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.C6H10O5;
                var fra02int = 50;
                var fra02comment = "[M-H]-C6H10O5";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - chain1Mass - MassDictionary.OxygenMass;
                var fra03int = 999;
                var fra03comment = "[M-H]-sn1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass - MassDictionary.OxygenMass;
                var fra04int = 500;
                var fra04comment = "[M-H]-sn2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra01mass - chain3Mass - MassDictionary.OxygenMass;
                var fra05int = 500;
                var fra05comment = "[M-H]-sn3";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra03mass + MassDictionary.H2OMass;
                var fra06int = 100;
                var fra06comment = "[M-H]-sn1+H20";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra04mass + MassDictionary.H2OMass;
                var fra07int = 100;
                var fra07comment = "[M-H]-sn2+H20";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra05mass + MassDictionary.H2OMass;
                var fra08int = 100;
                var fra08comment = "[M-H]-sn3+H20";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra03mass - chain2Mass - MassDictionary.OxygenMass;
                var fra09int = 200;
                var fra09comment = "M-sn1-sn2";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = fra09mass - 12 * 3 - MassDictionary.HydrogenMass * 5 - MassDictionary.OxygenMass;
                var fra10int = 400;
                var fra10comment = "M-sn1-sn2-C3H5O";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = chain1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra11int = 400;
                var fra11comment = "[SN1-H]-";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra12mass = chain2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra12int = 400;
                var fra12comment = "[SN2-H]-";
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                var fra13mass = chain3Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra13int = 400;
                var fra13comment = "[SN3-H]-";
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);
            }
        }

        public static void Ac4PIM2Fragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double, int chain3Carbon, int chain3Double, int chain4Carbon, int chain4Double)
        {
            //    { "Ac4PIM2", new List<string>() { "[M-H]-" } },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            var chain3Mass = acylChainMass(chain3Carbon, chain3Double, 0);
            var chain4Mass = acylChainMass(chain4Carbon, chain4Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = fra01mass - chain1Mass - MassDictionary.OxygenMass;
                var fra03int = 999;
                var fra03comment = "[M-H]-sn1";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - chain2Mass - MassDictionary.OxygenMass;
                var fra04int = 500;
                var fra04comment = "[M-H]-sn2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra01mass - chain3Mass - MassDictionary.OxygenMass;
                var fra05int = 500;
                var fra05comment = "[M-H]-sn3";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra02mass = fra01mass - chain4Mass - MassDictionary.OxygenMass;
                var fra02int = 500;
                var fra02comment = "[M-H]-sn4";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra06mass = fra03mass - chain2Mass - MassDictionary.OxygenMass;
                var fra06int = 100;
                var fra06comment = "[M-H]-sn1-sn2";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - 12 * 3 - MassDictionary.HydrogenMass * 4 - MassDictionary.OxygenMass;
                var fra07int = 300;
                var fra07comment = "[M-H]-sn1-sn2-C3H5O";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra05mass - chain4Mass - MassDictionary.OxygenMass;
                var fra08int = 100;
                var fra08comment = "[M-H]-sn3-sn4";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra08mass - 12 * 3 - MassDictionary.HydrogenMass * 4 - MassDictionary.OxygenMass;
                var fra09int = 300;
                var fra09comment = "[M-H]-sn3-sn4-C3H5O";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra11mass = chain1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra11int = 400;
                var fra11comment = "[SN1-H]-";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra12mass = chain2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra12int = 400;
                var fra12comment = "[SN2-H]-";
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);
            }
        }
        public static void GpnaeFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ {   "GPNAE" ,    new List<string>(){ "[M-H]-", "[M+H]+"}    }, },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - 12 * 3 - MassDictionary.HydrogenMass * 6 - MassDictionary.OxygenMass * 2;
                var fra02int = 10;
                var fra02comment = "[M-H-C3H6O2]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 3 + MassDictionary.HydrogenMass * 8 + MassDictionary.OxygenMass * 6 + MassDictionary.PhosphorusMass + MassDictionary.Electron; ;
                var fra03int = 600;
                var fra03comment = "C3H8PO6-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass + MassDictionary.Electron; ;
                var fra04int = 500;
                var fra04comment = "C3H6PO5-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron; ;
                var fra05int = 999;
                var fra05comment = "PO3-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = chain1Mass + 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "SN1+C2H6N+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

            }

        }


        private static double acylChainMass(int chainCarbon, int chainDouble, int chainOx)
        {
            return chainCarbon * 12 + (2 * chainCarbon - 2 * chainDouble - 2) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass + (MassDictionary.OxygenMass * chainOx);
        }

        private static double etherChainMass(int chainCarbon, int chainDouble, int chainOx)
        {
            return chainCarbon * 12 + (2 * chainCarbon - 2 * chainDouble) * MassDictionary.HydrogenMass + (MassDictionary.OxygenMass * chainOx);
        }


        ////template
        //public static void XXFragment(List<string> fragmentList, string adduct, double exactMass,int chain1Carbon , int chain1Double, int chain2Carbon, int chain2Double)
        //{
        //    //{ "**" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+H]+", "[M+CH3COO]-" }    },
        //    if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
        //    {
        //        var fra01mass = 0.0;
        //        var fra01int = 0;
        //        var fra01comment = "";
        //        if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
        //        {
        //            fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
        //            fra01int = 10;
        //            fra01comment = adduct;
        //            fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
        //        }
        //        var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
        //        var fra02int = 999;
        //        var fra02comment = "[M-H]-";
        //        fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
        //    }
        //    else if (adduct == "[M+H]+")
        //    {
        //        var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
        //        var fra01int = 50;
        //        var fra01comment = adduct;
        //        fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
        //    }

        //}
        ////template end

    }
}
