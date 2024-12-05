using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation;

public class ProteomeDataAnnotator {

    public void ExecuteSecondRoundAnnotationProcess(
        IReadOnlyList<AnalysisFileBean> files,
        IMatchResultRefer<PeptideMsReference?, MsScanMatchResult?> refer,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        DataBaseStorage dataBases,
        ParameterBase param,
        IProgress<int>? progress) {
        if (dataBases is null || dataBases.ProteomicsDataBases is null) return;

        ReportProgress reporter = ReportProgress.FromLength(progress, 0, 100);
        Console.WriteLine("Peptide score generation started");
        var scores = IntegrateAnnotatedPeptides(files, refer, evaluator);

        reporter.Report(20, 100);
        Console.WriteLine("PEPCalcContainer generation started");
        var pepContainer = new PEPCalcContainer(scores);
        reporter.Report(40, 100);

        Console.WriteLine("PEP calculator started");

        foreach (var score in scores) {
            var pepLength = score.PeptideLength;
            var modNum = score.Modifications;
            var missedCleavages = score.MissedCleavages;

            var pepScore = pepContainer.GetPosteriorErrorProbability(score.AndromedaScore, pepLength, modNum, missedCleavages);
            score.PosteriorErrorProb = (float)pepScore;
        }
        reporter.Report(60, 100);

        var cutoff = param.FalseDiscoveryRateForPeptide * 0.01;
        foreach (var file in files) {
            ResetPeptideAnnotationInformationByPEPScore(file, scores, evaluator, param.ProteomicsParam);
        }

        reporter.Report(80, 100);
        foreach (var file in files) {
            MappingToProteinDatabase(file, dataBases.ProteomicsDataBases, evaluator, refer, param);
        }
    }

    public void MappingToProteinDatabase(
        AnalysisFileBean file,
        List<DataBaseItem<ShotgunProteomicsDB>> databases,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer, 
        ParameterBase param) {
        var paiFile = file.PeakAreaBeanInformationFilePath;
        var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(paiFile);
        
        MappingToProteinDatabase(file.ProteinAssembledResultFilePath, features, databases, evaluator, refer, param);

        MsdialPeakSerializer.SaveChromatogramPeakFeatures(paiFile, features);
    }

    private void MappingToProteinDatabase(string file, List<ChromatogramPeakFeature> features,
        List<DataBaseItem<ShotgunProteomicsDB>> databases,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer,
        ParameterBase param) {

        var proteinMsResults = MappingToProteinDatabase(features, databases, refer, evaluator);
        var proteinGroups = ConvertToProteinGroups(proteinMsResults);
        var container = new ProteinResultContainer(param, proteinGroups, GetDB2ModificationContainer(databases));

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
        string file,
        AlignmentResultContainer alignmentContainer,
        List<DataBaseItem<ShotgunProteomicsDB>> databases,
        IMatchResultRefer<PeptideMsReference?, MsScanMatchResult?> refer,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        ParameterBase param) {

        var proteinMsResults = MappingToProteinDatabase(alignmentContainer.AlignmentSpotProperties.ToList(), databases, refer, evaluator);
        var proteinGroups = ConvertToProteinGroups(proteinMsResults);
        var container = new ProteinResultContainer(param, proteinGroups, GetDB2ModificationContainer(databases));

        DumpContainer(file, container);
        MsdialProteomicsSerializer.SaveProteinResultContainer(file, container);
    }

    public Dictionary<string, ModificationContainer> GetDB2ModificationContainer(List<DataBaseItem<ShotgunProteomicsDB>> databases) {
        var dict = new Dictionary<string, ModificationContainer>();
        foreach (var database in databases) {
            dict[database.DataBaseID] = database.DataBase.ModificationContainer;
        }
        return dict;
    }

    private List<ProteinGroup> ConvertToProteinGroups(List<ProteinMsResult> proteinMsResults) {
        if (proteinMsResults.IsEmptyOrNull()) return new List<ProteinGroup>(0);
        var groups = new List<ProteinGroup>();
        var dict = new Dictionary<int, List<ProteinMsResult>>();
        proteinMsResults = proteinMsResults.OrderByDescending(n => n.MatchedPeptideResults.Count()).ToList();

        var counter = 0;
        var mergedProteins = new List<int>();
        for (int i = 0; i < proteinMsResults.Count; i++) {
            //var pepIDs = proteinMsResults[i].MatchedPeptideResults.Select(n => n.Peptide.DatabaseOriginID + "|" + n.Peptide.Position.Start.ToString() + "|" + n.Peptide.Position.End.ToString()).ToList();
            var pepIDs = proteinMsResults[i].MatchedPeptideResults.Select(n => n.Peptide.Sequence).ToList();
            var proteinAID = proteinMsResults[i].Index;
            if (mergedProteins.Contains(proteinAID)) continue;
            //if (isProteinValuesContainsKey(dict, proteinAID)) {
            //    continue;
            //}
            dict[counter] = new List<ProteinMsResult>() { proteinMsResults[i] };
            mergedProteins.Add(proteinAID);

            for (int j = i + 1; j < proteinMsResults.Count; j++) {
                var proteinBID = proteinMsResults[j].Index;
                if (mergedProteins.Contains(proteinBID)) continue;

                //var cPepIDs = proteinMsResults[j].MatchedPeptideResults.Select(n => n.Peptide.DatabaseOriginID + "|" + n.Peptide.Position.Start.ToString() + "|" + n.Peptide.Position.End.ToString()).ToList();
                var cPepIDs = proteinMsResults[j].MatchedPeptideResults.Select(n => n.Peptide.Sequence).ToList();
                if (isMatchedIDs(pepIDs, cPepIDs)) {
                    dict[counter].Add(proteinMsResults[j]);
                    mergedProteins.Add(proteinBID);
                }
            }
            counter++;
        }

        foreach (var item in dict) {
            var proteinGroup = new ProteinGroup(item.Key, item.Value);
            groups.Add(proteinGroup);
        }

        var peptideList = new List<string>();
        var peptide2firstProteinID = new Dictionary<string, int>();
        for (int i = 0; i < groups.Count; i++) {
            var group = groups[i];
            for (int j = 0; j < group.ProteinMsResults.Count; j++) {
                var protein = group.ProteinMsResults[j];
                for (int k = 0; k < protein.MatchedPeptideResults.Count; k++) {
                    var peptide = protein.MatchedPeptideResults[k].Peptide;
                    var peptideString = peptide.Sequence;
                    if (peptideList.Contains(peptideString) && peptide2firstProteinID[peptideString] < j) {
                    //if (peptideList.Contains(peptideString)) {
                        //protein.MatchedPeptideResults.RemoveAt(k);
                        //k--;
                    }
                    else {
                        peptideList.Add(peptideString);
                        if (!peptide2firstProteinID.ContainsKey(peptideString)) {
                            peptide2firstProteinID[peptideString] = j;
                        }
                    }
                }
                //if (protein.MatchedPeptideResults.IsEmptyOrNull()) {
                //    group.ProteinMsResults.RemoveAt(j);
                //    j--;
                //}
            }
            if (group.ProteinMsResults.IsEmptyOrNull()) {
                groups.RemoveAt(i);
            }
        }


        ReflectToPeakObjects(groups);

        return groups;
    }

    private void ReflectToPeakObjects(List<ProteinGroup> groups) {
        foreach (var group in groups) {
            foreach (var protein in group.ProteinMsResults) {
                foreach (var pepObj in protein.MatchedPeptideResults) {
                    if (pepObj.ChromatogramPeakFeature != null) {
                        pepObj.ChromatogramPeakFeature.Protein = protein.FastaProperty.UniqueIdentifier + "|" + pepObj.Peptide.Position.Start + "-" + pepObj.Peptide.Position.End;
                        pepObj.ChromatogramPeakFeature.ProteinGroupID = group.GroupID;
                    }
                    else if (pepObj.AlignmentSpotProperty != null) {
                        pepObj.AlignmentSpotProperty.Protein = protein.FastaProperty.UniqueIdentifier + "|" + pepObj.Peptide.Position.Start + "-" + pepObj.Peptide.Position.End;
                        pepObj.AlignmentSpotProperty.ProteinGroupID = group.GroupID;
                    }
                }
            }
        }
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
        List<DataBaseItem<ShotgunProteomicsDB>> databases,
        IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer,
        IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        var featureObjs = DataAccess.GetChromPeakFeatureObjectsIntegratingRtAndDriftData(features);
        var isIonMobility = features.Count != featureObjs.Count;
        if (isIonMobility) featureObjs = featureObjs.Where(n => !n.IsMultiLayeredData()).ToList();
        var annotatedFeatures = featureObjs.Where(n => n.IsReferenceMatched(evaluator)).ToList();
        var results = InitializeProteinMsResults(databases);

        foreach (var result in results) {
            var fastaIdentifier = result.FastaProperty.UniqueIdentifier;
            var fastaPeptides = result.FastaProperty.Sequence;

            foreach (var feature in annotatedFeatures) {
                var matchedMsResult = feature.MatchResults.Representative;
                var matchedPeptideMs = feature.MatchResults.GetRepresentativeReference(refer);
                var identifier = matchedPeptideMs.Peptide.DatabaseOrigin;
                var matchedPeptideSequence = matchedPeptideMs.Peptide.Sequence;
                if (fastaPeptides.Contains(matchedPeptideSequence)) {
                    //if (fastaIdentifier == identifier) {
                    result.IsAnnotated = true;
                    result.MatchedPeptideResults.Add(new PeptideMsResult(matchedPeptideMs.Peptide, feature, result.DatabaseID));
                }
            }
            if (result.IsAnnotated)
                result.PropertyUpdates();
        }
        return results.Where(n => n.IsAnnotated).ToList();
    }

    public List<ProteinMsResult> MappingToProteinDatabase(
        List<AlignmentSpotProperty> features,
        List<DataBaseItem<ShotgunProteomicsDB>> databases,
        IMatchResultRefer<PeptideMsReference?, MsScanMatchResult?> refer,
        IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        var featureObjs = DataAccess.GetAlignmentSpotPropertiesIntegratingRtAndDriftData(features);
        var isIonMobility = features.Count != featureObjs.Count;
        if (isIonMobility) featureObjs = featureObjs.Where(n => !n.IsMultiLayeredData()).ToList();
        var annotatedFeatures = featureObjs.Where(n => n.IsReferenceMatched(evaluator)).ToList();
        var results = InitializeProteinMsResults(databases);

        foreach (var result in results) {
            var fastaIdentifier = result.FastaProperty.UniqueIdentifier;
            var fastaPeptides = result.FastaProperty.Sequence;
            foreach (var feature in annotatedFeatures) {
                var matchedMsResult = feature.MatchResults.Representative;
                var matchedPeptideMs = feature.MatchResults.GetRepresentativeReference(refer);
                var identifier = matchedPeptideMs.Peptide.DatabaseOrigin;
                var matchedPeptideSequence = matchedPeptideMs.Peptide.Sequence;
                if (fastaPeptides.Contains(matchedPeptideSequence)) {
                    //if (fastaIdentifier == identifier) {
                    result.IsAnnotated = true;
                    result.MatchedPeptideResults.Add(new PeptideMsResult(matchedPeptideMs.Peptide, feature, result.DatabaseID));
                }
            }
            if (result.IsAnnotated)
                result.PropertyUpdates();
        }
        return results.Where(n => n.IsAnnotated).ToList();
    }

    public List<ProteinMsResult> InitializeProteinMsResults(
        List<DataBaseItem<ShotgunProteomicsDB>> databases) {
        var proteins = new List<ProteinMsResult>();
        var counter = 0;
        foreach (var item in databases) {
            var databaseID = item.DataBaseID;
            var fastaQueries = item.DataBase.FastaQueries;
            foreach (var query in fastaQueries) {
                proteins.Add(new ProteinMsResult(counter, databaseID, query));
                counter++;
            }
        }
        return proteins;
    }

    public void ResetPeptideAnnotationInformationByPEPScore(
        AnalysisFileBean file,
        List<PeptideScore> scores,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
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
        var total = 0;
        foreach (var score in scoresOnFile) {
            total++;
            if (score.IsDecoy) counter++;
            var feature = featureObjs[score.PeakID];
            if (score.IsDecoy) {
                feature.MatchResults.DecoyRepresentative.PEPScore = score.PosteriorErrorProb;
                Console.WriteLine("Rank\t{0}\tIsDecoy\t{1}\tPeakID\t{2}\tMZ\t{3}\tRT\t{4}\tAndromeda\t{5}\tPepScore\t{6}", total, "TRUE", feature.MasterPeakID, feature.PeakFeature.Mass, feature.ChromXs.Value, score.AndromedaScore, score.PosteriorErrorProb);
            }
            else {
                feature.MatchResults.Representative.PEPScore = score.PosteriorErrorProb;
                Console.WriteLine("Rank\t{0}\tIsDecoy\t{1}\tPeakID\t{2}\tMZ\t{3}\tRT\t{4}\tAndromeda\t{5}\tPepScore\t{6}", total, "FALSE", feature.MasterPeakID, feature.PeakFeature.Mass, feature.ChromXs.Value, score.AndromedaScore, score.PosteriorErrorProb);
            }

            if (annotatedNum < minimumAnnotatedCount) {
                //if (score.IsDecoy == false && score.PosteriorErrorProb > param.FalseDiscoveryRateForPeptide * 0.01) {
                //    //feature.MatchResults.Representative.IsSpectrumMatch = false;
                //    feature.MatchResults.Representative.IsReferenceMatched = false;
                //    feature.MatchResults.Representative.IsAnnotationSuggested = true;
                //}
            }
            else {
                if (counter > decoyCutOffNum + 1 && score.IsDecoy == false) {
                    feature.MatchResults.Representative.IsReferenceMatched = false;
                    feature.MatchResults.Representative.IsAnnotationSuggested = true;
                    //feature.MatchResults.Representative.IsSpectrumMatch = false;
                }
            }
        }

        if (features.Count != featureObjs.Count) { // meaning lc-im-ms data (4D data)
            foreach (var feature in features) {
                if (feature.AllDriftFeaturesAreAnnotated(evaluator)) {
                    //feature.MatchResults.Representative.IsSpectrumMatch = false;
                }
            }
        }

        MsdialPeakSerializer.SaveChromatogramPeakFeatures(paiFile, features);
    }

    public List<PeptideScore> IntegrateAnnotatedPeptides(IReadOnlyList<AnalysisFileBean> files, IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        var scores = new List<PeptideScore>();
        foreach (var file in files) {
            var paiFile = file.PeakAreaBeanInformationFilePath;
            var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(paiFile);
            var fileID = file.AnalysisFileId;
            foreach (var feature in features.Where(n => n.IsReferenceMatched(evaluator))) {
                var representative = feature.MatchResults.Representative;
                var decoyRepresentive = feature.MatchResults.DecoyRepresentative;

                var refSpec = refer.Refer(representative);
                //var decoySpec = refer.Refer(decoyRepresentive);

                var refPepScore = GetPeptideScore(fileID, feature.MasterPeakID, representative, refSpec);
                var decoyScore = refPepScore.Clone();
                decoyScore.IsDecoy = true; decoyScore.AndromedaScore = decoyRepresentive.AndromedaScore;
                //var decoyScore = GetPeptideScore(fileID, feature.MasterPeakID, decoyRepresentive, decoySpec);
                scores.Add(refPepScore);
                scores.Add(decoyScore);
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
