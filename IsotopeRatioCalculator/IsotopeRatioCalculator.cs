using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This class is the old version of IsotopeCalculator.cs of MolecularFormulaFinder assembly.
    /// This class will be used to calculate the theoretical isotopic abundances from formula string.
    /// 
    /// This class is still used in MS-DIAL program but please use new version as MS-FINDER program uses.
    /// </summary>
    public sealed class IsotopeRatioCalculator
    {
        private IsotopeRatioCalculator() { }

        /// <summary>
        /// This method is a kind of parcer to get IupacReferenceBean from the file path including iupac information (in this program, iupac.txt).
        /// </summary>
        /// <param name="filepath">Put the file path of iupac.txt</param>
        /// <returns>IupacReferenceBean will be returned.</returns>
        public static IupacReferenceBean GetIupacReference(string filepath)
        {
            int iupacID = 0;
            string elementName = "";
            string line;
            string[] lineArray;

            IupacReferenceBean iupacReferenceBean = new IupacReferenceBean();
            List<IupacElementPropertyBean> iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
            IupacElementPropertyBean iupacElementPropertyBean = new IupacElementPropertyBean();

            using (StreamReader sr = new StreamReader(filepath))
            {
                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    line = sr.ReadLine();
                    if (line == string.Empty) break;

                    lineArray = line.Split('\t');

                    if (iupacID != int.Parse(lineArray[0]))
                    {
                        if (iupacID != 0) { iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList; iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList; }

                        iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
                        iupacID = int.Parse(lineArray[0]);
                        elementName = lineArray[1];

                        iupacElementPropertyBean = new IupacElementPropertyBean();
                        iupacElementPropertyBean.AccurateMass = double.Parse(lineArray[4]);
                        iupacElementPropertyBean.ElementName = elementName;
                        iupacElementPropertyBean.IupacID = iupacID;
                        iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                        iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                        iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                    }
                    else
                    {
                        iupacElementPropertyBean = new IupacElementPropertyBean();
                        iupacElementPropertyBean.AccurateMass = double.Parse(lineArray[4]);
                        iupacElementPropertyBean.ElementName = elementName;
                        iupacElementPropertyBean.IupacID = iupacID;
                        iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                        iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                        iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                    }
                }
                //reminder
                iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList;
                iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList;
            }

            return iupacReferenceBean;
        }

        /// <summary>
        /// This method calculate the theoretical isotopic abundances with the exact m/z from the molecular formula string such as C6H12O6.
        /// </summary>
        /// <param name="elementName">Put the formula string.</param>
        /// <param name="massFilter">Put the integar value that you want ot calculate until the isotopic value. Ex. if you put 3, this method calculate the isotopic abundances until M+3. </param>
        /// <param name="iupacReferenceBean">Put the iupac reference bean which can be retrived with GetIupacReference method.</param>
        /// <returns>This program returns the theoretical isotopic abundances with the exact m/z values.</returns>
        public static CompoundPropertyBean GetAccurateIsotopeProperty(string elementName, int massFilter, IupacReferenceBean iupacReferenceBean)
        {
            CompoundPropertyBean compoundPropertyBean = new CompoundPropertyBean();
            compoundPropertyBean.ElementName = elementName;
            compoundPropertyBean.ElementProfile = GetBasicCompoundElementProfile(elementName);

            if (compoundPropertyBean.ElementProfile == null) return null;

            setIupacReferenceInformation(compoundPropertyBean, iupacReferenceBean);

            if (compoundPropertyBean.ElementProfile == null) return null;

            setAccurateIsotopePropertyInformation(compoundPropertyBean, massFilter);
            setFinalAccurateIsotopeProfile(compoundPropertyBean, massFilter);

            return compoundPropertyBean;
        }

        /// <summary>
        /// This method calculate the theoretical isotopic abundances with the nominal m/z from the molecular formula string such as C6H12O6.
        /// </summary>
        /// <param name="elementName">Put the formula string.</param>
        /// <param name="massFilter">Put the integar value that you want ot calculate until the isotopic value. Ex. if you put 3, this method calculate the isotopic abundances until M+3. </param>
        /// <param name="iupacReferenceBean">Put the iupac reference bean which can be retrived with GetIupacReference method.</param>
        /// <returns>This program returns the theoretical isotopic abundances with the nominal m/z values.</returns>
        public static CompoundPropertyBean GetNominalIsotopeProperty(string elementName, int massFilter, IupacReferenceBean iupacReferenceBean)
        {
            CompoundPropertyBean compoundPropertyBean = new CompoundPropertyBean();
            compoundPropertyBean.ElementName = elementName;
            compoundPropertyBean.ElementProfile = GetBasicCompoundElementProfile(elementName);

            if (compoundPropertyBean.ElementProfile == null) return null;

            setIupacReferenceInformation(compoundPropertyBean, iupacReferenceBean);

            if (compoundPropertyBean.ElementProfile == null) return null;

            setNominalIsotopePropertyInformation(compoundPropertyBean, massFilter);
            setFinalNominalIsotopeProfile(compoundPropertyBean, massFilter);

            return compoundPropertyBean;
        }

        public static List<ElementPropertyBean> GetBasicCompoundElementProfile(string formula)
        {
            List<ElementPropertyBean> elementProfileList = new List<ElementPropertyBean>();
            ElementPropertyBean elementPropertyBean = new ElementPropertyBean();
            MatchCollection mc;

            if (char.IsNumber(formula[0]))
            {
                Console.WriteLine("The element composition name format is incorrect.");
                return null;
            }

            mc = Regex.Matches(formula, "C(?!a|d|e|l|o|r|s|u)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                elementPropertyBean = new ElementPropertyBean();
                elementPropertyBean.ElementName = "C";
                if (mc[0].Groups[1].Value == string.Empty) elementPropertyBean.ElementNumber = 1;
                else elementPropertyBean.ElementNumber = int.Parse(mc[0].Groups[1].Value);
                elementProfileList.Add(elementPropertyBean);
            }

            mc = Regex.Matches(formula, "H(?!e|f|g|o)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                elementPropertyBean = new ElementPropertyBean();
                elementPropertyBean.ElementName = "H";
                if (mc[0].Groups[1].Value == string.Empty) elementPropertyBean.ElementNumber = 1;
                else elementPropertyBean.ElementNumber = int.Parse(mc[0].Groups[1].Value);
                elementProfileList.Add(elementPropertyBean);
            }

            mc = Regex.Matches(formula, "N(?!a|b|d|e|i)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                elementPropertyBean = new ElementPropertyBean();
                elementPropertyBean.ElementName = "N";
                if (mc[0].Groups[1].Value == string.Empty) elementPropertyBean.ElementNumber = 1;
                else elementPropertyBean.ElementNumber = int.Parse(mc[0].Groups[1].Value);
                elementProfileList.Add(elementPropertyBean);
            }

            mc = Regex.Matches(formula, "O(?!s)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                elementPropertyBean = new ElementPropertyBean();
                elementPropertyBean.ElementName = "O";
                if (mc[0].Groups[1].Value == string.Empty) elementPropertyBean.ElementNumber = 1;
                else elementPropertyBean.ElementNumber = int.Parse(mc[0].Groups[1].Value);
                elementProfileList.Add(elementPropertyBean);
            }

            mc = Regex.Matches(formula, "S(?!b|c|e|i|m|n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                elementPropertyBean = new ElementPropertyBean();
                elementPropertyBean.ElementName = "S";
                if (mc[0].Groups[1].Value == string.Empty) elementPropertyBean.ElementNumber = 1;
                else elementPropertyBean.ElementNumber = int.Parse(mc[0].Groups[1].Value);
                elementProfileList.Add(elementPropertyBean);
            }

            mc = Regex.Matches(formula, "Br([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                elementPropertyBean = new ElementPropertyBean();
                elementPropertyBean.ElementName = "Br";
                if (mc[0].Groups[1].Value == string.Empty) elementPropertyBean.ElementNumber = 1;
                else elementPropertyBean.ElementNumber = int.Parse(mc[0].Groups[1].Value);
                elementProfileList.Add(elementPropertyBean);
            }

            mc = Regex.Matches(formula, "Cl([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                elementPropertyBean = new ElementPropertyBean();
                elementPropertyBean.ElementName = "Cl";
                if (mc[0].Groups[1].Value == string.Empty) elementPropertyBean.ElementNumber = 1;
                else elementPropertyBean.ElementNumber = int.Parse(mc[0].Groups[1].Value);
                elementProfileList.Add(elementPropertyBean);
            }

            mc = Regex.Matches(formula, "F(?!e)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                elementPropertyBean = new ElementPropertyBean();
                elementPropertyBean.ElementName = "F";
                if (mc[0].Groups[1].Value == string.Empty) elementPropertyBean.ElementNumber = 1;
                else elementPropertyBean.ElementNumber = int.Parse(mc[0].Groups[1].Value);
                elementProfileList.Add(elementPropertyBean);
            }

            mc = Regex.Matches(formula, "I(?!n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                elementPropertyBean = new ElementPropertyBean();
                elementPropertyBean.ElementName = "I";
                if (mc[0].Groups[1].Value == string.Empty) elementPropertyBean.ElementNumber = 1;
                else elementPropertyBean.ElementNumber = int.Parse(mc[0].Groups[1].Value);
                elementProfileList.Add(elementPropertyBean);
            }

            if (elementProfileList.Count == 0) return null;

            return elementProfileList;
        }

        private static void setIupacReferenceInformation(CompoundPropertyBean compoundPropertyBean, IupacReferenceBean iupacReferenceBean)
        {
            double accurateMass = 0; ;
            for (int i = 0; i < compoundPropertyBean.ElementProfile.Count; i++)
            {
                if (iupacReferenceBean.ElementName_IupacElementPropertyBeanList.ContainsKey(compoundPropertyBean.ElementProfile[i].ElementName))
                {
                    compoundPropertyBean.ElementProfile[i].IupacElementPropertyBeanList = iupacReferenceBean.ElementName_IupacElementPropertyBeanList[compoundPropertyBean.ElementProfile[i].ElementName];
                    compoundPropertyBean.ElementProfile[i].IupacID = compoundPropertyBean.ElementProfile[i].IupacElementPropertyBeanList[0].IupacID;
                    accurateMass += compoundPropertyBean.ElementProfile[i].IupacElementPropertyBeanList[0].AccurateMass * compoundPropertyBean.ElementProfile[i].ElementNumber;
                }
                else
                {
                    Console.WriteLine(compoundPropertyBean.ElementProfile[i].ElementName + " is not included in IUPAC reference.");
                    compoundPropertyBean.ElementProfile = null;
                    return;
                }
            }
            compoundPropertyBean.AccurateMass = accurateMass;
        }

        private static void setAccurateIsotopePropertyInformation(CompoundPropertyBean compoundPropertyBean, int massFilter)
        {
            for (int i = 0; i < compoundPropertyBean.ElementProfile.Count; i++)
            {
                compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList = getIsotopeElementProperty(compoundPropertyBean.ElementProfile[i].IupacElementPropertyBeanList);
                compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList = getAccurateIsotopeElementProperty(compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList, compoundPropertyBean.ElementProfile[i].ElementNumber, massFilter);
                compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList = compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList.OrderBy(n => n.MassDifferenceFromMonoisotopicIon).ToList();
            }
        }

        private static void setFinalAccurateIsotopeProfile(CompoundPropertyBean compoundPropertyBean, int massFilter)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBean = new List<IsotopeElementPropertyBean>();
            abundanceElementPropertyBean = compoundPropertyBean.ElementProfile[0].IsotopeElementPropertyBeanList;

            for (int i = 1; i < compoundPropertyBean.ElementProfile.Count; i++)
                abundanceElementPropertyBean = getAccurateMultiplatedIsotopeElement(abundanceElementPropertyBean, compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList, massFilter);

            compoundPropertyBean.IsotopeProfile = abundanceElementPropertyBean.OrderBy(n => n.MassDifferenceFromMonoisotopicIon).ToList();

            for (int i = 0; i < compoundPropertyBean.IsotopeProfile.Count; i++)
            {
                compoundPropertyBean.IsotopeProfile[i].MassDifferenceFromMonoisotopicIon += compoundPropertyBean.AccurateMass;
                compoundPropertyBean.IsotopeProfile[i].RelativeAbundance *= 100;
            }
        }

        private static void setNominalIsotopePropertyInformation(CompoundPropertyBean compoundPropertyBean, int massFilter)
        {
            for (int i = 0; i < compoundPropertyBean.ElementProfile.Count; i++)
            {
                compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList = getIsotopeElementProperty(compoundPropertyBean.ElementProfile[i].IupacElementPropertyBeanList);
                compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList = getNominalIsotopeElementProperty(compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList, compoundPropertyBean.ElementProfile[i].ElementNumber, massFilter);
                compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList = compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList.OrderBy(n => n.MassDifferenceFromMonoisotopicIon).ToList();
            }
        }

        private static void setFinalNominalIsotopeProfile(CompoundPropertyBean compoundPropertyBean, int massFilter)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBean = new List<IsotopeElementPropertyBean>();
            abundanceElementPropertyBean = compoundPropertyBean.ElementProfile[0].IsotopeElementPropertyBeanList;

            for (int i = 1; i < compoundPropertyBean.ElementProfile.Count; i++)
                abundanceElementPropertyBean = getNominalMultiplatedisotopeElement(abundanceElementPropertyBean, compoundPropertyBean.ElementProfile[i].IsotopeElementPropertyBeanList, massFilter);

            compoundPropertyBean.IsotopeProfile = abundanceElementPropertyBean.OrderBy(n => n.MassDifferenceFromMonoisotopicIon).ToList();

            for (int i = 0; i < compoundPropertyBean.IsotopeProfile.Count; i++)
            {
                compoundPropertyBean.IsotopeProfile[i].MassDifferenceFromMonoisotopicIon += compoundPropertyBean.AccurateMass;
                compoundPropertyBean.IsotopeProfile[i].RelativeAbundance *= 100;
            }
        }

        private static List<IsotopeElementPropertyBean> getIsotopeElementProperty(List<IupacElementPropertyBean> iupacElementPropertyBeanList)
        {
            List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList = new List<IsotopeElementPropertyBean>();

            double relativeAbundance, massDifference;

            for (int i = 0; i < iupacElementPropertyBeanList.Count; i++)
            {
                relativeAbundance = iupacElementPropertyBeanList[i].NaturalRelativeAbundance / iupacElementPropertyBeanList[0].NaturalRelativeAbundance;
                massDifference = iupacElementPropertyBeanList[i].AccurateMass - iupacElementPropertyBeanList[0].AccurateMass;
                isotopeElementPropertyBeanList.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance, MassDifferenceFromMonoisotopicIon = massDifference });
            }

            return isotopeElementPropertyBeanList;
        }

        private static List<IsotopeElementPropertyBean> getAccurateIsotopeElementProperty(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            if (isotopeElementPropertyBeanList.Count == 1) return isotopeElementPropertyBeanList;

            List<IsotopeElementPropertyBean> accurateIsotopeElementPropertyBeanList = new List<IsotopeElementPropertyBean>();

            switch (isotopeElementPropertyBeanList.Count)
            {
                case 2:
                    accurateIsotopeElementPropertyBeanList = getAllIsotopeElementPropertyForTwoIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 3:
                    accurateIsotopeElementPropertyBeanList = getAllIsotopeElementPropertyForThreeIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 4:
                    accurateIsotopeElementPropertyBeanList = getAllIsotopeElementPropertyForFourIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 5:
                    accurateIsotopeElementPropertyBeanList = getAllIsotopeElementPropertyForFiveIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 6:
                    accurateIsotopeElementPropertyBeanList = getAllIsotopeElementPropertyForSixIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 7:
                    accurateIsotopeElementPropertyBeanList = getAllIsotopeElementPropertyForSevenIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 8:
                    accurateIsotopeElementPropertyBeanList = getAllIsotopeElementPropertyForEightIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 9:
                    accurateIsotopeElementPropertyBeanList = getAllIsotopeElementPropertyForNineIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 10:
                    accurateIsotopeElementPropertyBeanList = getAllIsotopeElementPropertyForTenIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                default:
                    break;
            }
            return accurateIsotopeElementPropertyBeanList;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementProperty(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            if (isotopeElementPropertyBeanList.Count == 1) return isotopeElementPropertyBeanList;

            List<IsotopeElementPropertyBean> nominalIsotopeElementPropertyBeanList = new List<IsotopeElementPropertyBean>();

            switch (isotopeElementPropertyBeanList.Count)
            {
                case 2:
                    nominalIsotopeElementPropertyBeanList = getNominalIsotopeElementPropertyForTwoIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 3:
                    nominalIsotopeElementPropertyBeanList = getNominalIsotopeElementPropertyForThreeIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 4:
                    nominalIsotopeElementPropertyBeanList = getNominalIsotopeElementPropertyForFourIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 5:
                    nominalIsotopeElementPropertyBeanList = getNominalIsotopeElementPropertyForFiveIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 6:
                    nominalIsotopeElementPropertyBeanList = getNominalIsotopeElementPropertyForSixIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 7:
                    nominalIsotopeElementPropertyBeanList = getNominalIsotopeElementPropertyForSevenIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 8:
                    nominalIsotopeElementPropertyBeanList = getNominalIsotopeElementPropertyForEightIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 9:
                    nominalIsotopeElementPropertyBeanList = getNominalIsotopeElementPropertyForNineIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                case 10:
                    nominalIsotopeElementPropertyBeanList = getNominalIsotopeElementPropertyForTenIsotopomerElement(isotopeElementPropertyBeanList, n, filterMass);
                    break;
                default:
                    break;
            }
            return nominalIsotopeElementPropertyBeanList;
        }

        private static List<IsotopeElementPropertyBean> getAllIsotopeElementPropertyForTenIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList8 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList9 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double f_e_relativeAbundance = isotopeElementPropertyBeanList[6].RelativeAbundance / isotopeElementPropertyBeanList[5].RelativeAbundance;
            double f_e_massDifference = isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon;

            double g_f_relativeAbundance = isotopeElementPropertyBeanList[7].RelativeAbundance / isotopeElementPropertyBeanList[6].RelativeAbundance;
            double g_f_massDifference = isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon;

            double h_g_relativeAbundance = isotopeElementPropertyBeanList[8].RelativeAbundance / isotopeElementPropertyBeanList[7].RelativeAbundance;
            double h_g_massDifference = isotopeElementPropertyBeanList[8].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon;

            double i_h_relativeAbundance = isotopeElementPropertyBeanList[9].RelativeAbundance / isotopeElementPropertyBeanList[8].RelativeAbundance;
            double i_h_massDifference = isotopeElementPropertyBeanList[9].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[8].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;
            double relativeAbundance_fe, massDifference_fe;
            double relativeAbundance_gf, massDifference_gf;
            double relativeAbundance_hg, massDifference_hg;
            double relativeAbundance_ih, massDifference_ih;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = a_massDifference * i;

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = b_a_massDifference * j;

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = c_b_massDifference * k;

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = d_c_massDifference * l;

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;

                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = e_d_massDifference * m;

                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;

                                abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();

                                for (int o = 0; o <= m; o++)
                                {
                                    relativeAbundance_fe = (double)BasicMathematics.BinomialCoefficient(m, o) * Math.Pow(f_e_relativeAbundance, o);
                                    massDifference_fe = f_e_massDifference * o;

                                    if (Math.Abs(massDifference_fe) > filterMass - 1) break;

                                    abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();

                                    for (int p = 0; p <= o; p++)
                                    {
                                        relativeAbundance_gf = (double)BasicMathematics.BinomialCoefficient(o, p) * Math.Pow(g_f_relativeAbundance, p);
                                        massDifference_gf = g_f_massDifference * p;

                                        if (Math.Abs(massDifference_gf) > filterMass - 1) break;

                                        abundanceElementPropertyBeanList8 = new List<IsotopeElementPropertyBean>();

                                        for (int q = 0; q <= p; q++)
                                        {
                                            relativeAbundance_hg = (double)BasicMathematics.BinomialCoefficient(p, q) * Math.Pow(h_g_relativeAbundance, q);
                                            massDifference_hg = h_g_massDifference * q;

                                            if (Math.Abs(massDifference_hg) > filterMass - 1) break;

                                            abundanceElementPropertyBeanList9 = new List<IsotopeElementPropertyBean>();

                                            for (int r = 0; r <= q; r++)
                                            {
                                                relativeAbundance_ih = (double)BasicMathematics.BinomialCoefficient(q, r) * Math.Pow(i_h_relativeAbundance, r);
                                                massDifference_ih = i_h_massDifference * r;

                                                if (Math.Abs(massDifference_ih) > filterMass - 1) break;

                                                abundanceElementPropertyBeanList9.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance_ih, MassDifferenceFromMonoisotopicIon = massDifference_ih });
                                            }

                                            abundanceElementPropertyBeanList9 = getAccurateMultiplatedIsotopeElement(relativeAbundance_hg, massDifference_hg, abundanceElementPropertyBeanList9, filterMass);

                                            abundanceElementPropertyBeanList8 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList8, abundanceElementPropertyBeanList9);
                                        }

                                        abundanceElementPropertyBeanList8 = getAccurateMultiplatedIsotopeElement(relativeAbundance_gf, massDifference_gf, abundanceElementPropertyBeanList8, filterMass);

                                        abundanceElementPropertyBeanList7 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList7, abundanceElementPropertyBeanList8);
                                    }


                                    abundanceElementPropertyBeanList7 = getAccurateMultiplatedIsotopeElement(relativeAbundance_fe, massDifference_fe, abundanceElementPropertyBeanList7, filterMass);

                                    abundanceElementPropertyBeanList6 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList6, abundanceElementPropertyBeanList7);
                                }

                                abundanceElementPropertyBeanList6 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ed, massDifference_ed, abundanceElementPropertyBeanList6, filterMass);

                                abundanceElementPropertyBeanList5 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList5, abundanceElementPropertyBeanList6);
                            }

                            abundanceElementPropertyBeanList5 = getAccurateMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getAccurateMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getAccurateMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getAllIsotopeElementPropertyForNineIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList8 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double f_e_relativeAbundance = isotopeElementPropertyBeanList[6].RelativeAbundance / isotopeElementPropertyBeanList[5].RelativeAbundance;
            double f_e_massDifference = isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon;

            double g_f_relativeAbundance = isotopeElementPropertyBeanList[7].RelativeAbundance / isotopeElementPropertyBeanList[6].RelativeAbundance;
            double g_f_massDifference = isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon;

            double h_g_relativeAbundance = isotopeElementPropertyBeanList[8].RelativeAbundance / isotopeElementPropertyBeanList[7].RelativeAbundance;
            double h_g_massDifference = isotopeElementPropertyBeanList[8].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;
            double relativeAbundance_fe, massDifference_fe;
            double relativeAbundance_gf, massDifference_gf;
            double relativeAbundance_hg, massDifference_hg;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = a_massDifference * i;

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = b_a_massDifference * j;

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = c_b_massDifference * k;

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = d_c_massDifference * l;

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;

                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = e_d_massDifference * m;

                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;

                                abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();

                                for (int o = 0; o <= m; o++)
                                {
                                    relativeAbundance_fe = (double)BasicMathematics.BinomialCoefficient(m, o) * Math.Pow(f_e_relativeAbundance, o);
                                    massDifference_fe = f_e_massDifference * o;

                                    if (Math.Abs(massDifference_fe) > filterMass - 1) break;

                                    abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();

                                    for (int p = 0; p <= o; p++)
                                    {
                                        relativeAbundance_gf = (double)BasicMathematics.BinomialCoefficient(o, p) * Math.Pow(g_f_relativeAbundance, p);
                                        massDifference_gf = g_f_massDifference * p;

                                        if (Math.Abs(massDifference_gf) > filterMass - 1) break;

                                        abundanceElementPropertyBeanList8 = new List<IsotopeElementPropertyBean>();

                                        for (int q = 0; q <= p; q++)
                                        {
                                            relativeAbundance_hg = (double)BasicMathematics.BinomialCoefficient(p, q) * Math.Pow(h_g_relativeAbundance, q);
                                            massDifference_hg = h_g_massDifference * q;

                                            if (Math.Abs(massDifference_hg) > filterMass - 1) break;

                                            abundanceElementPropertyBeanList8.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance_hg, MassDifferenceFromMonoisotopicIon = massDifference_hg });
                                        }

                                        abundanceElementPropertyBeanList8 = getAccurateMultiplatedIsotopeElement(relativeAbundance_gf, massDifference_gf, abundanceElementPropertyBeanList8, filterMass);

                                        abundanceElementPropertyBeanList7 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList7, abundanceElementPropertyBeanList8);
                                    }


                                    abundanceElementPropertyBeanList7 = getAccurateMultiplatedIsotopeElement(relativeAbundance_fe, massDifference_fe, abundanceElementPropertyBeanList7, filterMass);

                                    abundanceElementPropertyBeanList6 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList6, abundanceElementPropertyBeanList7);
                                }

                                abundanceElementPropertyBeanList6 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ed, massDifference_ed, abundanceElementPropertyBeanList6, filterMass);

                                abundanceElementPropertyBeanList5 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList5, abundanceElementPropertyBeanList6);
                            }

                            abundanceElementPropertyBeanList5 = getAccurateMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getAccurateMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getAccurateMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getAllIsotopeElementPropertyForEightIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double f_e_relativeAbundance = isotopeElementPropertyBeanList[6].RelativeAbundance / isotopeElementPropertyBeanList[5].RelativeAbundance;
            double f_e_massDifference = isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon;

            double g_f_relativeAbundance = isotopeElementPropertyBeanList[7].RelativeAbundance / isotopeElementPropertyBeanList[6].RelativeAbundance;
            double g_f_massDifference = isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;
            double relativeAbundance_fe, massDifference_fe;
            double relativeAbundance_gf, massDifference_gf;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = a_massDifference * i;

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = b_a_massDifference * j;

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = c_b_massDifference * k;

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = d_c_massDifference * l;

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;

                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = e_d_massDifference * m;

                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;

                                abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();

                                for (int o = 0; o <= m; o++)
                                {
                                    relativeAbundance_fe = (double)BasicMathematics.BinomialCoefficient(m, o) * Math.Pow(f_e_relativeAbundance, o);
                                    massDifference_fe = f_e_massDifference * o;

                                    if (Math.Abs(massDifference_fe) > filterMass - 1) break;

                                    abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();

                                    for (int p = 0; p <= o; p++)
                                    {
                                        relativeAbundance_gf = (double)BasicMathematics.BinomialCoefficient(o, p) * Math.Pow(g_f_relativeAbundance, p);
                                        massDifference_gf = g_f_massDifference * p;

                                        if (Math.Abs(massDifference_gf) > filterMass - 1) break;

                                        abundanceElementPropertyBeanList7.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance_gf, MassDifferenceFromMonoisotopicIon = massDifference_gf });
                                    }

                                    abundanceElementPropertyBeanList7 = getAccurateMultiplatedIsotopeElement(relativeAbundance_fe, massDifference_fe, abundanceElementPropertyBeanList7, filterMass);

                                    abundanceElementPropertyBeanList6 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList6, abundanceElementPropertyBeanList7);
                                }

                                abundanceElementPropertyBeanList6 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ed, massDifference_ed, abundanceElementPropertyBeanList6, filterMass);

                                abundanceElementPropertyBeanList5 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList5, abundanceElementPropertyBeanList6);
                            }

                            abundanceElementPropertyBeanList5 = getAccurateMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getAccurateMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getAccurateMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getAllIsotopeElementPropertyForSevenIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double f_e_relativeAbundance = isotopeElementPropertyBeanList[6].RelativeAbundance / isotopeElementPropertyBeanList[5].RelativeAbundance;
            double f_e_massDifference = isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;
            double relativeAbundance_fe, massDifference_fe;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = a_massDifference * i;

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = b_a_massDifference * j;

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = c_b_massDifference * k;

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = d_c_massDifference * l;

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;

                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = e_d_massDifference * m;

                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;

                                abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();

                                for (int o = 0; o <= m; o++)
                                {
                                    relativeAbundance_fe = (double)BasicMathematics.BinomialCoefficient(m, o) * Math.Pow(f_e_relativeAbundance, o);
                                    massDifference_fe = f_e_massDifference * o;

                                    if (Math.Abs(massDifference_fe) > filterMass - 1) break;

                                    abundanceElementPropertyBeanList6.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance_fe, MassDifferenceFromMonoisotopicIon = massDifference_fe });
                                }

                                abundanceElementPropertyBeanList6 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ed, massDifference_ed, abundanceElementPropertyBeanList6, filterMass);

                                abundanceElementPropertyBeanList5 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList5, abundanceElementPropertyBeanList6);
                            }

                            abundanceElementPropertyBeanList5 = getAccurateMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getAccurateMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getAccurateMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getAllIsotopeElementPropertyForSixIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = a_massDifference * i;

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = b_a_massDifference * j;

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = c_b_massDifference * k;

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = d_c_massDifference * l;

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;

                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = e_d_massDifference * m;

                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;

                                abundanceElementPropertyBeanList5.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance_ed, MassDifferenceFromMonoisotopicIon = massDifference_ed });
                            }

                            abundanceElementPropertyBeanList5 = getAccurateMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getAccurateMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getAccurateMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getAllIsotopeElementPropertyForFiveIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = a_massDifference * i;

                if (Math.Abs(massDifference_a) > filterMass - 1) break;


                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = b_a_massDifference * j;

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = c_b_massDifference * k;

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = d_c_massDifference * l;

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;

                            abundanceElementPropertyBeanList4.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance_dc, MassDifferenceFromMonoisotopicIon = massDifference_dc });
                        }

                        abundanceElementPropertyBeanList4 = getAccurateMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getAccurateMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getAllIsotopeElementPropertyForFourIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = a_massDifference * i;

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = b_a_massDifference * j;

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = c_b_massDifference * k;

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList3.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance_cb, MassDifferenceFromMonoisotopicIon = massDifference_cb });
                    }

                    abundanceElementPropertyBeanList3 = getAccurateMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getAccurateMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }
            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getAllIsotopeElementPropertyForThreeIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = a_massDifference * i;

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

                for (int k = 0; k <= i; k++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, k) * Math.Pow(b_a_relativeAbundance, k);
                    massDifference_ba = b_a_massDifference * k;

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList2.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance_ba, MassDifferenceFromMonoisotopicIon = massDifference_ba });
                }

                abundanceElementPropertyBeanList2 = getAccurateMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);
                abundanceElementPropertyBeanList1 = getAccurateMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getAllIsotopeElementPropertyForTwoIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;


            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = a_massDifference * (double)i;

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance_a, MassDifferenceFromMonoisotopicIon = massDifference_a });
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getAccurateMultiplatedIsotopeElement(List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1, List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2, int filterMass)
        {
            List<IsotopeElementPropertyBean> multiplatedAbundanceElementBeanList = new List<IsotopeElementPropertyBean>();
            double relativeAbundance, massDifference;
            for (int i = 0; i < abundanceElementPropertyBeanList1.Count; i++)
            {
                for (int j = 0; j < abundanceElementPropertyBeanList2.Count; j++)
                {
                    relativeAbundance = abundanceElementPropertyBeanList1[i].RelativeAbundance * abundanceElementPropertyBeanList2[j].RelativeAbundance;
                    massDifference = abundanceElementPropertyBeanList1[i].MassDifferenceFromMonoisotopicIon + abundanceElementPropertyBeanList2[j].MassDifferenceFromMonoisotopicIon;
                    if (Math.Abs(massDifference) <= filterMass)
                        multiplatedAbundanceElementBeanList.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance, MassDifferenceFromMonoisotopicIon = massDifference });
                }
            }
            return multiplatedAbundanceElementBeanList;
        }

        private static List<IsotopeElementPropertyBean> getAccurateMultiplatedIsotopeElement(double relativeAbund, double massDiff, List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList, int filterMass)
        {
            List<IsotopeElementPropertyBean> multiplatedAbundanceElementBeanList = new List<IsotopeElementPropertyBean>();
            double relativeAbundance, massDifference;
            for (int i = 0; i < abundanceElementPropertyBeanList.Count; i++)
            {
                relativeAbundance = relativeAbund * abundanceElementPropertyBeanList[i].RelativeAbundance;
                massDifference = massDiff + abundanceElementPropertyBeanList[i].MassDifferenceFromMonoisotopicIon;
                if (Math.Abs(massDifference) <= filterMass)
                    multiplatedAbundanceElementBeanList.Add(new IsotopeElementPropertyBean() { RelativeAbundance = relativeAbundance, MassDifferenceFromMonoisotopicIon = massDifference });
            }
            return multiplatedAbundanceElementBeanList;
        }

        private static List<IsotopeElementPropertyBean> getAccurateMargedIsotopeElement(List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1, List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < abundanceElementPropertyBeanList1.Count; i++)
                abundanceElementPropertyBeanList.Add(abundanceElementPropertyBeanList1[i]);

            for (int i = 0; i < abundanceElementPropertyBeanList2.Count; i++)
                abundanceElementPropertyBeanList.Add(abundanceElementPropertyBeanList2[i]);

            return abundanceElementPropertyBeanList;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementPropertyForTenIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList8 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList9 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double f_e_relativeAbundance = isotopeElementPropertyBeanList[6].RelativeAbundance / isotopeElementPropertyBeanList[5].RelativeAbundance;
            double f_e_massDifference = isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon;

            double g_f_relativeAbundance = isotopeElementPropertyBeanList[7].RelativeAbundance / isotopeElementPropertyBeanList[6].RelativeAbundance;
            double g_f_massDifference = isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon;

            double h_g_relativeAbundance = isotopeElementPropertyBeanList[8].RelativeAbundance / isotopeElementPropertyBeanList[7].RelativeAbundance;
            double h_g_massDifference = isotopeElementPropertyBeanList[8].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon;

            double i_h_relativeAbundance = isotopeElementPropertyBeanList[9].RelativeAbundance / isotopeElementPropertyBeanList[8].RelativeAbundance;
            double i_h_massDifference = isotopeElementPropertyBeanList[9].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[8].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;
            double relativeAbundance_fe, massDifference_fe;
            double relativeAbundance_gf, massDifference_gf;
            double relativeAbundance_hg, massDifference_hg;
            double relativeAbundance_ih, massDifference_ih;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = Math.Round(a_massDifference * i, 0);

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
                for (int j = 0; j < filterMass; j++) { abundanceElementPropertyBeanList2.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = Math.Round(b_a_massDifference * j, 0);

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
                    for (int k = 0; k < filterMass; k++) { abundanceElementPropertyBeanList3.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = Math.Round(c_b_massDifference * k, 0);

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
                        for (int l = 0; l < filterMass; l++) { abundanceElementPropertyBeanList4.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = Math.Round(d_c_massDifference * l, 0);

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;

                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
                            for (int m = 0; m < filterMass; m++) { abundanceElementPropertyBeanList5.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = Math.Round(e_d_massDifference * m, 0);

                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;

                                abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
                                for (int o = 0; o < filterMass; o++) { abundanceElementPropertyBeanList6.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                for (int o = 0; o <= m; o++)
                                {
                                    relativeAbundance_fe = (double)BasicMathematics.BinomialCoefficient(m, o) * Math.Pow(f_e_relativeAbundance, o);
                                    massDifference_fe = Math.Round(f_e_massDifference * o, 0);

                                    if (Math.Abs(massDifference_fe) > filterMass - 1) break;

                                    abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();
                                    for (int p = 0; p < filterMass; p++) { abundanceElementPropertyBeanList7.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                    for (int p = 0; p <= o; p++)
                                    {
                                        relativeAbundance_gf = (double)BasicMathematics.BinomialCoefficient(o, p) * Math.Pow(g_f_relativeAbundance, p);
                                        massDifference_gf = Math.Round(g_f_massDifference * p, 0);

                                        if (Math.Abs(massDifference_gf) > filterMass - 1) break;

                                        abundanceElementPropertyBeanList8 = new List<IsotopeElementPropertyBean>();
                                        for (int q = 0; q < filterMass; q++) { abundanceElementPropertyBeanList8.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                        for (int q = 0; q <= p; q++)
                                        {
                                            relativeAbundance_hg = (double)BasicMathematics.BinomialCoefficient(p, q) * Math.Pow(h_g_relativeAbundance, q);
                                            massDifference_hg = Math.Round(h_g_massDifference * q, 0);

                                            if (Math.Abs(massDifference_hg) > filterMass - 1) break;

                                            abundanceElementPropertyBeanList9 = new List<IsotopeElementPropertyBean>();
                                            for (int r = 0; r < filterMass; r++) { abundanceElementPropertyBeanList9.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                            for (int r = 0; r <= q; r++)
                                            {
                                                relativeAbundance_ih = (double)BasicMathematics.BinomialCoefficient(q, r) * Math.Pow(i_h_relativeAbundance, r);
                                                massDifference_ih = Math.Round(i_h_massDifference * r, 0);

                                                if (Math.Abs(massDifference_ih) > filterMass - 1) break;

                                                abundanceElementPropertyBeanList9[(int)massDifference_ih].RelativeAbundance += relativeAbundance_ih;
                                                abundanceElementPropertyBeanList9[(int)massDifference_ih].MassDifferenceFromMonoisotopicIon = massDifference_ih;
                                            }

                                            abundanceElementPropertyBeanList9 = getNominalMultiplatedIsotopeElement(relativeAbundance_hg, massDifference_hg, abundanceElementPropertyBeanList9, filterMass);

                                            abundanceElementPropertyBeanList8 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList8, abundanceElementPropertyBeanList9);
                                        }

                                        abundanceElementPropertyBeanList8 = getNominalMultiplatedIsotopeElement(relativeAbundance_gf, massDifference_gf, abundanceElementPropertyBeanList8, filterMass);

                                        abundanceElementPropertyBeanList7 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList7, abundanceElementPropertyBeanList8);
                                    }


                                    abundanceElementPropertyBeanList7 = getNominalMultiplatedIsotopeElement(relativeAbundance_fe, massDifference_fe, abundanceElementPropertyBeanList7, filterMass);

                                    abundanceElementPropertyBeanList6 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList6, abundanceElementPropertyBeanList7);
                                }

                                abundanceElementPropertyBeanList6 = getNominalMultiplatedIsotopeElement(relativeAbundance_ed, massDifference_ed, abundanceElementPropertyBeanList6, filterMass);

                                abundanceElementPropertyBeanList5 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList5, abundanceElementPropertyBeanList6);
                            }

                            abundanceElementPropertyBeanList5 = getNominalMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getNominalMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getNominalMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getNominalMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementPropertyForNineIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList8 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double f_e_relativeAbundance = isotopeElementPropertyBeanList[6].RelativeAbundance / isotopeElementPropertyBeanList[5].RelativeAbundance;
            double f_e_massDifference = isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon;

            double g_f_relativeAbundance = isotopeElementPropertyBeanList[7].RelativeAbundance / isotopeElementPropertyBeanList[6].RelativeAbundance;
            double g_f_massDifference = isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon;

            double h_g_relativeAbundance = isotopeElementPropertyBeanList[8].RelativeAbundance / isotopeElementPropertyBeanList[7].RelativeAbundance;
            double h_g_massDifference = isotopeElementPropertyBeanList[8].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;
            double relativeAbundance_fe, massDifference_fe;
            double relativeAbundance_gf, massDifference_gf;
            double relativeAbundance_hg, massDifference_hg;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = Math.Round(a_massDifference * i, 0);

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
                for (int j = 0; j < filterMass; j++) { abundanceElementPropertyBeanList2.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = Math.Round(b_a_massDifference * j, 0);

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
                    for (int k = 0; k < filterMass; k++) { abundanceElementPropertyBeanList3.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = Math.Round(c_b_massDifference * k, 0);

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
                        for (int l = 0; l < filterMass; l++) { abundanceElementPropertyBeanList4.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = Math.Round(d_c_massDifference * l, 0);

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;

                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
                            for (int m = 0; m < filterMass; m++) { abundanceElementPropertyBeanList5.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = Math.Round(e_d_massDifference * m, 0);

                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;

                                abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
                                for (int o = 0; o < filterMass; o++) { abundanceElementPropertyBeanList6.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                for (int o = 0; o <= m; o++)
                                {
                                    relativeAbundance_fe = (double)BasicMathematics.BinomialCoefficient(m, o) * Math.Pow(f_e_relativeAbundance, o);
                                    massDifference_fe = Math.Round(f_e_massDifference * o, 0);

                                    if (Math.Abs(massDifference_fe) > filterMass - 1) break;

                                    abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();
                                    for (int p = 0; p < filterMass; p++) { abundanceElementPropertyBeanList7.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                    for (int p = 0; p <= o; p++)
                                    {
                                        relativeAbundance_gf = (double)BasicMathematics.BinomialCoefficient(o, p) * Math.Pow(g_f_relativeAbundance, p);
                                        massDifference_gf = Math.Round(g_f_massDifference * p, 0);

                                        if (Math.Abs(massDifference_gf) > filterMass - 1) break;

                                        abundanceElementPropertyBeanList8 = new List<IsotopeElementPropertyBean>();
                                        for (int q = 0; q < filterMass; q++) { abundanceElementPropertyBeanList8.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                        for (int q = 0; q <= p; q++)
                                        {
                                            relativeAbundance_hg = (double)BasicMathematics.BinomialCoefficient(p, q) * Math.Pow(h_g_relativeAbundance, q);
                                            massDifference_hg = Math.Round(h_g_massDifference * q, 0);

                                            if (Math.Abs(massDifference_hg) > filterMass - 1) break;

                                            abundanceElementPropertyBeanList8[(int)massDifference_hg].RelativeAbundance += relativeAbundance_hg;
                                            abundanceElementPropertyBeanList8[(int)massDifference_hg].MassDifferenceFromMonoisotopicIon = massDifference_hg;
                                        }

                                        abundanceElementPropertyBeanList8 = getNominalMultiplatedIsotopeElement(relativeAbundance_gf, massDifference_gf, abundanceElementPropertyBeanList8, filterMass);

                                        abundanceElementPropertyBeanList7 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList7, abundanceElementPropertyBeanList8);
                                    }


                                    abundanceElementPropertyBeanList7 = getNominalMultiplatedIsotopeElement(relativeAbundance_fe, massDifference_fe, abundanceElementPropertyBeanList7, filterMass);

                                    abundanceElementPropertyBeanList6 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList6, abundanceElementPropertyBeanList7);
                                }

                                abundanceElementPropertyBeanList6 = getNominalMultiplatedIsotopeElement(relativeAbundance_ed, massDifference_ed, abundanceElementPropertyBeanList6, filterMass);

                                abundanceElementPropertyBeanList5 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList5, abundanceElementPropertyBeanList6);
                            }

                            abundanceElementPropertyBeanList5 = getNominalMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getNominalMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getNominalMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getNominalMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementPropertyForEightIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double f_e_relativeAbundance = isotopeElementPropertyBeanList[6].RelativeAbundance / isotopeElementPropertyBeanList[5].RelativeAbundance;
            double f_e_massDifference = isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon;

            double g_f_relativeAbundance = isotopeElementPropertyBeanList[7].RelativeAbundance / isotopeElementPropertyBeanList[6].RelativeAbundance;
            double g_f_massDifference = isotopeElementPropertyBeanList[7].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;
            double relativeAbundance_fe, massDifference_fe;
            double relativeAbundance_gf, massDifference_gf;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = Math.Round(a_massDifference * i, 0);

                if (Math.Abs(massDifference_a) > filterMass - 1) break;


                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
                for (int j = 0; j < filterMass; j++) { abundanceElementPropertyBeanList2.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = Math.Round(b_a_massDifference * j, 0);

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;


                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
                    for (int k = 0; k < filterMass; k++) { abundanceElementPropertyBeanList3.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = Math.Round(c_b_massDifference * k, 0);

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
                        for (int l = 0; l < filterMass; l++) { abundanceElementPropertyBeanList4.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = Math.Round(d_c_massDifference * l, 0);

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;


                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
                            for (int m = 0; m < filterMass; m++) { abundanceElementPropertyBeanList5.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = Math.Round(e_d_massDifference * m, 0);

                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;

                                abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
                                for (int o = 0; o < filterMass; o++) { abundanceElementPropertyBeanList6.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                for (int o = 0; o <= m; o++)
                                {
                                    relativeAbundance_fe = (double)BasicMathematics.BinomialCoefficient(m, o) * Math.Pow(f_e_relativeAbundance, o);
                                    massDifference_fe = Math.Round(f_e_massDifference * o, 0);

                                    if (Math.Abs(massDifference_fe) > filterMass - 1) break;

                                    abundanceElementPropertyBeanList7 = new List<IsotopeElementPropertyBean>();
                                    for (int p = 0; p < filterMass; p++) { abundanceElementPropertyBeanList7.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                    for (int p = 0; p <= o; p++)
                                    {
                                        relativeAbundance_gf = (double)BasicMathematics.BinomialCoefficient(o, p) * Math.Pow(g_f_relativeAbundance, p);
                                        massDifference_gf = Math.Round(g_f_massDifference * p, 0);

                                        if (Math.Abs(massDifference_gf) > filterMass - 1) break;

                                        abundanceElementPropertyBeanList7[(int)massDifference_gf].RelativeAbundance += relativeAbundance_gf;
                                        abundanceElementPropertyBeanList7[(int)massDifference_gf].MassDifferenceFromMonoisotopicIon = massDifference_gf;
                                    }

                                    abundanceElementPropertyBeanList7 = getNominalMultiplatedIsotopeElement(relativeAbundance_fe, massDifference_fe, abundanceElementPropertyBeanList7, filterMass);

                                    abundanceElementPropertyBeanList6 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList6, abundanceElementPropertyBeanList7);
                                }

                                abundanceElementPropertyBeanList6 = getNominalMultiplatedIsotopeElement(relativeAbundance_ed, massDifference_ed, abundanceElementPropertyBeanList6, filterMass);

                                abundanceElementPropertyBeanList5 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList5, abundanceElementPropertyBeanList6);
                            }

                            abundanceElementPropertyBeanList5 = getNominalMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getNominalMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getNominalMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getNominalMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementPropertyForSevenIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double f_e_relativeAbundance = isotopeElementPropertyBeanList[6].RelativeAbundance / isotopeElementPropertyBeanList[5].RelativeAbundance;
            double f_e_massDifference = isotopeElementPropertyBeanList[6].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;
            double relativeAbundance_fe, massDifference_fe;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = Math.Round(a_massDifference * i, 0);

                if (Math.Abs(massDifference_a) > filterMass - 1) break;


                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
                for (int j = 0; j < filterMass; j++) { abundanceElementPropertyBeanList2.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = Math.Round(b_a_massDifference * j, 0);

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;


                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
                    for (int k = 0; k < filterMass; k++) { abundanceElementPropertyBeanList3.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = Math.Round(c_b_massDifference * k, 0);

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;


                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
                        for (int l = 0; l < filterMass; l++) { abundanceElementPropertyBeanList4.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = Math.Round(d_c_massDifference * l, 0);

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;


                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
                            for (int m = 0; m < filterMass; m++) { abundanceElementPropertyBeanList5.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = Math.Round(e_d_massDifference * m, 0);

                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;


                                abundanceElementPropertyBeanList6 = new List<IsotopeElementPropertyBean>();
                                for (int o = 0; o < filterMass; o++) { abundanceElementPropertyBeanList6.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                                for (int o = 0; o <= m; o++)
                                {
                                    relativeAbundance_fe = (double)BasicMathematics.BinomialCoefficient(m, o) * Math.Pow(f_e_relativeAbundance, o);
                                    massDifference_fe = Math.Round(f_e_massDifference * o, 0);

                                    if (Math.Abs(massDifference_fe) > filterMass - 1) break;

                                    abundanceElementPropertyBeanList6[(int)massDifference_fe].RelativeAbundance += relativeAbundance_fe;
                                    abundanceElementPropertyBeanList6[(int)massDifference_fe].MassDifferenceFromMonoisotopicIon = massDifference_fe;
                                }

                                abundanceElementPropertyBeanList6 = getNominalMultiplatedIsotopeElement(relativeAbundance_ed, massDifference_ed, abundanceElementPropertyBeanList6, filterMass);

                                abundanceElementPropertyBeanList5 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList5, abundanceElementPropertyBeanList6);
                            }

                            abundanceElementPropertyBeanList5 = getNominalMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getNominalMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getNominalMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getNominalMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementPropertyForSixIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double e_d_relativeAbundance = isotopeElementPropertyBeanList[5].RelativeAbundance / isotopeElementPropertyBeanList[4].RelativeAbundance;
            double e_d_massDifference = isotopeElementPropertyBeanList[5].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;
            double relativeAbundance_ed, massDifference_ed;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = Math.Round(a_massDifference * i, 0);

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
                for (int j = 0; j < filterMass; j++) { abundanceElementPropertyBeanList2.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = Math.Round(b_a_massDifference * j, 0);

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;


                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
                    for (int k = 0; k < filterMass; k++) { abundanceElementPropertyBeanList3.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = Math.Round(c_b_massDifference * k, 0);

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;


                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
                        for (int l = 0; l < filterMass; l++) { abundanceElementPropertyBeanList4.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = Math.Round(d_c_massDifference * l, 0);

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;


                            abundanceElementPropertyBeanList5 = new List<IsotopeElementPropertyBean>();
                            for (int m = 0; m < filterMass; m++) { abundanceElementPropertyBeanList5.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                            for (int m = 0; m <= l; m++)
                            {
                                relativeAbundance_ed = (double)BasicMathematics.BinomialCoefficient(l, m) * Math.Pow(e_d_relativeAbundance, m);
                                massDifference_ed = Math.Round(e_d_massDifference * m, 0);



                                if (Math.Abs(massDifference_ed) > filterMass - 1) break;

                                abundanceElementPropertyBeanList5[(int)massDifference_ed].RelativeAbundance += relativeAbundance_ed;
                                abundanceElementPropertyBeanList5[(int)massDifference_ed].MassDifferenceFromMonoisotopicIon = massDifference_ed;

                            }

                            abundanceElementPropertyBeanList5 = getNominalMultiplatedIsotopeElement(relativeAbundance_dc, massDifference_dc, abundanceElementPropertyBeanList5, filterMass);

                            abundanceElementPropertyBeanList4 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList4, abundanceElementPropertyBeanList5);
                        }

                        abundanceElementPropertyBeanList4 = getNominalMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getNominalMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getNominalMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementPropertyForFiveIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double d_c_relativeAbundance = isotopeElementPropertyBeanList[4].RelativeAbundance / isotopeElementPropertyBeanList[3].RelativeAbundance;
            double d_c_massDifference = isotopeElementPropertyBeanList[4].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;
            double relativeAbundance_dc, massDifference_dc;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = Math.Round(a_massDifference * i, 0);

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
                for (int j = 0; j < filterMass; j++) { abundanceElementPropertyBeanList2.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }


                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = Math.Round(b_a_massDifference * j, 0);

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;


                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
                    for (int k = 0; k < filterMass; k++) { abundanceElementPropertyBeanList3.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = Math.Round(c_b_massDifference * k, 0);

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList4 = new List<IsotopeElementPropertyBean>();
                        for (int l = 0; l < filterMass; l++) { abundanceElementPropertyBeanList4.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                        for (int l = 0; l <= k; l++)
                        {
                            relativeAbundance_dc = (double)BasicMathematics.BinomialCoefficient(k, l) * Math.Pow(d_c_relativeAbundance, l);
                            massDifference_dc = Math.Round(d_c_massDifference * l, 0);

                            if (Math.Abs(massDifference_dc) > filterMass - 1) break;

                            abundanceElementPropertyBeanList4[(int)massDifference_dc].RelativeAbundance += relativeAbundance_dc;
                            abundanceElementPropertyBeanList4[(int)massDifference_dc].MassDifferenceFromMonoisotopicIon = massDifference_dc;

                        }

                        abundanceElementPropertyBeanList4 = getNominalMultiplatedIsotopeElement(relativeAbundance_cb, massDifference_cb, abundanceElementPropertyBeanList4, filterMass);

                        abundanceElementPropertyBeanList3 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList3, abundanceElementPropertyBeanList4);
                    }

                    abundanceElementPropertyBeanList3 = getNominalMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getNominalMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementPropertyForFourIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double c_b_relativeAbundance = isotopeElementPropertyBeanList[3].RelativeAbundance / isotopeElementPropertyBeanList[2].RelativeAbundance;
            double c_b_massDifference = isotopeElementPropertyBeanList[3].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;
            double relativeAbundance_cb, massDifference_cb;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = Math.Round(a_massDifference * i, 0);

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
                for (int j = 0; j < filterMass; j++) { abundanceElementPropertyBeanList2.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = Math.Round(b_a_massDifference * j, 0);

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList3 = new List<IsotopeElementPropertyBean>();
                    for (int k = 0; k < filterMass; k++) { abundanceElementPropertyBeanList3.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                    for (int k = 0; k <= j; k++)
                    {
                        relativeAbundance_cb = (double)BasicMathematics.BinomialCoefficient(j, k) * Math.Pow(c_b_relativeAbundance, k);
                        massDifference_cb = Math.Round(c_b_massDifference * k, 0);

                        if (Math.Abs(massDifference_cb) > filterMass - 1) break;

                        abundanceElementPropertyBeanList3[(int)massDifference_cb].RelativeAbundance += relativeAbundance_cb;
                        abundanceElementPropertyBeanList3[(int)massDifference_cb].MassDifferenceFromMonoisotopicIon = massDifference_cb;
                    }

                    abundanceElementPropertyBeanList3 = getNominalMultiplatedIsotopeElement(relativeAbundance_ba, massDifference_ba, abundanceElementPropertyBeanList3, filterMass);

                    abundanceElementPropertyBeanList2 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList2, abundanceElementPropertyBeanList3);
                }

                abundanceElementPropertyBeanList2 = getNominalMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);

                abundanceElementPropertyBeanList1 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }
            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementPropertyForThreeIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();

            for (int i = 0; i < filterMass; i++) { abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double b_a_relativeAbundance = isotopeElementPropertyBeanList[2].RelativeAbundance / isotopeElementPropertyBeanList[1].RelativeAbundance;
            double b_a_massDifference = isotopeElementPropertyBeanList[2].MassDifferenceFromMonoisotopicIon - isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;
            double relativeAbundance_ba, massDifference_ba;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = Math.Round(a_massDifference * i, 0);

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList2 = new List<IsotopeElementPropertyBean>();
                for (int j = 0; j < filterMass; j++) { abundanceElementPropertyBeanList2.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

                for (int j = 0; j <= i; j++)
                {
                    relativeAbundance_ba = (double)BasicMathematics.BinomialCoefficient(i, j) * Math.Pow(b_a_relativeAbundance, j);
                    massDifference_ba = Math.Round(b_a_massDifference * j, 0);

                    if (Math.Abs(massDifference_ba) > filterMass - 1) break;

                    abundanceElementPropertyBeanList2[(int)massDifference_ba].RelativeAbundance += relativeAbundance_ba;
                    abundanceElementPropertyBeanList2[(int)massDifference_ba].MassDifferenceFromMonoisotopicIon = (int)massDifference_ba;
                }

                abundanceElementPropertyBeanList2 = getNominalMultiplatedIsotopeElement(relativeAbundance_a, massDifference_a, abundanceElementPropertyBeanList2, filterMass);
                abundanceElementPropertyBeanList1 = getNominalMargedIsotopeElement(abundanceElementPropertyBeanList1, abundanceElementPropertyBeanList2);
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalIsotopeElementPropertyForTwoIsotopomerElement(List<IsotopeElementPropertyBean> isotopeElementPropertyBeanList, int n, int filterMass)
        {
            List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1 = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { abundanceElementPropertyBeanList1.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            double a_relativeAbundance = isotopeElementPropertyBeanList[1].RelativeAbundance;
            double a_massDifference = isotopeElementPropertyBeanList[1].MassDifferenceFromMonoisotopicIon;

            double relativeAbundance_a, massDifference_a;

            for (int i = 0; i <= n; i++)
            {
                relativeAbundance_a = (double)BasicMathematics.BinomialCoefficient(n, i) * Math.Pow(a_relativeAbundance, i);
                massDifference_a = Math.Round(a_massDifference * (double)i, 0);

                if (Math.Abs(massDifference_a) > filterMass - 1) break;

                abundanceElementPropertyBeanList1[(int)massDifference_a].RelativeAbundance += relativeAbundance_a;
                abundanceElementPropertyBeanList1[(int)massDifference_a].MassDifferenceFromMonoisotopicIon = (int)massDifference_a;
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalMargedIsotopeElement(List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1, List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2)
        {
            for (int i = 0; i < abundanceElementPropertyBeanList1.Count; i++)
            {
                abundanceElementPropertyBeanList1[i].RelativeAbundance += abundanceElementPropertyBeanList2[i].RelativeAbundance;
                abundanceElementPropertyBeanList1[i].MassDifferenceFromMonoisotopicIon = i;
            }

            return abundanceElementPropertyBeanList1;
        }

        private static List<IsotopeElementPropertyBean> getNominalMultiplatedisotopeElement(List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList1, List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList2, int filterMass)
        {
            int massDiff;
            List<IsotopeElementPropertyBean> multipliedIsotopeElementList = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { multipliedIsotopeElementList.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = i }); }

            for (int i = 0; i < abundanceElementPropertyBeanList1.Count; i++)
            {
                for (int j = 0; j < abundanceElementPropertyBeanList2.Count; j++)
                {
                    massDiff = (int)(abundanceElementPropertyBeanList1[i].MassDifferenceFromMonoisotopicIon + abundanceElementPropertyBeanList2[j].MassDifferenceFromMonoisotopicIon);
                    if (massDiff <= filterMass - 1)
                    {
                        multipliedIsotopeElementList[massDiff].RelativeAbundance += abundanceElementPropertyBeanList1[i].RelativeAbundance * abundanceElementPropertyBeanList2[j].RelativeAbundance;
                    }
                }
            }

            return multipliedIsotopeElementList;
        }

        private static List<IsotopeElementPropertyBean> getNominalMultiplatedIsotopeElement(double relativeAbund, double massDiff, List<IsotopeElementPropertyBean> abundanceElementPropertyBeanList, int filterMass)
        {
            List<IsotopeElementPropertyBean> multiplatedAbundanceElementBeanList = new List<IsotopeElementPropertyBean>();
            for (int i = 0; i < filterMass; i++) { multiplatedAbundanceElementBeanList.Add(new IsotopeElementPropertyBean() { RelativeAbundance = 0, MassDifferenceFromMonoisotopicIon = 0 }); }

            double relativeAbundance, massDifference;
            for (int i = 0; i < abundanceElementPropertyBeanList.Count; i++)
            {
                relativeAbundance = relativeAbund * abundanceElementPropertyBeanList[i].RelativeAbundance;
                massDifference = Math.Round(massDiff + abundanceElementPropertyBeanList[i].MassDifferenceFromMonoisotopicIon, 0);

                if (Math.Abs(massDifference) <= filterMass - 1)
                {
                    multiplatedAbundanceElementBeanList[(int)massDifference].RelativeAbundance += relativeAbundance;
                    multiplatedAbundanceElementBeanList[(int)massDifference].MassDifferenceFromMonoisotopicIon = (int)massDifference;
                }
            }
            return multiplatedAbundanceElementBeanList;
        }

    }
}
