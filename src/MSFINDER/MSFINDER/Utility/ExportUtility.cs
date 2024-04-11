using Accord;
using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public static class ExportUtility
    {
        public static void PeakAnnotationResultExportAsMsp(MainWindow mainWindow, MainWindowVM mainWindowVM, string exportFilePath)
        {
            using (var sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                var files = mainWindowVM.AnalysisFiles;
                var param = mainWindowVM.DataStorageBean.AnalysisParameter;
                var error = string.Empty;

                foreach (var rawfile in files)
                {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error).OrderByDescending(n => n.TotalScore).ToList();
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }

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
            if (rawData.RetentionIndex > 0)
                sw.WriteLine("RETENTIONINDEX: " + rawData.RetentionIndex);
            sw.WriteLine("PRECURSORMZ: " + rawData.PrecursorMz);
            sw.WriteLine("PRECURSORTYPE: " + rawData.PrecursorType);
            if (rawData.Ccs > 0)
                sw.WriteLine("CCS: " + rawData.Ccs);
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

            var spectraList = new List<string>();
            var ms2Peaklist = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
            var maxIntensity = ms2Peaklist.Max(n => n.Intensity);
            //var commentList = FragmentAssigner.IsotopicPeakAssignmentForComment(ms2Peaklist, param.Mass2Tolerance, param.MassTolType);
            for (int i = 0; i < ms2Peaklist.Count; i++)
            {
                var mz = ms2Peaklist[i].Mz;
                var intensity = ms2Peaklist[i].Intensity;
                if (intensity / maxIntensity * 100 < param.RelativeAbundanceCutOff) continue;
                var comment = "";

                var originalComment = ms2Peaklist[i].Comment;
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

        public static void PeakAnnotationResultExportAsMassBankRecord(MainWindowVM mainWindowVM, string exportFolderPath) {
            var files = mainWindowVM.AnalysisFiles;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var error = string.Empty;

            foreach (var rawfile in files)
            {
                var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error).OrderByDescending(n => n.TotalScore).ToList();
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }

                var sfdFiles = Directory.GetFiles(rawfile.StructureFolderPath);
                var sfdResults = new List<FragmenterResult>();

                foreach (var sfdFile in sfdFiles)
                {
                    var sfdResult = FragmenterResultParcer.FragmenterResultReader(sfdFile);
                    var formulaString = Path.GetFileNameWithoutExtension(sfdFile);
                    sfdResultMerge(sfdResults, sfdResult, formulaString);
                }
                sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();
                using (var stream = File.Open(Path.Combine(exportFolderPath, getMassBankRecordAccession(rawData) + ".txt"), FileMode.Create, FileAccess.Write)) {
                    writeResultAsMassBankRecord(stream, rawData, formulaResults, sfdResults, param);
                }
            }
        }

        private static string getMassBankRecordAccession(RawData rawData) {
            var Identifier = "MSFINDER";
            var ContributorIdentifier = "XXXX";
            var id = rawData.ScanNumber;
            if (id < 0) {
                id = rawData.GetHashCode() % 100_000_000;
            }

            // accession
            return $"{Identifier}-{ContributorIdentifier}-{id:D8}";
        }

        private static void writeResultAsMassBankRecord(Stream stream, RawData rawData, List<FormulaResult> formulaResults, List<FragmenterResult> sfdResults, AnalysisParamOfMsfinder param) {
            using (var sw = new StreamWriter(stream, Encoding.ASCII, 4096, false)) {
                var Identifier = "MSFINDER";
                var ContributorIdentifier = "XXXX";

                // accession
                var id = rawData.ScanNumber;
                if (id < 0) {
                    id = rawData.GetHashCode() % 100_000_000;
                }
                sw.WriteLine($"ACCESSION: {Identifier}-{ContributorIdentifier}-{id:D8}");

                var MSType = "MS2";
                var name = string.IsNullOrEmpty(rawData.MetaboliteName) ? rawData.Name : rawData.MetaboliteName;
                // record title
                sw.WriteLine($"RECORD_TITLE: {name}; {rawData.InstrumentType}; {MSType}");

                // date
                var now = DateTime.UtcNow;
                sw.WriteLine($"DATE: {now:yyyy.MM.dd}");

                // authors
                sw.WriteLine($"AUTHORS: {rawData.Authors}");

                // license
                sw.WriteLine($"LICENSE: {rawData.License}");

                // copyright
                // publication
                // project

                // comment
                sw.WriteLineIfNotEmpty("COMMENT: {0}", rawData.Comment);
                
                // ch$name
                sw.WriteLine($"CH$NAME: {name}");

                // ch$compound class Natural product or Non-Natural product
                sw.WriteLine($"CH$COMPOUND_CLASS: {rawData.Ontology}");

                // ch$formula
                sw.WriteLine($"CH$FORMULA: {rawData.Formula}");

                // ch$exact_mass
                if (!string.IsNullOrEmpty(rawData.Formula) && FormulaStringParcer.IsOrganicFormula(rawData.Formula)) {
                    sw.WriteLine($"CH$EXACT_MASS: {FormulaStringParcer.OrganicElementsReader(rawData.Formula).Mass:F5}");
                }
                else {
                    var precursorMz = rawData.PrecursorMz;
                    var adduct = AdductIonParcer.GetAdductIonBean(rawData.PrecursorType);
                    sw.WriteLine($"CH$EXACT_MASS: {precursorMz * adduct.ChargeNumber - adduct.AdductIonAccurateMass:F5}");
                }

                // ch$smiles
                sw.WriteLine($"CH$SMILES: {rawData.Smiles}");

                // ch$iupac
                sw.WriteLine($"CH$IUPAC: {rawData.Inchi}");

                // ch$cdkdepict
                // ch$link
                sw.WriteLineIfNotEmpty("CH$LINK: INCHIKEY {0}", rawData.InchiKey);

                // sp$scientific_name
                // sp$lineage
                // sp$link
                // sp$sample

                // ac$instrument
                sw.WriteLine($"AC$INSTRUMENT: {rawData.Instrument}");

                // ac$instrument_type
                sw.WriteLine($"AC$INSTRUMENT_TYPE: {rawData.InstrumentType}");

                // ac$mass_spectrometry: ion_mode
                sw.WriteLine($"AC$MASS_SPECTROMETRY: ION_MODE {rawData.IonMode}");

                // ac$mass_spectrometry mstype
                sw.WriteLine($"AC$MASS_SPECTROMETRY: MS_TYPE {MSType}");

                // ac$mass_spectrometry: subtag
                sw.WriteLineIfPositive("AC$MASS_SPECTROMETRY: COLLISION_ENERGY {0}", rawData.CollisionEnergy);

                // ac$chromatography: subtag
                sw.WriteLineIfPositive("AC$CHROMATOGRAPHY: CCS {0}", rawData.Ccs);
                sw.WriteLineIfPositive("AC$CHROMATOGRAPHY: KOVATS_RTI {0}", rawData.RetentionIndex);
                sw.WriteLineIfPositive("AC$CHROMATOGRAPHY: RETENTION_TIME {0}", rawData.RetentionTime);

                // ac$general: subtag
                // ac$ion_mobility: subtag

                // ms$focused_ion: base_peak
                // ms$focused_ion: subtag
                sw.WriteLine($"MS$FOCUSED_ION: PRECURSOR_M/Z {rawData.PrecursorMz:F5}");
                sw.WriteLine($"MS$FOCUSED_ION: PRECURSOR_TYPE {rawData.PrecursorType}");

                // ms$data_processing: subtag

                // pk$splash
                var spectra = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
                var maxIntensity = spectra.Max(p => p.Intensity);
                spectra = spectra.Where(p => p.Intensity / maxIntensity * 100 >= param.RelativeAbundanceCutOff).ToList();
                if (spectra.Count > 0) {
                    var splashHandler = new NSSplash.Splash();
                    var splash = splashHandler.splashIt(new NSSplash.impl.MSSpectrum(string.Join(" ", spectra.Select(p => $"{p.Mz:F5}:{p.Intensity}"))));
                    sw.WriteLine($"PK$SPLASH: {splash}");
                }
                else {
                    sw.WriteLine("PK$SPLASH: NA");
                }

                // pk$annotation
                sw.WriteLine($"PK$ANNOTATION: m/z comment");
                for (int i = 0; i < spectra.Count; i++) {
                    var mz = spectra[i].Mz;

                    var comment = "";
                    var originalComment = spectra[i].Comment;
                    var additionalComment = getProductIonComment(mz, formulaResults, sfdResults, rawData.IonMode);
                    if (originalComment != "") {
                        comment = originalComment + "; " + additionalComment;
                    }
                    else {
                        comment = additionalComment;
                    }

                    if (comment != string.Empty) {
                        sw.WriteLine($"  {mz:F5} {comment}");
                    }
                }

                // pk$num_peak
                sw.WriteLine($"PK$NUM_PEAK: {spectra.Count}");

                // pk$peak
                sw.WriteLine("PK$PEAK: m/z int. rel.int.");

                for (int i = 0; i < spectra.Count; i++) {
                    var mz = spectra[i].Mz;
                    var intensity = spectra[i].Intensity;
                    sw.WriteLine($"  {mz:F5} {intensity} {intensity / maxIntensity * 999:F0}");
                }

                sw.WriteLine("//");
            }
        }

        private static void WriteLineIfNotEmpty(this StreamWriter sw, string format, string arg0) {
            if (!string.IsNullOrEmpty(arg0)) {
                sw.WriteLine(format, arg0);
            }
        }

        private static void WriteLineIfPositive(this StreamWriter sw, string format, double arg0) {
            if (arg0 > 0) {
                sw.WriteLine(format, arg0);
            }
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
            var formulaFile = Path.Combine(exportFolderPath , "Formula result-" + dateString + ".txt");
            var structureFile = Path.Combine(exportFolderPath, "Structure result-" + dateString + ".txt");

            var files = mainWindowVM.AnalysisFiles;
            var param = mainWindowVM.DataStorageBean.AnalysisParameter;
            var error = string.Empty;

            using (var sw = new StreamWriter(formulaFile, false, Encoding.ASCII))
            {
                writeFormulaResultHeader(sw, exportNumber);

                foreach (var rawfile in files)
                {
                    var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }

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
                    var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                    if (error != string.Empty) {
                        Console.WriteLine(error);
                    }

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
            var error = string.Empty;

            foreach (var rawfile in files) {
                var formulaFile = Path.Combine(exportFolderPath, "Formula result-" + rawfile.RawDataFileName + "-" + dateString + ".txt");
                var structureFile = Path.Combine(exportFolderPath, "Structure result-" + rawfile.RawDataFileName + "-" + dateString + ".txt");
                var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);

                var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }

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
            var error = string.Empty;

            foreach (var rawfile in files) {
                var rawData = RawDataParcer.RawDataFileReader(rawfile.RawDataFilePath, param);
                var formulaResults = FormulaResultParcer.FormulaResultReader(rawfile.FormulaFilePath, out error);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }

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

            int cnt = 0;
            if (formulaResults.Count > count) {
                cnt = count;
            } else {
                cnt = formulaResults.Count;
            }
            for (int i = 0; i < cnt; i++) {
                sw.Write(filepath + "\t");
                sw.Write(filename + "\t");
                sw.Write(rawData.Name + "\t");
                sw.Write(rawData.Ms1PeakNumber + "\t");
                sw.Write(rawData.Ms2PeakNumber + "\t");
                sw.Write(rawData.PrecursorMz + "\t");
                sw.Write(rawData.PrecursorType + "\t");
                sw.Write(formulaResults[i].Formula.FormulaString + "\t");
                sw.Write(formulaResults[i].Formula.Mass + "\t");
                sw.Write(formulaResults[i].MassDiff + "\t");
                sw.Write(formulaResults[i].TotalScore + "\t");
                sw.Write(formulaResults[i].ResourceNames);
                sw.WriteLine();
            }            
        }

        private static void writeStructureResult(StreamWriter sw, RawData rawData, List<FragmenterResult> sfdResults, int count)
        {
            var filepath = rawData.RawdataFilePath;
            var filename = System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath);

            int cnt = 0;
            if (sfdResults.Count > count) {
                cnt = count;
            } else {
                cnt = sfdResults.Count;
            }
            for (int i = 0; i < cnt; i++) {
                sw.Write(filepath + "\t");
                sw.Write(filename + "\t");
                sw.Write(rawData.Name + "\t");
                sw.Write(rawData.Ms1PeakNumber + "\t");
                sw.Write(rawData.Ms2PeakNumber + "\t");
                sw.Write(rawData.PrecursorMz + "\t");
                sw.Write(rawData.PrecursorType + "\t");
                sw.Write(sfdResults[i].Title + "\t");
                sw.Write(sfdResults[i].TotalScore + "\t");
                sw.Write(sfdResults[i].Resources + "\t");
                sw.Write(sfdResults[i].Formula + "\t");
                sw.Write(sfdResults[i].Ontology + "\t");
                sw.Write(sfdResults[i].Inchikey + "\t");
                sw.Write(sfdResults[i].Smiles);
                sw.WriteLine();
            }
        }


        private static void writeFormulaResultHeader(StreamWriter sw, int count)
        {
            sw.Write("File path\t");
            sw.Write("File name\t");
            sw.Write("Title\t");
            sw.Write("MS1 count\t");
            sw.Write("MSMS count\t");
            sw.Write("Precursor mz\t");
            sw.Write("Precursor type\t");

            sw.Write("Formula\t");
            sw.Write("Theoretical mass\t");
            sw.Write("Mass error\t");
            sw.Write("Formula score\t");
            sw.Write("Databases");
            
            sw.WriteLine();
        }

        private static void writeStructureResultHeader(StreamWriter sw, int count)
        {
            sw.Write("File path\t");
            sw.Write("File name\t");
            sw.Write("Title\t");
            sw.Write("MS1 count\t");
            sw.Write("MSMS count\t");
            sw.Write("Precursor mz\t");
            sw.Write("Precursor type\t");

            sw.Write("Structure\t");
            sw.Write("Total score\t");
            sw.Write("Databases\t");
            sw.Write("Formula\t");
            sw.Write("Ontology\t");
            sw.Write("InChIKey\t");
            sw.Write("SMILES");

            sw.WriteLine();
        }
    }
}
