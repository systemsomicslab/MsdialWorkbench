using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CompMs.Common.DataStructure;
using CompMs.Common.Enum;

namespace CompMs.Common.Mathematics.Statistics {
   
    public class MultivariateAnalysisResult {
        public MultivariateAnalysisResult() { }

        // model set
        public StatisticsObject StatisticsObject { get; set; } = new StatisticsObject();
        public MultivariateAnalysisOption MultivariateAnalysisOption { get; set; } = MultivariateAnalysisOption.Plsda;

        // cv result
        public int NFold { get; set; } = 7;
        public int OptimizedFactor { get; set; } = 0;
        public int OptimizedOrthoFactor { get; set; } = 0;
        public ObservableCollection<double> SsCVs { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Presses { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Totals { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Q2Values { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Q2Cums { get; set; } = new ObservableCollection<double>();


        // modeled set
        public ObservableCollection<double> SsPreds { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> CPreds { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double[]> UPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> TPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> WPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> PPreds { get; set; } = new ObservableCollection<double[]>();

        public ObservableCollection<double> Coefficients { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Vips { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> PredictedYs { get; set; } = new ObservableCollection<double>();
        public double Rmsee { get; set; } = 0.0;

        // opls
        public ObservableCollection<double[]> ToPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> WoPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> PoPreds { get; set; } = new ObservableCollection<double[]>();
        public double stdevT { get; set; } = 0.0;
        public ObservableCollection<double> StdevFilteredXs { get; set; } = new ObservableCollection<double>();
        public double[,] FilteredXArray { get; set; }
        public ObservableCollection<double[]> PPredCovs { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> PPredCoeffs { get; set; } = new ObservableCollection<double[]>();

        // pca
        public ObservableCollection<double> Contributions { get; set; } = new ObservableCollection<double>();

        // hca
        public DirectedTree XDendrogram { get; set; }
        public DirectedTree YDendrogram { get; set; }

        public void WriteResult(string output) {
            switch (MultivariateAnalysisOption) {
                case MultivariateAnalysisOption.Pca:
                    WritePcaResult(output);
                    break;
                case MultivariateAnalysisOption.Plsda:
                case MultivariateAnalysisOption.Plsr:
                    WritePlsResult(output);
                    break;
                case MultivariateAnalysisOption.Oplsda:
                case MultivariateAnalysisOption.Oplsr:
                    WriteOplsResult(output);
                    break;
            }
        }

        public void WritePlsResult(string output) {
            using (StreamWriter sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Method\t" + MultivariateAnalysisOption.ToString());
                sw.WriteLine("Optimized factor\t" + OptimizedFactor);
                sw.WriteLine();
                sw.WriteLine("Cross validation N fold\t" + NFold);
                sw.WriteLine("Component\tSSCV\tPRESS\tQ2\tQ2cum");
                for (int i = 0; i < Presses.Count; i++) {
                    sw.WriteLine((i + 1).ToString() + "\t" + SsCVs[i] +
                        "\t" + Presses[i] + "\t" + Q2Values[i] +
                        "\t" + Q2Cums[i]);
                }
                sw.WriteLine();

                var scoreSeq = new List<string>();
                var loadSeq = new List<string>();

                for (int i = 0; i < OptimizedFactor; i++) {
                    scoreSeq.Add("T" + (i + 1).ToString());
                    loadSeq.Add("P" + (i + 1).ToString());
                }

                scoreSeq.Add("Y experiment"); scoreSeq.Add("Y predicted");
                loadSeq.Add("VIP"); loadSeq.Add("Coefficients");

                var scoreSeqString = String.Join("\t", scoreSeq);
                var loadSeqString = String.Join("\t", loadSeq);

                //header set
                var tpredSize = TPreds.Count;
                var toPredSize = ToPreds.Count;
                var metSize = StatisticsObject.XIndexes.Count;
                var fileSize = StatisticsObject.YIndexes.Count;

                sw.WriteLine("Score" + "\t" + scoreSeqString);

                //Scores
                for (int i = 0; i < fileSize; i++) {
                    var tList = new List<double>();
                    for (int j = 0; j < TPreds.Count; j++) {
                        tList.Add(TPreds[j][i]);
                    }
                    tList.Add(StatisticsObject.YVariables[i]);
                    tList.Add(PredictedYs[i]);

                    sw.WriteLine(StatisticsObject.YLabels[i] + "\t" +
                        String.Join("\t", tList));
                }
                sw.WriteLine();

                //Loadings
                sw.WriteLine("Loading" + "\t" + loadSeqString);
                for (int i = 0; i < metSize; i++) {
                    var pList = new List<double>();
                    for (int j = 0; j < PPreds.Count; j++) {
                        pList.Add(PPreds[j][i]);
                    }
                    pList.Add(Vips[i]);
                    pList.Add(Coefficients[i]);

                    sw.WriteLine(StatisticsObject.XLabels[i] + "\t" +
                        String.Join("\t", pList));
                }
            }
        }

        public void WriteOplsResult(string output) {
            using (StreamWriter sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Method\t" + MultivariateAnalysisOption.ToString());
                sw.WriteLine("Optimized biological factor\t" + OptimizedFactor);
                sw.WriteLine("Optimized orthogonal factor\t" + OptimizedOrthoFactor);
                sw.WriteLine();
                sw.WriteLine("Cross validation N fold\t" + NFold);
                sw.WriteLine("Component\tSSCV\tPRESS\tQ2\tQ2cum");
                for (int i = 0; i < Presses.Count; i++) {
                    sw.WriteLine((i + 1).ToString() + "\t" + SsCVs[i] +
                        "\t" + Presses[i] + "\t" + Q2Values[i] +
                        "\t" + Q2Cums[i]);
                }
                sw.WriteLine();

                var scoreSeq = new List<string>();
                var loadSeq = new List<string>();

                for (int i = 0; i < OptimizedFactor; i++) {
                    scoreSeq.Add("T" + (i + 1).ToString());
                    loadSeq.Add("P" + (i + 1).ToString());
                }

                for (int i = 0; i < OptimizedOrthoFactor; i++) {
                    scoreSeq.Add("To" + (i + 1).ToString());
                    loadSeq.Add("Po" + (i + 1).ToString());
                }

                scoreSeq.Add("Y experiment"); scoreSeq.Add("Y predicted");
                loadSeq.Add("VIP"); loadSeq.Add("Coefficients");

                var scoreSeqString = String.Join("\t", scoreSeq);
                var loadSeqString = String.Join("\t", loadSeq);

                //header set
                var tpredSize = TPreds.Count;
                var toPredSize = ToPreds.Count;
                var metSize = StatisticsObject.XIndexes.Count;
                var fileSize = StatisticsObject.YIndexes.Count;

                sw.WriteLine("Score" + "\t" + scoreSeqString);

                //Scores
                for (int i = 0; i < fileSize; i++) {
                    var tList = new List<double>();
                    for (int j = 0; j < TPreds.Count; j++) {
                        tList.Add(TPreds[j][i]);
                    }
                    for (int j = 0; j < ToPreds.Count; j++) {
                        tList.Add(ToPreds[j][i]);
                    }
                    tList.Add(StatisticsObject.YVariables[i]);
                    tList.Add(PredictedYs[i]);

                    sw.WriteLine(StatisticsObject.YLabels[i] + "\t" +
                        String.Join("\t", tList));
                }
                sw.WriteLine();

                //Loadings
                sw.WriteLine("Loading" + "\t" + loadSeqString);
                for (int i = 0; i < metSize; i++) {
                    var pList = new List<double>();
                    for (int j = 0; j < PPreds.Count; j++) {
                        pList.Add(PPreds[j][i]);
                    }
                    for (int j = 0; j < PoPreds.Count; j++) {
                        pList.Add(PoPreds[j][i]);
                    }
                    pList.Add(Vips[i]);
                    pList.Add(Coefficients[i]);

                    sw.WriteLine(StatisticsObject.XLabels[i] + "\t" +
                        String.Join("\t", pList));
                }
            }
        }

        public void WritePcaResult(string output) {

            using (StreamWriter sw = new StreamWriter(output, false, Encoding.ASCII)) {
                //header set
                sw.WriteLine("Contribution");
                for (int i = 0; i < Contributions.Count; i++)
                    sw.WriteLine((i + 1).ToString() + "\t" + Contributions[i]);
                sw.WriteLine();

                var compSize = Contributions.Count;
                var filesize = StatisticsObject.YLabels.Count;
                var metsize = StatisticsObject.XLabels.Count;
                var compSequence = new List<int>();
                for (int i = 0; i < compSize; i++) {
                    compSequence.Add(i + 1);
                }
                var compSeqString = String.Join("\t", compSequence);

                //header set
                sw.WriteLine("Score" + "\t" + compSeqString);

                for (int i = 0; i < filesize; i++) {
                    var tList = new List<double>();
                    for (int j = 0; j < compSize; j++)
                        tList.Add(TPreds[j][i]);
                    sw.WriteLine(StatisticsObject.YLabels[i] + "\t" + String.Join("\t", tList));
                }

                sw.WriteLine();

                //header set
                sw.WriteLine("Loading" + "\t" + compSeqString);

                for (int i = 0; i < metsize; i++) {
                    var pList = new List<double>();
                    for (int j = 0; j < compSize; j++)
                        pList.Add(PPreds[j][i]);
                    sw.WriteLine(StatisticsObject.XLabels[i] + "\t" + String.Join("\t", pList));
                }
            }
        }
    }
}