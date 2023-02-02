using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class FormulaStringParcer
    {
        private FormulaStringParcer() { }

        public static Formula OrganicElementsReader(string formulaString)
        {
            if (formulaString == null) return null;
            if (formulaString == "Unknown") return null;
                     
            int cnum = 0, hnum = 0, pnum = 0, snum = 0, onum = 0, nnum = 0, fnum = 0, clnum = 0, brnum = 0, inum = 0, sinum = 0, tmsCount = 0, meoxCount = 0;

            var elements = formulaString;
            
            //check the existense of TMS or MEOX information in the string
            if (formulaString.Contains('_')) {
                var stringArray = formulaString.Split('_');
                elements = stringArray[0];
                for (int i = 1; i < stringArray.Length; i++) {
                    string numberChars = string.Empty;
                    foreach (var charValue in stringArray[i])
                        if (char.IsNumber(charValue)) numberChars += charValue;
                        else break;

                    if (stringArray[i].Contains("TMS") && numberChars != string.Empty) tmsCount = int.Parse(numberChars);
                    else if (stringArray[i].Contains("MEOX") && numberChars != string.Empty) meoxCount = int.Parse(numberChars);
                }
            }

            setElementNumbers(elements, out cnum, out hnum, out nnum, out onum, out pnum, out snum, out fnum, out clnum, out brnum, out inum, out sinum, tmsCount, meoxCount);

            var formula = new Formula(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, tmsCount, meoxCount);

            return formula;
        }
    
        public static Formula OrganicElementsReader(string formulaString, double cLabelMass, double hLabelMass, double nLabelMass, 
            double oLabelMass, double pLabelMass, double sLabelMass, double fLabelMass, double clLabelMass, double brLabelMass,
            double iLabelMass, double siLabelMass)
        {
            int cnum = 0, hnum = 0, pnum = 0, snum = 0, onum = 0, nnum = 0, fnum = 0, clnum = 0, brnum = 0, inum = 0, sinum = 0;

            setElementNumbers(formulaString, out cnum, out hnum, out nnum, out onum, out pnum, out snum, out fnum, out clnum, out brnum, out inum, out sinum);

            var formula = new Formula(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, cLabelMass, hLabelMass, nLabelMass, oLabelMass, pLabelMass, 
                sLabelMass, fLabelMass, clLabelMass, brLabelMass, iLabelMass, siLabelMass);

            return formula;
        }

        public static bool IsOrganicFormula(string formulaString)
        {
            MatchCollection mc;

            var cnum = 0;
            var hnum = 0;
            var pnum = 0;
            var snum = 0;
            var onum = 0;
            var nnum = 0;
            var fnum = 0;
            var clnum = 0;
            var brnum = 0;
            var inum = 0;
            var sinum = 0;

            #region // element parser

            mc = Regex.Matches(formulaString, "C(?!a|d|e|l|o|r|s|u)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) cnum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out cnum);
                }
            }

            mc = Regex.Matches(formulaString, "H(?!e|f|g|o)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) hnum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out hnum);
                }
            }

            mc = Regex.Matches(formulaString, "N(?!a|b|d|e|i)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) nnum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out nnum);
                }
            }

            mc = Regex.Matches(formulaString, "O(?!s)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) onum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out onum);
                }
            }

            mc = Regex.Matches(formulaString, "S(?!b|c|e|i|m|n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) snum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out snum);
                }
            }

            mc = Regex.Matches(formulaString, "P(?!d|t|b|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) pnum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out pnum);
                }
            }

            mc = Regex.Matches(formulaString, "Br([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) brnum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out brnum);
                }
            }

            mc = Regex.Matches(formulaString, "Cl([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) clnum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out clnum);
                }
            }

            mc = Regex.Matches(formulaString, "F(?!e)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) fnum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out fnum);
                }
            }

            mc = Regex.Matches(formulaString, "I(?!n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) inum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out inum);
                }
            }

            mc = Regex.Matches(formulaString, "Si([0-9]*)", RegexOptions.None);
            if (mc.Count > 0) {
                if (mc[0].Groups[1].Value == string.Empty) sinum = 1;
                else {
                    int.TryParse(mc[0].Groups[1].Value, out sinum);
                }
            }
            #endregion

            var formula = new Formula(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, 0, 0);
            if (formula.FormulaString.Length == formulaString.Length) return true;
            else return false;
        }

        private static void setElementNumbers(string formulaString, out int cnum, out int hnum, out int nnum, out int onum, out int pnum, out int snum, out int fnum,
            out int clnum, out int brnum, out int inum, out int sinum, int tmsCount = 0, int meoxCount = 0)
        {
            MatchCollection mc;

            cnum = 0;
            hnum = 0;
            pnum = 0;
            snum = 0;
            onum = 0;
            nnum = 0;
            fnum = 0;
            clnum = 0;
            brnum = 0;
            inum = 0;
            sinum = 0;

            #region // element parser

            mc = Regex.Matches(formulaString, "C(?!a|d|e|l|o|r|s|u)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) cnum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out cnum);
                }
            }

            mc = Regex.Matches(formulaString, "H(?!e|f|g|o)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) hnum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out hnum);
                }
            }

            mc = Regex.Matches(formulaString, "N(?!a|b|d|e|i)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) nnum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out nnum);
                }
            }

            mc = Regex.Matches(formulaString, "O(?!s)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) onum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out onum);
                }
            }

            mc = Regex.Matches(formulaString, "S(?!b|c|e|i|m|n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) snum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out snum);
                }
            }

            mc = Regex.Matches(formulaString, "P(?!d|t|b|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) pnum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out pnum);
                }
            }

            mc = Regex.Matches(formulaString, "Br([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) brnum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out brnum);
                }
            }

            mc = Regex.Matches(formulaString, "Cl([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) clnum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out clnum);
                }
            }

            mc = Regex.Matches(formulaString, "F(?!e)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) fnum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out fnum);
                }
            }

            mc = Regex.Matches(formulaString, "I(?!n|r)([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) inum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out inum);
                }
            }

            mc = Regex.Matches(formulaString, "Si([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) sinum = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out sinum);
                }
            }
            #endregion

            cnum += tmsCount * 3 + meoxCount * 1;
            hnum += tmsCount * 8 + meoxCount * 3;
            nnum += meoxCount;
            sinum += tmsCount;
        }
    }
}
