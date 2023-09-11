using Msdial.Lcms.Dataprocess.Scoring;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder;
using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Result;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MsfinderCommon.Process
{
    public class MsfinderStructureFinder
    {
        private string tempSdfDirect;

        public MsfinderStructureFinder()
        {
            tempFolderInitialize();
        }

        public void InsilicoFragmentGenerator(string filepath, string output) {
            var smilesCodes = new List<string>();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line != string.Empty)
                        smilesCodes.Add(line.Trim());
                }
            }
            var counter = 0;
            var syncObj = new object();
            Parallel.ForEach(smilesCodes, smiles => {
                var fragments = MainProcess.InsilicoFragmentGenerator(smiles);
                lock (syncObj) {
                    FragmenterResultParcer.InsilicoFragmentResultWriter(output, smiles, fragments);

                    counter++;
                    if (!Console.IsOutputRedirected) {
                        Console.Write("Progress {0}/{1}", counter, smilesCodes.Count);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("Progress {0}/{1}", counter, smilesCodes.Count);
                    }
                }
            }); 
        }

        public void StructureFinderMainProcess(Rfx.Riken.OsakaUniv.RawData rawData, 
            FormulaResult formulaResult, 
            AnalysisParamOfMsfinder analysisParam, 
            string exportFilePath, 
            List<ExistStructureQuery> existStructureDB, 
            List<ExistStructureQuery> userDefinedDB, 
            List<ExistStructureQuery> mineStructureDB, 
            List<FragmentLibrary> fragmentDB, 
            List<FragmentOntology> fragmentOntologies,
            List<MspFormatCompoundInformationBean> mspDB)
        {
            if (rawData.Ms2PeakNumber <= 0) return;
            if (formulaResult == null || formulaResult.Formula == null || formulaResult.Formula.FormulaString == string.Empty) return;
            System.IO.File.Create(exportFilePath).Close();

            //by spectral databases
            if (formulaResult.Formula.FormulaString == "Spectral DB search") {
                if (!analysisParam.IsRunSpectralDbSearch || mspDB == null || mspDB.Count == 0) return;
                GenerateSpectralSearchResult(rawData, analysisParam, exportFilePath, mspDB);
                return;
            }

            var searchFormula = MolecularFormulaUtility.ConvertTmsMeoxSubtractedFormula(formulaResult.Formula);
            var eQueries = DatabaseAccessUtility.GetStructureQueries(searchFormula, existStructureDB, analysisParam.DatabaseQuery);
            GenerateFragmenterResult(eQueries, rawData, formulaResult, analysisParam, exportFilePath, fragmentDB, fragmentOntologies);

            List<ExistStructureQuery> uQueries = null;
            List<ExistStructureQuery> mQueries = null;

            //by user-defined DB
            if (userDefinedDB != null && userDefinedDB.Count > 0)
            {
                uQueries = DatabaseAccessUtility.GetStructureQueries(searchFormula, userDefinedDB);
                GenerateFragmenterResult(uQueries, rawData, formulaResult, analysisParam, exportFilePath, fragmentDB, fragmentOntologies);
            }

            //by MINEs DB
            if (analysisParam.IsMinesAllTime ||
                (analysisParam.IsMinesOnlyUseForNecessary && (eQueries == null || eQueries.Count == 0) && (uQueries == null || uQueries.Count == 0)))
            {
                mQueries = DatabaseAccessUtility.GetStructureQueries(searchFormula, mineStructureDB);
                if (mQueries != null)
                {
                    ExistStructureDbParcer.SetExistStructureDbInfoToUserDefinedDB(existStructureDB, mQueries, true);
                    GenerateFragmenterResult(mQueries, rawData, formulaResult, analysisParam, exportFilePath, fragmentDB, fragmentOntologies);
                }
            }

            //by PubChem
            if (analysisParam.IsPubChemNeverUse == true) return;
            if (analysisParam.IsPubChemOnlyUseForNecessary && ((eQueries != null && eQueries.Count != 0)
                || (uQueries != null && uQueries.Count != 0) || (mQueries != null && mQueries.Count != 0))) return;

            var formulaDirect = Path.Combine(tempSdfDirect, formulaResult.Formula.FormulaString);
            var ePubCIDs = DatabaseAccessUtility.GetExistingPubChemCIDs(eQueries);

            if (!formulaFolderExistsCheck(formulaDirect)) {
                var pubRestSdfFinder = new PugRestProtocol();
                if (!pubRestSdfFinder.SearchSdfByFormula(searchFormula.FormulaString,
                    formulaDirect, analysisParam.StructureMaximumReportNumber - eQueries.Count, ePubCIDs)) {
                    return;
                }
            }

            //if (!formulaFolderExistsCheck(formulaDirect)) return;
            var sdfFiles = System.IO.Directory.GetFiles(formulaDirect, "*.sdf", System.IO.SearchOption.TopDirectoryOnly);
            GenerateFragmenterResult(sdfFiles, rawData, formulaResult, analysisParam, exportFilePath,
                existStructureDB, fragmentDB, fragmentOntologies);
        }

        private void GenerateSpectralSearchResult(Rfx.Riken.OsakaUniv.RawData rawData, AnalysisParamOfMsfinder param, string exportFilePath, List<MspFormatCompoundInformationBean> mspDB)
        {
            var eSpectrum = getExperimentalSpectrum(rawData, param);
            var lSpectra = getSpectralRecords(rawData, param, mspDB);
            if (lSpectra.Count == 0) return;

            var structureResults = new List<FragmenterResult>();
           
            foreach (var mspRecord in lSpectra) {
                if (rawData.IonMode != mspRecord.IonMode) continue;
                if (mspRecord.MzIntensityCommentBeanList == null || mspRecord.MzIntensityCommentBeanList.Count == 0) continue;
                var result = getFragmenterResult(rawData, eSpectrum, mspRecord, param);
                if (result != null)
                    structureResults.Add(result);
            }

            structureResults = structureResults.OrderByDescending(n => n.TotalScore).ToList();
            var refinedResults = new List<FragmenterResult>();
            for (int i = 0; i < param.StructureMaximumReportNumber; i++) {
                if (i > structureResults.Count - 1) break;
                refinedResults.Add(structureResults[i]);
            }
            FragmenterResultParcer.FragmenterResultWriter(exportFilePath, refinedResults, true);
        }

        private ObservableCollection<double[]> getExperimentalSpectrum(Rfx.Riken.OsakaUniv.RawData rawData, AnalysisParamOfMsfinder param) {
            var eSpectrum = new ObservableCollection<double[]>();
            var peaks = rawData.Ms2Spectrum.PeakList;
            var maxIntensity = rawData.Ms2Spectrum.PeakList.Max(m => m.Intensity);
            var cutoff = param.RelativeAbundanceCutOff * 0.01;
            foreach (var peak in peaks) {
                if (peak.Intensity > maxIntensity * cutoff) {
                    eSpectrum.Add(new double[] { peak.Mz, peak.Intensity / maxIntensity * 100 });
                }
            }
            return eSpectrum;
        }

        private static int getMspStartIndex(Rfx.Riken.OsakaUniv.RawData rawData, List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsfinder param)
        {
            int startIndex = 0, endIndex = mspDB.Count - 1;
            if (param.RetentionType == RetentionType.RT && rawData.RetentionTime <= 0) return 0;
            if (param.RetentionType == RetentionType.RI && rawData.RetentionIndex <= 0) return 0;

            var targetRT = rawData.RetentionIndex - param.RtToleranceForSpectralSearching;
            if (param.RetentionType == RetentionType.RT) targetRT = rawData.RetentionTime - param.RtToleranceForSpectralSearching;

            int counter = 0;
            while (counter < 5) {
                if (param.RetentionType == RetentionType.RT) {
                    if (mspDB[startIndex].RetentionTime <= targetRT && targetRT < mspDB[(startIndex + endIndex) / 2].RetentionTime) {
                        endIndex = (startIndex + endIndex) / 2;
                    }
                    else if (mspDB[(startIndex + endIndex) / 2].RetentionTime <= targetRT && targetRT < mspDB[endIndex].RetentionTime) {
                        startIndex = (startIndex + endIndex) / 2;
                    }
                }
                else {
                    if (mspDB[startIndex].RetentionIndex <= targetRT && targetRT < mspDB[(startIndex + endIndex) / 2].RetentionIndex) {
                        endIndex = (startIndex + endIndex) / 2;
                    }
                    else if (mspDB[(startIndex + endIndex) / 2].RetentionIndex <= targetRT && targetRT < mspDB[endIndex].RetentionIndex) {
                        startIndex = (startIndex + endIndex) / 2;
                    }
                }
                counter++;
            }
            return startIndex;
        }

        private FragmenterResult getFragmenterResult(Rfx.Riken.OsakaUniv.RawData rawData, ObservableCollection<double[]> eSpectrum, MspFormatCompoundInformationBean mspRecord, AnalysisParamOfMsfinder param)
        {
            var ms1tol = (float)param.Mass1Tolerance;
            if (param.MassTolType == MassToleranceType.Ppm) ms1tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(200.0000, param.Mass1Tolerance); // use 200.0000 for ppm -> Da convert of MS2

            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1tol));
            #region // practical parameter changes
            if (rawData.PrecursorMz > 500) {
                ms1tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(rawData.PrecursorMz, ppm);
            }
            #endregion

            var ms2tol = (float)param.Mass2Tolerance;
            if (param.MassTolType == MassToleranceType.Ppm) ms2tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(200.0000, param.Mass2Tolerance); // use 200.0000 for ppm -> Da convert of MS2
            var ccsTol = param.CcsToleranceForSpectralSearching;

            var lSpectrum = mspRecord.MzIntensityCommentBeanList.Where(n => n.Intensity > mspRecord.MzIntensityCommentBeanList.Max(m => m.Intensity) * param.RelativeAbundanceCutOff * 0.01).ToList();
            var targetOmics = mspRecord.CompoundClass != null && mspRecord.CompoundClass != string.Empty ? TargetOmics.Lipidomics : TargetOmics.Metablomics;

            var dotProduct = LcmsScoring.GetMassSpectraSimilarity(eSpectrum, lSpectrum, ms2tol, (float)param.MassRangeMin, (float)param.MassRangeMax);
            var revDotProduct = LcmsScoring.GetReverseSearchSimilarity(eSpectrum, lSpectrum, ms2tol, (float)param.MassRangeMin, (float)param.MassRangeMax);
            var mzSimilarity = LcmsScoring.GetGaussianSimilarity((float)rawData.PrecursorMz, mspRecord.PrecursorMz, ms1tol);
            var matchedRatio = LcmsScoring.GetFragmentPresenceScore(eSpectrum, mspRecord, ms2tol, (float)param.MassRangeMin, (float)param.MassRangeMax, mspRecord.IonMode, targetOmics);
            
            var rtSimilarity = FragmenterScoring.RetentionTimeSimilairty(rawData, mspRecord, param);
            var ccsSimilarity = mspRecord.CollisionCrossSection >= 0 && rawData.Ccs >= 0 ? LcmsScoring.GetGaussianSimilarity((float)rawData.Ccs, mspRecord.CollisionCrossSection, (float)ccsTol) : -1;

            var totalScore = 0.0;
            var dotproductFactor = 1.0;
            var revDotProductFactor = 1.0;
            var matchedRatioFactor = 1.0;
            var rtFactor = 1.0;

            var spectrumPenalty = false;
            if (lSpectrum != null &&
                lSpectrum.Count <= 1) spectrumPenalty = true;

            var isMinRequired = matchedRatio > 0.4 || dotProduct > 0.15 || revDotProduct > 0.5 ? true : false;
            if (targetOmics == TargetOmics.Lipidomics) {
                if ((mspRecord.CompoundClass == "EtherTG" || mspRecord.CompoundClass == "EtherDG" || mspRecord.CompoundClass == "LPE") && dotProduct < 0.15) {
                    isMinRequired = false;
                }
            }

            var metabolitename = mspRecord.Name;

            if (param.IsTmsMeoxDerivative)
            {
                dotproductFactor = 2.0; revDotProductFactor = 2.0; matchedRatio = 1.0;
                if (rtSimilarity > 0) rtFactor = 2.0;
                totalScore = (dotproductFactor * dotProduct + revDotProductFactor * revDotProduct
                   + matchedRatioFactor * matchedRatio + rtFactor * rtSimilarity)
                   / (dotproductFactor + revDotProductFactor + matchedRatioFactor + rtFactor);
            }
            else if (targetOmics == TargetOmics.Lipidomics)
            {
                totalScore = LcmsScoring.GetTotalSimilarity(mzSimilarity, rtSimilarity, ccsSimilarity, -1, dotProduct,
                    revDotProduct, matchedRatio, spectrumPenalty, TargetOmics.Lipidomics,
                    param.IsUseRtForFilteringCandidates, param.IsUseCcsForFilteringCandidates);
            }
            else
            {
                totalScore = LcmsScoring.GetTotalSimilarity(mzSimilarity, rtSimilarity, ccsSimilarity, -1, dotProduct,
                    revDotProduct, matchedRatio, spectrumPenalty, TargetOmics.Metablomics,
                    param.IsUseRtForFilteringCandidates, param.IsUseCcsForFilteringCandidates);
            }

            if (totalScore * 100 > param.ScoreCutOffForSpectralMatch && isMinRequired) {

                if (targetOmics == TargetOmics.Lipidomics) {
                    var isLipidClassMatched = false;
                    var isLipidChainMatched = false;
                    var isLipidPositionMatched = false;
                    var isOtherLipids = false;
                    metabolitename = LcmsScoring.GetRefinedLipidAnnotationLevel(eSpectrum, mspRecord, ms2tol, out isLipidClassMatched, out isLipidChainMatched, out isLipidPositionMatched, out isOtherLipids);
                    if (metabolitename == string.Empty) return null;
                }

                var result = new FragmenterResult(mspRecord, metabolitename);
                result.TotalScore = totalScore * 10.0;

                if (param.IsUseExperimentalRtForSpectralSearching) {
                    if (param.RetentionType == RetentionType.RI)
                        result.RiSimilarityScore = rtSimilarity;
                    else
                        result.RtSimilarityScore = rtSimilarity;
                }

                return result;
            }

            return null;
        }

        private List<MspFormatCompoundInformationBean> getSpectralRecords(Rfx.Riken.OsakaUniv.RawData rawData, AnalysisParamOfMsfinder param, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (param.IsPrecursorOrientedSearch == false) {
                var startIndex = 0;
                if ((param.RetentionType == RetentionType.RT && rawData.RetentionTime <= 0) ||
                    (param.RetentionType == RetentionType.RI && rawData.RetentionIndex <= 0)) return mspDB;

                if (param.IsUseExperimentalRtForSpectralSearching) startIndex = getMspStartIndex(rawData, mspDB, param); // ad hoc

                var rMspDB = new List<MspFormatCompoundInformationBean>();
                for (int i = startIndex; i < mspDB.Count; i++) {

                    if (param.RetentionType == RetentionType.RT) {
                        if (mspDB[i].RetentionTime < rawData.RetentionTime - param.RtToleranceForSpectralSearching) continue;
                    }
                    else if (param.RetentionType == RetentionType.RI) {
                        if (mspDB[i].RetentionIndex > rawData.RetentionIndex + param.RtToleranceForSpectralSearching) continue;
                    }

                    if (param.RetentionType == RetentionType.RT) {
                        if (mspDB[i].RetentionTime > rawData.RetentionTime + param.RtToleranceForSpectralSearching) break;
                    }
                    else if (param.RetentionType == RetentionType.RI) {
                        if (mspDB[i].RetentionIndex > rawData.RetentionIndex + param.RtToleranceForSpectralSearching) break;
                    }
                    rMspDB.Add(mspDB[i]);
                }

                return rMspDB;
            }
            else {
                var precursorMz = rawData.PrecursorMz;
                var massTolerance = param.Mass1Tolerance;
                var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + massTolerance));
                var ccsTol = param.CcsToleranceForSpectralSearching;
                var rtTol = param.RtToleranceForSpectralSearching;
                var isUseRtForFilter = param.IsUseRtForFilteringCandidates;
                var isUseCcsForFilter = param.IsUseCcsForFilteringCandidates;

                #region // practical parameter changes
                if (precursorMz > 500) {
                    massTolerance = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(precursorMz, ppm);
                }
                #endregion

                var startIndex = DatabaseAccessUtility.getQueryStartIndex(precursorMz, massTolerance, mspDB);

                var rMspDB = new List<MspFormatCompoundInformationBean>();
                for (int i = startIndex; i < mspDB.Count; i++) {
                    var libPrecursor = mspDB[i].PrecursorMz;

                    if (libPrecursor < precursorMz - massTolerance) continue;
                    if (libPrecursor > precursorMz + massTolerance) break;

                    if (isUseRtForFilter && mspDB[i].RetentionTime > 0.0 && rawData.RetentionTime > 0 && Math.Abs(mspDB[i].RetentionTime - rawData.RetentionTime) > rtTol) continue;
                    if (isUseCcsForFilter && mspDB[i].CollisionCrossSection > 0.0 && rawData.Ccs > 0 && Math.Abs(mspDB[i].CollisionCrossSection - rawData.Ccs) > ccsTol) continue;

                    rMspDB.Add(mspDB[i]);
                }

                return rMspDB;
            }
        }

        private void GenerateFragmenterResult(List<ExistStructureQuery> queries,
            Rfx.Riken.OsakaUniv.RawData rawData, FormulaResult formulaResult, 
            AnalysisParamOfMsfinder analysisParam, string filePath, 
            List<FragmentLibrary> fragmentDB, List<FragmentOntology> fragmentOntologies)
        {
            if (queries == null || queries.Count == 0) return;

            var adductIon = AdductIonParcer.GetAdductIonBean(rawData.PrecursorType);
            var curetedPeaklist = getCuratedPeaklist(formulaResult.ProductIonResult);
            
            var centroidSpectrum = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
            var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(centroidSpectrum, analysisParam.RelativeAbundanceCutOff,
                rawData.PrecursorMz, analysisParam.Mass2Tolerance, analysisParam.MassTolType, true);

            var results = MainProcess.Fragmenter(queries, rawData, curetedPeaklist, refinedPeaklist, adductIon, formulaResult, analysisParam, fragmentDB, fragmentOntologies);

            //addFormulaScore(results, formulaResult);

            FragmenterResultParcer.FragmenterResultWriter(filePath, results, true);
        }

        public void GenerateFragmenterResult(string[] sdfFiles, Rfx.Riken.OsakaUniv.RawData rawData, FormulaResult formulaResult, 
            AnalysisParamOfMsfinder analysisParam, string filePath, List<ExistStructureQuery> existStructureDB,
            List<FragmentLibrary> fragmentDB, List<FragmentOntology> fragmentOntologies)
        {
            var syncObj = new object();
            var adductIon = AdductIonParcer.GetAdductIonBean(rawData.PrecursorType);
            var curetedPeaklist = getCuratedPeaklist(formulaResult.ProductIonResult);

            var centroidSpectrum = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
            var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(centroidSpectrum, analysisParam.RelativeAbundanceCutOff,
                rawData.PrecursorMz, analysisParam.Mass2Tolerance, analysisParam.MassTolType, true);

            Parallel.ForEach(sdfFiles, file =>
            {
                var results = MainProcess.Fragmenter(file, rawData, curetedPeaklist, refinedPeaklist, adductIon, formulaResult, 
                    analysisParam, existStructureDB, fragmentDB, fragmentOntologies);

                //addFormulaScore(results, formulaResult);

                lock (syncObj)
                {
                    FragmenterResultParcer.FragmenterResultWriter(filePath, results, true);
                }
            });
        }

        private List<Peak> getCuratedPeaklist(List<ProductIon> productIons)
        {
            var peaks = new List<Peak>();
            foreach (var ion in productIons) {
                peaks.Add(new Peak() { Mz = ion.Mass, Intensity = ion.Intensity, Comment = ion.Formula.FormulaString });
            }

            return peaks;
        }

        //private void addFormulaScore(List<FragmenterResult> results, FormulaResult formulaResult)
        //{
        //    foreach (var result in results)
        //    {
        //        result.TotalScore += formulaResult.TotalScore;
        //    }
        //}

        private void tempFolderInitialize()
        {
            var currentDir = System.AppDomain.CurrentDomain.BaseDirectory;
            tempSdfDirect = currentDir + "TEMP_SDF";
            //tempSdfDirect = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\TEMP_SDF";
            if (System.IO.Directory.Exists(tempSdfDirect))
            {

            }
            else
            {
                var di = System.IO.Directory.CreateDirectory(tempSdfDirect);
            }
        }

        private bool formulaFolderExistsCheck(string formulaDirect)
        {
            if (System.IO.Directory.Exists(formulaDirect))
            {
                if (sdfFileExistsCheck(formulaDirect))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var di = System.IO.Directory.CreateDirectory(formulaDirect);
                return false;
            }
        }

        private bool sdfFileExistsCheck(string folderPath)
        {
            var files = System.IO.Directory.GetFiles(folderPath, "*.sdf", System.IO.SearchOption.TopDirectoryOnly);
            if (files.Length == 0) return false;
            else return true;
        }
    }
}
