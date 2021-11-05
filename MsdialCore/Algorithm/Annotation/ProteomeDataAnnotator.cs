using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm.Annotation {
    public class ProteomeDataAnnotator {

        public List<PeptideScore> GetPeptideScoreList(
            IReadOnlyList<AnalysisFileBean> files, 
            DataBaseMapper mapper, 
            ProteomicsParameter param, 
            Action<int> reportAction) {

            Console.WriteLine("Peptide score generation started");
            var scores = IntegrateAnnotatedPeptides(files, mapper);

            ReportProgress.Show(0, 100, 25, 100, reportAction);
            Console.WriteLine("PEPCalcContainer generation started");
            var pepContainer = new PEPCalcContainer(scores);
            ReportProgress.Show(0, 100, 50, 100, reportAction);

            Console.WriteLine("PEP calculator started");
            foreach (var score in scores) {
                var pepLength = score.PeptideLength;
                var modNum = score.Modifications;
                var missedCleavages = score.MissedCleavages;

                var pepScore = pepContainer.GetPosteriorErrorProbability(score.AndromedaScore, pepLength, modNum, missedCleavages);
                score.PosteriorErrorProb = (float)pepScore;
            }
            ReportProgress.Show(0, 100, 75, 100, reportAction);



            return scores;
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
