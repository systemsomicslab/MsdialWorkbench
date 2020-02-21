using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class ExportUtility
    {
        public static void PeakAnnotationResultExportAsMsp(MainWindow mainWindow, MainWindowVM mainWindowVM, string exportFilePath)
        {
            using (var sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;

                foreach (var rawfile in files)
                {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath).OrderByDescending(n => n.TotalScore).ToList();
                    var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                    var sfdResults = new List<FragmenterResult>();

                    foreach (var sfdFile in sfdFiles)
                    {
                        var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                        var formulaString = System.IO.Path.GetFileNameWithoutExtension(sfdFile);
                        sfdResultMerge(sfdResults, sfdResult, formulaString);
                    }
                    sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();
                    writeResultAsMsp(sw, rawData, formulaResults, sfdResults, param);
                }
            }
        }

        private static void writeResultAsMsp(StreamWriter sw, RawData rawData, List<FormulaResult> formulaResults, List<FragmenterResult> sfdResults, AnalysisParamOfMsfinder param)
        {
            sw.WriteLine("NAME: " + rawData.Name);
            sw.WriteLine("SCANNUMBER: " + rawData.ScanNumber);
            sw.WriteLine("RETENTIONTIME: " + rawData.RetentionTime);
            sw.WriteLine("PRECURSORMZ: " + rawData.PrecursorMz);
            sw.WriteLine("PRECURSORTYPE: " + rawData.PrecursorType);
            sw.WriteLine("IONMODE: " + rawData.IonMode);
            sw.WriteLine("SPECTRUMTYPE: " + rawData.SpectrumType);
            sw.WriteLine("FORMULA: " + rawData.Formula);
            sw.WriteLine("INCHIKEY: " + rawData.InchiKey);
            sw.WriteLine("INCHI: " + rawData.Inchi);
            sw.WriteLine("SMILES: " + rawData.Smiles);
            sw.WriteLine("AUTHORS: " + rawData.Authors);
            sw.WriteLine("COLLISIONENERGY: " + rawData.CollisionEnergy);
            sw.WriteLine("INSTRUMENT: " + rawData.Instrument);
            sw.WriteLine("INSTRUMENTTYPE: " + rawData.InstrumentType);
            sw.WriteLine("IONIZATION: " + rawData.Ionization);
            sw.WriteLine("LICENSE: " + rawData.License);
            sw.WriteLine("COMMENT: " + rawData.Comment);

            var spectra = rawData.Ms2Spectrum.PeakList.OrderBy(n => n.Mz).ToList();
            var maxIntensity = spectra.Max(n => n.Intensity);
            var spectraList = new List<string>();
            var ms2Peaklist = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
            //var commentList = FragmentAssigner.IsotopicPeakAssignmentForComment(ms2Peaklist, param.Mass2Tolerance, param.MassTolType);
            for (int i = 0; i < rawData.Ms2PeakNumber; i++)
            {
                var mz = spectra[i].Mz;
                var intensity = spectra[i].Intensity;
                if (intensity / maxIntensity * 100 < param.RelativeAbundanceCutOff) continue;
                var comment = "";

                var originalComment = spectra[i].Comment;
                var additionalComment = getProductIonComment(mz, formulaResults, sfdResults, rawData.IonMode);
                if (originalComment != "")
                    comment = originalComment + "; " + additionalComment;
                else
                    comment = additionalComment;

                var peakString = string.Empty;
                if (comment == string.Empty)
                    peakString = Math.Round(mz, 5) + "\t" + intensity;
                else
                    peakString = Math.Round(mz, 5) + "\t" + intensity + "\t" + "\"" + comment + "\"";

                spectraList.Add(peakString);
            }
            sw.WriteLine("Num Peaks: " + spectraList.Count);
            for (int i = 0; i < spectraList.Count; i++)
                sw.WriteLine(spectraList[i]);

            sw.WriteLine();
        }

        private static string getProductIonComment(double mz, List<FormulaResult> formulaResults, List<FragmenterResult> sfdResults, IonMode ionMode) {
            if (sfdResults == null || sfdResults.Count == 0) return string.Empty;
            if (formulaResults == null || formulaResults.Count == 0) return string.Empty;

            var productIonResult = formulaResults[0].ProductIonResult;
            var annotationResult = formulaResults[0].AnnotatedIonResult;
            var fragments = sfdResults[0].FragmentPics;
            if (fragments == null || fragments.Count == 0) { return ""; }

            foreach (var frag in fragments) {
                if (Math.Abs(frag.Peak.Mz - mz) < 0.00001) {
                    var annotation = UiAccessUtility.GetLabelForInsilicoSpectrum(frag.MatchedFragmentInfo.Formula, frag.MatchedFragmentInfo.RearrangedHydrogen, ionMode, frag.MatchedFragmentInfo.AssignedAdductString);
                    var comment = "Theoretical m/z " + Math.Round(frag.MatchedFragmentInfo.MatchedMass, 6) + ", Mass diff " + Math.Round(frag.MatchedFragmentInfo.Massdiff, 3) + " (" + Math.Round(frag.MatchedFragmentInfo.Ppm, 3) + " ppm), SMILES " + frag.MatchedFragmentInfo.Smiles + ", " + "Annotation " + annotation + ", " + "Rule of HR " + frag.MatchedFragmentInfo.IsHrRule;
                    return comment;
                }
            }

            if (productIonResult.Count == 0) return string.Empty;

            foreach (var product in productIonResult) {
                if (Math.Abs(product.Mass - mz) < 0.00001) {
                    var ppm = Math.Round((mz - product.Mass) / product.Mass * 1000000, 3);
                    var comment = "Theoretical m/z " + Math.Round(product.Formula.Mass, 6) + ", Mass diff " + Math.Round(product.MassDiff, 3) +" (" + ppm + " ppm), Formula " + product.Formula.FormulaString;
                    return comment;
                }
            }

            if (annotationResult.Count == 0) return string.Empty;

            foreach (var ion in annotationResult) {
                if(Math.Abs(ion.AccurateMass - mz) < 0.00001) {                    
                    var comment = "";
                    if(ion.PeakType == AnnotatedIon.AnnotationType.Adduct) {
                        comment = "Adduct ion, " + ion.AdductIon.AdductIonName + ", linkedMz " + ion.LinkedAccurateMass;
                    }else if(ion.PeakType == AnnotatedIon.AnnotationType.Isotope) {
                        comment = "Isotopic ion, M+" + ion.IsotopeWeightNumber  + ", linkedMz " + ion.LinkedAccurateMass;
                     //   comment = "Isotopic ion, " + ion.IsotopeName + ", linkedMz " + ion.LinkedAccurateMass;
                    }
                    return comment;
                }
            }

            return string.Empty;
        }

/*        private static string getProductIonComment2(double mz, List<FormulaResult> formulaResults, List<FragmenterResult> sfdResults, IonMode ionMode)
        {
            if (sfdResults == null || sfdResults.Count == 0) return string.Empty;
            if (formulaResults == null || formulaResults.Count == 0) return string.Empty;

            var productIonResult = formulaResults[0].ProductIonResult;
            var fragments = sfdResults[0].FragmentPics;
            
            foreach (var frag in fragments)
            {
                var peak = frag.Peak;
                var matchedInfo = frag.MatchedFragmentInfo;


                if (Math.Abs(peak.Mz - mz) < 0.00001)
                {
                    var annotation = UiAccessUtility.GetLabelForInsilicoSpectrum(matchedInfo.Formula, matchedInfo.RearrangedHydrogen, ionMode, matchedInfo.AssignedAdductString);
                    var comment = "SMILES: " + matchedInfo.Smiles + ", " + "Annotation: " + annotation + ", " + "Rule of HR: " + matchedInfo.IsHrRule;
                    return comment;
                }
            }

            if (productIonResult.Count == 0) return string.Empty;

            foreach (var product in productIonResult)
            {
                if (Math.Abs(product.Mass - mz) < 0.00001)
                {
                    var comment = "Formula: " + product.Formula.FormulaString;
                    return comment;
                }
            }

            return string.Empty;
        }

    */
        public static void BatchExport(MainWindow mainWindow, MainWindowVM mainWindowVM, string exportFolderPath, int exportNumber)
        {
            var dt = DateTime.Now;
            var dateString = dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute;
            var formulaFile = exportFolderPath + "\\" + "Formula result-" + dateString + ".txt";
            var structureFile = exportFolderPath + "\\" + "Structure result-" + dateString + ".txt";

            var files = mainWindowVM.AnalysisFiles;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;

            using (var sw = new StreamWriter(formulaFile, false, Encoding.ASCII))
            {
                writeFormulaResultHeader(sw, exportNumber);

                foreach (var rawfile in files)
                {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath);

                    if (formulaResults.Count > 0) {
                        formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                    }
                    writeFormulaResult(sw, rawData, formulaResults, exportNumber);
                }
            }

            using (var sw = new StreamWriter(structureFile, false, Encoding.ASCII))
            {
                writeStructureResultHeader(sw, exportNumber);

                foreach (var rawfile in files)
                {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath);
                    var sfdResults = new List<FragmenterResult>();

                    if (formulaResults.Count > 0) {
                        formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                        var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);

                        foreach (var sfdFile in sfdFiles) {
                            var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                            var formulaString = System.IO.Path.GetFileNameWithoutExtension(sfdFile);
                            sfdResultMerge(sfdResults, sfdResult, formulaString);
                        }

                        if (sfdResults.Count > 0) {
                            sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();
                        }
                    }
                    writeStructureResult(sw, rawData, sfdResults, exportNumber);
                }
            }
        }

        public static void BatchExportForEachFile(MainWindow mainWindow, MainWindowVM mainWindowVM, string exportFolderPath, int exportNumber)
        {
            var dt = DateTime.Now;
            var dateString = dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute;
            var files = mainWindowVM.AnalysisFiles;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;

            foreach (var rawfile in files) {
                var formulaFile = exportFolderPath + "\\" + "Formula result-" + rawfile.RawDataFileName + "-" + dateString + ".txt";
                var structureFile = exportFolderPath + "\\" + "Structure result-" + rawfile.RawDataFileName + "-" + dateString + ".txt";
                var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);

                var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath);
                formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();

                var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                var sfdResults = new List<FragmenterResult>();

                foreach (var sfdFile in sfdFiles) {
                    var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                    var formulaString = System.IO.Path.GetFileNameWithoutExtension(sfdFile);
                    sfdResultMerge(sfdResults, sfdResult, formulaString);
                }
                sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();

                writeFormulaResult(formulaFile, rawData, formulaResults);
                writeStructureResult(structureFile, rawData, sfdResults);
            }
        }

        public static void ReflectMsfinderResultToMspFile(MainWindow mainWindow, MainWindowVM mainWindowVM) {
            var files = mainWindowVM.AnalysisFiles;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;

            foreach (var rawfile in files) {
                var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath);
                formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                if (formulaResults.Count == 0) continue;

                var sfdFiles = System.IO.Directory.GetFiles(rawfile.StructureFolderPath);
                var sfdResults = new List<FragmenterResult>();

                foreach (var sfdFile in sfdFiles) {
                    var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                    var formulaString = System.IO.Path.GetFileNameWithoutExtension(sfdFile);
                    sfdResultMerge(sfdResults, sfdResult, formulaString);
                }
                sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();
                if (sfdResults.Count == 0) {
                    var result = formulaResults[0];
                    rawData.Name = "(MF) " + result.Formula.FormulaString;
                    rawData.Formula = result.Formula.FormulaString;
                }
                else {
                    var result = sfdResults[0];
                    rawData.Name = "(MF) " + result.Title;
                    rawData.Formula = result.Formula;
                    rawData.Ontology = result.Ontology;
                    rawData.Smiles = result.Smiles;
                    rawData.InchiKey = result.Inchikey;
                }
                RawDataParcer.RawDataFileWriter(rawData.RawdataFilePath, rawData);
            }
        }

        private static void writeStructureResult(string output, RawData rawData, List<FragmenterResult> structureResults)
        {
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                writeRawdataMetadataField(sw, rawData);
                sw.WriteLine("Rank\tTitle\tScore\tRetention time\tRetention index\tResources\tFormula\tOntology\tInChIKey\tSMILES");
                for (int i = 0; i < structureResults.Count; i++) {
                    var result = structureResults[i];
                    sw.WriteLine((i + 1).ToString() + "\t" + result.Title + "\t" + result.TotalScore + "\t" + result.RetentionTime + "\t" + 
                        result.RetentionIndex + "\t" + result.Resources + "\t" + result.Formula + "\t" + result.Ontology + "\t" + result.Inchikey + "\t" + result.Smiles);
                }
            }
        }

        private static void writeFormulaResult(string output, RawData rawData, List<FormulaResult> formulaResults)
        {
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                writeRawdataMetadataField(sw, rawData);
                sw.WriteLine("Rank\tFormula\tMass\tMass error\tScore\tResources");
                for (int i = 0; i < formulaResults.Count; i++) {
                    var result = formulaResults[i];
                    sw.WriteLine((i + 1).ToString() + "\t" + result.Formula.FormulaString + "\t" + result.Formula.Mass + "\t" + result.MassDiff + "\t" + result.TotalScore + "\t" + result.ResourceNames);
                }
            }
        }

        private static void writeRawdataMetadataField(StreamWriter sw, RawData rawData)
        {
            sw.WriteLine("File path: " + rawData.RawdataFilePath);
            sw.WriteLine("File name: " + System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath));
            sw.WriteLine("Title: " + rawData.Name);
            sw.WriteLine("MS1 count: " + rawData.Ms1PeakNumber);
            sw.WriteLine("MSMS count: " + rawData.Ms2PeakNumber);
            sw.WriteLine("PRECURSORMZ: " + rawData.PrecursorMz);
            sw.WriteLine("PRECURSORTYPE: " + rawData.PrecursorType);
            sw.WriteLine("Intensity: " + rawData.Intensity);
            sw.WriteLine("Retention time: " + rawData.RetentionTime);
            sw.WriteLine("Retention index: " + rawData.RetentionIndex);
        }

        private static void sfdResultMerge(List<FragmenterResult> mergedList, List<FragmenterResult> results, string formulaString = "")
        {
            if (results == null || results.Count == 0) return;

            foreach (var result in results)
            {
                result.Formula = formulaString;
                mergedList.Add(result);
            }
        }

        private static void writeFormulaResult(StreamWriter sw, RawData rawData, List<FormulaResult> formulaResults, int count)
        {
            var filepath = rawData.RawdataFilePath;
            var filename = System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath);

            sw.Write(filepath + "\t");
            sw.Write(filename + "\t");
            sw.Write(rawData.Name + "\t");
            sw.Write(rawData.Ms1PeakNumber + "\t");
            sw.Write(rawData.Ms2PeakNumber + "\t");
            sw.Write(rawData.PrecursorMz + "\t");
            sw.Write(rawData.PrecursorType + "\t");

            for (int i = 0; i < count; i++)
            {
                if (formulaResults.Count - 1 < i)
                    sw.Write("" + "\t" + "" + "\t" + "" + "\t" + "" + "\t" + "" + "\t");
                else
                    sw.Write(formulaResults[i].Formula.FormulaString + "\t" + formulaResults[i].Formula.Mass + "\t" + formulaResults[i].MassDiff + "\t" + formulaResults[i].TotalScore + "\t" + formulaResults[i].ResourceNames + "\t");
            }
            sw.WriteLine();
        }

        private static void writeStructureResult(StreamWriter sw, RawData rawData, List<FragmenterResult> sfdResults, int count)
        {
            var filepath = rawData.RawdataFilePath;
            var filename = System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath);

            sw.Write(filepath + "\t");
            sw.Write(filename + "\t");
            sw.Write(rawData.Name + "\t");
            sw.Write(rawData.Ms1PeakNumber + "\t");
            sw.Write(rawData.Ms2PeakNumber + "\t");
            sw.Write(rawData.PrecursorMz + "\t");
            sw.Write(rawData.PrecursorType + "\t");

            for (int i = 0; i < count; i++)
            {
                if (sfdResults.Count - 1 < i)
                    sw.Write("" + "\t" + "" + "\t" + "" + "\t" + "" + "\t" + "" + "\t" + "" + "\t" + "" + "\t");
                else if (sfdResults[i].Title == null || sfdResults[i].Title == string.Empty)
                    sw.Write("" + "\t" + "" + "\t" + "" + "\t" + "" + "\t" + "" + "\t" + "" + "\t" + "" + "\t");
                else
                    sw.Write(sfdResults[i].Title + "\t" + sfdResults[i].TotalScore + "\t" + sfdResults[i].Resources + "\t" +
                        sfdResults[i].Formula + "\t" + sfdResults[i].Ontology + "\t" + sfdResults[i].Inchikey + "\t" + sfdResults[i].Smiles + "\t");
            }
            sw.WriteLine();
        }


        private static void writeFormulaResultHeader(StreamWriter sw, int count)
        {
            sw.Write("File path\t");
            sw.Write("File name\t");
            sw.Write("Title\t");
            sw.Write("MS1 count\t");
            sw.Write("MSMS count\t");
            sw.Write("PRECURSORMZ\t");
            sw.Write("PRECURSORTYPE\t");

            for (int i = 1; i <= count; i++)
                sw.Write("Formula rank " + i.ToString() + "\t" + "Theoretical mass" + "\t" + "Mass error" + "\t" + "Formula score" + "\t" + "Databases" + "\t");
            sw.WriteLine();
        }

        private static void writeStructureResultHeader(StreamWriter sw, int count)
        {
            sw.Write("File path\t");
            sw.Write("File name\t");
            sw.Write("Title\t");
            sw.Write("MS1 count\t");
            sw.Write("MSMS count\t");
            sw.Write("PRECURSORMZ\t");
            sw.Write("PRECURSORTYPE\t");

            for (int i = 1; i <= count; i++)
                sw.Write("Structure rank " + i.ToString() + "\t" + "Total score" + "\t" + "Databases" + "\t" + "Formula" + "\t" + "Ontology" + "\t" + "InChIKey" + "\t" + "SMILES" + "\t");

            sw.WriteLine();
        }
    }
}
