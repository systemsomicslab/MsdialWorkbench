using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MspGenerator
{
    public class OtherLipidFragmentation
    {
        ////template
        //public static void XXFragment(List<string> fragmentList, string adduct, double exactMass, double chain1Mass, double chain2Mass)
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

        public static void cholesterylEsterFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain1Ox)
        {
            //{ "CE" ,    new List<string>() { "[M+Na]+", "[M+NH4]+", }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, chain1Ox);
            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 50;
                //var fra02comment = adduct;
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "NL of H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain1Mass - MassDictionary.OxygenMass;
                var fra04int = 999;
                var fra04comment = "Sterol fragment";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
            if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

            }

        }

        public static void cholicAcidEsterFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain1Ox, string lipidClass)
        {
            //{ "DCAE",  new List<string>() { "[M-H]-", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, chain1Ox);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass - MassDictionary.OxygenMass;
                var fra02int = chain1Ox > 0 ? 100 : 50;
                var fra02comment = "[M-Acyl-H2O-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra03int = 50;
                var fra03comment = "FA-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                if (chain1Ox > 0)
                {
                    var fra04mass = fra03mass - MassDictionary.H2OMass;
                    var fra04int = 50;
                    var fra04comment = "FA-H2O-";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                }
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 50;
                //var fra02comment = adduct;
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 0.0;
                if (lipidClass == "DCAE" || lipidClass == "GDCAE" || lipidClass == "TDCAE" || lipidClass == "KDCAE")
                {
                    fra03mass = fra02mass - MassDictionary.H2OMass - MassDictionary.OxygenMass - chain1Mass;
                }
                else if (lipidClass == "GLCAE" || lipidClass == "TLCAE" || lipidClass == "LCAE" || lipidClass == "KLCAE")
                {
                    fra03mass = fra02mass - MassDictionary.OxygenMass - chain1Mass;
                }

                var fra03int = 999;
                var fra03comment = "[steroid fragment]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
        }

        public static void steroidalEsterFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain1Ox)
        {
            //{ "BRSE" ,    new List<string>() { "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, chain1Ox);

            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 50;
                //var fra02comment = adduct;
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                //var fra03mass = fra01mass - MassDictionary.H2OMass;
                //var fra03int = 50;
                //var fra03comment = "NL of H2O";
                //fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain1Mass - MassDictionary.OxygenMass;
                var fra04int = 999;
                var fra04comment = "[sterol base structure]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void ergosterolEsterFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain1Ox)
        {
            // {"Ergosterol",  new List<string>(){ "[M+H]+", "[M+NH4]+","[M+NH4]+" } },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, chain1Ox);

            if (adduct == "[M+H]+" || adduct == "[M+NH4]+" || adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct == "[M+NH4]+" ? 5 : 200;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);


                var fra02mass = exactMass + MassDictionary.Proton;
                if (adduct == "[M+NH4]+")
                {
                    var fra02int = 10;
                    var fra02comment = "[M+H]+";
                    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
                }

                //var fra03mass = fra01mass - MassDictionary.H2OMass;
                //var fra03int = 50;
                //var fra03comment = "NL of H2O";
                //fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain1Mass - MassDictionary.OxygenMass;
                var fra04int = 999;
                var fra04comment = "[sterol base structure]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void desmosterolEsterFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain1Ox)
        {
            // {"Ergosterol",  new List<string>(){ "[M+H]+", "[M+NH4]+","[M+NH4]+" } },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, chain1Ox);

            if (adduct == "[M+H]+" || adduct == "[M+NH4]+" || adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct == "[M+NH4]+" ? 5 : 200;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);


                var fra02mass = exactMass + MassDictionary.Proton;
                if (adduct == "[M+NH4]+")
                {
                    var fra02int = 10;
                    var fra02comment = "[M+H]+";
                    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
                }

                //var fra03mass = fra01mass - MassDictionary.H2OMass;
                //var fra03int = 50;
                //var fra03comment = "NL of H2O";
                //fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - chain1Mass - MassDictionary.OxygenMass;
                var fra04int = 999;
                var fra04comment = "[sterol base structure]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }


        public static void aHexSteroidalEsterFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain1Ox)
        {
            //{ "AHexBRS" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, chain1Ox);

            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 999;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                //var fra02int = 999;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra03int = 500;
                var fra03comment = "FA-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 999;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain1Mass - MassDictionary.OxygenMass - MassDictionary.C6H10O5;
                var fra03int = 999;
                var fra03comment = "Sterol Fragment";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }

        }


        public static void carFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //                {   "ACar" ,    new List<string>(){ "[M]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 4 + MassDictionary.HydrogenMass * 5 + MassDictionary.OxygenMass * 2 - MassDictionary.Electron;
                var fra02int = 800;
                var fra02comment = "[C4H5O2]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

            }

        }

        public static void vitAEsterFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{   "VAE" ,    new List<string>(){ "[M+H]+", "[M+Na]+"  , }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 999;
                //var fra01comment = "[M+H]+";
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain1Mass - MassDictionary.OxygenMass;
                var fra02int = 999;
                var fra02comment = "SN1 loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - (12 * 2 + MassDictionary.HydrogenMass * 6);
                var fra03int = 400;
                var fra03comment = "NL of FA and 2*CH3 loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - (12 * 3 + MassDictionary.HydrogenMass * 4);
                var fra04int = 600;
                var fra04comment = "C3H4 loss (SN1 and C5H10 loss)";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - (12 * 1 + MassDictionary.HydrogenMass * 3);
                var fra05int = 600;
                var fra05comment = "CH3 loss (SN1 and C6H13 loss)";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - (12 * 1 + MassDictionary.HydrogenMass * 3);
                var fra06int = 999;
                var fra06comment = "CH3 loss (SN1 and C7H16 loss)";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = (12 * 9 + MassDictionary.HydrogenMass * 10) + MassDictionary.Proton;
                var fra07int = 400;
                var fra07comment = "[C9H10+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = (12 * 6 + MassDictionary.HydrogenMass * 6) + MassDictionary.Proton;
                var fra08int = 200;
                var fra08comment = "[C6H6+H]+";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }

            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 999;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra09mass = exactMass + MassDictionary.Proton;

                var fra02mass = fra09mass - chain1Mass - MassDictionary.OxygenMass;
                var fra02int = 999;
                var fra02comment = "SN1 loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - (12 * 4 + MassDictionary.HydrogenMass * 8);
                var fra03int = 200;
                var fra03comment = "NL of FA and C4H8 loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - (12 * 1 + MassDictionary.HydrogenMass * 2);
                var fra04int = 200;
                var fra04comment = "CH2 loss (SN1 and C5H10 loss)";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - (12 * 2 + MassDictionary.HydrogenMass * 2);
                var fra05int = 300;
                var fra05comment = "C2H2 loss";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = (12 * 9 + MassDictionary.HydrogenMass * 10) + MassDictionary.Proton;
                var fra06int = 300;
                var fra06comment = "[C9H10+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = (12 * 8 + MassDictionary.HydrogenMass * 10) + MassDictionary.Proton;
                var fra07int = 200;
                var fra07comment = "[C8H10+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = (12 * 6 + MassDictionary.HydrogenMass * 8) + MassDictionary.Proton;
                var fra08int = 100;
                var fra08comment = "[C6H8+H]+";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

            }

        }

        public static void anandamideFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{   "NAE" ,    new List<string>(){ "[M+H]+" , "[M+HCOO]-", "[M+CH3COO]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

            }
            else if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 900;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - (MassDictionary.HydrogenMass * 2);
                var fra03int = 999;
                var fra03comment = "[M-H]- - 2H";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 + MassDictionary.Electron;
                var fra04int = 50;
                var fra04comment = "C3H4NO2-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }

        }


        public static void alphaOxFAFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "OxFA" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                var fra03mass = fra01mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "[NL of H2O]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
                var fra04mass = fra01mass - 2 * MassDictionary.HydrogenMass - (12 + MassDictionary.OxygenMass * 2);
                var fra04int = 999;
                var fra04comment = "[NL of CO2]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void aahfaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{ "AAHFA" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                var fra02mass = fra01mass - chain2Mass + MassDictionary.HydrogenMass * 2;
                var fra02int = 999;
                var fra02comment = "[NL of FA(acyl)]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 800;
                var fra03comment = "[NL of FA]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
                var fra04mass = fra02mass - 2 * MassDictionary.HydrogenMass - (12 + MassDictionary.OxygenMass * 2);
                var fra04int = 500;
                var fra04comment = "[HFA-CO2]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
                var fra05mass = chain2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra05int = 500;
                var fra05comment = "[FA-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
        }


        public static void fahfaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{ "FAHFA" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                var fra02mass = chain1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra02int = 150;
                var fra02comment = "[HFA-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "[HFA-H-H2O]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
                var fra04mass = chain2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra04int = 999;
                var fra04comment = "[FA-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }

        }

        public static void fahfaOrnFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "FAHFAmide-Orn" ,    new List<string>() { "[M+H]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 300;
                var fra01comment = "[M+H]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain2Mass - MassDictionary.OxygenMass;
                var fra02int = 300;
                var fra02comment = "M-sn2-H2O";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 500;
                var fra03comment = "M-sn2-2H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain1Mass - MassDictionary.HydrogenMass * 2 - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra04int = 100;
                var fra04comment = "[HFA-OH-H2O+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 5 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                var fra05int = 50;
                var fra05comment = "[Orn]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 999;
                var fra06comment = "[C5H10N2O +H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = 12 * 4 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra07int = 500;
                var fra07comment = "[C4H7N +H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }
        public static void fahfaGlyFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "FAHFAmide-Gly" ,    new List<string>() { "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - chain2Mass - MassDictionary.OxygenMass;
                var fra02int = 999;
                var fra02comment = "M-SN2-H2O";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 12 - MassDictionary.OxygenMass * 2;
                var fra03int = 150;
                var fra03comment = "M-SN2-H2O-CO2";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = chain2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra04int = 150;
                var fra04comment = "SN2";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 - MassDictionary.Proton;
                var fra05int = 150;
                var fra05comment = "gly";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
            else if (adduct == "[M+H]+" || adduct == "[M+NH4]+")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+NH4]+")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 300;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = adduct == "[M+H]+" ? 1 : 50;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain2Mass - MassDictionary.OxygenMass;
                var fra03int = 999;
                var fra03comment = "M-sn2-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra06mass = 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                var fra06int = 750;
                var fra06comment = "gly";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra04mass = fra03mass - fra06mass + MassDictionary.Proton;
                var fra04int = 500;
                var fra04comment = "M-sn2-H2O-gly";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 500;
                var fra05comment = "M-sn2-2H2O-gly";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }
        }

        public static void fahfaGlySerFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //    { "FAHFAmide-Gly-Ser" ,    new List<string>() { "[M-H]-", "[M+NH4]+", }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 700;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 5;
                var fra02comment = "M-H2O";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - chain2Mass + MassDictionary.HydrogenMass * 2;
                var fra03int = 400;
                var fra03comment = "M-SN2-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - 12 - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "M-SN2-H2O-CH2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.OxygenMass * 2;
                var fra05int = 400;
                var fra05comment = "M-SN2-2H2O-CO2";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = chain2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra06int = 400;
                var fra06comment = "SN2";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+NH4]+")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+NH4]+")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 200;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = adduct == "[M+H]+" ? 1 : 50;
                //var fra02comment = "[M+H]+";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 200;
                var fra03comment = "M-H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - chain2Mass + MassDictionary.HydrogenMass * 2;
                var fra04int = 500;
                var fra04comment = "M-sn2-H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 500;
                var fra05comment = "M-sn2-2H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra09mass = 12 * 3 + MassDictionary.OxygenMass * 3 + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 7 + MassDictionary.Proton;
                var fra09int = 200;
                var fra09comment = "ser";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = 12 * 5 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 3 + MassDictionary.Proton;
                var fra10int = 300;
                var fra10comment = "gly-ser-H2O";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra06mass = fra04mass - fra09mass + MassDictionary.Proton;
                var fra06int = 999;
                var fra06comment = "M-Sn2-H2O-ser";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - 12 - MassDictionary.OxygenMass;
                var fra07int = 400;
                var fra07comment = "M-Sn2-H2O-ser-CO";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra04mass - fra10mass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra08int = 800;
                var fra08comment = "M-Sn2-H2O-ser-gly";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

            }
        }

        public static void oxFAGlySerFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //    { "FAHFAmide-Gly-Ser" ,    new List<string>() { "[M-H]-", "[M+NH4]+", }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 6 + MassDictionary.HydrogenMass * 10 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 4 - MassDictionary.Proton;
                var fra02int = 400;
                var fra02comment = "C6H10N2O4-H- 173.057 GlySer+C-H";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 6 + MassDictionary.HydrogenMass * 10 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 2 - MassDictionary.Proton;
                var fra03int = 400;
                var fra03comment = "C6H10N2O2-H- 141.066 GlySer+C-2O-H";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 4 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra04int = 400;
                var fra04comment = "C4H8N2O1-H- 99.056 GlySer-H2O-CO2-H";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 3 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra05int = 400;
                var fra05comment = "C3H8N2O1-H- 87.056  GlySer-H2O-CO2-C-H";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 - MassDictionary.Proton;
                var fra06int = 400;
                var fra06comment = "C2H5NO2-H- 74.024 Gly";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }
            else if (adduct == "[M+H]+")
            {
                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 1;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra09mass = 12 * 3 + MassDictionary.OxygenMass * 3 + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 7 + MassDictionary.Proton;
                var fra09int = 700;
                var fra09comment = "ser";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = 12 * 5 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 3 + MassDictionary.Proton;
                var fra10int = 999;
                var fra10comment = "gly-ser-O";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

            }
        }

        public static void oxFAGlyFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //    { "FAHFAmide-Gly" ,    new List<string>() { "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 - MassDictionary.Proton;
                var fra05int = 999;
                var fra05comment = "gly";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 1;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra06mass = 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                var fra06int = 999;
                var fra06comment = "gly";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }

        public static void oxFAOrnFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //    { "FAHFAmide-Orn" ,    new List<string>() { "[M+H]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);

            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = "[M+H]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra04mass = fra01mass - MassDictionary.H2OMass * 2;
                var fra04int = 100;
                var fra04comment = "[M+H-2H2O+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 5 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                //var fra05int = 50;
                //var fra05comment = "[Orn]+";
                //fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 500;
                var fra06comment = "[C5H10N2O +H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = 12 * 4 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra07int = 999;
                var fra07comment = "[C4H7N +H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }

        //add 20220318
        public static void FATauFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{   "NATau_FA" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.SulfurMass + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra02int = 50;
                var fra02comment = "Tau";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 3;
                var fra03int = 50;
                var fra03comment = "Tau-NH3";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = MassDictionary.SulfurMass + MassDictionary.OxygenMass * 3 + MassDictionary.Electron;
                var fra04int = 200;
                var fra04comment = "[SO3]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
            else if (adduct == "[M+H]+" || adduct == "[M+NH4]+")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+NH4]+")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 1;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 1;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.SulfurMass + MassDictionary.OxygenMass * 3 + MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "Tau";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - fra03mass + MassDictionary.Proton;
                var fra04int = 50;
                var fra04comment = "[M-Tau+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void oxFATauFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{   "NATau_OxFA" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.SulfurMass + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "Tau";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 3;
                var fra03int = 50;
                var fra03comment = "Tau-NH3";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = MassDictionary.SulfurMass + MassDictionary.OxygenMass * 3 + MassDictionary.Electron;
                var fra04int = 200;
                var fra04comment = "[SO3]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
            else if (adduct == "[M+H]+" || adduct == "[M+NH4]+")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+NH4]+")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 1;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 1;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.SulfurMass + MassDictionary.OxygenMass * 3 + MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "Tau";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - fra03mass + MassDictionary.Proton;
                var fra04int = 50;
                var fra04comment = "[M-Tau+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }
        public static void FAPheFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{   "NAPhe_FA" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },

            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 9 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 - MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "Phe"; // 164
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 3;
                var fra03int = 500;
                var fra03comment = "Phe-NH3";  //147
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - 12 - MassDictionary.OxygenMass * 2;
                var fra04int = 200;
                var fra04comment = "Phe-NH3-CO2";  //103
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 7 + MassDictionary.HydrogenMass * 8 - MassDictionary.Proton;
                var fra05int = 150;
                var fra05comment = "benzene + CH2";  //91
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra01mass - fra03mass - MassDictionary.Proton;
                var fra06int = 50;
                var fra06comment = "[M-Phe+N-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 9 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                var fra02int = 200;
                var fra02comment = "Phe"; // 166
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 12 - MassDictionary.OxygenMass * 2 - MassDictionary.HydrogenMass * 2;
                var fra03int = 999;
                var fra03comment = "Phe-H2CO2";  //120
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
        }
        public static void oxFAPheFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{   "NAPhe_OxFA" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 1);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 9 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 - MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "Phe"; // 164
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 3;
                var fra03int = 500;
                var fra03comment = "Phe-NH3";  //147
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - 12 - MassDictionary.OxygenMass * 2;
                var fra04int = 200;
                var fra04comment = "Phe-NH3-CO2";  //103
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 7 + MassDictionary.HydrogenMass * 8 - MassDictionary.Proton;
                var fra05int = 150;
                var fra05comment = "benzene + CH2";  //91
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra01mass - MassDictionary.H2OMass - fra03mass - MassDictionary.Proton;
                var fra06int = 50;
                var fra06comment = "[M-H2O-Phe+N-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra01mass - MassDictionary.H2OMass;
                var fra07int = 500;
                var fra07comment = "[M-H2O-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = "[M+H]+ Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 9 + MassDictionary.HydrogenMass * 11 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                var fra02int = 200;
                var fra02comment = "Phe"; // 166
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 12 - MassDictionary.OxygenMass * 2 - MassDictionary.HydrogenMass * 2;
                var fra03int = 999;
                var fra03comment = "Phe-H2CO2";  //120
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
        }
        public static void FAGlyFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{   "NAGly_FA" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 - MassDictionary.Proton;
                var fra05int = 999;
                var fra05comment = "gly";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
            else if (adduct == "[M+H]+" || adduct == "[M+NH4]+")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+NH4]+")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 1;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 1;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra06mass = 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                var fra06int = 999;
                var fra06comment = "gly";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }
        public static void FAOrnFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{   "NAOrn_FA" ,    new List<string>(){ "[M+H]+"  }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = "[M+H]+";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                //var fra04mass = fra01mass - MassDictionary.H2OMass * 2;
                //var fra04int = 100;
                //var fra04comment = "[M+H-2H2O+H]+";
                //fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 5 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                //var fra05int = 50;
                //var fra05comment = "[Orn]+";
                //fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 800;
                var fra06comment = "[C5H10N2O +H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = 12 * 4 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.Proton;
                var fra07int = 999;
                var fra07comment = "[C4H7N +H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }


        public static void steroidWithLpaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "CSLPHex" ,    new List<string>() { "[M-H]-", "[M+NH4]+", }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = chain1Mass - MassDictionary.Proton + (12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass);
                var fra02int = 50;
                var fra02comment = "[FA+G3P]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra03int = 150;
                var fra03comment = "[FA-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 50;
                //var fra02comment = "[FA+G3P]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + MassDictionary.Proton + (3 * 12 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 2);
                var fra03int = 999;
                var fra03comment = "[MAG-H2O]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass + MassDictionary.C6H10O5 + MassDictionary.PhosphorusMass + MassDictionary.OxygenMass * 3 + MassDictionary.HydrogenMass;
                var fra04int = 300;
                var fra04comment = "[M-Cho-H2O+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void steroidWithPaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double, int chain2Carbon, int chain2Double)
        {
            //{   "CSPHex" ,    new List<string>(){ "[M-H]-", "[M+NH4]+"  , }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            var chain2Mass = acylChainMass(chain2Carbon, chain2Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = chain1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra02int = 50;
                var fra02comment = "[FA1-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra03int = 50;
                var fra03comment = "[FA2-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 20;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                //var fra02int = 50;
                //var fra02comment = "[FA+G3P]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = chain1Mass + chain2Mass + MassDictionary.Proton + (3 * 12 + MassDictionary.HydrogenMass * 2 + MassDictionary.OxygenMass * 2);
                var fra03int = 999;
                var fra03comment = "[DAG-OH]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass + MassDictionary.C6H10O5 + MassDictionary.PhosphorusMass + MassDictionary.OxygenMass * 4 + MassDictionary.HydrogenMass * 3;
                var fra04int = 50;
                var fra04comment = "[M-Cho+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - fra04mass + MassDictionary.Proton;
                var fra05int = 20;
                var fra05comment = "Sterol Fragment";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
        }
        public static void baHexFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            //    { "BAHex" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-", "[M+NH4]+", }    },
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 800;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = MassDictionary.C6H10O5 + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra03int = 800;
                var fra03comment = "[Hexose-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass - MassDictionary.C6H10O5 - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "Sterol Fragment";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

            }

        }
        public static void steroidHexFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            //    { "SHex" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-","[M+NH4]+" }    },
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 800;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = MassDictionary.C6H10O5 + MassDictionary.H2OMass - MassDictionary.Proton;
                var fra03int = 800;
                var fra03comment = "[Hexose-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass - MassDictionary.C6H10O5 - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "Sterol Fragment";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 10 + MassDictionary.HydrogenMass * 15 - MassDictionary.Electron;
                var fra03int = 50;
                var fra03comment = "C10H15";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 11 + MassDictionary.HydrogenMass * 15 - MassDictionary.Electron;
                var fra04int = 50;
                var fra04comment = "C11H15";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 12 + MassDictionary.HydrogenMass * 17 - MassDictionary.Electron;
                var fra05int = 50;
                var fra05comment = "C12H17";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 13 + MassDictionary.HydrogenMass * 19 - MassDictionary.Electron;
                var fra06int = 50;
                var fra06comment = "C13H19";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }
        public static void baSulfateFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            //    { "BASulfate" ,    new List<string>() { "[M-H]-", "[M+NH4]+", }    },
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]-";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = MassDictionary.HydrogenMass + MassDictionary.SulfurMass + MassDictionary.OxygenMass * 4 + MassDictionary.Electron;
                var fra02int = 100;
                var fra02comment = "HSO4-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass - MassDictionary.SulfurMass - MassDictionary.OxygenMass * 4 - MassDictionary.HydrogenMass - MassDictionary.Electron;
                var fra02int = 999;
                var fra02comment = "Sterol Fragment";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
        }

        public static void steroidSulfateFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            //    { "SSulfate" ,    new List<string>() { "[M-H]-", "[M+NH4]+", }    },
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]-";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = MassDictionary.HydrogenMass + MassDictionary.SulfurMass + MassDictionary.OxygenMass * 4 + MassDictionary.Electron;
                var fra02int = 100;
                var fra02comment = "HSO4-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass - MassDictionary.SulfurMass - MassDictionary.OxygenMass * 4 - MassDictionary.HydrogenMass - MassDictionary.Electron;
                var fra02int = 999;
                var fra02comment = "Sterol Fragment";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
        }

        public static void steroidPEFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            //    { "SPE" ,    new List<string>() { "[M-H]-", "[M+H]+", }    },
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 700;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = MassDictionary.HydrogenMass + MassDictionary.PhosphorusMass + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra02int = 400;
                var fra02comment = "[HO3P-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass + 12 * 2 + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass;
                var fra03int = 999;
                var fra03comment = "[EtAmP]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "[C2H8NO4P+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - fra02mass + MassDictionary.Proton;
                var fra03int = 600;
                var fra03comment = "Sterol Fragment";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }

        }
        public static void steroidPEHexFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            //    { "SPEHex" ,    new List<string>() { "[M+NH4]+" }    },
            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra03int = 700;
                var fra03comment = "[C2H8NO4P+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass + MassDictionary.C6H10O5 - MassDictionary.H2OMass;
                var fra04int = 950;
                var fra04comment = "[EtAmP+Hex-H2O+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass + MassDictionary.H2OMass;
                var fra05int = 750;
                var fra05comment = "[EtAmP+Hex-H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra02mass = exactMass - fra05mass + MassDictionary.HydrogenMass + MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "Sterol Fragment";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }

        }
        public static void steroidPGHexFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            //    { "SPGHex" ,    new List<string>() { "[M-H]-", "[M+NH4]+", }    },
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]-";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 3 + MassDictionary.HydrogenMass * 7 + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra02int = 300;
                var fra02comment = "[G3P-H2O-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass + MassDictionary.C6H10O5;
                var fra03int = 50;
                var fra03comment = "[G3P+Hex-H2O-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 3 + MassDictionary.HydrogenMass * 9 + MassDictionary.OxygenMass * 6 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "[G3P+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass + MassDictionary.C6H10O5 - MassDictionary.H2OMass;
                var fra03int = 650;
                var fra03comment = "[G3P+Hex-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = exactMass - fra03mass - MassDictionary.OxygenMass - MassDictionary.Electron;
                var fra04int = 600;
                var fra04comment = "Sterol Fragment";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void vitaminEFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            //{   "VitaminE" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-" , }    },
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
            {
                var fra01mass = 0.0;
                //var fra01int = 0;
                //var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    //fra01int = 1;
                    //fra01comment = adduct;
                    //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = adduct == "[M-H]-" ? 200 : 800;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 10 + MassDictionary.HydrogenMass * 12 + MassDictionary.OxygenMass * 2 - MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[C10H11O2]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                if (adduct == "[M-H]-")
                {
                    var fra04mass = fra02mass - 12 - MassDictionary.HydrogenMass * 3;
                    var fra04int = 100;
                    var fra04comment = "M-CH3";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    var fra05mass = 12 * 9 + MassDictionary.HydrogenMass * 12 + MassDictionary.OxygenMass - MassDictionary.Proton;
                    var fra05int = 100;
                    var fra05comment = "[C9H11O]-";
                    fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
                }
            }
        }


        public static void vitaminDFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            //{ "Vitamin_D" ,    new List<string>() { "[M+H]+" }    },
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 999;
                var fra02comment = "[M-H2O+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "[M-2H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
        }

        public static void coenzymeQFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            // {   "CoQ" ,    new List<string>(){ "[M+H]+","[M+Na]+", "[M+NH4]+" }    },
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 10 + MassDictionary.HydrogenMass * 12 + MassDictionary.OxygenMass * 4 + MassDictionary.Proton;
                var fra02int = 750;
                var fra02comment = "[(C9H9O4)+CH3+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = 12 * 10 + MassDictionary.HydrogenMass * 12 + MassDictionary.OxygenMass * 4 + MassDictionary.Proton;
                var fra02int = 5;
                var fra02comment = "[(C9H9O4)+CH3+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = exactMass + MassDictionary.Proton;
                var fra03int = 10;
                var fra03comment = "[M+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra02mass = 12 * 10 + MassDictionary.HydrogenMass * 12 + MassDictionary.OxygenMass * 4 + MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "[(C9H9O4)+CH3+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }

        }

        public static void lipidAFragmantation(List<string> fragmentList, string adduct, double exactMass,
                               int hfa01Carbon, int hfa01Double, int hfa02Carbon, int hfa02Double, int fahfaHfa01Carbon, int fahfaHfa01Double,
                               int fahfaHfa02Carbon, int fahfaHfa02Double, int fahfaFa01Carbon, int fahfaFa01Double, int fahfaFa02Carbon, int fahfaFa02Double)
        {
            var hfa01Mass = acylChainMass(hfa01Carbon, hfa01Double, 1);
            var hfa02Mass = acylChainMass(hfa02Carbon, hfa02Double, 1);
            var fahfaHfa01Mass = acylChainMass(fahfaHfa01Carbon, fahfaHfa01Double, 1);
            var fahfaHfa02Mass = acylChainMass(fahfaHfa02Carbon, fahfaHfa02Double, 1);
            var fahfaFa01Mass = acylChainMass(fahfaFa01Carbon, fahfaFa01Double, 0);
            var fahfaFa02Mass = acylChainMass(fahfaFa02Carbon, fahfaFa02Double, 0);

            //     {"LipidA", new List<string>(){ "[M-H]-" } },
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 500;
                var fra01comment = "[M-H]-";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - (MassDictionary.HydrogenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.OxygenMass * 3);
                var fra02int = 50;
                var fra02comment = "[M-H]-PO3H-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.OxygenMass;
                var fra03int = 600;
                var fra03comment = "[M-H]-PO4H3";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - fahfaFa01Mass - MassDictionary.OxygenMass;
                var fra04int = 250;
                var fra04comment = "[M-H]-R2'-O-FA";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra06mass = fra01mass - fahfaFa02Mass - MassDictionary.OxygenMass;
                var fra06int = 250;
                var fra06comment = "[M-H]-R3'-O-FA";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra01mass - hfa01Mass - MassDictionary.OxygenMass;
                var fra07int = 999;
                var fra07comment = "[M-H]-R2 acyl FA";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra05mass = fra01mass - hfa02Mass - MassDictionary.OxygenMass;
                var fra05int = 999;
                var fra05comment = "[M-H]-R3 acyl FA";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra11mass = fra03mass - hfa01Mass - MassDictionary.OxygenMass;
                var fra11int = 250;
                var fra11comment = "[M-H]-R2-PO4H3";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra09mass = fra03mass - hfa02Mass - MassDictionary.OxygenMass;
                var fra09int = 250;
                var fra09comment = "[M-H]-R3-PO4H3";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra08mass = fra03mass - fahfaFa01Mass - MassDictionary.OxygenMass;
                var fra08int = 300;
                var fra08comment = "[M-H]-PO4H3-R2'-O-FA";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra10mass = fra03mass - fahfaFa02Mass - MassDictionary.OxygenMass;
                var fra10int = 300;
                var fra10comment = "[M-H]-PO4H3-R3'-O-FA";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);
            }
        }

        public static void sterolsFragment(List<string> fragmentList, string adduct, double exactMass)
        {
            // {"ST",  new List<string>(){ "[M+H]+", "[M+NH4]+", "[M+Na]+", "[M+H-H2O]+" } },

            if (adduct == "[M+H]+" || adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra04mass = fra01mass - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "[sterol base structure - H2O]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }

            if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + MassDictionary.Proton;
                var fra02int = 999;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra04mass = fra02mass - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "[sterol base structure - H2O]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }

            if (adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra07mass = 12 * 13 + MassDictionary.HydrogenMass * 19;
                var fra07int = 200;
                var fra07comment = "C13H19";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 12 + MassDictionary.HydrogenMass * 17;
                var fra08int = 200;
                var fra08comment = "C12H17";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = 12 * 11 + MassDictionary.HydrogenMass * 15;
                var fra09int = 200;
                var fra09comment = "C11H15";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = 12 * 10 + MassDictionary.HydrogenMass * 15;
                var fra10int = 200;
                var fra10comment = "C10H15";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);
            }
        }

        public static void fahfaDmedFragment(List<string> fragmentList, string adduct, double exactMass, int baseCarbon, int baseDouble, int extraCarbon, int extraDouble, int baseOxPosition)
        {
            var baseMass = acylChainMass(baseCarbon, baseDouble, 1);
            var extraChainMass = acylChainMass(extraCarbon, extraDouble, 0);
            var dmesMass = 12 * 4 + MassDictionary.NitrogenMass * 2 + MassDictionary.HydrogenMass * 10;
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
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
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = baseMass + dmesMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra02int = 500;
                var fra02comment = "extra acyl chain loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 12 * 2 - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 7;
                var fra03int = 999;
                var fra03comment = "extra chain and C2NH7 loss"; ;
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - 12 * 2 - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 7;
                var fra04int = 50;
                var fra04comment = "C2NH7 loss"; ;
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void fahfahfaDmedFragment(List<string> fragmentList, string adduct, double exactMass, int baseCarbon, int baseDouble, int extraCarbon, int extraDouble, int baseOxPosition)
        {
            var baseMass = acylChainMass(baseCarbon, baseDouble, 1);
            var extraChainMass = acylChainMass(extraCarbon, extraDouble, 0);
            var dmesMass = 12 * 4 + MassDictionary.NitrogenMass * 2 + MassDictionary.HydrogenMass * 10;
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
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
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = baseMass + dmesMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra02int = 500;
                var fra02comment = "base HFA chain -H2O";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 12 * 2 - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 7;
                var fra03int = 500;
                var fra03comment = "base HFA chain -H2O and C2NH7 loss"; ;
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - 12 * 2 - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 7;
                var fra04int = 500;
                var fra04comment = "C2NH7 loss"; ;
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra01mass - extraChainMass - MassDictionary.OxygenMass;
                var fra05int = 100;
                var fra05comment = "extra acyl chain loss";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
        }

        public static void fahfahfaFragment(List<string> fragmentList, string adduct, double exactMass, int baseCarbon, int baseDouble, int extraCarbon, int extraDouble, int baseOxPosition)
        {
            var baseMass = acylChainMass(baseCarbon, baseDouble, 1);
            var extraChainMass = acylChainMass(extraCarbon, extraDouble, 0);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
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
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = baseMass - MassDictionary.H2OMass - MassDictionary.Proton;
                var fra03int = 500;
                var fra03comment = "base HFA chain -H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - extraChainMass - MassDictionary.OxygenMass;
                var fra04int = 500;
                var fra04comment = "extra acyl chain loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = extraChainMass + MassDictionary.OxygenMass- MassDictionary.Proton;
                var fra05int = 500;
                var fra05comment = "extra acyl chain fragment";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);



                var fra04mass = fra01mass - 12 * 2 - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 7;
                var fra04int = 500;
                var fra04comment = "C2NH7 loss"; ;
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }
        }
        public static void FaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            //{ "FA" ,    new List<string>() { "[M-H]-" }    },
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);

            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
            }
        }
        public static void DmedFaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                var fra04mass = fra01mass - 12 * 2 - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 7;
                var fra04int = 500;
                var fra04comment = "C2NH7 loss"; ;
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        public static void DmedOxFaFragment(List<string> fragmentList, string adduct, double exactMass, int chain1Carbon, int chain1Double)
        {
            var chain1Mass = acylChainMass(chain1Carbon, chain1Double, 0);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                var fra03mass = fra01mass - 12 * 2 - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 7;
                var fra03int = 500;
                var fra03comment = "C2NH7 loss"; ;
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 500;
                var fra04comment = "C2NH7 loss - H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }

        private static double acylChainMass(int chainCarbon, int chainDouble, int chainOx)
        {
            return chainCarbon * 12 + (2 * chainCarbon - 2 * chainDouble) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass + (MassDictionary.OxygenMass * chainOx);
        }

    }
}
