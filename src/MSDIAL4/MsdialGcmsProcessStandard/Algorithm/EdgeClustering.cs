using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Gcms.Dataprocess.Algorithm {

    public class EdgeInformation {
        public string Source { get; set; }
        public string Target { get; set; }
        public double Score { get; set; }
        public string SourceName { get; set; }
        public string TargetName { get; set; }
        public string Comment { get; set; }
    }

    public sealed class EimsClustering {
        private EimsClustering() { }

        public static void EimsSpectrumNetwork(string input, string output) {
            var queries = MspFileParcer.MspFileReader(input);
            var edges = GetEdgeInformations(queries, 1, 0.2);
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("source\ttarget\tscore\tsource name\ttarget name");
                foreach (var edge in edges) {
                    sw.WriteLine(edge.Source + "\t" + edge.Target + "\t" + edge.Score + "\t" + edge.SourceName + "\t" + edge.TargetName);
                }
            }
        }

        public static List<EdgeInformation> GetEdgeInformations(List<MspFormatCompoundInformationBean> mspQueries, double cutoff, double masstol) {

            var totalCount = mspQueries.Count * mspQueries.Count;
            var counter = 0;
            var pairDone = new List<string>();
            var edges = new List<EdgeInformation>();

            for (int i = 0; i < mspQueries.Count; i++) {
                for (int j = 0; j < mspQueries.Count; j++) {
                    if (i == j) continue;

                    var eimsClusterScore = EimsSimilarityScore(mspQueries[i], mspQueries[j], masstol);
                    if (eimsClusterScore >= cutoff) {

                        var pairKey = Math.Min(i, j) + "-" + Math.Max(i, j);
                        if (!pairDone.Contains(pairKey)) {

                            var sourceID = Math.Min(i, j);
                            var targetID = Math.Max(i, j);

                            var edge = new EdgeInformation() {
                                Source = mspQueries[sourceID].Comment,
                                Target = mspQueries[targetID].Comment,
                                Score = Math.Round(eimsClusterScore, 3),
                                Comment = "EI-MS similarity",
                                SourceName = mspQueries[sourceID].Name,
                                TargetName = mspQueries[targetID].Name
                            };
                            edges.Add(edge);
                            pairDone.Add(pairKey);
                        }
                    }
                    counter++;
                }
            }

            return edges;
        }

        public static List<EdgeInformation> GetEdgeInformations(List<MspFormatCompoundInformationBean> mspQueries, double cutoff, double masstol, 
            BackgroundWorker bgWorker, double maxCoeff = 1.0) {

            var totalCount = (mspQueries.Count - 1) * (mspQueries.Count - 1) * 0.5;
            var counter = 0;
            var pairDone = new List<string>();
            var edges = new List<EdgeInformation>();
            var reportedProgress = 0.0;

            for (int i = 0; i < mspQueries.Count; i++) {
                for (int j = i + 1; j < mspQueries.Count; j++) {

                    var eimsClusterScore = EimsSimilarityScore(mspQueries[i], mspQueries[j], masstol);
                    if (eimsClusterScore >= cutoff * 0.01) {

                        var sourceID = Math.Min(i, j);
                        var targetID = Math.Max(i, j);

                        var edge = new EdgeInformation() {
                            Source = mspQueries[sourceID].Comment,
                            Target = mspQueries[targetID].Comment,
                            Score = Math.Round(eimsClusterScore, 3),
                            Comment = "EI-MS similarity",
                            SourceName = mspQueries[sourceID].Name,
                            TargetName = mspQueries[targetID].Name
                        };
                        edges.Add(edge);
                    }
                    counter++;

                    var progress = (double)counter / (double)totalCount * 100.0 * maxCoeff;
                    if (progress - reportedProgress > 1) {
                        bgWorker.ReportProgress((int)progress);
                        reportedProgress = progress;
                    }
                }
            }

            return edges;
        }

        public static List<EdgeInformation> GetEdgeInformations(MspFormatCompoundInformationBean tartedQuery, 
            List<MspFormatCompoundInformationBean> mspQueries, double cutoff, double masstol,
            BackgroundWorker bgWorker, double maxCoeff = 1.0) {

            var totalCount = mspQueries.Count - 1;
            var counter = 0;
            var pairDone = new List<string>();
            var edges = new List<EdgeInformation>();
            var reportedProgress = 0.0;

            for (int i = 0; i < mspQueries.Count; i++) {

                var eimsClusterScore = EimsSimilarityScore(mspQueries[i], tartedQuery, masstol);
                if (eimsClusterScore >= cutoff * 0.01) {

                    //var sourceID = Math.Min(i, int.Parse(tartedQuery.Comment));
                    //var targetID = Math.Max(i, int.Parse(tartedQuery.Comment));

                    var edge = new EdgeInformation() {
                        Source = tartedQuery.Comment,
                        Target = mspQueries[i].Comment,
                        Score = Math.Round(eimsClusterScore, 3),
                        Comment = "EI-MS similarity",
                        SourceName = tartedQuery.Name,
                        TargetName = mspQueries[i].Name
                    };
                    edges.Add(edge);
                }
                counter++;

                var progress = (double)counter / (double)totalCount * 100.0 * maxCoeff;
                if (progress - reportedProgress > 1) {
                    bgWorker.ReportProgress((int)progress);
                    reportedProgress = progress;
                }
            }

            return edges;
        }

        private static double EimsSimilarityScore(MspFormatCompoundInformationBean spectrumA, MspFormatCompoundInformationBean spectrumB, double masstol) {

            var dotProduct = GcmsScoring.GetDotProduct(spectrumA.MzIntensityCommentBeanList, spectrumB.MzIntensityCommentBeanList, (float)masstol, 85, 500);
            var revProduct = GcmsScoring.GetReverseDotProduct(spectrumA.MzIntensityCommentBeanList, spectrumB.MzIntensityCommentBeanList, (float)masstol, 85, 500);
            var presences = GcmsScoring.GetPresencePercentage(spectrumA.MzIntensityCommentBeanList, spectrumB.MzIntensityCommentBeanList, (float)masstol, 85, 500);
            var totalSimilarity = GcmsScoring.GetEiSpectraSimilarity(dotProduct, revProduct, presences);

            return dotProduct;
        }
    }
}
