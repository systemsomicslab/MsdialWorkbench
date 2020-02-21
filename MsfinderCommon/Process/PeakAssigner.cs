using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Query;
using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder;
using Riken.Metabolomics.StructureFinder.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Riken.Metabolomics.MsfinderCommon.Process
{
    public sealed class PeakAssigner
    {
        private PeakAssigner() { }

        public static void Process(MsfinderQueryFile analysisFile, Rfx.Riken.OsakaUniv.RawData rawData, AnalysisParamOfMsfinder param, List<ProductIon> productIonDB, 
            List<NeutralLoss> neutralLossDB, List<ExistFormulaQuery> existFormulaDB, 
            List<FragmentLibrary> fragmentDB, List<FragmentOntology> fragmentOntologies)
        {
            var formulaResult = MolecularFormulaFinder.GetMolecularFormulaScore(productIonDB, neutralLossDB, existFormulaDB, rawData, param);
            if (formulaResult == null) return;

            var formualResults = new List<FormulaResult>() { formulaResult };
            FormulaResultParcer.FormulaResultsWriter(analysisFile.FormulaFilePath, formualResults);

            var structureFiles = System.IO.Directory.GetFiles(analysisFile.StructureFolderPath, "*.sfd");
            if (structureFiles.Length > 0) FileStorageUtility.DeleteSfdFiles(structureFiles);

            var exportStructureFilePath = FileStorageUtility.GetStructureDataFilePath(analysisFile.StructureFolderPath, formulaResult.Formula.FormulaString);
            if (rawData.Ms2PeakNumber <= 0 || rawData.Smiles == null || rawData.Smiles == string.Empty) return;
            
            var structureQuery = new ExistStructureQuery(rawData.Name, rawData.InchiKey, rawData.InchiKey, new List<int>(), formulaResult.Formula, rawData.Smiles, string.Empty, 0, new DatabaseQuery());
            if (structureQuery == null) return;

            var eQueries = new List<ExistStructureQuery>() { structureQuery };
            System.IO.File.Create(exportStructureFilePath).Close();

            var adductIon = AdductIonParcer.GetAdductIonBean(rawData.PrecursorType);
            var centroidSpectrum = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
            var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(centroidSpectrum, param.RelativeAbundanceCutOff, 
                rawData.PrecursorMz, param.Mass2Tolerance, param.MassTolType, true);
            var curatedPeaklist = getCuratedPeaklist(formulaResult.ProductIonResult);
            
            var results = MainProcess.Fragmenter(eQueries, rawData, curatedPeaklist, refinedPeaklist, adductIon, formulaResult, param, fragmentDB, fragmentOntologies);

            foreach (var result in results) {
                result.TotalScore += formulaResult.TotalScore;
                result.Ontology = rawData.Ontology;
            }

            FragmenterResultParcer.FragmenterResultWriter(exportStructureFilePath, results, true);
        }

        private static List<Peak> getCuratedPeaklist(List<ProductIon> productIons)
        {
            var peaks = new List<Peak>();
            foreach (var ion in productIons) {
                peaks.Add(new Peak() { Mz = ion.Mass, Intensity = ion.Intensity, Comment = ion.Formula.FormulaString });
            }

            return peaks;
        }
    }
}
