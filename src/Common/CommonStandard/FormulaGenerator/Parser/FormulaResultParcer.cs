using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.Common.FormulaGenerator.Parser {
    public sealed class FormulaResultParcer
    {
        /// <summary>
        /// This method is now used in MS-FINDER program.
        /// Since MS-FINDER stores the result of formula calculations as .FGT file format, this is the parcer to retrieve the result from FGT file.
        /// </summary>
        /// <param name="filePath">Add the FGT file path.</param>
        /// <returns></returns>
        public static List<FormulaResult> FormulaResultReader(string filePath, out string error)
        {
            error = string.Empty;
            var formulaResults = new List<FormulaResult>();
            var result = new FormulaResult();
            string wkstr;
            bool firstFlg = false;

            if (!System.IO.File.Exists(filePath)) {
                return formulaResults;
            }

            using (var sr = new StreamReader(filePath, Encoding.ASCII))
            {
                var versionNum = 1;
                var isHeaderChecked = false;
               
                while (sr.Peek() > -1)
                {
                    wkstr = sr.ReadLine();

                    if (isHeaderChecked == false) {
                        if (wkstr.Contains("Version:")) {
                            var versionString = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                            switch (versionString) {
                                case "2": versionNum = 2; break;
                            }
                        }
                        isHeaderChecked = true;
                    }

                    double doubleValue = 0;
                    int intValue = 0;
                    bool flg = false;

                    if (Regex.IsMatch(wkstr, "NAME:", RegexOptions.IgnoreCase))
                    {
                        var formulaString = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();

                        if (!firstFlg) {
                            firstFlg = true;
                        }
                        else {
                            formulaResults.Add(result);
                            result = new FormulaResult();
                        }

                        if (formulaString == "Spectral DB search") { // this is a tag for dealing with 'spectral DB search'
                            result.Formula = new Formula() { FormulaString = "Spectral DB search", Mass = -1 };
                        }
                        else {
                            result.Formula = FormulaStringParcer.OrganicElementsReader(formulaString);
                            result.Formula.M1IsotopicAbundance = SevenGoldenRulesCheck.GetM1IsotopicAbundance(result.Formula);
                            result.Formula.M2IsotopicAbundance = SevenGoldenRulesCheck.GetM2IsotopicAbundance(result.Formula);
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "PUBCHEMCIDS:", RegexOptions.IgnoreCase))
                    {
                        if (wkstr.Split(':')[1].Trim() == string.Empty) result.PubchemResources = new List<int>();
                        else { result.PubchemResources = getPubchemRecords(wkstr.Split(':')[1].Trim().Split(',')); }
                    }
                    else if (Regex.IsMatch(wkstr, "EXACTMASS:", RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.Formula.Mass = doubleValue; else { error = errorValue(); return null; } 
                    }
                    else if (Regex.IsMatch(wkstr, "ISSELECTED:", RegexOptions.IgnoreCase))
                    {
                        if (bool.TryParse(wkstr.Split(':')[1].Trim(), out flg)) result.IsSelected = flg; else { result.IsSelected = flg; } 
                    }
                    else if (Regex.IsMatch(wkstr, "ACCURATEMASS:", RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.MatchedMass = doubleValue; else { error = errorValue(); return null; } 
                    }
                    else if (Regex.IsMatch(wkstr, "MASSDIFFERENCE:", RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.MassDiff = doubleValue; else { error = errorValue(); return null; } 
                    }
                    else if (Regex.IsMatch(wkstr, Regex.Escape("ISOTOPICINTENSITY[M+1]:"), RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.M1IsotopicIntensity = doubleValue; else { result.M1IsotopicIntensity = 0; }
                    }
                    else if (Regex.IsMatch(wkstr, Regex.Escape("ISOTOPICINTENSITY[M+2]:"), RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.M2IsotopicIntensity = doubleValue; else { result.M2IsotopicIntensity = 0; }
                    }
                    else if (Regex.IsMatch(wkstr, Regex.Escape("ISOTOPICDIFF[M+1]:"), RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.M1IsotopicDiff = doubleValue; else { result.M1IsotopicDiff = 0; } 
                    }
                    else if (Regex.IsMatch(wkstr, Regex.Escape("ISOTOPICDIFF[M+2]:"), RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.M2IsotopicDiff = doubleValue; else { result.M2IsotopicDiff = 0; } 
                    }
                    else if (Regex.IsMatch(wkstr, "TOTALSCORE:", RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.TotalScore = doubleValue; else { error = errorValue(); return null; }
                    }
                    else if (Regex.IsMatch(wkstr, "MASSDIFFSCORE:", RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.MassDiffScore = doubleValue; else { error = errorValue(); return null; } 
                    }
                    else if (Regex.IsMatch(wkstr, "ISOTOPICSCORE:", RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.IsotopicScore = doubleValue; else { error = errorValue(); return null; } 
                    }
                    else if (Regex.IsMatch(wkstr, "PRODUCTIONSCORE:", RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.ProductIonScore = doubleValue; else { result.ProductIonScore = 0; } 
                    }
                    else if (Regex.IsMatch(wkstr, "NEUTRALLOSSSCORE:", RegexOptions.IgnoreCase))
                    {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue))
                            result.NeutralLossScore = doubleValue;
                        else { result.NeutralLossScore = 0; }
                    }
                    else if (Regex.IsMatch(wkstr, "ChemOntDescriptions:", RegexOptions.IgnoreCase)) {

                        var trimWkstr = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                        if (trimWkstr == null || trimWkstr == string.Empty)
                            result.ChemicalOntologyDescriptions = new List<string>();
                        else if (!trimWkstr.Contains(';'))
                            result.ChemicalOntologyDescriptions.Add(trimWkstr);
                        else if (trimWkstr.Contains(';')) {
                            var trimedArray = trimWkstr.Split(';');
                            foreach (var desc in trimedArray)
                                result.ChemicalOntologyDescriptions.Add(desc);
                        }

                    }
                    else if (Regex.IsMatch(wkstr, "ChemOntIDs:", RegexOptions.IgnoreCase)) {
                        var trimWkstr = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                        if (trimWkstr == null || trimWkstr == string.Empty)
                            result.ChemicalOntologyIDs = new List<string>();
                        else if (!trimWkstr.Contains(';'))
                            result.ChemicalOntologyIDs.Add(trimWkstr);
                        else if (trimWkstr.Contains(';')) {
                            var trimedArray = trimWkstr.Split(';');
                            foreach (var desc in trimedArray)
                                result.ChemicalOntologyIDs.Add(desc);
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "ChemOntScores:", RegexOptions.IgnoreCase)) {
                        var trimWkstr = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();

                        if (trimWkstr == null || trimWkstr == string.Empty)
                            result.ChemicalOntologyScores = new List<double>();
                        else if (!trimWkstr.Contains(';')) {
                            if (double.TryParse(trimWkstr.Trim(), out doubleValue))
                                result.ChemicalOntologyScores.Add(doubleValue);
                            else
                                result.ChemicalOntologyScores.Add(-1.0);
                        }
                        else if (trimWkstr.Contains(';')) {
                            var trimedArray = trimWkstr.Split(';');
                            foreach (var desc in trimedArray) {
                                if (double.TryParse(desc.Trim(), out doubleValue))
                                    result.ChemicalOntologyScores.Add(doubleValue);
                                else
                                    result.ChemicalOntologyScores.Add(-1.0);
                            }
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "ChemOntInChIKeys:", RegexOptions.IgnoreCase)) {
                        var trimWkstr = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                        if (trimWkstr == null || trimWkstr == string.Empty)
                            result.ChemicalOntologyRepresentativeInChIKeys = new List<string>();
                        else if (!trimWkstr.Contains(';'))
                            result.ChemicalOntologyRepresentativeInChIKeys.Add(trimWkstr);
                        else if (trimWkstr.Contains(';')) {
                            var trimedArray = trimWkstr.Split(';');
                            foreach (var desc in trimedArray)
                                result.ChemicalOntologyRepresentativeInChIKeys.Add(desc);
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "PRODUCTIONPEAKNUMBER:", RegexOptions.IgnoreCase)) {
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue)) result.ProductIonNum = intValue; else { result.ProductIonNum = 0; }
                    }
                    else if (Regex.IsMatch(wkstr, "PRODUCTIONHITSNUMBER:", RegexOptions.IgnoreCase)) {
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue)) result.ProductIonHits = intValue; else { result.ProductIonHits = 0; }
                    }
                    else if (Regex.IsMatch(wkstr, "NEUTRALLOSSPEAKNUMBER:", RegexOptions.IgnoreCase))
                    {
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue)) result.NeutralLossNum = intValue; else { result.NeutralLossNum = 0; } 
                    }
                    else if (Regex.IsMatch(wkstr, "NEUTRALLOSSHITSNUMBER:", RegexOptions.IgnoreCase))
                    {
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue)) result.NeutralLossHits = intValue; else { result.NeutralLossHits = 0; } 
                    }
                    else if (Regex.IsMatch(wkstr, "RESOURCENAMES:", RegexOptions.IgnoreCase))
                    {
                        result.ResourceNames = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "RESOURCERECORDS:", RegexOptions.IgnoreCase))
                    {
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                        {
                            if (intValue > 0)
                            {
                                result.ResourceRecords = intValue;
                                result.ResourceScore = 0.5 * (1.0 + (double)intValue / 13.0);
                            }
                            else
                            {
                                result.ResourceRecords = 0;
                                result.ResourceScore = 0;
                            }
                        }
                        else 
                        { 
                            result.ResourceRecords = 0;
                            result.ResourceScore = 0;
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "Num ProductIon", RegexOptions.IgnoreCase))
                    {
                        intValue = 0;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                        {
                            if (intValue == 0) continue;
                            for (int i = 0; i < intValue; i++)
                            {
                                if (!readProductIonArray(result.ProductIonResult, sr.ReadLine(), versionNum, out error)) { return null; }
                            }
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "Num NeutralLoss", RegexOptions.IgnoreCase))
                    {
                        intValue = 0;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                        {
                            if (intValue == 0) continue;
                            for (int i = 0; i < intValue; i++)
                            {
                                if (!readNeutralLossArray(result.NeutralLossResult, sr.ReadLine(), versionNum, out error)) { return null; }
                            }
                        }
                    }

                    else if (Regex.IsMatch(wkstr, "Num AdductIon", RegexOptions.IgnoreCase)) {
                        intValue = 0;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue)) {
                            if (intValue == 0) continue;
                            for (int i = 0; i < intValue; i++) {
                                if (!readAdductIonArray(result.AnnotatedIonResult, sr.ReadLine(), versionNum, out error)) { return null; }
                            }
                        }
                    }

                    else if (Regex.IsMatch(wkstr, "Num IsotopicIon", RegexOptions.IgnoreCase)) {
                        intValue = 0;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue)) {
                            if (intValue == 0) continue;
                            for (int i = 0; i < intValue; i++) {
                                if (!readIsotopicIonArray(result.AnnotatedIonResult, sr.ReadLine(), versionNum, out error)) { return null; }
                            }
                        }
                    }


                }
                formulaResults.Add(result); // remainder
            }

            return formulaResults;
        }

        /// <summary>
        /// this method is for faster reader of formula results
        /// it only parse title, formula and score
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<FormulaResult> FormulaResultFastReader(string filePath, out string error) {
            error = string.Empty;
            
            var formulaResults = new List<FormulaResult>();
            var result = new FormulaResult();
            string wkstr;
            bool firstFlg = false;

            if (!System.IO.File.Exists(filePath)) {
                return formulaResults;
            }

            using (var sr = new StreamReader(filePath, Encoding.ASCII)) {
                var versionNum = 1;
                var isHeaderChecked = false;

                while (sr.Peek() > -1) {
                    wkstr = sr.ReadLine();

                    if (isHeaderChecked == false) {
                        if (wkstr.Contains("Version:")) {
                            var versionString = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                            switch (versionString) {
                                case "2": versionNum = 2; break;
                            }
                        }
                        isHeaderChecked = true;
                    }

                    double doubleValue = 0;

                    if (Regex.IsMatch(wkstr, "NAME:", RegexOptions.IgnoreCase)) {
                        var formulaString = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();

                        if (!firstFlg) {
                            firstFlg = true;
                        }
                        else {
                            formulaResults.Add(result);
                            result = new FormulaResult();
                        }

                        if (formulaString == "Spectral DB search") { // this is a tag for dealing with 'spectral DB search'
                            result.Formula = new Formula() { FormulaString = "Spectral DB search", Mass = -1 };
                        }
                        else {
                            result.Formula = FormulaStringParcer.OrganicElementsReader(formulaString);
                            result.Formula.M1IsotopicAbundance = SevenGoldenRulesCheck.GetM1IsotopicAbundance(result.Formula);
                            result.Formula.M2IsotopicAbundance = SevenGoldenRulesCheck.GetM2IsotopicAbundance(result.Formula);
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "TOTALSCORE:", RegexOptions.IgnoreCase)) {
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out doubleValue)) result.TotalScore = doubleValue; else { error = errorValue(); return null; }
                    }
                }
                formulaResults.Add(result); // remainder
            }

            return formulaResults;
        }

        private static List<int> getPubchemRecords(string[] cids)
        {
            var cidlist = new List<int>();
            int key;
            foreach (var cid in cids)
            {
                if (int.TryParse(cid, out key)) { cidlist.Add(key); }
            }
            return cidlist;
        }

        private static bool readNeutralLossArray(List<NeutralLoss> lossResult, string line, int versionNum, out string error)
        {
            error = string.Empty;
            string[] array = line.Split('\t');
            if (array.Length < 9) { error = errorNeutralLoss(); return false; }

            var neutralLoss = new NeutralLoss();
            double value;


            neutralLoss.Formula = FormulaStringParcer.OrganicElementsReader(array[0]);
            if (double.TryParse(array[1], out value)) neutralLoss.Formula.Mass = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[2], out value)) neutralLoss.PrecursorMz = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[3], out value)) neutralLoss.ProductMz = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[4], out value)) neutralLoss.PrecursorIntensity = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[5], out value)) neutralLoss.ProductIntensity = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[6], out value)) neutralLoss.MassLoss = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[7], out value)) neutralLoss.MassError = value; else { error = errorValue(); return false; }

            if (versionNum == 1) {
                neutralLoss.Comment = array[8];
            }
            else if (versionNum == 2) { // in May 31, 2017, I decided to use this version
                var inchikeys = array[8].Split(';');
                foreach (var inchikey in inchikeys) {
                    neutralLoss.CandidateInChIKeys.Add(inchikey);
                }
                if (array.Length >= 10) {
                    var ontologies = array[9].Split(';');
                    foreach (var ontology in ontologies) {
                        neutralLoss.CandidateOntologies.Add(ontology);
                    }
                }
            }

            lossResult.Add(neutralLoss);

            return true;
        }

        private static bool readProductIonArray(List<ProductIon> productIons, string line, int versionNum, out string error)
        {
            error = string.Empty;
            string[] array = line.Split('\t');
            if (array.Length < 5) { error= errorProductIon(); return false; }

            var productIon = new ProductIon();
            double value;

            productIon.Formula = FormulaStringParcer.OrganicElementsReader(array[0]);
            if (productIon.Formula == null) { productIon.Formula = new Formula(); }
            if (double.TryParse(array[1], out value)) productIon.Formula.Mass = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[2], out value)) productIon.Mass = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[3], out value)) productIon.Intensity = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[4], out value)) productIon.MassDiff = value; else { error = errorValue(); return false; }
            if (array.Length >= 6 && versionNum == 2) {
                var inchikeys = array[5].Split(';');
                foreach (var inchikey in inchikeys) {
                    productIon.CandidateInChIKeys.Add(inchikey);
                }
                if (array.Length >= 7) {
                    var ontologies = array[6].Split(';');
                    foreach (var ontology in ontologies) {
                        productIon.CandidateOntologies.Add(ontology);
                    }
                }
            }

            productIons.Add(productIon);

            return true;
        }

        private static bool readAdductIonArray(List<AnnotatedIon> adductIons, string line, int versionNum, out string error) {
            error = string.Empty;
            string[] array = line.Split('\t');
            //if (array.Length < 5) { errorProductIon(); return false; }

            var adduct = new AnnotatedIon() { PeakType = AnnotatedIon.AnnotationType.Adduct};
            double value;

            adduct.AdductIon = AdductIon.GetAdductIon(array[0]);
            if (double.TryParse(array[1], out value)) adduct.AccurateMass = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[2], out value)) adduct.LinkedAccurateMass = value; else { error = errorValue(); return false; }
            

            adductIons.Add(adduct);

            return true;
        }


        private static bool readIsotopicIonArray(List<AnnotatedIon> isotopicIons, string line, int versionNum, out string error) {
            error = string.Empty;
            string[] array = line.Split('\t');
            //if (array.Length < 5) { errorProductIon(); return false; }

            var isotope = new AnnotatedIon() { PeakType = AnnotatedIon.AnnotationType.Isotope };
            double value;
            int intVal;
            isotope.IsotopeName = array[0];
            if (double.TryParse(array[1], out value)) isotope.AccurateMass = value; else { error = errorValue(); return false; }
            if (double.TryParse(array[2], out value)) isotope.LinkedAccurateMass = value; else { error = errorValue(); return false; }
            if (int.TryParse(array[4], out intVal)) isotope.IsotopeWeightNumber = intVal; else { error = errorValue(); return false; }

            isotopicIons.Add(isotope);

            return true;
        }


        //private static bool setFormulaElementNumber(Formula formula, string formulaElements)
        //{
        //    string[] elementArray = formulaElements.Split(',');
        //    int num;

        //    if (elementArray.Length != 11)
        //    {
        //        errorElement();
        //        return false;
        //    }

        //    if (int.TryParse(elementArray[0], out num)) formula.Cnum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[1], out num)) formula.Hnum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[2], out num)) formula.Nnum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[3], out num)) formula.Onum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[4], out num)) formula.Pnum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[5], out num)) formula.Snum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[6], out num)) formula.Fnum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[7], out num)) formula.Clnum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[8], out num)) formula.Brnum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[9], out num)) formula.Inum = num; else { errorElement(); return false; }
        //    if (int.TryParse(elementArray[10], out num)) formula.Sinum = num; else { errorElement(); return false; }

        //    formula.M1IsotopicAbundance = SevenGoldenRulesCheck.GetM1IsotopicAbundance(formula);
        //    formula.M2IsotopicAbundance = SevenGoldenRulesCheck.GetM2IsotopicAbundance(formula);

        //    return true;
        //}

        /// <summary>
        /// This method is now used in MS-FINDER program.
        /// The results of formula finder will be stored as .FGT file format by this method.
        /// The result will be stored as ASCII format.
        /// </summary>
        /// <param name="filePath">Add the FGT file path</param>
        /// <param name="results">Add the list of formularesult</param>
        public static void FormulaResultsWriter(string filePath, List<FormulaResult> results)
        {
            using (var sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                sw.WriteLine("Version: 2");
                foreach (var result in results) { formulaStreamWriter(sw, result); }
            }
        }

        private static void formulaStreamWriter(StreamWriter sw, FormulaResult result)
        {
            sw.WriteLine("NAME: " + result.Formula.FormulaString);
            sw.WriteLine("EXACTMASS: " + result.Formula.Mass);
            sw.WriteLine("ISSELECTED: " + result.IsSelected.ToString());
            sw.WriteLine("ACCURATEMASS: " + result.MatchedMass);
            sw.WriteLine("MASSDIFFERENCE: " + result.MassDiff);
            sw.WriteLine("TOTALSCORE: " + result.TotalScore);
            sw.WriteLine("ISOTOPICINTENSITY[M+1]: " + result.M1IsotopicIntensity);
            sw.WriteLine("ISOTOPICINTENSITY[M+2]: " + result.M2IsotopicIntensity);
            sw.WriteLine("ISOTOPICDIFF[M+1]: " + result.M1IsotopicDiff);
            sw.WriteLine("ISOTOPICDIFF[M+2]: " + result.M2IsotopicDiff);
            sw.WriteLine("MASSDIFFSCORE: " + result.MassDiffScore);
            sw.WriteLine("ISOTOPICSCORE: " + result.IsotopicScore);
            sw.WriteLine("PRODUCTIONSCORE: " + result.ProductIonScore);
            sw.WriteLine("NEUTRALLOSSSCORE: " + result.NeutralLossScore);
            sw.WriteLine("PRODUCTIONPEAKNUMBER: " + result.ProductIonNum);
            sw.WriteLine("PRODUCTIONHITSNUMBER: " + result.ProductIonHits);
            sw.WriteLine("NEUTRALLOSSPEAKNUMBER: " + result.NeutralLossNum);
            sw.WriteLine("NEUTRALLOSSHITSNUMBER: " + result.NeutralLossHits);
            sw.WriteLine("RESOURCENAMES: " + result.ResourceNames);
            sw.WriteLine("RESOURCERECORDS: " + result.ResourceRecords);

            sw.WriteLine("ChemOntDescriptions: " + writeStringTextForChemOntology(result.ChemicalOntologyDescriptions));
            sw.WriteLine("ChemOntIDs: " + writeStringTextForChemOntology(result.ChemicalOntologyIDs));
            sw.WriteLine("ChemOntScores: " + writeStringTextForChemOntology(result.ChemicalOntologyScores));
            sw.WriteLine("ChemOntInChIKeys: " + writeStringTextForChemOntology(result.ChemicalOntologyRepresentativeInChIKeys));

            writePubchemCidsInfo(sw, result.PubchemResources);

            sw.WriteLine("Num ProductIon (Formula ExactMass AccurateMass Intensity Error CandidateInChIKeys CandidateOntologies): " + result.ProductIonResult.Count);
            foreach (var product in result.ProductIonResult) { writeProductArray(sw, product); }

            sw.WriteLine("Num NeutralLoss (Formula ExactMass PrecursorMz ProductMz PrecursorIntensity ProductIntensity LossMass Error CandidateInChIKeys CandidateOntologies): " + result.NeutralLossResult.Count);
            foreach (var loss in result.NeutralLossResult) { writeNeutralLossArray(sw, loss); }

            if (result.AnnotatedIonResult != null && result.AnnotatedIonResult.Count > 0) {
                sw.WriteLine("Num AdductIon (AdductType Mass LinkedMz): " + result.AnnotatedIonResult.Count(x => x.PeakType == AnnotatedIon.AnnotationType.Adduct));
                foreach (var line in result.AnnotatedIonResult) { writeAdduct(sw, line); }

                sw.WriteLine("Num IsotopicIon (IsotopeName Mass LinkedMz WeightedForm WeightNumber): " + result.AnnotatedIonResult.Count(x => x.PeakType == AnnotatedIon.AnnotationType.Isotope));
                foreach (var line in result.AnnotatedIonResult) { writeIsotope(sw, line); }
            }

            sw.WriteLine();
        }

        private static string writeStringTextForChemOntology(List<string> scripts) {
            var script = string.Empty;
            for (int i = 0; i < scripts.Count; i++) {
                if (i == scripts.Count - 1)
                    script += scripts[i];
                else
                    script += scripts[i] + ";";
            }
            return script;
        }

        private static string writeStringTextForChemOntology(List<double> scripts) {
            var script = string.Empty;
            for (int i = 0; i < scripts.Count; i++) {
                if (i == scripts.Count - 1) {
                    //script += Math.Round(scripts[i], 10);
                    script += scripts[i].ToString();
                }
                else {
                    //script += Math.Round(scripts[i], 10) + ";";
                    script += scripts[i].ToString() + ";";
                }
            }
            return script;
        }


        private static void writePubchemCidsInfo(StreamWriter sw, List<int> cids)
        {
            if (cids == null || cids.Count == 0) sw.WriteLine("PUBCHEMCIDS: ");
            else
            {
                sw.Write("PUBCHEMCIDS: ");
                for (int i = 0; i < cids.Count; i++)
                {
                    if (i == cids.Count - 1) sw.WriteLine(cids[i]);
                    else sw.Write(cids[i] + ",");
                }
            }
        }

        private static string ElementArray(Formula formula)
        {
            return formula.Cnum + "," + formula.Hnum + "," + formula.Nnum + "," + formula.Onum + "," + formula.Pnum + "," + formula.Snum + "," + formula.Fnum + "," + formula.Clnum + "," + formula.Brnum + "," + formula.Inum + "," + formula.Sinum;
        }

        private static void writeProductArray(StreamWriter sw, ProductIon product)
        {
            var candidateInChIKeys = getCandidateInChIKeys(product.CandidateInChIKeys);
            var candidateOntologies = getCandidateOntologies(product.CandidateOntologies);

            sw.WriteLine(product.Formula.FormulaString + "\t" + product.Formula.Mass + "\t" + product.Mass + "\t" + 
                product.Intensity + "\t" + Math.Round(product.MassDiff, 7) + "\t" + candidateInChIKeys + "\t" + candidateOntologies);
        }

        private static void writeNeutralLossArray(StreamWriter sw, NeutralLoss loss)
        {
            var candidateInChIKeys = getCandidateInChIKeys(loss.CandidateInChIKeys);
            var candidateOntologies = getCandidateOntologies(loss.CandidateOntologies);

            sw.WriteLine(loss.Formula.FormulaString + "\t" + loss.Formula.Mass + "\t" + 
                loss.PrecursorMz + "\t" + loss.ProductMz + "\t" + loss.PrecursorIntensity + "\t" + 
                loss.ProductIntensity + "\t" + loss.MassLoss + "\t" + Math.Round(loss.MassError, 7) 
                + "\t" + candidateInChIKeys + "\t" + candidateOntologies);
        }

        private static void writeAdduct(StreamWriter sw, AnnotatedIon annotation) {
            if (annotation.PeakType == AnnotatedIon.AnnotationType.Adduct) {
                sw.WriteLine(annotation.AdductIon.AdductIonName + "\t"  + annotation.AccurateMass + "\t" + annotation.LinkedAccurateMass);
            }
        }

        private static void writeIsotope(StreamWriter sw, AnnotatedIon annotation) {
            if (annotation.PeakType == AnnotatedIon.AnnotationType.Isotope) {
                sw.WriteLine(annotation.IsotopeName + "\t" +  + annotation.AccurateMass  + "\t" + annotation.LinkedAccurateMass + "\tM+" + annotation.IsotopeWeightNumber + "\t" + annotation.IsotopeWeightNumber );
            }
        }

        private static string getCandidateInChIKeys(List<string> candidateInChIKeys)
        {
            var candidateInChIKeysString = "NA";
            if (candidateInChIKeys != null && candidateInChIKeys.Count != 0) {
                candidateInChIKeysString = string.Empty;
                for (int i = 0; i < candidateInChIKeys.Count; i++) {
                    if (i == candidateInChIKeys.Count - 1)
                        candidateInChIKeysString += candidateInChIKeys[i];
                    else
                        candidateInChIKeysString += candidateInChIKeys[i] + ";";
                }
            }
            return candidateInChIKeysString;
        }

        private static string getCandidateOntologies(List<string> candidateOntologies) {
            var candidateOntologiesString = "NA";
            if (candidateOntologies != null && candidateOntologies.Count != 0) {
                candidateOntologiesString = string.Empty;
                for (int i = 0; i < candidateOntologies.Count; i++) {
                    if (i == candidateOntologies.Count - 1)
                        candidateOntologiesString += candidateOntologies[i];
                    else
                        candidateOntologiesString += candidateOntologies[i] + ";";
                }
            }
            return candidateOntologiesString;
        }

        private static string errorElement()
        {
            return "Bad format: Element number";
            //MessageBox.Show("Bad format: Element number", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string errorValue()
        {
            return "Bad format: Empty value exists.";
            //MessageBox.Show("Bad format: Empty value exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string errorProductIon()
        {
            return "Bad format: Product ion array.";
            //MessageBox.Show("Bad format: Product ion array.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string errorNeutralLoss()
        {
            return "Bad format: Neutral loss array.";
            //MessageBox.Show("Bad format: Neutral loss array.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static FormulaResult GetFormulaResultTemplateForSpectralDbSearch()
        {
            return new FormulaResult()
            {
                Formula = new Formula() { FormulaString = "Spectral DB search", Mass = -1 },
                IsSelected = true,
                TotalScore = 5.0
            };
        }
    }
}
