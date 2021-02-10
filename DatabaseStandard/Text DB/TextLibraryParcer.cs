using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class TextLibraryParcer
    {
        private TextLibraryParcer() { }

        public static List<TextFormatCompoundInformationBean> TextLibraryReader(string filePath, out string error)
        {
            error = string.Empty;

            var textQueries = new List<TextFormatCompoundInformationBean>();
            var textQuery = new TextFormatCompoundInformationBean(); 

            string line, errorMessage = string.Empty, adductString = string.Empty, inchikey = string.Empty;
            string[] lineArray;
            float retentionTime, accurateMass, ccs;
            int counter = 1;

            using (StreamReader sr = new StreamReader(filePath, Encoding.ASCII))
            {
                sr.ReadLine();

                while (sr.Peek() > -1)
                {
                    #region
                    line = sr.ReadLine();
                    counter++;
                    
                    if (line == string.Empty) break;

                    lineArray = line.Split('\t');

                    if (lineArray.Length < 2) { errorMessage += "Error type 1: line " + counter + " is not suitable.\r\n"; continue; }

                    retentionTime = -1; accurateMass = -1;

                    textQuery = new TextFormatCompoundInformationBean();
                    textQuery.MetaboliteName = lineArray[0];
                    textQuery.ReferenceId = counter - 2;
                    textQuery.RetentionTime = -1;

                    if (lineArray.Length >= 2)
                    {
                        if (float.TryParse(lineArray[1], out accurateMass))
                        {
                            if (accurateMass < 0)
                            {
                                errorMessage += "Error type 3: line " + counter + "includes negative value for accurate mass information.\r\n";
                                continue;
                            }
                            else
                            {
                                textQuery.AccurateMass = accurateMass;
                            }
                        }
                        else
                        {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for accurate mass information.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 3)
                    {
                        if (float.TryParse(lineArray[2], out retentionTime))
                        {
                            textQuery.RetentionTime = retentionTime;
                        }
                        else
                        {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for retention time information.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 4)
                    {
                        adductString = lineArray[3];
                        if (adductString != null && adductString != string.Empty)
                        {
                            textQuery.AdductIon = AdductIonParcer.GetAdductIonBean(adductString);
                        }
                    }

                    if (lineArray.Length >= 5) {
                        inchikey = lineArray[4];
                        textQuery.Inchikey = inchikey;
                    }

                    if (lineArray.Length >= 6) {
                        textQuery.Formula = FormulaStringParcer.OrganicElementsReader(lineArray[5]);
                    }

                    if (lineArray.Length >= 7) {
                        textQuery.Smiles = lineArray[6];
                    }

                    if (lineArray.Length >= 8) {
                        textQuery.Ontology = lineArray[7];
                    }

                    if (lineArray.Length >= 9) {
                        if (float.TryParse(lineArray[8], out ccs)) {
                            textQuery.Ccs = ccs;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for CCS information.\r\n";
                            continue;
                        }
                    }


                    #endregion

                    textQueries.Add(textQuery);
                }

                if (textQueries.Count == 0)
                {
                    errorMessage += "Error type 4: This library doesn't include suitable information.\r\n";
                }
            }

            if (errorMessage != string.Empty)
            {
                errorMessage += "\r\n";
                errorMessage += "You should write the following information as the reference library for post identification method.\r\n";
                errorMessage += "First- and second columns are required, and the others are option.\r\n";

                errorMessage += "[0]Compound Name\t[1]m/z\t[2]Retention time[min]\t[3]adduct\t[4]inchikey\t[5]formula\t[6]smiles\t[7]ontology\t[8]CCS\r\n";
                errorMessage += "Metabolite A\t100.000\t5.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";
                errorMessage += "Metabolite B\t200.000\t6.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";
                errorMessage += "Metabolite C\t300.000\t7.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";
                errorMessage += "Metabolite D\t400.000\t8.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";
                errorMessage += "Metabolite E\t500.000\t9.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";

                error = errorMessage;
                //MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }
            if (textQueries.Count > 0) {
                textQueries = textQueries.OrderBy(n => n.AccurateMass).ToList();
                for (int i = 0; i < textQueries.Count; i++)
                    textQueries[i].ReferenceId = i;
            }
            return textQueries;
        }

        public static List<TextFormatCompoundInformationBean> TargetFormulaLibraryReader(string filePath, out string error)
        {
            error = string.Empty;
            var textQueries = new List<TextFormatCompoundInformationBean>();
            var textQuery = new TextFormatCompoundInformationBean();

            string line, errorMessage = string.Empty, adductString = string.Empty, inchikey = string.Empty;
            string[] lineArray;
            float retentionTime, accurateMass, ccs;
            int counter = 1;

            using (StreamReader sr = new StreamReader(filePath, Encoding.ASCII))
            {
                sr.ReadLine();

                while (sr.Peek() > -1)
                {
                    #region
                    line = sr.ReadLine();
                    counter++;

                    if (line == string.Empty) break;

                    lineArray = line.Split('\t');

                    if (lineArray.Length < 2) { errorMessage += "Error type 1: line " + counter + " is not suitable.\r\n"; continue; }

                    retentionTime = -1; accurateMass = -1;

                    textQuery = new TextFormatCompoundInformationBean();
                    textQuery.MetaboliteName = lineArray[0];
                    textQuery.ReferenceId = counter - 2;
                    textQuery.Formula = FormulaStringParcer.OrganicElementsReader(lineArray[0]);

                    if (lineArray.Length >= 2)
                    {
                        if (float.TryParse(lineArray[1], out accurateMass))
                        {
                            if (accurateMass < 0)
                            {
                                errorMessage += "Error type 3: line " + counter + "includes negative value for accurate mass information.\r\n";
                                continue;
                            }
                            else
                            {
                                textQuery.AccurateMass = accurateMass;
                            }
                        }
                        else
                        {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for accurate mass information.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 3)
                    {
                        if (float.TryParse(lineArray[2], out retentionTime))
                        {
                            textQuery.RetentionTime = retentionTime;
                        }
                        else
                        {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for retention time information.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 4)
                    {
                        adductString = lineArray[3];
                        if (adductString != null && adductString != string.Empty)
                        {
                            textQuery.AdductIon = AdductIonParcer.GetAdductIonBean(adductString);
                        }
                    }

                    if (lineArray.Length >= 5) {
                        inchikey = lineArray[4];
                        textQuery.Inchikey = inchikey;
                    }

                    if (lineArray.Length >= 6) {
                        textQuery.Formula = FormulaStringParcer.OrganicElementsReader(lineArray[5]);
                    }

                    if (lineArray.Length >= 7) {
                        textQuery.Smiles = lineArray[6];
                    }

                    if (lineArray.Length >= 8) {
                        textQuery.Ontology = lineArray[7];
                    }

                    if (lineArray.Length >= 9) {
                        if (float.TryParse(lineArray[8], out ccs)) {
                            textQuery.Ccs = ccs;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for CCS information.\r\n";
                            continue;
                        }
                    }

                    #endregion

                    textQueries.Add(textQuery);
                }

                if (textQueries.Count == 0)
                {
                    errorMessage += "Error type 4: This library doesn't include suitable information.\r\n";
                }
            }

            if (errorMessage != string.Empty)
            {
                errorMessage += "\r\n";
                errorMessage += "You should write the following information as the target formula library.\r\n";
                errorMessage += "The first-, second, and third columns are required, and the others are option.\r\n";
                errorMessage += "The first column must be a formula.\r\n";

                errorMessage += "[0]Formula\t[1]m/z\t[2]Retention time [min]\t[3]adduct\t[4]inchikey\t[5]formula\t[6]smiles\t[7]ontology\t[8]CCS\r\n";
                errorMessage += "Formula A\t100.000\t5.0\t[M+H]+\tAAAAAAAAAAAAAAA-BBBBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";
                errorMessage += "Formula B\t200.000\t6.0\t[M+Na]+\tAAAAAAAAAAAAAAA-BBBBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";
                errorMessage += "Formula C\t300.000\t7.0\t[M+2H2O]+\tAAAAAAAAAAAAAAA-BBBBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";
                errorMessage += "Formula D\t400.000\t8.0\t[M+2H]2+\tAAAAAAAAAAAAAAA-BBBBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";
                errorMessage += "Formula E\t500.000\t9.0\t[M+H]+\tAAAAAAAAAAAAAAA-BBBBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2\r\n";

                error = errorMessage;
                //MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }

            return textQueries;
        }

        public static List<Peak> TextToSpectrumList(string specText, char mzIntSep, char peakSep, double threshold = 0.0) {
            var peaks = new List<Peak>();

            if (specText == null || specText == string.Empty) return peaks;
            var specArray = specText.Split(peakSep).ToList();
            if (specArray.Count == 0) return peaks;

            foreach (var spec in specArray) {
                var mzInt = spec.Split(mzIntSep).ToArray();
                if (mzInt.Length >= 2) {
                    var mz = mzInt[0];
                    var intensity = mzInt[1];

                    var peak = new Peak() { Mz = double.Parse(mz), Intensity = double.Parse(intensity) };
                    if (peak.Intensity > threshold) {
                        peaks.Add(peak);
                    }
                }
            }

            return peaks;
        }

        public static List<TextFormatCompoundInformationBean> StandardTextLibraryReader(string filePath, out string error) {
            error = string.Empty;

            var textQueries = new List<TextFormatCompoundInformationBean>();
            var textQuery = new TextFormatCompoundInformationBean();

            string line, errorMessage = string.Empty, adductString = string.Empty, inchikey = string.Empty;
            string[] lineArray;
            float retentionTime, accurateMass, rtTol, massTol, minInt;
            int counter = 1;

            using (StreamReader sr = new StreamReader(filePath, Encoding.ASCII)) {
                sr.ReadLine();

                while (sr.Peek() > -1) {
                    #region
                    line = sr.ReadLine();
                    counter++;

                    if (line == string.Empty) break;

                    lineArray = line.Split('\t');

                    if (lineArray.Length < 2) { errorMessage += "Error type 1: line " + counter + " is not suitable.\r\n"; continue; }

                    retentionTime = -1; accurateMass = -1; rtTol = -1; massTol = -1;

                    textQuery = new TextFormatCompoundInformationBean();
                    textQuery.MetaboliteName = lineArray[0];
                    textQuery.ReferenceId = textQueries.Count;

                    if (lineArray.Length >= 2) {
                        if (float.TryParse(lineArray[1], out retentionTime)) {
                            textQuery.RetentionTime = retentionTime;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for retention time information.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 4) {
                        if (float.TryParse(lineArray[3], out accurateMass)) {
                            if (accurateMass < 0) {
                                errorMessage += "Error type 3: line " + counter + "includes negative value for accurate mass information.\r\n";
                                continue;
                            }
                            else {
                                textQuery.AccurateMass = accurateMass;
                            }
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for accurate mass information.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 3) {
                        if (float.TryParse(lineArray[2], out rtTol)) {
                            textQuery.RetentionTimeTolerance = rtTol;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for retention time tolerance.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 5) {
                        if (float.TryParse(lineArray[4], out massTol)) {
                            textQuery.AccurateMassTolerance = massTol;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for accurate mass tolerance.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 6) {
                        if (float.TryParse(lineArray[5], out minInt)) {
                            textQuery.MinimumPeakHeight = minInt;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for minimum peak height.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 7) {
                        if (bool.TryParse(lineArray[6], out var res)) {
                            textQuery.IsTarget = res; 
                        }
                    }

                    #endregion

                    textQueries.Add(textQuery);
                }

                if (textQueries.Count == 0) {
                    errorMessage += "Error type 4: This library doesn't include suitable information.\r\n";
                }
            }

            if (errorMessage != string.Empty) {
                errorMessage += "\r\n";
                errorMessage += "You should write the following information as the target standard library.\r\n";

                errorMessage += "[0]Name\t[1]RetentionTime\t[2]RT tolerance\t[3]m/z\t[4]m/z tolerance\t[5]Min Height\t[6]Include\r\n";
                errorMessage += "Name A\t2.0\t0.1\t100.000\t0.01\t1000\ttrue\r\n";
                errorMessage += "Name B\t4.0\t0.1\t200.000\t0.01\t1000\ttrue\r\n";
                errorMessage += "Name C\t6.0\t0.1\t300.000\t0.01\t1000\ttrue\r\n";
                errorMessage += "Name D\t7.0\t0.1\t400.000\t0.01\t1000\ttrue\r\n";
                errorMessage += "Name E\t10.0\t0.1\t500.000\t0.01\t1000\ttrue\r\n";

                error = errorMessage;
                //MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }

            return textQueries;
        }

        public static List<TextFormatCompoundInformationBean> CompoundListInTargetModeReader(string filePath, out string error) {
            error = string.Empty;

            var textQueries = new List<TextFormatCompoundInformationBean>();
            var textQuery = new TextFormatCompoundInformationBean();

            string line, errorMessage = string.Empty, adductString = string.Empty, inchikey = string.Empty;
            string[] lineArray;
            float accurateMass, massTol;
            int counter = 1;

            using (StreamReader sr = new StreamReader(filePath, Encoding.ASCII)) {
                sr.ReadLine();

                while (sr.Peek() > -1) {
                    #region
                    line = sr.ReadLine();
                    counter++;

                    if (line == string.Empty) break;

                    lineArray = line.Split('\t');

                    if (lineArray.Length < 2) { errorMessage += "Error type 1: line " + counter + " is not suitable.\r\n"; continue; }

                    accurateMass = -1; massTol = -1;

                    textQuery = new TextFormatCompoundInformationBean();
                    textQuery.MetaboliteName = lineArray[0];
                    textQuery.ReferenceId = textQueries.Count;

                    if (lineArray.Length >= 2) {
                        if (float.TryParse(lineArray[1], out accurateMass)) {
                            textQuery.AccurateMass = accurateMass;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for accurate mass information.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 4) {
                        if (float.TryParse(lineArray[3], out massTol)) {
                            if (massTol < 0) {
                                errorMessage += "Error type 3: line " + counter + "includes negative value for accurate mass tolerance.\r\n";
                                continue;
                            }
                            else {
                                textQuery.AccurateMassTolerance = massTol;
                            }
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for accurate mass tolerance information.\r\n";
                            continue;
                        }
                    }
                                        #endregion

                    textQueries.Add(textQuery);
                }

                if (textQueries.Count == 0) {
                    errorMessage += "Error type 4: This library doesn't include suitable information.\r\n";
                }
            }

            if (errorMessage != string.Empty) {
                errorMessage += "\r\n";
                errorMessage += "You should write the following information as the target standard library.\r\n";

                errorMessage += "[0]Name\t[1]m/z\t[2]m/z tolerance\r\n";
                errorMessage += "Name A\t100.000\t0.01\r\n";
                errorMessage += "Name B\t200.000\t0.01\r\n";
                errorMessage += "Name C\t300.000\t0.01\r\n";

                error = errorMessage;
                //MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }

            return textQueries;
        }
    }
}
