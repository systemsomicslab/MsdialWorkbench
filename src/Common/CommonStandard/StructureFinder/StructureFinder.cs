using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parameter;
using CompMs.Common.PugRestApiStandard;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.StructureFinder.Parser;
using CompMs.Common.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Common.StructureFinder
{
    public class StructureFinder
    {
        private string tempSdfDirect;

        public StructureFinder()
        {
            TempFolderInitialize();
        }

        public void InsilicoFragmentGenerator(string filepath, string output)
        {
            var smilesCodes = new List<string>();
            using (var sr = new StreamReader(filepath, Encoding.ASCII))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line != string.Empty)
                        smilesCodes.Add(line.Trim());
                }
            }
            var counter = 0;
            var syncObj = new object();
            Parallel.ForEach(smilesCodes, smiles =>
            {
                var fragments = MainProcess.InsilicoFragmentGenerator(smiles);
                lock (syncObj)
                {
                    FragmenterResultParser.InsilicoFragmentResultWriter(output, smiles, fragments);

                    counter++;
                    if (!Console.IsOutputRedirected)
                    {
                        Console.Write("Progress {0}/{1}", counter, smilesCodes.Count);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else
                    {
                        Console.WriteLine("Progress {0}/{1}", counter, smilesCodes.Count);
                    }
                }
            });
        }

        public void StructureFinderMainProcess(RawData rawData,
            FormulaResult formulaResult,
            AnalysisParamOfMsfinder analysisParam,
            string exportFilePath,
            List<ExistStructureQuery> existStructureDB,
            List<ExistStructureQuery> userDefinedDB,
            List<ExistStructureQuery> mineStructureDB,
            List<FragmentLibrary> fragmentDB,
            List<FragmentOntology> fragmentOntologies,
            List<MoleculeMsReference> mspDB)
        {
            if (rawData.Ms2PeakNumber <= 0) return;
            if (formulaResult is null || formulaResult.Formula is null || formulaResult.Formula.FormulaString == string.Empty) return;
            File.Create(exportFilePath).Close();

            //by spectral databases
            //if (formulaResult.Formula.FormulaString == "Spectral DB search")
            //{
            //    if (!analysisParam.IsRunSpectralDbSearch || mspDB == null || mspDB.Count == 0) return;
            //    GenerateSpectralSearchResult(rawData, analysisParam, exportFilePath, mspDB);
            //    return;
            //}

            var searchFormula = MolecularFormulaUtility.ConvertTmsMeoxSubtractedFormula(formulaResult.Formula);
            var eQueries = DatabaseAccessUtility.GetStructureQueries(searchFormula, existStructureDB, analysisParam.DatabaseQuery);
            GenerateFragmenterResult(eQueries, rawData, formulaResult, analysisParam, exportFilePath, fragmentDB, fragmentOntologies);

            List<ExistStructureQuery> uQueries = null;
            List<ExistStructureQuery> mQueries = null;

            //by user-defined DB
            if (userDefinedDB is not null && userDefinedDB.Count > 0) {
                uQueries = DatabaseAccessUtility.GetStructureQueries(searchFormula, userDefinedDB);
                GenerateFragmenterResult(uQueries, rawData, formulaResult, analysisParam, exportFilePath, fragmentDB, fragmentOntologies);
            }

            //by MINEs DB
            if (analysisParam.IsMinesAllTime ||
                (analysisParam.IsMinesOnlyUseForNecessary && (eQueries is null || eQueries.Count == 0) && (uQueries is null || uQueries.Count == 0))) {
                mQueries = DatabaseAccessUtility.GetStructureQueries(searchFormula, mineStructureDB);
                if (mQueries is not null) {
                    ExistStructureDbParser.SetExistStructureDbInfoToUserDefinedDB(existStructureDB, mQueries, true);
                    GenerateFragmenterResult(mQueries, rawData, formulaResult, analysisParam, exportFilePath, fragmentDB, fragmentOntologies);
                }
            }

            //by PubChem
            if (analysisParam.IsPubChemNeverUse is true) return;
            if (analysisParam.IsPubChemOnlyUseForNecessary && ((eQueries is not null && eQueries.Count != 0)
                || (uQueries is not null && uQueries.Count != 0) || (mQueries is not null && mQueries.Count != 0))) return;

            var formulaDirect = Path.Combine(tempSdfDirect, formulaResult.Formula.FormulaString);
            var ePubCIDs = DatabaseAccessUtility.GetExistingPubChemCIDs(eQueries);

            if (!FormulaFolderExistsCheck(formulaDirect)) {
                var pubRestSdfFinder = new PugRestProtocol();
                if (!pubRestSdfFinder.SearchSdfByFormula(searchFormula.FormulaString,
                    formulaDirect, analysisParam.StructureMaximumReportNumber - eQueries.Count, ePubCIDs)) {
                    return;
                }
            }

            var sdfFiles = Directory.GetFiles(formulaDirect, "*.sdf", SearchOption.TopDirectoryOnly);
            GenerateFragmenterResult(sdfFiles, rawData, formulaResult, analysisParam, exportFilePath,
                existStructureDB, fragmentDB, fragmentOntologies);
        }

        private void GenerateFragmenterResult(List<ExistStructureQuery> queries,
            RawData rawData, FormulaResult formulaResult,
            AnalysisParamOfMsfinder analysisParam, string filePath,
            List<FragmentLibrary> fragmentDB, List<FragmentOntology> fragmentOntologies)
        {
            if (queries == null || queries.Count == 0) return;

            var adductIon = AdductIon.GetAdductIon(rawData.PrecursorType);
            var curetedPeaklist = GetCuratedPeaklist(formulaResult.ProductIonResult);

            var centroidSpectrum = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
            var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(centroidSpectrum, analysisParam.RelativeAbundanceCutOff, 0.0,
                rawData.PrecursorMz, analysisParam.Mass2Tolerance, analysisParam.MassTolType, 1000, false, !analysisParam.CanExcuteMS2AdductSearch);

            var results = MainProcess.Fragmenter(queries, rawData, curetedPeaklist, refinedPeaklist, adductIon, formulaResult, analysisParam, fragmentDB, fragmentOntologies);

            FragmenterResultParser.FragmenterResultWriter(filePath, results, true);
        }

        public void GenerateFragmenterResult(string[] sdfFiles, RawData rawData, FormulaResult formulaResult,
            AnalysisParamOfMsfinder analysisParam, string filePath, List<ExistStructureQuery> existStructureDB,
            List<FragmentLibrary> fragmentDB, List<FragmentOntology> fragmentOntologies)
        {
            var syncObj = new object();
            var adductIon = AdductIon.GetAdductIon(rawData.PrecursorType);
            var curetedPeaklist = GetCuratedPeaklist(formulaResult.ProductIonResult);

            var centroidSpectrum = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
            var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(centroidSpectrum, analysisParam.RelativeAbundanceCutOff, 0.0,
                rawData.PrecursorMz, analysisParam.Mass2Tolerance, analysisParam.MassTolType, 1000, false, !analysisParam.CanExcuteMS2AdductSearch);

            Parallel.ForEach(sdfFiles, file =>
            {
                var results = MainProcess.Fragmenter(file, rawData, curetedPeaklist, refinedPeaklist, adductIon, formulaResult,
                    analysisParam, existStructureDB, fragmentDB, fragmentOntologies);

                lock (syncObj)
                {
                    FragmenterResultParser.FragmenterResultWriter(filePath, results, true);
                }
            });
        }

        private static List<SpectrumPeak> GetCuratedPeaklist(List<ProductIon> productIons) {
            var peaks = new List<SpectrumPeak>();
            foreach (var ion in productIons) {
                peaks.Add(new SpectrumPeak() { Mass = ion.Mass, Intensity = ion.Intensity, Comment = ion.Formula.FormulaString });
            }
            return peaks;
        }

        private void TempFolderInitialize() {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            tempSdfDirect = currentDir + "TEMP_SDF";
            //tempSdfDirect = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\TEMP_SDF";
            if (Directory.Exists(tempSdfDirect)) {

            } else {
                Directory.CreateDirectory(tempSdfDirect);
            }
        }

        private static bool FormulaFolderExistsCheck(string formulaDirect) {
            if (Directory.Exists(formulaDirect)) {
                if (SdfFileExistsCheck(formulaDirect)) {
                    return true;
                } else {
                    return false;
                }
            } else {
                Directory.CreateDirectory(formulaDirect);
                return false;
            }
        }

        private static bool SdfFileExistsCheck(string folderPath) {
            var files = Directory.GetFiles(folderPath, "*.sdf", SearchOption.TopDirectoryOnly);
            if (files.Length == 0) return false;
            else return true;
        }
    }
}
