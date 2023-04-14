using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.FormulaGenerator.Parser
{
    public static class FormulaStringParcer
    {
        public static Formula OrganicElementsReader(string formulaString)
        {
            if (formulaString == null) return null;
            if (formulaString == string.Empty) return null;
            if (formulaString == "Unknown") return null;
            if (formulaString == "null") return null;
                     
            int cnum = 0, hnum = 0, pnum = 0, snum = 0, onum = 0, nnum = 0, fnum = 0, clnum = 0, brnum = 0, inum = 0, sinum = 0, c13num = 0, h2num = 0, tmsCount = 0, meoxCount = 0;

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

            setElementNumbers(elements, out cnum, out hnum, out nnum, out onum, out pnum, out snum, out fnum, out clnum, out brnum, out inum, out sinum, out c13num, out h2num, tmsCount, meoxCount);
            var formula = new Formula(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, tmsCount, meoxCount);
            if (c13num>0||h2num>0)
            {
                formula = new Formula(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum, c13num, h2num, tmsCount, meoxCount);
            }

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

        public static Formula MQComposition2FormulaObj(string composition) {
            var elements = composition.Split(' ');
            var dict = new Dictionary<string, int>();
            foreach (var elem in elements) {
                if (!elem.Contains("(")) {
                    dict[elem] = 1;
                }
                else {
                    var elemName = elem.Split('(')[0];
                    var elemCountString = elem.Split('(')[1].Split(')')[0];
                    if (int.TryParse(elemCountString, out int elemCount)) {
                        dict[elemName] = elemCount;
                    }
                }
            }

            return new Formula(dict);
        }

        public static Formula Convert2FormulaObjV2(string formulaString) {
           // Console.WriteLine(formulaString);
            var dict = new Dictionary<string, int>(); // key: C, value: 3; key: H, value 4 etc..
            var elemString = string.Empty;
            var numString = string.Empty;

            for (int i = 0; i < formulaString.Length; i++) {
                var elem = formulaString[i];
                if (elem == '[') {  // start element
                    elemString = elem.ToString();
                    numString = string.Empty;
                    var endflag = false;
                    for (int j = i + 1; j < formulaString.Length; j++) {
                        elem = formulaString[j];
                        if (elem == ']') {
                            elemString += elem;
                            endflag = true;
                            continue;
                        }
                        if (!endflag) {
                            elemString += elem;
                            continue;
                        }
                        if (Char.IsNumber(elem)) {
                            numString += elem;
                        }
                        else if (Char.IsUpper(elem) || elem == '[') {
                            i = j - 1;
                            break;
                        }
                    }
                    var num = numString == string.Empty ? 1 : int.Parse(numString);
                    if (dict.ContainsKey(elemString))
                        dict[elemString] += num;
                    else
                        dict[elemString] = num;
                }
                else if (Char.IsUpper(elem)) { // start element
                    elemString = elem.ToString();
                    numString = string.Empty;
                    for (int j = i + 1; j < formulaString.Length; j++) {
                        elem = formulaString[j];
                        if (char.IsWhiteSpace(elem)) {
                            continue;
                        }
                        if (Char.IsNumber(elem)) {
                            numString += elem;
                        }
                        else if (elem == '[') {
                            i = j - 1;
                            break;
                        }
                        else if (!Char.IsUpper(elem)) {
                            elemString += elem;
                        }
                        else if (Char.IsUpper(elem)) {
                            i = j - 1;
                            break;
                        }
                    }
                    var num = numString == string.Empty ? 1 : int.Parse(numString);
                    if (dict.ContainsKey(elemString))
                        dict[elemString] += num;
                    else
                        dict[elemString] = num;
                }
            }

            return new Formula(dict);
        }

        internal static Formula Convert2FormulaObjV3(string formulaString) {
            return ToFormula(TokenizeFormula(formulaString));
        }

        private static Formula ToFormula(List<(string, int)> elements) {
            var result = new Dictionary<string, int>();
            foreach ((var element, var number) in elements) {
                if (result.ContainsKey(element)) {
                    result[element] += number;
                }
                else {
                    result[element] = number;
                }
            }
            return new Formula(result);
        }

        public static List<(string, int)> TokenizeFormula(string formulaString) {
            return TokenizeFormulaCharacter(formulaString).Select(ParseToken).ToList();
        }

        private static List<string> TokenizeFormulaCharacter(string formulaString) {
            // C12H24O12 -> C12, H24, O12
            var result = new List<string>();
            var i = 0;
            while (i < formulaString.Length) {
                while (i < formulaString.Length && !char.IsUpper(formulaString[i]) && formulaString[i] != '[') {
                    i++;
                }

                if (i >= formulaString.Length) {
                    break;
                }

                if (char.IsUpper(formulaString[i])) {
                    var j = i;
                    j++;
                    while (j < formulaString.Length && (!char.IsUpper(formulaString[j]) && formulaString[j] != '[')) {
                        j++;
                    }
                    result.Add(formulaString.Substring(i, j - i));
                    i = j;
                }
                else if (formulaString[i] == '[') {
                    var j = i;
                    j++;
                    while (j < formulaString.Length && formulaString[j] != ']') {
                        j++;
                    }
                    while (j < formulaString.Length && (!char.IsUpper(formulaString[j]) && formulaString[j] != '[')) {
                        j++;
                    }
                    result.Add(formulaString.Substring(i, j - i));
                    i = j;
                }
                else {
                    throw new ArgumentException(nameof(formulaString));
                }
            }
            return result;
        }

        private static (string, int) ParseToken(string token) {
            // C2 -> C, 2 , [13C]2 -> [13C], 2
            var cleaned = new string(token.Where(c => !char.IsWhiteSpace(c)).ToArray());
            int i = 0, j = 0;
            if (char.IsUpper(cleaned[i])) {
                while (j < cleaned.Length && !char.IsNumber(cleaned[j])) {
                    j++;
                }
                var element = cleaned.Substring(i, j - i);
                var number = int.TryParse(cleaned.Substring(j, cleaned.Length - j), out var result) ? result : 1;
                return (element, number);
            }
            else if (cleaned[i] == '[') {
                while (j < cleaned.Length && cleaned[j] != ']') {
                    j++;
                }
                j++;
                var element = cleaned.Substring(i, j - i);
                var number = int.TryParse(cleaned.Substring(j, cleaned.Length - j), out var result) ? result : 1;
                return (element, number);
            }
            else {
                throw new ArgumentException(nameof(token));
            }
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

        private static void setElementNumbers(string formulaString, out int cnum, out int hnum, out int nnum, out int onum, out int pnum, out int snum, out int fnum,
            out int clnum, out int brnum, out int inum, out int sinum, out int c13num, out int h2num, int tmsCount = 0, int meoxCount = 0)
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
            c13num = 0;
            h2num = 0;

            if (formulaString.Contains("D"))
            {
                formulaString = formulaString.Replace("D", "[2H]");
            }

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
            mc = Regex.Matches(formulaString, "\\[2H\\]([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) h2num = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out h2num);
                }
            }
            mc = Regex.Matches(formulaString, "\\[13C\\]([0-9]*)", RegexOptions.None);
            if (mc.Count > 0)
            {
                if (mc[0].Groups[1].Value == string.Empty) c13num = 1;
                else
                {
                    int.TryParse(mc[0].Groups[1].Value, out c13num);
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
