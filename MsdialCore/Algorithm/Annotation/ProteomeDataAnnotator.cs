using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm.Annotation {
    public class ProteomeDataAnnotator {

        public void ExecuteSecondRoundAnnotationProcess(
            IReadOnlyList<AnalysisFileBean> files, 
            DataBaseMapper mapper,
            DataBaseStorage dataBases,
            ParameterBase param, 
            Action<int> reportAction) {
            if (dataBases is null || dataBases.ProteomicsDataBases is null) return;

            Console.WriteLine("Peptide score generation started");
            var scores = IntegrateAnnotatedPeptides(files, mapper);

            ReportProgress.Show(0, 100, 20, 100, reportAction);
            Console.WriteLine("PEPCalcContainer generation started");
            var pepContainer = new PEPCalcContainer(scores);
            ReportProgress.Show(0, 100, 40, 100, reportAction);

            Console.WriteLine("PEP calculator started");
            foreach (var score in scores) {
                var pepLength = score.PeptideLength;
                var modNum = score.Modifications;
                var missedCleavages = score.MissedCleavages;

                var pepScore = pepContainer.GetPosteriorErrorProbability(score.AndromedaScore, pepLength, modNum, missedCleavages);
                score.PosteriorErrorProb = (float)pepScore;
            }
            ReportProgress.Show(0, 100, 60, 100, reportAction);

            var cutoff = param.FalseDiscoveryRateForPeptide * 0.01;
            foreach (var file in files) {
                ResetPeptideAnnotationInformationByPEPScore(file, scores, mapper, param.ProteomicsParam);
            }

            ReportProgress.Show(0, 100, 80, 100, reportAction);
            foreach (var file in files) {
                MappingToProteinDatabase(file, dataBases.ProteomicsDataBases, mapper, param);
            }
        }

        public void MappingToProteinDatabase(
            AnalysisFileBean file,
            List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> databases,
            DataBaseMapper mapper, 
            ParameterBase param) {
            var fileID = file.AnalysisFileId;
            var paiFile = file.PeakAreaBeanInformationFilePath;
            var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(paiFile);
            
            MappingToProteinDatabase(file.ProteinAssembledResultFilePath, features, databases, mapper, param);
        }

        private void MappingToProteinDatabase(string file, List<ChromatogramPeakFeature> features, 
            List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> databases,
            DataBaseMapper mapper, 
            ParameterBase param) {

            var proteinMsResults = MappingToProteinDatabase(features, databases, mapper, param.ProteomicsParam);
            var proteinGroups = ConvertToProteinGroups(proteinMsResults);
            var container = new ProteinResultContainer(param, proteinGroups);

            DumpContainer(file, container);

            MsdialProteomicsSerializer.SaveProteinResultContainer(file, container);
        }

        private void DumpContainer(string file, ProteinResultContainer container) {
            Console.WriteLine("Dump proteomics result of {0}", file);
            if (container.ProteinGroups is null) {
                Console.WriteLine("no protein group");
                return;
            }
            Console.WriteLine("Protein group count: {0}", container.ProteinGroups.Count);
            foreach (var group in container.ProteinGroups) {
                Console.WriteLine("Protein group {0}, proteins {1}", group.GroupID, group.ProteinMsResults.Count);
                foreach (var protein in group.ProteinMsResults) {
                    Console.WriteLine("Protein name {0}, Coverage {1}, Score {2}, Peptides count {3}", protein.FastaProperty.UniqueIdentifier, protein.PeptideCoverage, protein.Score, protein.MatchedPeptideResults.Count());
                    Console.WriteLine("Sequence");
                    Console.WriteLine(protein.SequenceWithMatchedInfo);
                    Console.WriteLine("Heights");
                    var heights = protein.PeakHeights;
                    Console.WriteLine(String.Join("\t", heights));
                }
            }
        }

        public void MappingToProteinDatabase(
            AlignmentFileBean file,
            List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> databases,
            DataBaseMapper mapper,
            ParameterBase param) {
            var resultfile = file.FilePath;
            var features = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(resultfile);

            MappingToProteinDatabase(file.ProteinAssembledResultFilePath, features, databases, mapper, param);
        }

        public void MappingToProteinDatabase(
            string file, 
            AlignmentResultContainer alignmentContainer, 
            List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> databases, 
            DataBaseMapper mapper, 
            ParameterBase param) {

            var proteinMsResults = MappingToProteinDatabase(alignmentContainer.AlignmentSpotProperties.ToList(), databases, mapper, param.ProteomicsParam);
            var proteinGroups = ConvertToProteinGroups(proteinMsResults);
            var container = new ProteinResultContainer(param, proteinGroups);

            DumpContainer(file, container);
            MsdialProteomicsSerializer.SaveProteinResultContainer(file, container);
        }

        private List<ProteinGroup> ConvertToProteinGroups(List<ProteinMsResult> proteinMsResults) {
            if (proteinMsResults.IsEmptyOrNull()) return null;
            var groups = new List<ProteinGroup>();
            var dict = new Dictionary<int, List<ProteinMsResult>>();
            proteinMsResults = proteinMsResults.OrderByDescending(n => n.MatchedPeptideResults.Count()).ToList();

            var counter = 0;
            for (int i = 0; i < proteinMsResults.Count; i++) {
                var pepIDs = proteinMsResults[i].MatchedPeptideResults.Select(n => n.Peptide.DatabaseOriginID + "|" + n.Peptide.Position.Start.ToString() + "|" + n.Peptide.Position.End.ToString()).ToList();
                var proteinAID = proteinMsResults[i].Index;
                if (isProteinValuesContainsKey(dict, proteinAID)) {
                    continue;
                }
                dict[counter] = new List<ProteinMsResult>() { proteinMsResults[i] };

                for (int j = i + 1; j < proteinMsResults.Count; j++) {
                    var proteinBID = proteinMsResults[j].Index;
                    var cPepIDs = proteinMsResults[j].MatchedPeptideResults.Select(n => n.Peptide.DatabaseOriginID + "|" + n.Peptide.Position.Start.ToString() + "|" + n.Peptide.Position.End.ToString()).ToList();
                    if (isMatchedIDs(pepIDs, cPepIDs)) {
                        dict[counter].Add(proteinMsResults[j]);
                    }
                }
                counter++;
            }

            foreach (var item in dict) {
                var proteinGroup = new ProteinGroup(item.Key, item.Value);
                groups.Add(proteinGroup);
            }

            var peptideList = new List<string>();
            for (int i = 0; i < groups.Count; i++) {
                var group = groups[i];
                for (int j = 0; j < group.ProteinMsResults.Count; j++) {
                    var protein = group.ProteinMsResults[j];
                    for (int k = 0; k < protein.MatchedPeptideResults.Count; k++) {
                        var peptide = protein.MatchedPeptideResults[k].Peptide;
                        var peptideString = peptide.ModifiedSequence;
                        if (peptideList.Contains(peptideString)) {
                            protein.MatchedPeptideResults.RemoveAt(k);
                            k--;
                        }
                        else {
                            peptideList.Add(peptideString);
                        }
                    }
                    if (protein.MatchedPeptideResults.IsEmptyOrNull()) {
                        group.ProteinMsResults.RemoveAt(j);
                        j--;
                    }
                }
                if (group.ProteinMsResults.IsEmptyOrNull()) {
                    groups.RemoveAt(i);
                }
            }
            return groups;
        }

        private bool isProteinValuesContainsKey(Dictionary<int, List<ProteinMsResult>> dict, int proteinID) {
            foreach (var item in dict.OrEmptyIfNull()) {
                foreach (var protein in item.Value.OrEmptyIfNull()) {
                    if (protein.Index == proteinID) {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool isMatchedIDs(List<string> pepsA, List<string> pepsB) {
            var flag = false;
            if (pepsA.Count >= pepsB.Count) {
                foreach (var pep in pepsB) {
                    if (!pepsA.Contains(pep)) {
                        flag = true;
                        break;
                    }
                }
            }
            else {
                foreach (var pep in pepsA) {
                    if (!pepsB.Contains(pep)) {
                        flag = true;
                        break;
                    }
                }
            }
            if (flag) return false;
            else return true;
        }

        public List<ProteinMsResult> MappingToProteinDatabase(
            List<ChromatogramPeakFeature> features,
            List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> databases,
            DataBaseMapper mapper, 
            ProteomicsParameter param) {
            var featureObjs = DataAccess.GetChromPeakFeatureObjectsIntegratingRtAndDriftData(features);
            var isIonMobility = features.Count == featureObjs.Count ? false : true;
            if (isIonMobility) featureObjs = featureObjs.Where(n => n.IsMultiLayeredData() == false).ToList();
            var annotatedFeatures = featureObjs.Where(n => n.IsReferenceMatched(mapper));
            var results = InitializeProteinMsResults(databases);

            foreach (var result in results) {
                var fastaIdentifier = result.FastaProperty.UniqueIdentifier;
                foreach (var feature in annotatedFeatures) {
                    var matchedMsResult = feature.MatchResults.Representative;
                    var matchedPeptideMs = feature.MatchResults.RepresentativePeptideMsReference(mapper);
                    var identifier = matchedPeptideMs.Peptide.DatabaseOrigin;
                    if (fastaIdentifier == identifier) {
                        result.IsAnnotated = true;
                        result.MatchedPeptideResults.Add(new PeptideMsResult(matchedPeptideMs.Peptide, feature));
                    }
                }
            }
            return results.Where(n => n.IsAnnotated).ToList();
        }

        public List<ProteinMsResult> MappingToProteinDatabase(
            List<AlignmentSpotProperty> features,
            List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> databases,
            DataBaseMapper mapper,
            ProteomicsParameter param) {
            var featureObjs = DataAccess.GetAlignmentSpotPropertiesIntegratingRtAndDriftData(features);
            var isIonMobility = features.Count == featureObjs.Count ? false : true;
            if (isIonMobility) featureObjs = featureObjs.Where(n => n.IsMultiLayeredData() == false).ToList();
            var annotatedFeatures = featureObjs.Where(n => n.IsReferenceMatched(mapper));
            var results = InitializeProteinMsResults(databases);

            foreach (var result in results) {
                var fastaIdentifier = result.FastaProperty.UniqueIdentifier;
                foreach (var feature in annotatedFeatures) {
                    var matchedMsResult = feature.MatchResults.Representative;
                    var matchedPeptideMs = feature.MatchResults.RepresentativePeptideMsReference(mapper);
                    var identifier = matchedPeptideMs.Peptide.DatabaseOrigin;
                    if (fastaIdentifier == identifier) {
                        result.IsAnnotated = true;
                        result.MatchedPeptideResults.Add(new PeptideMsResult(matchedPeptideMs.Peptide, feature));
                    }
                }
            }
            return results.Where(n => n.IsAnnotated).ToList();
        }

        public List<ProteinMsResult> InitializeProteinMsResults(
            List<DataBaseItem<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB>> databases) {
            var proteins = new List<ProteinMsResult>();
            var counter = 0;
            foreach (var item in databases) {
                var databaseID = item.DataBaseID;
                var fastaQueries = item.DataBase.FastaQueries;
                foreach (var query in fastaQueries) {
                    proteins.Add(new ProteinMsResult(databaseID, counter, query));
                    counter++;
                }
            }
            return proteins;
        }

        public void ResetPeptideAnnotationInformationByPEPScore(
            AnalysisFileBean file, 
            List<PeptideScore> scores,
            DataBaseMapper mapper,
            ProteomicsParameter param) {
            var fileID = file.AnalysisFileId;
            var paiFile = file.PeakAreaBeanInformationFilePath;
            var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(paiFile);

            var scoresOnFile = scores.Where(n => n.FileID == fileID).OrderBy(n => n.PosteriorErrorProb).ToList();
            var annotatedNum = scoresOnFile.Count;
            var decoyCutOffNum = annotatedNum * param.FalseDiscoveryRateForPeptide * 0.01;

            var minimumAnnotatedCount = 200;
            var counter = 0;
            var featureObjs = DataAccess.GetChromPeakFeatureObjectsIntegratingRtAndDriftData(features);
            foreach (var score in scoresOnFile) {
                if (score.IsDecoy) counter++;
                var feature = featureObjs[score.PeakID];
                feature.MatchResults.Representative.PEPScore = score.PosteriorErrorProb;
                if (annotatedNum < minimumAnnotatedCount) {
                    if (score.IsDecoy == false && score.PosteriorErrorProb > param.FalseDiscoveryRateForPeptide * 0.01) {
                        feature.MatchResults.Representative.IsSpectrumMatch = false;
                    }
                }
                else {
                    if (counter > 50 && counter > decoyCutOffNum && score.IsDecoy == false) {
                        feature.MatchResults.Representative.IsSpectrumMatch = false;
                    }
                }
            }

            if (features.Count != featureObjs.Count) { // meaning lc-im-ms data (4D data)
                foreach (var feature in features) {
                    if (feature.AllDriftFeaturesAreNotAnnotated(mapper)) {
                        //feature.MatchResults.Representative.IsSpectrumMatch = false;
                    }
                }
            }

            MsdialPeakSerializer.SaveChromatogramPeakFeatures(paiFile, features);
        }

        public List<PeptideScore> IntegrateAnnotatedPeptides(IReadOnlyList<AnalysisFileBean> files, DataBaseMapper mapper) {
            var scores = new List<PeptideScore>();
            foreach (var file in files) {
                var paiFile = file.PeakAreaBeanInformationFilePath;
                var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(paiFile);
                var fileID = file.AnalysisFileId;
                foreach (var feature in features.Where(n => n.IsReferenceMatched(mapper))) {
                    var representative = feature.MatchResults.Representative;
                    var decoyRepresentive = feature.MatchResults.DecoyRepresentative;

                    var refSpec = mapper.PeptideMsRefer(representative);
                    var decoySpec = mapper.PeptideMsRefer(decoyRepresentive);

                    var refPepScore = GetPeptideScore(fileID, feature.MasterPeakID, representative, refSpec);
                    var decoyScore = GetPeptideScore(fileID, feature.MasterPeakID, decoyRepresentive, decoySpec);
                    scores.Add(refPepScore);
                    scores.Add(decoyScore);

                    //Console.WriteLine("Score\t{0}\tType\t{1}", refPepScore.AndromedaScore, "Forward");
                    //Console.WriteLine("Score\t{0}\tType\t{1}", decoyScore.AndromedaScore, "Decoy");
                }
            }
            return scores;
        }

        private PeptideScore GetPeptideScore(int fileID, int peakID, Common.DataObj.Result.MsScanMatchResult representative, PeptideMsReference refSpec) {
            var pep = refSpec.Peptide;
            var score = new PeptideScore(representative.AndromedaScore, fileID, peakID, pep.SequenceObj.Count,
                pep.MissedCleavages, pep.CountModifiedAminoAcids(), pep.IsDecoy);
            return score;
        }
    }
}
