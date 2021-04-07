using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MspGenerator
{
    public class CermideFragmentation
    {
        public static void CerAsFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //  { "Cer_AS" , new List<string>() { "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-" } },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
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

                var fra03mass = fra02mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra03int = 50;
                var fra03comment = "[M-O-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - 12 - MassDictionary.HydrogenMass * 4 - MassDictionary.OxygenMass;
                var fra04int = 50;
                var fra04comment = "[M-CO-3H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - 12 - MassDictionary.HydrogenMass * 4 - MassDictionary.OxygenMass * 2;
                var fra05int = 100;
                var fra05comment = "[M-CO-O-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = sphMass - MassDictionary.NitrogenMass - 12 * 2 - MassDictionary.HydrogenMass * 7 - MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra06int = 100;
                var fra06comment = "[Sph-NCCO-3H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = sphMass - MassDictionary.NitrogenMass - 12 * 2 - MassDictionary.HydrogenMass * 5 - MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra07int = 100;
                var fra07comment = "[Sph-NCCO-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = sphMass - MassDictionary.HydrogenMass * 2 - MassDictionary.Proton;
                //var fra08int = 150;
                //var fra08comment = "[Sph-3H]-";
                //fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra08mass - MassDictionary.OxygenMass;
                var fra09int = 50;
                var fra09comment = "[Sph-O-3H]-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = acylMass - MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 3;
                var fra10int = 80;
                var fra10comment = "[FA+N-H]-";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = acylMass - MassDictionary.Proton + MassDictionary.OxygenMass + MassDictionary.HydrogenMass * 2;
                var fra11int = 100;
                var fra11comment = "[FA+O-H]-";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra12mass = sphMass - MassDictionary.Proton - 12 - MassDictionary.OxygenMass - MassDictionary.HydrogenMass * 2;
                var fra12int = 100;
                var fra12comment = "[Sph-CO-H]-";
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                var fra13mass = acylMass - MassDictionary.Proton;
                var fra13int = 100;
                var fra13comment = "[FA-3H]-";
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);

                var fra14mass = fra13mass - 12 - MassDictionary.OxygenMass;
                var fra14int = 150;
                var fra14comment = "[FA-CO-3H]-";
                fragmentList.Add(fra14mass + "\t" + fra14int + "\t" + fra14comment);

            }

        }
        public static void CerAdsFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "Cer_ADS" ,new List<string>() { "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
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

                var fra03mass = fra02mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra03int = 50;
                var fra03comment = "[M-O-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - 12 - MassDictionary.HydrogenMass * 4 - MassDictionary.OxygenMass;
                var fra04int = 50;
                var fra04comment = "[M-CO-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - 12 - MassDictionary.HydrogenMass * 4 - MassDictionary.OxygenMass * 2;
                var fra05int = 50;
                var fra05comment = "[M-CO-O-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = sphMass - MassDictionary.NitrogenMass - 12 * 2 - MassDictionary.HydrogenMass * 7 - MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra06int = 100;
                var fra06comment = "[Sph-NCCO-3H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = sphMass - MassDictionary.OxygenMass - 2 * MassDictionary.HydrogenMass - MassDictionary.Proton;
                var fra07int = 50;
                var fra07comment = "[Sph-O-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = sphMass - MassDictionary.Proton;
                var fra08int = 100;
                var fra08comment = "[Sph-H]-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra08mass - 4 * MassDictionary.HydrogenMass;
                var fra09int = 100;
                var fra09comment = "[Sph+C=O-3H]-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = fra09mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra10int = 50;
                var fra10comment = "[Sph+C=O-O-3H]-";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra12mass = sphMass - MassDictionary.Proton - 12 - MassDictionary.OxygenMass - MassDictionary.HydrogenMass * 4;
                var fra12int = 100;
                var fra12comment = "[Sph-CO-H]-";
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                var fra13mass = acylMass - MassDictionary.Proton;
                var fra13int = 50;
                var fra13comment = "[FA-3H]-";
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);

                var fra14mass = fra13mass - 12 - MassDictionary.OxygenMass;
                var fra14int = 200;
                var fra14comment = "[FA-CO-3H]-";
                fragmentList.Add(fra14mass + "\t" + fra14int + "\t" + fra14comment);
            }
        }
        public static void CerApFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //   {   "Cer_AP" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
            var sphMass = sphingo3OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);

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

                var fra03mass = acylMass - MassDictionary.Proton + 12 * 3 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass + MassDictionary.HydrogenMass * 7;
                var fra03int = 100;
                var fra03comment = "[FA+NC(C)CO-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = acylMass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 400;
                var fra04comment = "[FA+O-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = acylMass - MassDictionary.Proton - 12 - MassDictionary.OxygenMass;
                var fra05int = 200;
                var fra05comment = "[FA-CO-3H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = sphMass - MassDictionary.Proton - MassDictionary.NitrogenMass - 12 * 3 - MassDictionary.HydrogenMass * 9 - MassDictionary.OxygenMass * 2;
                var fra06int = 200;
                var fra06comment = "[Sph-NC(CO)CO-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct != "[M+H]+" ? 999 : 300;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = sphMass + MassDictionary.Proton;
                var fra05int = 200;
                var fra05comment = "[Sph+H]+";

                var fra06mass = fra05mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra06int = 400;
                var fra06comment = "[Sph-H2O+H]+";

                var fra07mass = fra06mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra07int = adduct == "[M+Na]+" ? 50 : 600;
                var fra07comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
                {
                    var fra02mass = fra01mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra02int = 999;
                    var fra02comment = "[M-H2O+H]+";
                    if (adduct == "[M+H]+")
                    {
                        fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
                    }
                    else if (adduct == "[M+H-H2O]+")
                    {
                        fra02mass = fra01mass;
                    }

                    var fra03mass = fra02mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra03int = 400;
                    var fra03comment = "[M-2H2O+H]+";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    var fra04mass = fra03mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra04int = 50;
                    var fra04comment = "[M-3H2O+H]+";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
                    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                    var fra08mass = fra07mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra08int = 800;
                    var fra08comment = "[Sph-3H2O+H]+";
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
                }

            }
        }

        public static void CerNsFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            // {   "Cer_NS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+H]+", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
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
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = sphMass - MassDictionary.Proton - MassDictionary.NitrogenMass - 12 * 2 - MassDictionary.HydrogenMass * 7 - MassDictionary.OxygenMass;
                var fra03int = 300;
                var fra03comment = "[Sph-NCCO-3H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = acylMass - MassDictionary.Proton + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass + 12 * 2;
                var fra04int = 500;
                var fra04comment = "[FA+NCCO-O-3H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = acylMass - MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 3;
                var fra05int = 300;
                var fra05comment = "[FA+N-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = acylMass - MassDictionary.Proton;
                var fra06int = 300;
                var fra06comment = "[FA-3H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra04mass + MassDictionary.OxygenMass;
                var fra07int = 200;
                var fra07comment = "[FA+NCCO-3H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = sphMass - MassDictionary.Proton - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 5 - MassDictionary.OxygenMass;
                var fra08int = 300;
                var fra08comment = "[Sph-N-O-5H]-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra02mass - MassDictionary.OxygenMass - 12 - MassDictionary.HydrogenMass * 2;
                var fra09int = 300;
                var fra09comment = "[M-CO-H]-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = fra09mass - 2 * MassDictionary.HydrogenMass;
                var fra10int = 200;
                var fra10comment = "[M-CO-3H]-";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = fra10mass - MassDictionary.OxygenMass;
                var fra11int = 200;
                var fra11comment = "[M-CO-O-3H]-";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct == "[M+H]+" ? 50 : adduct == "[M+Na]+" ? 999 : 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = sphMass + MassDictionary.Proton;
                //var fra05int = 200;
                //var fra05comment = "[Sph+H]+";
                //fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra06int = 200;
                var fra06comment = "[Sph-H2O+H]+";

                var fra07mass = fra06mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra07int = adduct == "[M+Na]+" ? 50 : 999;
                var fra07comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
                {
                    var fra02mass = fra01mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra02int = 100;
                    var fra02comment = "[M-H2O+H]+";
                    if (adduct == "[M+H]+")
                    {
                        fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
                    }
                    else if (adduct == "[M+H-H2O]+")
                    {
                        fra02mass = fra01mass;
                    }

                    var fra03mass = fra02mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra03int = 100;
                    var fra03comment = "[M-2H2O+H]+";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    var fra04mass = acylMass + MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 3;
                    var fra04int = 100;
                    var fra04comment = "[FAA+H]+";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                    var fra08mass = fra07mass - 12;
                    var fra08int = 200;
                    var fra08comment = "[Sph-CH4O2+H]+";
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
                }
            }

        }
        public static void CerNdsFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //  {   "Cer_NDS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
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

                var fra03mass = sphMass - MassDictionary.Proton - MassDictionary.NitrogenMass - 12 * 2 - MassDictionary.HydrogenMass * 7 - MassDictionary.OxygenMass;
                var fra03int = 300;
                var fra03comment = "[Sph-NCCO-3H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = acylMass - MassDictionary.Proton + 5 * MassDictionary.HydrogenMass + MassDictionary.NitrogenMass + 12 * 2;
                var fra04int = 400;
                var fra04comment = "[FA+NCCO-O-3H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = acylMass - MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 3;
                var fra05int = 100;
                var fra05comment = "[FA+N-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = acylMass - MassDictionary.Proton;
                var fra06int = 200;
                var fra06comment = "[FA-3H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra04mass + MassDictionary.OxygenMass;
                var fra07int = 300;
                var fra07comment = "[FA+NCCO-3H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = sphMass - MassDictionary.Proton;
                var fra08int = 50;
                var fra08comment = "[Sph-H]-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra02mass - MassDictionary.OxygenMass - 12 - MassDictionary.HydrogenMass * 2;
                //var fra09int = 300;
                //var fra09comment = "[M-CO-H]-";
                //fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = fra09mass - 2 * MassDictionary.HydrogenMass;
                var fra10int = 300;
                var fra10comment = "[M-CO-3H]-";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = fra10mass - MassDictionary.OxygenMass;
                var fra11int = 200;
                var fra11comment = "[M-CO-O-3H]-";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct == "[M+H]+" ? 50 : adduct == "[M+Na]+" ? 999 : 100;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = sphMass + MassDictionary.Proton;
                //var fra05int = 200;
                //var fra05comment = "[Sph+H]+";
                //fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra06int = 200;
                var fra06comment = "[Sph-H2O+H]+";

                var fra07mass = fra06mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra07int = adduct == "[M+Na]+" ? 50 : 999;
                var fra07comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);


                if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
                {
                    var fra02mass = fra01mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra02int = 100;
                    var fra02comment = "[M-H2O+H]+";
                    if (adduct == "[M+H]+")
                    {
                        fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
                    }
                    else if (adduct == "[M+H-H2O]+")
                    {
                        fra02mass = fra01mass;
                    }

                    var fra03mass = fra02mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra03int = 100;
                    var fra03comment = "[M-2H2O+H]+";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    var fra04mass = acylMass + MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 3;
                    var fra04int = 100;
                    var fra04comment = "[FAA+H]+";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                    var fra08mass = fra07mass - 12;
                    var fra08int = 200;
                    var fra08comment = "[Sph-CH4O2+H]+";
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
                }
            }

        }
        public static void CerNpFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "Cer_NP" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-" , "[M+H]+", "[M+Na]+"}    },
            var sphMass = sphingo3OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
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

                var fra03mass = fra02mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra03int = 100;
                var fra03comment = "[M-H2O-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra04int = 200;
                var fra04comment = "[M-2H2O-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = acylMass - MassDictionary.Proton + 12 * 3 + MassDictionary.OxygenMass + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass;
                var fra05int = 700;
                var fra05comment = "[FAA+C3H5O-H]-"; ;
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - 12;
                var fra06int = 700;
                var fra06comment = "[FAA+C2H5O-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - 2 * MassDictionary.HydrogenMass;
                var fra07int = 200;
                var fra07comment = "[FAA+C2H3O-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra07mass - MassDictionary.OxygenMass;
                var fra08int = 200;
                var fra08comment = "[FAA+C2H3-H]-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = acylMass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra09int = 500;
                var fra09comment = "[FA+O-H]-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = acylMass - MassDictionary.Proton + MassDictionary.NitrogenMass + 3 * MassDictionary.HydrogenMass;
                var fra10int = 250;
                var fra10comment = "[FAA-H]-";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = sphMass - MassDictionary.Proton - 12 - 7 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass - MassDictionary.NitrogenMass;
                var fra11int = 500;
                var fra11comment = "[Sph-CH9NO-H]-";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra12mass = fra11mass - 12;
                var fra12int = 200;
                var fra12comment = "[Sph-C2H9NO-H]-";
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                var fra13mass = fra12mass - 12 - MassDictionary.HydrogenMass * 2 - MassDictionary.OxygenMass;
                var fra13int = 50;
                var fra13comment = "[Sph-C3H11NO2-H]-";
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);
            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct == "[M+Na]+" ? 999 : 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = sphMass + MassDictionary.Proton;
                //var fra05int = 200;
                //var fra05comment = "[Sph+H]+";
                //fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra06int = 800;
                var fra06comment = "[Sph-H2O+H]+";

                var fra07mass = fra06mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra07int = adduct == "[M+Na]+" ? 50 : 800;
                var fra07comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);


                if (adduct == "[M+H]+")
                {
                    var fra02mass = fra01mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra02int = 800;
                    var fra02comment = "[M-H2O+H]+";
                    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                    var fra03mass = fra02mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra03int = 200;
                    var fra03comment = "[M-2H2O+H]+";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                    var fra08mass = fra07mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra08int = 999;
                    var fra08comment = "[Sph-3H2O+H]+";
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                    var fra09mass = fra08mass - 12;
                    var fra09int = 200;
                    var fra09comment = "[Sph-CH6O3+H]+";
                    fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                    var fra10mass = acylMass + MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass;
                    var fra10int = 300;
                    var fra10comment = "[FAA+H]+";
                    fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                }
            }
        }

        public static void CerBsFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "Cer_BS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-"   },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
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
                var fra02int = 300;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra03int = 100;
                var fra03comment = "[M-H2O-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = sphMass - MassDictionary.Proton + 12;
                var fra04int = 200;
                var fra04comment = "[Sph+C-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = sphMass - MassDictionary.Proton + 12 * 2 + MassDictionary.OxygenMass + MassDictionary.HydrogenMass * 2;
                var fra05int = 999;
                var fra05comment = "[Sph+C2H2O-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = sphMass - MassDictionary.Proton - MassDictionary.HydrogenMass * 5 - MassDictionary.OxygenMass - MassDictionary.NitrogenMass;
                var fra06int = 200;
                var fra06comment = "[Sph-H5NO-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - 12 * 2 - MassDictionary.HydrogenMass * 2;
                var fra07int = 200;
                var fra07comment = "[Sph-C2H7NO-H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }

        public static void CerBdsFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "Cer_BDS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-"} }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
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

                var fra03mass = sphMass - MassDictionary.Proton + 12 * 2 + MassDictionary.OxygenMass + MassDictionary.HydrogenMass * 2;
                var fra03int = 999;
                var fra03comment = "[Sph+CC=O-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = sphMass - MassDictionary.Proton + 12 - MassDictionary.HydrogenMass * 2;
                var fra04int = 200;
                var fra04comment = "[Sph+CC=O-CO-3H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = sphMass - MassDictionary.Proton;
                var fra05int = 200;
                var fra05comment = "[Sph-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = sphMass - MassDictionary.Proton - MassDictionary.NitrogenMass - 12 * 2 - MassDictionary.OxygenMass - MassDictionary.HydrogenMass * 7;
                var fra06int = 100;
                var fra06comment = "[Sph-NCCO-3H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }


        public static void CerHFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "Cer_HS" ,    new List<string>() { "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
            //{ "Cer_HDS" ,    new List<string>() { "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },

            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            //if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
            //{
            //    var fra01mass = 0.0;
            //    var fra01int = 0;
            //    var fra01comment = "";
            //    if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            //    {
            //        fra01mass = exactMass + AdductDic.adductIonDic[adduct].AdductIonMass;
            //        fra01int = 10;
            //        fra01comment = adduct;
            //        fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
            //    }
            //    var fra02mass = exactMass + AdductDic.adductIonDic["[M-H]-"].AdductIonMass;
            //    var fra02int = 999;
            //    var fra02comment = "[M-H]-";
            //    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            //}
            if (adduct == "[M+H]+" || adduct == "[M+Na]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct != "[M+H]+" ? 999 : 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = sphMass + MassDictionary.Proton;
                //var fra05int = 200;
                //var fra05comment = "[Sph+H]+";
                //fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra06int = 300;
                var fra06comment = "[Sph-H2O+H]+";

                var fra07mass = fra06mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                var fra07int = adduct == "[M+Na]+" ? 50 : 999;
                var fra07comment = "[Sph-2H2O+H]+";

                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
                {
                    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                    var fra02mass = fra01mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra02int = 999;
                    var fra02comment = "[M-H2O+H]+";
                    if (adduct == "[M+H]+")
                    {
                        fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
                    }
                    else if (adduct == "[M+H-H2O]+")
                    {
                        fra02mass = fra01mass;
                    }


                    var fra08mass = fra07mass - 12;
                    var fra08int = 300;
                    var fra08comment = "[Sph-CH4O2+H]+";
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
                }
            }
        }


        public static void HexCerApFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "HexCer_AP" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },

            var sphMass = sphingo3OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);

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

                var fra03mass = fra02mass - MassDictionary.C6H10O5;
                var fra03int = 100;
                var fra03comment = "[M-C6H10O5-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = acylMass - MassDictionary.Proton + MassDictionary.NitrogenMass + 12 * 3 + MassDictionary.OxygenMass + MassDictionary.HydrogenMass * 7;
                var fra04int = 700;
                var fra04comment = "[FA+NC(C)CO-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = acylMass - MassDictionary.Proton + MassDictionary.NitrogenMass + 12 * 2 + MassDictionary.HydrogenMass * 5;
                var fra05int = 100;
                var fra05comment = "[FA+NCC-3H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = acylMass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra06int = 200;
                var fra06comment = "[FA+O-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = acylMass - MassDictionary.Proton - 12 - MassDictionary.OxygenMass;
                var fra07int = 100;
                var fra07comment = "[FA-CO-3H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 6 + MassDictionary.HydrogenMass * 12 + MassDictionary.OxygenMass * 6 - MassDictionary.Proton;
                var fra08int = 200;
                var fra08comment = "[C6H12O6-H]-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct == "[M+H]+" ? 50 : adduct == "[M+Na]+" ? 999 : 1;
                var fra01comment = adduct;
                if (adduct == "[M+H]+" || adduct == "[M+Na]+")
                {
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }

                var fra05mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra05int = 200;
                var fra05comment = "[Sph-H2O+H]+";

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = adduct == "[M+Na]+" ? 50 : 400;
                var fra06comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
                {
                    fra01mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;

                    var fra02mass = fra01mass - MassDictionary.C6H10O5;
                    var fra02int = 500;
                    var fra02comment = "[M-C6H10O5+H]+";
                    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                    var fra03mass = fra02mass - MassDictionary.H2OMass;
                    var fra03int = 999;
                    var fra03comment = "[M-Hex-H2O+H]+";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    var fra04mass = fra03mass - MassDictionary.H2OMass;
                    var fra04int = 300;
                    var fra04comment = "[M-Hex-2H2O+H]+";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                    var fra07mass = fra06mass - MassDictionary.H2OMass;
                    var fra07int = 200;
                    var fra07comment = "[Sph-3H2O+H]+";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
                }
            }
        }

        public static void HexCerNFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "HexCer_NDS" ,    new List<string>() { "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
            //{ "HexCer_NS" ,    new List<string>() { "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);

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

                var fra03mass = fra02mass - MassDictionary.C6H10O5;
                var fra03int = 500;
                var fra03comment = "[M-C6H10O5-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = sphMass - MassDictionary.Proton - 12 * 2 - MassDictionary.HydrogenMass * 7 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass;
                var fra04int = 200;
                var fra04comment = "[Sph-C2H7NO-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = acylMass - MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 5 + 12 * 2;
                var fra05int = 200;
                var fra05comment = "[FAA+C2H3-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = acylMass - MassDictionary.Proton - MassDictionary.OxygenMass + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 2;
                var fra06int = 200;
                var fra06comment = "[FAA-OH-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = 12 * 6 + MassDictionary.HydrogenMass * 12 + MassDictionary.OxygenMass * 6 - MassDictionary.Proton;
                var fra07int = 200;
                var fra07comment = "[C6H11O6]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct != "[M+H]+" ? 999 : 50;
                var fra01comment = adduct;
                if (adduct == "[M+H]+" || adduct == "[M+Na]+")
                {
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }

                var fra05mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra05int = 200;
                var fra05comment = "[Sph-H2O+H]+";

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = adduct == "[M+Na]+" ? 50 : 999;
                var fra06comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
                {
                    fra01mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;

                    var fra02mass = fra01mass - MassDictionary.C6H10O5;
                    var fra02int = 50;
                    var fra02comment = "[M-C6H10O5+H]+";
                    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                    var fra03mass = fra02mass - MassDictionary.H2OMass;
                    var fra03int = 100;
                    var fra03comment = "[M-Hex-H2O+H]+";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    var fra04mass = fra03mass - MassDictionary.H2OMass;
                    var fra04int = 100;
                    var fra04comment = "[M-Hex-2H2O+H]+";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                    var fra07mass = sphMass + MassDictionary.Proton - 12 - MassDictionary.HydrogenMass * 4 - MassDictionary.OxygenMass * 2;
                    var fra07int = 200;
                    var fra07comment = "[Sph-CH4O2+H]+";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                    var fra08mass = acylMass + MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 3;
                    var fra08int = 100;
                    var fra08comment = "[FAA+H]";
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
                }
            }
        }

        public static void HexCerHFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "HexCer_HS" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+", "[M-H]-" }    },
            //{ "HexCer_HDS" ,    new List<string>() { "[M+HCOO]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+H-H2O]+", "[M-H]-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);

            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
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
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.C6H10O5;
                var fra03int = 200;
                var fra03comment = "[M-C6H10O5-H2O-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = acylMass - MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass;
                var fra04int = 100;
                var fra04comment = "FAA-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 6 + MassDictionary.HydrogenMass * 12 + MassDictionary.OxygenMass * 6 - MassDictionary.Proton;
                var fra05int = 100;
                var fra05comment = "C6H11O6-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct != "[M+H]+" ? 999 : 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra06mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra06int = 300;
                var fra06comment = "[Sph-H2O+H]+";

                var fra07mass = fra06mass - MassDictionary.H2OMass;
                var fra07int = adduct == "[M+Na]+" ? 50 : 700;
                var fra07comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
                {
                    var fra02mass = fra01mass - 2 * MassDictionary.HydrogenMass - MassDictionary.OxygenMass;
                    var fra02int = 999;
                    var fra02comment = "[M-H2O+H]+";
                    if (adduct == "[M+H]+")
                    {
                        fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
                    }
                    else if (adduct == "[M+H-H2O]+")
                    {
                        fra01mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                    }

                    var fra03mass = fra01mass - MassDictionary.C6H10O5 - MassDictionary.H2OMass;
                    var fra03int = 700;
                    var fra03comment = "[M-Hex-H2O+H]+";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    var fra04mass = fra03mass - MassDictionary.H2OMass;
                    var fra04int = 800;
                    var fra04comment = "[M-Hex-2H2O+H]+";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    var fra05mass = fra04mass - 12;
                    var fra05int = 400;
                    var fra05comment = "[M-Hex-2H2O-C+H]+";
                    fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                    var fra08mass = fra07mass - 12;
                    var fra08int = 300;
                    var fra08comment = "[Sph-CH4O2+H]+";
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
                }
            }
        }


        public static void Hex2CerFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "Hex2Cer" ,    new List<string>() { "[M+H]+", "[M+HCOO]-", "[M+CH3COO]-" , "[M+Na]+"}    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 30;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.C6H10O5;
                var fra03int = 100;
                var fra03comment = "[M-C6H10O5-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.C6H10O5;
                var fra04int = 100;
                var fra04comment = "[M-C12H20O10-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 6 + MassDictionary.HydrogenMass * 12 + MassDictionary.OxygenMass * 6 - MassDictionary.Proton;
                var fra05int = 100;
                var fra05comment = "[C6H11O6]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 3 + MassDictionary.HydrogenMass * 6 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra06int = 50;
                var fra06comment = "C3H5O3-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct == "[M+Na]+" ? 999 : 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra05int = 50;
                var fra05comment = "[Sph-H2O+H]+";

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = adduct == "[M+Na]+" ? 50 : 500;
                var fra06comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (adduct == "[M+H]+")
                {
                    var fra02mass = fra01mass - MassDictionary.C6H10O5;
                    var fra02int = 50;
                    var fra02comment = "[M-Hex+H]+";
                    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                    var fra03mass = fra02mass - MassDictionary.H2OMass;
                    var fra03int = 500;
                    var fra03comment = "[M-Hex-H2O+H]+";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    var fra04mass = fra03mass - MassDictionary.H2OMass;
                    var fra04int = 50;
                    var fra04comment = "[M-Hex-2H2O+H]+";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                    var fra07mass = fra06mass - 12;
                    var fra07int = 50;
                    var fra07comment = "[Sph-CH4O2+H]+";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                    var fra08mass = fra01mass - MassDictionary.H2OMass;
                    var fra08int = 999;
                    var fra08comment = "[M-H2O+H]+";
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                    var fra09mass = fra02mass - MassDictionary.C6H10O5;
                    var fra09int = 50;
                    var fra09comment = "[M-2Hex+H]+";
                    fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                    var fra10mass = fra09mass - MassDictionary.H2OMass;
                    var fra10int = 400;
                    var fra10comment = "[M-2Hex-H2O+H]+";
                    fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                    var fra11mass = fra10mass - MassDictionary.H2OMass;
                    var fra11int = 100;
                    var fra11comment = "[M-2Hex-2H2O+H]+";
                    fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);
                }
            }
        }

        public static void Hex3CerFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "Hex3Cer" ,    new List<string>() { "[M+H]+", "[M+HCOO]-", "[M+CH3COO]-", "[M+Na]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 30;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.C6H10O5;
                var fra03int = 80;
                var fra03comment = "[M-C6H10O5-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.C6H10O5;
                var fra04int = 125;
                var fra04comment = "[M-C12H20O10-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.C6H10O5;
                var fra05int = 50;
                var fra05comment = "[M-C18H30O15-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = MassDictionary.C6H10O5 - MassDictionary.Proton;
                var fra06int = 15;
                var fra06comment = "C6H9O5-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass + MassDictionary.H2OMass;
                var fra07int = 35;
                var fra07comment = "C6H11O6-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                //var fra08mass = MassDictionary.C6H10O5 + MassDictionary.C6H10O5 + MassDictionary.H2OMass - MassDictionary.Proton;
                //var fra08int = 60;
                //var fra08comment = "C6H10O5+Hac-H-";
                //fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = adduct == "[M+Na]+" ? 999 : 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra05mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra05int = 50;
                var fra05comment = "[Sph-H2O+H]+";

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = adduct == "[M+Na]+" ? 50 : 400;
                var fra06comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                if (adduct == "[M+H]+")
                {
                    var fra02mass = fra01mass - MassDictionary.C6H10O5;
                    var fra02int = 50;
                    var fra02comment = "[M-C6H10O5+H]+";
                    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                    var fra03mass = fra02mass - MassDictionary.H2OMass;
                    var fra03int = 300;
                    var fra03comment = "[M-Hex-H2O+H]+";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    var fra04mass = fra03mass - MassDictionary.H2OMass;
                    var fra04int = 50;
                    var fra04comment = "[M-Hex-2H2O+H]+";
                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                    var fra07mass = fra06mass - 12;
                    var fra07int = 50;
                    var fra07comment = "[Sph-CH4O2+H]+";
                    fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                    var fra08mass = fra01mass - MassDictionary.H2OMass;
                    var fra08int = 500;
                    var fra08comment = "[M-H2O+H]+";
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                    var fra09mass = fra02mass - MassDictionary.C6H10O5;
                    var fra09int = 50;
                    var fra09comment = "[M-2Hex+H]+";
                    fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                    var fra10mass = fra09mass - MassDictionary.H2OMass;
                    var fra10int = 300;
                    var fra10comment = "[M-2Hex-H2O+H]+";
                    fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                    var fra11mass = fra10mass - MassDictionary.H2OMass;
                    var fra11int = 50;
                    var fra11comment = "[M-2Hex-2H2O+H]+";
                    fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                    var fra12mass = fra09mass - MassDictionary.C6H10O5;
                    var fra12int = 100;
                    var fra12comment = "[M-3Hex+H]+";
                    fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                    var fra13mass = fra12mass - MassDictionary.H2OMass;
                    var fra13int = 999;
                    var fra13comment = "[M-3Hex-H2O+H]+";
                    fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);

                    var fra14mass = fra13mass - MassDictionary.H2OMass;
                    var fra14int = 100;
                    var fra14comment = "[M-3Hex-2H2O+H]+";
                    fragmentList.Add(fra14mass + "\t" + fra14int + "\t" + fra14comment);
                }
            }
        }

        public static void MipcFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {

            var sphMass = sphingo3OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);

            if (adduct == "[M-H]-")
            {
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.C6H10O5 * 2;
                var fra03int = 10;
                var fra03comment = "1sugar and ino loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 10;
                var fra04comment = "1sugar and ino and H2O loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 12 + MassDictionary.OxygenMass * 14 + MassDictionary.HydrogenMass * 23 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra05int = 10;
                var fra05comment = "header + O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 10;
                var fra06comment = "header + O - H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = sphMass - MassDictionary.Proton - 12 * 2 - MassDictionary.OxygenMass - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 9;
                var fra07int = 5;
                var fra07comment = "[Sph-NCCO-3H]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 10;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.C6H10O5;
                var fra02int = 100;
                var fra02comment = "1sugar loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - (12 * 12 + MassDictionary.OxygenMass * 13 + MassDictionary.HydrogenMass * 21 + MassDictionary.PhosphorusMass);
                var fra03int = 10;
                var fra03comment = "header loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "header loss - H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 10;
                var fra05comment = "header loss - 2H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = sphMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra06int = 10;
                var fra06comment = "[Sph-H2O+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - MassDictionary.H2OMass;
                var fra07int = 10;
                var fra07comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
        }

        public static void CerPFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "CerP" ,    new List<string>() { "[M-H]-", "[M+H]+" , "[M+Na]+"}    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 10;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 100;
                var fra02comment = "[M-H]-H2O (-18)";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - acylMass;
                var fra03int = 200;
                var fra03comment = "[M-H]-sn2";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 200;
                var fra04comment = "[M-H]-sn2-H20";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = MassDictionary.HydrogenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.OxygenMass * 4 - MassDictionary.Proton;
                var fra05int = 999;
                var fra05comment = "ion H2PO4- (96.96908)";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = MassDictionary.PhosphorusMass + MassDictionary.OxygenMass * 3 + MassDictionary.Electron;
                var fra06int = 999;
                var fra06comment = "ion PO3- (78.95851)";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra04mass = fra01mass - MassDictionary.HydrogenMass * 3 - MassDictionary.PhosphorusMass - MassDictionary.OxygenMass * 4;
                var fra04int = 200;
                var fra04comment = "[M+H]+ (-H3PO4) (-97.9769)";

                var fra06mass = fra04mass - acylMass - MassDictionary.OxygenMass;
                var fra06int = adduct == "[M+Na]+" ? 50 : 300;
                var fra06comment = "[M+H]+ (-H3PO4) -acyl-O ";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);


                if (adduct == "[M+H]+")
                {
                    var fra02mass = fra01mass - MassDictionary.H2OMass;
                    var fra02int = 250;
                    var fra02comment = "[M+H]+ (-H20)";
                    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                    var fra03mass = fra01mass - MassDictionary.HydrogenMass * 1 - MassDictionary.PhosphorusMass - MassDictionary.OxygenMass * 3;
                    var fra03int = 200;
                    var fra03comment = "[M+H]+ (-HPO3) (-79.96633)";
                    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                    var fra05mass = fra04mass - MassDictionary.H2OMass;
                    var fra05int = 200;
                    var fra05comment = "[M+H]+ (-H3PO4-H20)";
                    fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
                }
            }
        }

        public static void GM3Fragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "GM3" ,    new List<string>() { "[M+NH4]+", "[M-H]-", "[M-2H]2-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 100;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 10;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                var fra02int = 10;
                var fra02comment = "[M-C11H17NO8(290)-H]- ";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 100;
                var fra03comment = "[C11H17NO8-H]- (290)";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 10;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                //var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                //var fra02int = 10;
                //var fra02comment = "[M-C11H17NO8(290)-H]- ";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }

            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 1;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                var fra02int = 200;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 600;
                var fra03comment = "[M-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                var fra04int = 300;
                var fra04comment = "[M-H2O-C11H17NO8+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass - (12 * 11 + MassDictionary.HydrogenMass * 15 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 7);
                var fra05int = 150;
                var fra05comment = "[M-H2O-C11H15NO7+H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra03mass - (12 * 17 + MassDictionary.HydrogenMass * 27 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 13);
                var fra06int = 450;
                var fra06comment = "[M-H2O-C17H27NO13+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra03mass - (12 * 17 + MassDictionary.HydrogenMass * 25 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 12);
                var fra07int = 450;
                var fra07comment = "[M-H2O-C17H25NO12+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra03mass - (12 * 23 + MassDictionary.HydrogenMass * 37 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 18);
                var fra08int = 999;
                var fra08comment = "[M-H2O-C23H37NO18+H]+";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra03mass - (12 * 23 + MassDictionary.HydrogenMass * 35 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 17);
                var fra09int = 300;
                var fra09comment = "[M-H2O-C23H35NO17+H]+";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) + MassDictionary.Proton;
                var fra10int = 350;
                var fra10comment = "[C11H17NO8+H]+";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = sphMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra11int = 100;
                var fra11comment = "[Sph-H2O+H]+";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra12mass = fra11mass - MassDictionary.H2OMass;
                var fra12int = 200;
                var fra12comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);
            }
        }
        //

        public static void GD2Fragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "GD2" ,    new List<string>() { "[M-H]-", "[M-2H]2-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 200;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 100;
                var fra05comment = "[(C11H17NO8)*2 -H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 10;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                //var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                //var fra02int = 10;
                //var fra02comment = "[M-C11H17NO8(290)-H]- ";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 200;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 100;
                var fra05comment = "[(C11H17NO8)*2 -H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);


                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 10;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                //var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                //var fra02int = 10;
                //var fra02comment = "[M-C11H17NO8(290)-H]- ";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
        }
        public static void GD1aFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            // { "GD1a" ,    new List<string>() { "[M-H]-", "[M-2H]2-", "[M+2NH4]2+", "[M+2H]2+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 100;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 100;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                //var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                //var fra02int = 10;
                //var fra02comment = "[M-C11H17NO8(290)-H]- ";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 100;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 100;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                //var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                //var fra02int = 10;
                //var fra02comment = "[M-C11H17NO8(290)-H]- ";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M+2NH4]2+" || adduct == "[M+2H]2+")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                //var fra01int = 1;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = sphMass + acylMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra02int = 500;
                var fra02comment = "Ceramide fragment"; // 36:1 -> 548.5
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "[Ceramide fragment-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra031mass = fra02mass + MassDictionary.H2OMass;
                var fra031int = 50;
                var fra031comment = "[Ceramide fragment+H2O+H]+";
                fragmentList.Add(fra031mass + "\t" + fra031int + "\t" + fra031comment);

                var fra04mass = fra02mass + MassDictionary.C6H10O5;
                var fra04int = 100;
                var fra04comment = "Ceramide fragment + 1sugar";// 36:1 -> 710.6
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra041mass = fra04mass + MassDictionary.H2OMass;
                var fra041int = 50;
                var fra041comment = "Ceramide fragment + 1sugar + H2O"; // 36:1 -> 728.6
                fragmentList.Add(fra041mass + "\t" + fra041int + "\t" + fra041comment);

                var fra05mass = fra04mass + MassDictionary.C6H10O5;
                var fra05int = 100;
                var fra05comment = "Ceramide fragment + 2sugars"; // 36:1 -> 872.6
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass + MassDictionary.H2OMass;
                var fra06int = 50;
                var fra06comment = "Ceramide fragment + 2sugars + H2O"; // 36:1 -> 890.7
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = 12 * 25 + MassDictionary.HydrogenMass * 40 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 18 + MassDictionary.Proton;
                var fra07int = 300;
                var fra07comment = "[C25H40N2O18+H]+"; // 657
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 + MassDictionary.Proton;
                var fra08int = 600;
                var fra08comment = "[C11H17NO8+H]+"; //292
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra08mass - MassDictionary.H2OMass;
                var fra09int = 999;
                var fra09comment = "[C11H17NO8+H]+ - H2O"; //274
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = (12 * 8 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5) + MassDictionary.Proton;
                var fra10int = 500;
                var fra10comment = "[C8H13NO5+H]+"; // 204
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra13mass = (12 * 14 + MassDictionary.HydrogenMass * 23 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 10) + MassDictionary.Proton;
                var fra13int = 200;
                var fra13comment = "[C14H23NO10+H]+"; // 366
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);

                var fra11mass = sphMass - MassDictionary.H2OMass * 2 + MassDictionary.Proton;
                var fra11int = 100;
                var fra11comment = "[Sph-2H2O+H]+";  // chain solve fragment
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                if (adduct == "[M+2NH4]2+")
                {
                    var fra12mass = (12 * 17 + MassDictionary.HydrogenMass * 27 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 13) + MassDictionary.Proton;
                    var fra12int = 50;
                    var fra12comment = "[C17H27NO13+H]+"; // 454
                    fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                }
            }
        }

        public static void GD1bFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //     { "GD1b" ,    new List<string>() { "[M-H]-", "[M-2H]2-", "[M+2NH4]2+", "[M+2H]2+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 100;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 10;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                //var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                //var fra02int = 10;
                //var fra02comment = "[M-C11H17NO8(290)-H]- ";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 100;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 700;
                var fra05comment = "[(C11H17NO8)*2 -H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                //var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                //var fra02int = 10;
                //var fra02comment = "[M-C11H17NO8(290)-H]- ";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M+2NH4]2+" || adduct == "[M+2H]2+")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                //var fra01int = 1;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = sphMass + acylMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra02int = 500;
                var fra02comment = "Ceramide fragment"; // 36:1 -> 548.5
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "[Ceramide fragment-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra031mass = fra02mass + MassDictionary.H2OMass;
                var fra031int = 50;
                var fra031comment = "[Ceramide fragment+H2O+H]+";
                fragmentList.Add(fra031mass + "\t" + fra031int + "\t" + fra031comment);

                var fra04mass = fra02mass + MassDictionary.C6H10O5;
                var fra04int = 100;
                var fra04comment = "Ceramide fragment + 1sugar";// 36:1 -> 710.6
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra041mass = fra04mass + MassDictionary.H2OMass;
                var fra041int = 50;
                var fra041comment = "Ceramide fragment + 1sugar + H2O"; // 36:1 -> 728.6
                fragmentList.Add(fra041mass + "\t" + fra041int + "\t" + fra041comment);

                var fra05mass = fra04mass + MassDictionary.C6H10O5;
                var fra05int = 100;
                var fra05comment = "Ceramide fragment + 2sugars"; // 36:1 -> 872.6
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass + MassDictionary.H2OMass;
                var fra06int = 50;
                var fra06comment = "Ceramide fragment + 2sugars + H2O"; // 36:1 -> 890.7
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                //var fra07mass = 12 * 25 + MassDictionary.HydrogenMass * 40 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 18 + MassDictionary.Proton;
                //var fra07int = 300;
                //var fra07comment = "[C25H40N2O18+H]+"; // 657
                //fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 + MassDictionary.Proton;
                var fra08int = 600;
                var fra08comment = "[C11H17NO8+H]+"; //292
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra08mass - MassDictionary.H2OMass;
                var fra09int = 999;
                var fra09comment = "[C11H17NO8+H]+ - H2O"; //274
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = (12 * 8 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5) + MassDictionary.Proton;
                var fra10int = 700;
                var fra10comment = "[C8H13NO5+H]+"; // 204
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra13mass = (12 * 14 + MassDictionary.HydrogenMass * 23 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 10) + MassDictionary.Proton;
                var fra13int = 700;
                var fra13comment = "[C14H23NO10+H]+"; // 366
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);

                var fra11mass = sphMass - MassDictionary.H2OMass * 2 + MassDictionary.Proton;
                var fra11int = 400;
                var fra11comment = "[Sph-2H2O+H]+";  // chain solve fragment
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra12mass = (12 * 17 + MassDictionary.HydrogenMass * 27 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 13) + MassDictionary.Proton;
                var fra12int = 50;
                var fra12comment = "[C17H27NO13+H]+"; // 454
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);
            }

        }

        public static void GD3Fragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "GD2" ,    new List<string>() { "[M-H]-", "[M-2H]2-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 10;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 10;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 100;
                var fra05comment = "[(C11H17NO8)*2 -H]-"; //581
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                var fra02int = 100;
                var fra02comment = "[M-C11H17NO8(290)-H]- ";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 300;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[C11H17NO8-H]-"; //290
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 300;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 300;
                var fra05comment = "[(C11H17NO8)*2 -H]-"; //581
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 19 + MassDictionary.HydrogenMass * 30 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 13 - MassDictionary.Proton;
                var fra06int = 50;
                var fra06comment = "[C19H30N2O12-H]-"; //493 
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = sphMass + acylMass - MassDictionary.Proton;
                var fra07int = 50;
                var fra07comment = "ceramide fragment";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                //var fra08mass = fra07mass -  MassDictionary.H2OMass ;
                //var fra08int = 100;
                //var fra08comment = "ceramide fragment - H2O";
                //fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);


                //var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                //var fra02int = 10;
                //var fra02comment = "[M-C11H17NO8(290)-H]- ";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
        }

        public static void GM1Fragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "GM1" ,    new List<string>() { "[M-H]-", "[M-2H]2-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 5;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 5;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 5;
                var fra05comment = "[(C11H17NO8)*2 -H]-"; //581
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                var fra02int = 5;
                var fra02comment = "[M-C11H17NO8(290)-H]- ";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 200;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[C11H17NO8-H]-"; //290
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 300;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass + MassDictionary.H2OMass;
                var fra05int = 300;
                var fra05comment = "[C11H17NO8-H + H2O]-"; //308
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra07mass = sphMass + acylMass - MassDictionary.Proton;
                var fra07int = 50;
                var fra07comment = "ceramide fragment";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                //var fra08mass = fra07mass - MassDictionary.H2OMass;
                //var fra08int = 100;
                //var fra08comment = "ceramide fragment - H2O";
                //fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 1;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                var fra02int = 500;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 200;
                var fra03comment = "[M-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - (12 * 14 + MassDictionary.HydrogenMass * 23 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 10);
                var fra04int = 300;
                var fra04comment = "[M-C14H23NO10+H]+"; // -365
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 150;
                var fra05comment = "[M-C14H23NO10-H2O+H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra04mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                var fra06int = 50;
                var fra06comment = "[M-C14H23NO10-C11H17NO8+H]+"; // -365 - 291
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - MassDictionary.H2OMass;
                var fra07int = 50;
                var fra07comment = "[M-C14H23NO10-C11H17NO8-H2O+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra06mass - MassDictionary.C6H10O5;
                var fra08int = 200;
                var fra08comment = "[M-C14H23NO10-C11H17NO8-162+H]+"; // -365 - 291 - 1sugar
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra08mass - MassDictionary.H2OMass;
                var fra09int = 100;
                var fra09comment = "[M-C14H23NO10-C11H17NO8-162-H2O+H]+";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = (12 * 8 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5) + MassDictionary.Proton;
                var fra10int = 200;
                var fra10comment = "[C8H13NO5+H]+"; // 204
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra15mass = fra10mass - MassDictionary.H2OMass;
                var fra15int = 100;
                var fra15comment = "[C8H13NO5+H]+"; // 204 - 18
                fragmentList.Add(fra15mass + "\t" + fra15int + "\t" + fra15comment);

                var fra13mass = (12 * 14 + MassDictionary.HydrogenMass * 23 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 10) + MassDictionary.Proton;
                var fra13int = 999;
                var fra13comment = "[C14H23NO10+H]+"; // 366
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);

                var fra11mass = sphMass - MassDictionary.H2OMass * 2 + MassDictionary.Proton;
                var fra11int = 100;
                var fra11comment = "[Sph-2H2O+H]+";  // chain solve fragment
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra12mass = sphMass + acylMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra12int = 200;
                var fra12comment = "Ceramide fragment"; // 36:1 -> 548.5
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                var fra14mass = fra12mass + MassDictionary.H2OMass;
                var fra14int = 100;
                var fra14comment = "[Ceramide fragment+H]+ +H2O";
                fragmentList.Add(fra14mass + "\t" + fra14int + "\t" + fra14comment);
            }
            else if (adduct == "[M+2NH4]2+" || adduct == "[M+2H]2+")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                //var fra01int = 1;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = sphMass + acylMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra02int = 700;
                var fra02comment = "Ceramide fragment"; // 36:1 -> 548.5
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 200;
                var fra03comment = "[Ceramide fragment+H]+ -H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass + MassDictionary.C6H10O5;
                var fra04int = 300;
                var fra04comment = "Ceramide fragment + 1sugar";// 36:1 -> 710.6
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                if (adduct == "[M+2NH4]2+")
                {
                    var fra041mass = fra04mass + MassDictionary.H2OMass;
                    var fra041int = 50;
                    var fra041comment = "Ceramide fragment + 1sugar + H2O"; // 36:1 -> 728.6
                    fragmentList.Add(fra041mass + "\t" + fra041int + "\t" + fra041comment);

                    var fra031mass = fra02mass + MassDictionary.H2OMass;
                    var fra031int = 100;
                    var fra031comment = "[Ceramide fragment+H2O+H]+";
                    fragmentList.Add(fra031mass + "\t" + fra031int + "\t" + fra031comment);
                }

                var fra06mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 + MassDictionary.Proton;
                var fra06int = 150;
                var fra06comment = "[C11H17NO8+H]+"; // 292
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - MassDictionary.H2OMass;
                var fra07int = 500;
                var fra07comment = "[C11H17NO8+H]+ -H2O";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra10mass = (12 * 8 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5) + MassDictionary.Proton;
                var fra10int = 999;
                var fra10comment = "[C8H13NO5+H]+"; // 204
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra15mass = fra10mass - MassDictionary.H2OMass;
                var fra15int = 500;
                var fra15comment = "[C8H13NO5+H]+ -H2O"; // 204 - 18
                fragmentList.Add(fra15mass + "\t" + fra15int + "\t" + fra15comment);

                var fra16mass = (12 * 6 + MassDictionary.HydrogenMass * 9 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 3) + MassDictionary.Proton;
                var fra16int = 300;
                var fra16comment = "[C6H9NO3+H]+ "; // 144
                fragmentList.Add(fra16mass + "\t" + fra16int + "\t" + fra16comment);

                var fra17mass = (12 * 4 + MassDictionary.HydrogenMass * 9 + MassDictionary.OxygenMass * 5) + MassDictionary.Proton;
                var fra17int = 300;
                var fra17comment = "[C4H9O5+H]+"; // 138
                fragmentList.Add(fra17mass + "\t" + fra17int + "\t" + fra17comment);

                var fra13mass = (12 * 14 + MassDictionary.HydrogenMass * 23 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 10) + MassDictionary.Proton;
                var fra13int = 600;
                var fra13comment = "[C14H23NO10+H]+"; // 366
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);

                var fra11mass = sphMass - MassDictionary.H2OMass * 2 + MassDictionary.Proton;
                var fra11int = 800;
                var fra11comment = "[Sph-2H2O+H]+";  // chain solve fragment
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

            }
        }

        public static void GQ1bFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "GQ1b" ,    new List<string>() { "[M-H]-", "[M-2H]2-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 5;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 5;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 5;
                var fra05comment = "[(C11H17NO8)*2 -H]-"; //581
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                var fra02int = 5;
                var fra02comment = "[M-C11H17NO8(290)-H]- ";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 300;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 500;
                var fra03comment = "[C11H17NO8-H]-"; //290
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 10;
                var fra04comment = "[C3H4O3-H]-"; //87
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 999;
                var fra05comment = "[(C11H17NO8)*2 -H]-"; //581
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - 12 - MassDictionary.OxygenMass * 2;
                var fra06int = 100;
                var fra06comment = "[(C11H17NO8)*2 -H]- - CO2"; //537 (581 - 44) 
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = ((exactMass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) - MassDictionary.Proton) / 2);
                var fra07int = 100;
                var fra07comment = "[M-C11H17NO8(290)-H]- /2 ";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = ((exactMass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2) / 2) - MassDictionary.Proton;
                var fra08int = 100;
                var fra08comment = "[M-(581)-H]- /2 ";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }
            else if (adduct == "[M+2NH4]2+")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                //var fra01int = 5;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = sphMass + acylMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra02int = 500;
                var fra02comment = "Ceramide fragment"; // 36:1 -> 548.5
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 50;
                var fra03comment = "[Ceramide fragment-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra031mass = fra02mass + MassDictionary.H2OMass;
                var fra031int = 50;
                var fra031comment = "[Ceramide fragment+H2O+H]+";
                fragmentList.Add(fra031mass + "\t" + fra031int + "\t" + fra031comment);

                var fra04mass = fra02mass + MassDictionary.C6H10O5;
                var fra04int = 100;
                var fra04comment = "Ceramide fragment + 1sugar";// 36:1 -> 710.6
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra041mass = fra04mass + MassDictionary.H2OMass;
                var fra041int = 50;
                var fra041comment = "Ceramide fragment + 1sugar + H2O"; // 36:1 -> 728.6
                fragmentList.Add(fra041mass + "\t" + fra041int + "\t" + fra041comment);

                var fra05mass = fra04mass + MassDictionary.C6H10O5;
                var fra05int = 150;
                var fra05comment = "Ceramide fragment + 2sugars"; // 36:1 -> 872.6
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass + MassDictionary.H2OMass;
                var fra06int = 100;
                var fra06comment = "Ceramide fragment + 2sugars + H2O"; // 36:1 -> 890.7
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra061mass = fra05mass + (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                var fra061int = 150;
                var fra061comment = "Ceramide fragment + 2sugars + C11H17NO8"; // 36:1 -> 1163.746
                fragmentList.Add(fra061mass + "\t" + fra061int + "\t" + fra061comment);

                var fra20mass = 12 * 36 + MassDictionary.HydrogenMass * 57 + MassDictionary.NitrogenMass * 3 + MassDictionary.OxygenMass * 26 + MassDictionary.Proton;
                var fra20int = 300;
                var fra20comment = "sugars"; //  948.33
                fragmentList.Add(fra20mass + "\t" + fra20int + "\t" + fra20comment);

                var fra21mass = 12 * 22 + MassDictionary.HydrogenMass * 34 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 16 + MassDictionary.C6H10O5 + MassDictionary.Proton;
                var fra21int = 100;
                var fra21comment = "sugars"; // 745.24  ?
                fragmentList.Add(fra21mass + "\t" + fra21int + "\t" + fra21comment);

                var fra22mass = 12 * 22 + MassDictionary.HydrogenMass * 34 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 16 + MassDictionary.Proton;
                var fra22int = 100;
                var fra22comment = "sugars"; // 583.2
                fragmentList.Add(fra22mass + "\t" + fra22int + "\t" + fra22comment);

                var fra07mass = 12 * 25 + MassDictionary.HydrogenMass * 40 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 18 + MassDictionary.Proton;
                var fra07int = 300;
                var fra07comment = "[C25H40N2O18+H]+"; // 657  ?
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 + MassDictionary.Proton;
                var fra08int = 600;
                var fra08comment = "[C11H17NO8+H]+"; //292
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra08mass - MassDictionary.H2OMass;
                var fra09int = 999;
                var fra09comment = "[C11H17NO8+H]+ - H2O"; //274
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = (12 * 8 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5) + MassDictionary.Proton;
                var fra10int = 200;
                var fra10comment = "[C8H13NO5+H]+"; // 204
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra13mass = (12 * 14 + MassDictionary.HydrogenMass * 23 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 10) + MassDictionary.Proton;
                var fra13int = 250;
                var fra13comment = "[C14H23NO10+H]+"; // 366
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);

                var fra12mass = (12 * 17 + MassDictionary.HydrogenMass * 27 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 13) + MassDictionary.Proton;
                var fra12int = 250;
                var fra12comment = "[C17H27NO13+H]+"; // 454
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                var fra11mass = sphMass - MassDictionary.H2OMass * 2 + MassDictionary.Proton;
                var fra11int = 50;
                var fra11comment = "[Sph-2H2O+H]+";  // chain solve fragment
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra15mass = 12 * 25 + MassDictionary.HydrogenMass * 40 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 18 + MassDictionary.Proton;
                var fra15int = 300;
                var fra15comment = "[C25H40N2O18+H]+"; // 657
                fragmentList.Add(fra15mass + "\t" + fra15int + "\t" + fra15comment);
            }
        }
        public static void GT1bFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "GQ1b" ,    new List<string>() { "[M-H]-", "[M-2H]2-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 5;
                var fra03comment = "[C11H17NO8-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 5;
                var fra04comment = "[C3H4O3-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 5;
                var fra05comment = "[(C11H17NO8)*2 -H]-"; //581
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8);
                var fra02int = 5;
                var fra02comment = "[M-C11H17NO8(290)-H]- ";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 300;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 - MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[C11H17NO8-H]-"; //290
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 50;
                var fra04comment = "[C3H4O3-H]-"; //87
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8) * 2 - MassDictionary.Proton;
                var fra05int = 150;
                var fra05comment = "[(C11H17NO8)*2 -H]-"; //581
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - 12 - MassDictionary.OxygenMass * 2;
                var fra06int = 50;
                var fra06comment = "[(C11H17NO8)*2 -H]- - CO2"; //537 (581 - 44) 
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                //var fra07mass = fra01mass - fra03mass + MassDictionary.Proton;
                //var fra07int = 100;
                //var fra07comment = "precursor - 291";
                //fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = ((exactMass - fra03mass + adductDic.adductIonDic[adduct].AdductIonMass) / 2);
                var fra08int = 100;
                var fra08comment = "[M - 291]/2)";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }
            else if (adduct == "[M+2NH4]2+" || adduct == "[M+2H]2+")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                //var fra01int = 1;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = sphMass + acylMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra02int = 400;
                var fra02comment = "Ceramide fragment"; // 36:1 -> 548.5
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 100;
                var fra03comment = "[Ceramide fragment+H]+ -H2O";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass + MassDictionary.C6H10O5;
                var fra04int = 150;
                var fra04comment = "Ceramide fragment + 1sugar";// 36:1 -> 710.6
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra041mass = fra04mass + MassDictionary.H2OMass;
                var fra041int = 50;
                var fra041comment = "Ceramide fragment + 1sugar + H2O"; // 36:1 -> 728.6
                fragmentList.Add(fra041mass + "\t" + fra041int + "\t" + fra041comment);

                var fra05mass = fra04mass + MassDictionary.C6H10O5;
                var fra05int = 100;
                var fra05comment = "Ceramide fragment + 1sugar";// 36:1 -> 872
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra051mass = fra05mass + MassDictionary.H2OMass;
                var fra051int = 50;
                var fra051comment = "Ceramide fragment + 1sugar + H2O"; // 36:1 -> 890
                fragmentList.Add(fra051mass + "\t" + fra051int + "\t" + fra051comment);

                if (adduct == "[M+2NH4]2+")
                {
                    var fra011mass = fra05mass + (12 * 8 + MassDictionary.HydrogenMass * 13 + MassDictionary.OxygenMass * 5 + MassDictionary.NitrogenMass);
                    var fra011int = 100;
                    var fra011comment = adduct;
                    fragmentList.Add(fra011mass + "\t" + fra011int + "\t" + fra011comment);
                }

                //var fra031mass = fra02mass + MassDictionary.H2OMass;
                //var fra031int = 100;
                //var fra031comment = "[Ceramide fragment+H2O+H]+";
                //fragmentList.Add(fra031mass + "\t" + fra031int + "\t" + fra031comment);

                var fra06mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 8 + MassDictionary.Proton;
                var fra06int = 800;
                var fra06comment = "[C11H17NO8+H]+"; // 292
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - MassDictionary.H2OMass;
                var fra07int = 999;
                var fra07comment = "[C11H17NO8+H]+ -H2O"; //274
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra10mass = (12 * 8 + MassDictionary.HydrogenMass * 13 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5) + MassDictionary.Proton;
                var fra10int = 400;
                var fra10comment = "[C8H13NO5+H]+"; // 204
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra15mass = fra10mass - MassDictionary.H2OMass;
                var fra15int = 100;
                var fra15comment = "[C8H13NO5+H]+ -H2O"; // 204 - 18
                fragmentList.Add(fra15mass + "\t" + fra15int + "\t" + fra15comment);

                var fra12mass = (12 * 17 + MassDictionary.HydrogenMass * 27 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 13) + MassDictionary.Proton;
                var fra12int = 200;
                var fra12comment = "[C17H27NO13+H]+"; // 454
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);

                var fra22mass = 12 * 22 + MassDictionary.HydrogenMass * 34 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 16 + MassDictionary.Proton;
                var fra22int = 100;
                var fra22comment = "sugars"; // 583.2
                fragmentList.Add(fra22mass + "\t" + fra22int + "\t" + fra22comment);

                var fra13mass = (12 * 14 + MassDictionary.HydrogenMass * 23 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 10) + MassDictionary.Proton;
                var fra13int = 200;
                var fra13comment = "[C14H23NO10+H]+"; // 366
                fragmentList.Add(fra13mass + "\t" + fra13int + "\t" + fra13comment);

                var fra11mass = sphMass - MassDictionary.H2OMass * 2 + MassDictionary.Proton;
                var fra11int = 100;
                var fra11comment = "[Sph-2H2O+H]+";  // chain solve fragment
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra16mass = 12 * 25 + MassDictionary.HydrogenMass * 40 + MassDictionary.NitrogenMass * 2 + MassDictionary.OxygenMass * 18 + MassDictionary.Proton;
                var fra16int = 300;
                var fra16comment = "[C25H40N2O18+H]+"; // 657
                fragmentList.Add(fra16mass + "\t" + fra16int + "\t" + fra16comment);


            }
        }
        public static void NGcGM3Fragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "NGcGM3" ,    new List<string>() { "[M-H]-", "[M-2H]2-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 9 - MassDictionary.Proton;
                var fra03int = 100;
                var fra03comment = "[C11H17NO9-H]-"; //306
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 5;
                var fra04comment = "[C3H4O3-H]-"; //87
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra02mass = fra01mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 9);
                var fra02int = 5;
                var fra02comment = "[M-C11H17NO9(306)-H]- "; // precursor - 306
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
            else if (adduct == "[M-2H]2-")
            {
                var fra01mass = (exactMass + adductDic.adductIonDic[adduct].AdductIonMass) / 2;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra03mass = 12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 9 - MassDictionary.Proton;
                var fra03int = 5;
                var fra03comment = "[C11H17NO9-H]-"; //306
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = 12 * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 3 - MassDictionary.Proton;
                var fra04int = 5;
                var fra04comment = "[C3H4O3-H]-"; //87
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
            else if (adduct == "[M+NH4]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 1;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                var fra02int = 50;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 400;
                var fra03comment = "[M-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 9);
                var fra04int = 300;
                var fra04comment = "[M-H2O-C11H17NO9+H]+"; // M-H2O-307
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra041mass = fra04mass + MassDictionary.H2OMass;
                var fra041int = 150;
                var fra041comment = "[M-H2O-C11H17NO9+H]+ + H2O"; // M-307 
                fragmentList.Add(fra041mass + "\t" + fra041int + "\t" + fra041comment);

                var fra05mass = fra04mass - MassDictionary.C6H10O5;
                var fra05int = 500;
                var fra05comment = "[M-H2O-C11H15NO7+H]+ - sugar";// M-H2O-307-1sugar
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra051mass = fra05mass + MassDictionary.H2OMass;
                var fra051int = 250;
                var fra051comment = "[M-H2O-C11H17NO9+H]+ - sugar + H2O"; //  M-307-1sugar
                fragmentList.Add(fra051mass + "\t" + fra051int + "\t" + fra051comment);

                var fra06mass = fra05mass - MassDictionary.C6H10O5;
                var fra06int = 999;
                var fra06comment = "[M-H2O-C11H15NO7+H]+ - 2sugars";// M-H2O-307-2sugars
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra061mass = fra06mass - MassDictionary.H2OMass;
                var fra061int = 150;
                var fra061comment = "[M-H2O-C11H15NO7+H]+ - 2sugars-H2O";//
                fragmentList.Add(fra061mass + "\t" + fra061int + "\t" + fra061comment);

                var fra062mass = fra06mass + MassDictionary.H2OMass;
                var fra062int = 150;
                var fra062comment = "[M-H2O-C11H15NO7+H]+ - 2sugars+H2O";// 
                fragmentList.Add(fra062mass + "\t" + fra062int + "\t" + fra062comment);

                var fra11mass = sphMass - MassDictionary.H2OMass + MassDictionary.Proton;
                var fra11int = 100;
                var fra11comment = "[Sph-H2O+H]+";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

                var fra12mass = fra11mass - MassDictionary.H2OMass;
                var fra12int = 200;
                var fra12comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);
            }
            //else if (adduct == "[M+H]+")
            //{
            //    var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
            //    var fra01int = 50;
            //    var fra01comment = adduct;
            //    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

            //    var fra02mass = fra01mass - MassDictionary.HydrogenMass *4;
            //    var fra02int = 999;
            //    var fra02comment = "M - 4";
            //    fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

            //    var fra03mass = fra01mass - MassDictionary.H2OMass;
            //    var fra03int = 400;
            //    var fra03comment = "[M-H2O+H]+";
            //    fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

            //    var fra04mass = fra03mass - (12 * 11 + MassDictionary.HydrogenMass * 17 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 9);
            //    var fra04int = 50;
            //    var fra04comment = "[M-H2O-C11H17NO9+H]+"; // M-H2O-307
            //    fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            //    var fra041mass = fra04mass + MassDictionary.OxygenMass - MassDictionary.HydrogenMass * 2;
            //    var fra041int = 200;
            //    var fra041comment = "";//
            //    fragmentList.Add(fra041mass + "\t" + fra041int + "\t" + fra041comment);

            //    var fra05mass = fra04mass - MassDictionary.C6H10O5;
            //    var fra05int = 200;
            //    var fra05comment = "[M-H2O-C11H15NO7+H]+ - sugar";// M-H2O-307-1sugar
            //    fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

            //    var fra051mass = fra05mass +MassDictionary.H2OMass;
            //    var fra051int = 200;
            //    var fra051comment = "";// 
            //    fragmentList.Add(fra051mass + "\t" + fra051int + "\t" + fra051comment);

            //    var fra06mass = fra05mass - MassDictionary.C6H10O5;
            //    var fra06int = 300;
            //    var fra06comment = "[M-H2O-C11H15NO7+H]+ - 2sugars";// M-H2O-307-2sugars
            //    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            //    var fra061mass = fra06mass + MassDictionary.H2OMass;
            //    var fra061int = 200;
            //    var fra061comment = "";// 
            //    fragmentList.Add(fra061mass + "\t" + fra061int + "\t" + fra061comment);

            //    //var fra11mass = sphMass - MassDictionary.H2OMass + MassDictionary.Proton;
            //    //var fra11int = 100;
            //    //var fra11comment = "[Sph-H2O+H]+";
            //    //fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

            //    var fra12mass = sphMass - MassDictionary.H2OMass* 2 + MassDictionary.Proton;
            //    var fra12int = 150;
            //    var fra12comment = "[Sph-2H2O+H]+";
            //    fragmentList.Add(fra12mass + "\t" + fra12int + "\t" + fra12comment);
            //}

        }

        //

        public static void SmFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            // {   "SM" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+Na]+"  }
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
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
                //var fra02int = 999;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 12 - MassDictionary.HydrogenMass * 2;
                var fra03int = 999;
                var fra03comment = "[M-CH3]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - acylMass - 12 - MassDictionary.HydrogenMass * 2;
                var fra04int = 10;
                var fra04comment = "Sph-C-H";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 4 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra05int = 200;
                var fra05comment = "[C4H11NO4P]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra06int = 50;
                var fra06comment = "[PO3]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 200;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 10;
                var fra02comment = "[M-H2O+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[C5H15NO4P]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = sphMass + MassDictionary.Proton - 2 * MassDictionary.H2OMass;
                var fra04int = 5;
                var fra04comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }

            else if (adduct == "[M+Na]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - 12 * 3 - MassDictionary.HydrogenMass * 9 - MassDictionary.NitrogenMass;
                var fra02int = 800;
                var fra02comment = "[M-C3H9N+Na]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - (12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass);
                var fra03int = 999;
                var fra03comment = "[M-C5H14NO4P+Na]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = exactMass - (12 * 5 + MassDictionary.HydrogenMass * 16 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 5 + MassDictionary.PhosphorusMass) + MassDictionary.Proton;
                var fra04int = 200;
                var fra04comment = "[M-C5H16NO5P+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }


        }

        public static void SmAddOFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "SM+O" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-" }    },
            var sphMass = sphingo3OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
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
                //var fra02int = 999;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 12 - MassDictionary.HydrogenMass * 2;
                var fra03int = 999;
                var fra03comment = "[M-CH3]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra05mass = 12 * 4 + MassDictionary.HydrogenMass * 12 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra05int = 100;
                var fra05comment = "[C4H11NO4P]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra06int = 50;
                var fra06comment = "[PO3]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);


            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 700;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 500;
                var fra02comment = "[M-H2O+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[C5H15NO4P]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

            }

        }

        public static void sulfonolipidFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "SL" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
            var sphMass = sphingo1OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra01int = 999;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass * 2 - 12 - MassDictionary.OxygenMass * 2 - MassDictionary.SulfurMass;
                var fra02int = 5;
                var fra02comment = "M-H2O-CH2O4S";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - acylMass;// + MassDictionary.HydrogenMass * 2;
                var fra03int = 50;
                var fra03comment = "SN2 acyl loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 3;
                var fra04int = 50;
                var fra04comment = "SN2 loss -NH3";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = MassDictionary.OxygenMass * 3 + MassDictionary.SulfurMass + MassDictionary.Electron;
                var fra05int = 5;
                var fra05comment = "SO3-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = MassDictionary.HydrogenMass + MassDictionary.OxygenMass * 3 + MassDictionary.SulfurMass + MassDictionary.Electron;
                var fra06int = 5;
                var fra06comment = "HSO3-";
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
                    fra01int = 50;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }

                var fra02mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                var fra02int = 50;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 150;
                var fra03comment = "[M-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - acylMass;// + MassDictionary.HydrogenMass * 2;
                var fra04int = 500;
                var fra04comment = "SN2 loss";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass; ;
                var fra05int = 999;
                var fra05comment = "SN2 loss-H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra02mass - sphMass + 12 * 2 + MassDictionary.HydrogenMass * 9 + MassDictionary.NitrogenMass;
                var fra06int = 200;
                var fra06comment = "[M-Sph + C2H8N + H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - MassDictionary.H2OMass;
                var fra07int = 500;
                var fra07comment = "[M-Sph + C2H8N + H]+ - H2O";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = 12 * 2 + MassDictionary.HydrogenMass * 6 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 3 + MassDictionary.SulfurMass - MassDictionary.Electron;
                var fra08int = 200;
                var fra08comment = "[C2H6NO3S]+";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }
        }

        public static void sladdOFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "SL+O" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
            var sphMass = sphingo1OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra01int = 300;
                var fra01comment = "[M-H]- Precursor Mass";
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - acylMass;
                var fra02int = 999;
                var fra02comment = "SN2 acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra05mass = MassDictionary.OxygenMass * 3 + MassDictionary.SulfurMass + MassDictionary.Electron;
                var fra05int = 5;
                var fra05comment = "SO3-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = MassDictionary.HydrogenMass + MassDictionary.OxygenMass * 3 + MassDictionary.SulfurMass + MassDictionary.Electron;
                var fra06int = 5;
                var fra06comment = "HSO3-";
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
                    fra01int = 50;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                var fra02int = 100;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "[M-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - acylMass - MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "SN2 loss-H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = acylMass + MassDictionary.Proton;
                var fra05int = 600;
                var fra05comment = "SN2+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra08mass = 12 * 2 + MassDictionary.HydrogenMass * 6 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 3 + MassDictionary.SulfurMass - MassDictionary.Electron;
                var fra08int = 200;
                var fra08comment = "[C2H6NO3S]";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
            }
        }

        public static void SHexCerFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "SHexCer" ,    new List<string>(){ "[M+H]+", "[M-H]-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = MassDictionary.HydrogenMass * 2 + MassDictionary.OxygenMass * 4 + MassDictionary.SulfurMass - MassDictionary.Proton;
                var fra03int = 10;
                var fra03comment = "[H2SO4-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 0;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.OxygenMass * 3 - MassDictionary.SulfurMass;
                var fra02int = 150;
                var fra02comment = "[M-SO3+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "[M-H2SO4+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra01mass - MassDictionary.C6H10O5 - MassDictionary.OxygenMass * 3 - MassDictionary.SulfurMass;
                var fra04int = 100;
                var fra04comment = "[M-C6H10O8S+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 450;
                var fra05comment = "[M-C6H12O9S+H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 400;
                var fra06comment = "[M-C6H12O9S-H2O+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra07int = 300;
                var fra07comment = "[Sph-H2O+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra07mass - MassDictionary.H2OMass;
                var fra08int = 800;
                var fra08comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra08mass - 12;
                var fra09int = 250;
                var fra09comment = "[Sph-CH4O2+H]+";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = acylMass + MassDictionary.Proton + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass;
                var fra10int = 250;
                var fra10comment = "[FAA+H]+";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

                var fra11mass = fra01mass - acylMass - MassDictionary.OxygenMass * 4 - MassDictionary.SulfurMass;
                var fra11int = 200;
                var fra11comment = "[M-FA-H2SO4+H]+";
                fragmentList.Add(fra11mass + "\t" + fra11int + "\t" + fra11comment);

            }

        }

        public static void SHexCerAddOFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "SHexCer+O" ,    new List<string>(){ "[M-H]-", "[M+H]+", "[M+NH4]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = MassDictionary.HydrogenMass * 2 + MassDictionary.OxygenMass * 4 + MassDictionary.SulfurMass - MassDictionary.Proton;
                var fra03int = 5;
                var fra03comment = "[H2SO4-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
            else if (adduct == "[M+H]+" || adduct == "[M+NH4]+")
            {
                var fra01mass = 0.0;
                //var fra01int = 0;
                //var fra01comment = "";
                if (adduct == "[M+NH4]+")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    //fra01int = 50;
                    //fra01comment = adduct;
                    //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                var fra02int = adduct == "[M+NH4]+" ? 100 : 400;
                var fra02comment = "[M+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.OxygenMass * 3 - MassDictionary.SulfurMass - MassDictionary.H2OMass;
                var fra03int = adduct == "[M+NH4]+" ? 999 : 400;
                var fra03comment = "[M-H2SO4+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - MassDictionary.C6H10O5 - MassDictionary.OxygenMass * 3 - MassDictionary.SulfurMass - MassDictionary.H2OMass;
                var fra04int = adduct == "[M+NH4]+" ? 600 : 500;
                var fra04comment = "[M-C6H10O8S-H2O+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 600;
                var fra05comment = "[M-C6H10O8S-2H2O+H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra06int = 150;
                var fra06comment = "[Sph-H2O+H]+";

                var fra07mass = fra06mass - MassDictionary.H2OMass;
                var fra07int = adduct == "[M+NH4]+" ? 500 : 999;
                var fra07comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra07mass - 12;
                var fra08int = 100;
                var fra08comment = "[Sph-CH4O2+H]+";
                if (adduct == "[M+H]+")
                {
                    fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
                    fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);
                }

            }
        }

        public static void peCerdFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "PECer_d" ,    new List<string>(){ "[M-H]-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 10;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - acylMass;
                var fra02int = 10;
                var fra02comment = "acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra03int = 300;
                var fra03comment = "[C2H8NO4P-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra04int = 150;
                var fra04comment = "PO3-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

            }

        }
        public static void peCerdAddOFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "PECer_d+O" ,    new List<string>(){ "[M-H]-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 10;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - acylMass;
                var fra02int = 10;
                var fra02comment = "acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 2 + MassDictionary.HydrogenMass * 8 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass - MassDictionary.Proton;
                var fra03int = 300;
                var fra03comment = "[C2H8NO4P-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra04int = 150;
                var fra04comment = "PO3-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);
            }
        }
        public static void piCerdAddOFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "PICer_d+O" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.C6H10O5;
                var fra02int = 5;
                var fra02comment = "NL of C6H10O5";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - acylMass;
                var fra03int = 5;
                var fra03comment = "NL of Acyl";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = MassDictionary.C6H10O5 + MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra04int = 150;
                var fra04comment = "C6H10O8P-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra05int = 200;
                var fra05comment = "PO3-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.C6H10O5 - (MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.HydrogenMass * 3);
                var fra02int = 999;
                var fra02comment = "NL of C6H13O9P";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
        }

        public static void piCerFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{   "PICer_d+O" ,    new List<string>(){ "[M-H]-", "[M+H]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            if (adduct == "[M-H]-")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 999;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.C6H10O5;
                var fra02int = 5;
                var fra02comment = "NL of C6H10O5";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - acylMass;
                var fra03int = 5;
                var fra03comment = "NL of Acyl";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = MassDictionary.C6H10O5 + MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra04int = 150;
                var fra04comment = "C6H10O8P-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = MassDictionary.OxygenMass * 3 + MassDictionary.PhosphorusMass + MassDictionary.Electron;
                var fra05int = 200;
                var fra05comment = "PO3-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 5;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.C6H10O5 - (MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.HydrogenMass * 3);
                var fra02int = 999;
                var fra02comment = "NL of C6H13O9P";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);
            }
        }

        //3 chains
        public static void asmFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx, int extraAcylCarbon, int extraAcylDouble, int extraAcylOx)
        {
            //{ "ASM" ,    new List<string>() { "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            var extraAcylMass = acylMassCalc(extraAcylCarbon, extraAcylDouble, extraAcylOx);
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
                //var fra02int = 999;
                //var fra02comment = "[M-H]-";
                //fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - 12 - MassDictionary.HydrogenMass * 2;
                var fra03int = 999;
                var fra03comment = "[M-CH3]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = extraAcylMass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 125;
                var fra04comment = "[FA-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra03mass - extraAcylMass - MassDictionary.H2OMass;
                var fra05int = 150;
                var fra05comment = "M-CH3-FA";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 4 + MassDictionary.HydrogenMass * 10 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra06int = 100;
                var fra06comment = "[C4H11NO4P]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
            else if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - extraAcylMass - MassDictionary.H2OMass;
                var fra02int = 10;
                var fra02comment = "ext Acyl loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = 12 * 5 + MassDictionary.HydrogenMass * 14 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass * 4 + MassDictionary.PhosphorusMass + MassDictionary.Proton;
                var fra03int = 999;
                var fra03comment = "[C5H15NO4P]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);
            }
        }


        public static void cerEbdsFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx, int extraAcylCarbon, int extraAcylDouble, int extraAcylOx)
        {
            //   {   "Cer_EBDS" ,    new List<string>(){ "[M+HCOO]-", "[M+CH3COO]-", "[M+H-H2O]+", "[M-H]-" }    }
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            var extraAcylMass = acylMassCalc(extraAcylCarbon, extraAcylDouble, extraAcylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 5;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 50;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = extraAcylMass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "[ExtraFA-H]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - fra03mass - MassDictionary.HydrogenMass;
                var fra04int = 70;
                var fra04comment = "[NLofExtraFA-3H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass + MassDictionary.H2OMass;
                var fra05int = 20;
                var fra05comment = "[NLofExtraFA-3H+H2O]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra02mass - extraAcylMass - sphMass + 12 * 3 + MassDictionary.HydrogenMass * 9 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass;
                var fra06int = 60;
                var fra06comment = "[M-ExtraFA-Sph+C3H7NO2]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

            }

        }

        public static void acylHexCerFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx, int extraAcylCarbon, int extraAcylDouble, int extraAcylOx)
        {
            //{   "AHexCer" , new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+H-H2O]+", "[M-H]-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            var extraAcylMass = acylMassCalc(extraAcylCarbon, extraAcylDouble, extraAcylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
            {
                var fra01mass = 0.0;
                //var fra01int = 0;
                //var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    //fra01int = 0;
                    //fra01comment = adduct;
                    //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - extraAcylMass;
                var fra03int = 200;
                var fra03comment = "[M-FA]-";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 175;
                var fra04comment = "[M-FA-H2O]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - sphMass + 12 * 2 + MassDictionary.HydrogenMass * 7 + MassDictionary.NitrogenMass + MassDictionary.OxygenMass;
                var fra05int = 200;
                var fra05comment = "[M-Sph+C2H6NO]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra02mass - sphMass + 12 * 2 + MassDictionary.HydrogenMass * 5 + MassDictionary.NitrogenMass;
                var fra06int = 175;
                var fra06comment = "[M-Sph+C2H4N]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra03mass - MassDictionary.C6H10O5 - MassDictionary.H2OMass;
                var fra07int = 150;
                var fra07comment = "[Cer-H2O]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra03mass - MassDictionary.C6H10O5;
                var fra08int = 20;
                var fra08comment = "Cer-";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = extraAcylMass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra09int = 100;
                var fra09comment = "[FA-H]-";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

            }
            else if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                var fra01int = 150;
                var fra01comment = adduct;
                if (adduct == "[M+H]+")
                {
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 999;
                var fra02comment = "H2O loss";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra01mass - extraAcylMass - MassDictionary.C6H10O5;
                var fra03int = 175;
                var fra03comment = "Acyl and Hex loss";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 150;
                var fra04comment = "Acyl and Hex loss - H2O";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 150;
                var fra05comment = "Acyl and Hex loss - 2H2O";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = extraAcylMass + MassDictionary.C6H10O5 + MassDictionary.Proton;
                var fra06int = 200;
                var fra06comment = "Acyl and Hex";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra07int = 100;
                var fra07comment = "sph";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);

                var fra08mass = fra07mass - MassDictionary.H2OMass;
                var fra08int = 200;
                var fra08comment = "sph-H2O";
                fragmentList.Add(fra08mass + "\t" + fra08int + "\t" + fra08comment);

                var fra09mass = fra07mass - 12 * 2 - MassDictionary.NitrogenMass - MassDictionary.HydrogenMass * 5;
                var fra09int = 150;
                var fra09comment = "sph-C2H5N";
                fragmentList.Add(fra09mass + "\t" + fra09int + "\t" + fra09comment);

                var fra10mass = fra07mass - 12 - MassDictionary.OxygenMass - MassDictionary.HydrogenMass * 2;
                var fra10int = 50;
                var fra10comment = "sph-CH2O";
                fragmentList.Add(fra10mass + "\t" + fra10int + "\t" + fra10comment);

            }

        }
        public static void cerEodsFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx, int extraAcylCarbon, int extraAcylDouble, int extraAcylOx)
        {
            //    { "Cer_EODS" ,    new List<string>() { "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            var extraAcylMass = acylMassCalc(extraAcylCarbon, extraAcylDouble, extraAcylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 50;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 200;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - extraAcylMass;
                var fra03int = 100;
                var fra03comment = "NL of EFA";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = extraAcylMass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "[EFA-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = acylMass + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 3 - MassDictionary.Proton;
                var fra05int = 50;
                var fra05comment = "[FAA-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass + 12 * 2 + MassDictionary.HydrogenMass * 3 - MassDictionary.Proton;
                var fra06int = 50;
                var fra06comment = "[FAA+C2H2-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = sphMass - 12 * 2 - MassDictionary.HydrogenMass * 7 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra07int = 50;
                var fra07comment = "[Sph-C2H8NO]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);


            }

        }

        public static void cerEosFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx, int extraAcylCarbon, int extraAcylDouble, int extraAcylOx)
        {
            // {   "Cer_EOS" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+H]+", "[M+CH3COO]-", "[M+H-H2O]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            var extraAcylMass = acylMassCalc(extraAcylCarbon, extraAcylDouble, extraAcylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 50;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 200;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - extraAcylMass;
                var fra03int = 100;
                var fra03comment = "NL of EFA";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = extraAcylMass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 999;
                var fra04comment = "[EFA-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = acylMass + MassDictionary.NitrogenMass + MassDictionary.HydrogenMass * 3 - MassDictionary.Proton;
                var fra05int = 50;
                var fra05comment = "[FAA-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass + 12 * 2 + MassDictionary.HydrogenMass * 3 - MassDictionary.Proton;
                var fra06int = 50;
                var fra06comment = "[FAA+C2H2-H]-";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = sphMass - 12 * 2 - MassDictionary.HydrogenMass * 7 - MassDictionary.NitrogenMass - MassDictionary.OxygenMass - MassDictionary.Proton;
                var fra07int = 50;
                var fra07comment = "[Sph-C2H8NO]-";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }

            else if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                var fra01int = 300;
                var fra01comment = adduct;
                if (adduct == "[M+H]+")
                {
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 999;
                var fra02comment = "[M-H2O+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 400;
                var fra03comment = "[M-2H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra04int = 200;
                var fra04comment = "[Sph-H2O+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 999;
                var fra05comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - 12;
                var fra06int = 200;
                var fra06comment = "[Sph-CH4O2+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }

        public static void hexCerEosFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx, int extraAcylCarbon, int extraAcylDouble, int extraAcylOx)
        {
            //    {   "HexCer_EOS" ,    new List<string>(){ "[M+HCOO]-", "[M+H]+", "[M+CH3COO]-", "[M+H-H2O]+", "[M-H]-" }     },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            var acylMass = acylMassCalc(acylCarbon, acylDouble, acylOx);
            var extraAcylMass = acylMassCalc(extraAcylCarbon, extraAcylDouble, extraAcylOx);
            if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-" || adduct == "[M-H]-")
            {
                var fra01mass = 0.0;
                var fra01int = 0;
                var fra01comment = "";
                if (adduct == "[M+HCOO]-" || adduct == "[M+CH3COO]-")
                {
                    fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                    fra01int = 50;
                    fra01comment = adduct;
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }
                var fra02mass = exactMass + adductDic.adductIonDic["[M-H]-"].AdductIonMass;
                var fra02int = 999;
                var fra02comment = "[M-H]-";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - extraAcylMass;
                var fra03int = 100;
                var fra03comment = "NL of EFA";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = extraAcylMass - MassDictionary.Proton + MassDictionary.H2OMass;
                var fra04int = 50;
                var fra04comment = "[EFA-H]-";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra02mass - MassDictionary.C6H10O5;
                var fra05int = 50;
                var fra05comment = "[M-C6H10O5-H]-";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra03mass - MassDictionary.H2OMass;
                var fra06int = 500;
                var fra06comment = "NL of EFA+H2O";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra03mass - MassDictionary.C6H10O5;
                var fra07int = 50;
                var fra07comment = "NL of EFA+C6H10O5";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }
            else if (adduct == "[M+H]+" || adduct == "[M+H-H2O]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic["[M+H]+"].AdductIonMass;
                var fra01int = 50;
                var fra01comment = adduct;
                if (adduct == "[M+H]+")
                {
                    fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
                }

                var fra02mass = fra01mass - MassDictionary.C6H10O5;
                var fra02int = 500;
                var fra02comment = "[M-C6H10O5+H]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 999;
                var fra03comment = "[M-Hex-H2O+H]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - MassDictionary.H2OMass;
                var fra04int = 300;
                var fra04comment = "[M-Hex-2H2O+H]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = sphMass + MassDictionary.Proton - MassDictionary.H2OMass;
                var fra05int = 200;
                var fra05comment = "[Sph-H2O+H]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = fra05mass - MassDictionary.H2OMass;
                var fra06int = 999;
                var fra06comment = "[Sph-2H2O+H]+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                var fra07mass = fra06mass - 12;
                var fra07int = 200;
                var fra07comment = "[Sph-CH4O2+H]+";
                fragmentList.Add(fra07mass + "\t" + fra07int + "\t" + fra07comment);
            }

        }

        public static void sphingosineFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble)
        {
            //{ "Sph" ,    new List<string>() { "[M+H]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 50;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 999;
                var fra02comment = "[M+H-H2O]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 400;
                var fra03comment = "[M+H-2H2O]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - 12;
                var fra04int = 500;
                var fra04comment = "[M+H-H2O-CH2O]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 7 + MassDictionary.HydrogenMass * 8 + MassDictionary.Proton;
                var fra05int = 300;
                var fra05comment = "C7H9+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 6 + MassDictionary.HydrogenMass * 6 + MassDictionary.Proton;
                var fra06int = 500;
                var fra06comment = "C6H7+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }

        public static void sphinganineFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble)
        {
            //    { "DHSph" ,    new List<string>() { "[M+H]+" }    },
            var sphMass = sphingo2OHMassCalc(sphCarbon, sphDouble);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 50;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass;
                var fra02int = 400;
                var fra02comment = "[M+H-H2O]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 300;
                var fra03comment = "[M+H-2H2O]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra03mass - 12;
                var fra04int = 999;
                var fra04comment = "[M+H-H2O-CH2O]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = 12 * 7 + MassDictionary.HydrogenMass * 10 + MassDictionary.Proton;
                var fra05int = 550;
                var fra05comment = "C7H11+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                var fra06mass = 12 * 6 + MassDictionary.HydrogenMass * 8 + MassDictionary.Proton;
                var fra06int = 600;
                var fra06comment = "C6H9+";
                fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);
            }
        }

        public static void phytosphingosineFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble)
        {
            //    { "PhytoSph" ,    new List<string>() { "[M+H]+" }    },
            var sphMass = sphingo3OHMassCalc(sphCarbon, sphDouble);
            if (adduct == "[M+H]+")
            {
                var fra01mass = exactMass + adductDic.adductIonDic[adduct].AdductIonMass;
                //var fra01int = 50;
                //var fra01comment = adduct;
                //fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                var fra02mass = fra01mass - MassDictionary.H2OMass * 2;
                var fra02int = 999;
                var fra02comment = "[M+H-2H2O]+";
                fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                var fra03mass = fra02mass - MassDictionary.H2OMass;
                var fra03int = 500;
                var fra03comment = "[M+H-3H2O]+";
                fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                var fra04mass = fra02mass - 12;
                var fra04int = 999;
                var fra04comment = "[M+H-H2O-CH2O]+";
                fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                var fra05mass = fra04mass - MassDictionary.H2OMass;
                var fra05int = 500;
                var fra05comment = "[M+H-2H2O-CH2O]+";
                fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);
            }
        }

        public static double sphingo2OHMassCalc(int sphCarbon, int sphDouble)
        {
            var sphMass = sphCarbon * 12 + (2 * sphCarbon - 2 * sphDouble + 3) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass * 2 + MassDictionary.NitrogenMass;
            return sphMass;
        }
        public static double sphingo3OHMassCalc(int sphCarbon, int sphDouble)
        {
            var sphMass = sphCarbon * 12 + (2 * sphCarbon - 2 * sphDouble + 3) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass * 3 + MassDictionary.NitrogenMass;
            return sphMass;
        }
        public static double sphingo1OHMassCalc(int sphCarbon, int sphDouble)
        {
            var sphMass = sphCarbon * 12 + (2 * sphCarbon - 2 * sphDouble + 3) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass + MassDictionary.NitrogenMass;
            return sphMass;
        }
        public static double acylMassCalc(int acylCarbon, int acylDouble, int acylOx)
        {
            var acylMass = acylCarbon * 12 + (2 * acylCarbon - 2 * acylDouble - 2) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass + (MassDictionary.OxygenMass * acylOx);
            return acylMass;
        }



        //template
        public static void CerXXFragment(List<string> fragmentList, string adduct, double exactMass, int sphCarbon, int sphDouble, int acylCarbon, int acylDouble, int acylOx)
        {
            //{ "Cer_**" ,    new List<string>(){ "[M+HCOO]-", "[M-H]-", "[M+H]+", "[M+CH3COO]-" }    },
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
                var fra01int = 50;
                var fra01comment = adduct;
                fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);
            }

        }
        //template end

    }
}
