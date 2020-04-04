using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Property;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.RtPrediction {

    public class RtStructureQuery {
        public string Name { get; set; }
        public double ExperimentRt { get; set; }
        public double PredictedRt { get; set; }
        public Structure Structure { get; set; }
    }

    public class RtPredictionResult {
        public double Coefficient { get; set; }
        public double Intercept { get; set; }
        public double Rsqure { get; set; }
        public double MaxRtDiff { get; set; }
        public double MinRtDiff { get; set; }
        public double ExperimentRtMin { get; set; }
        public double ExperimentRtMax { get; set; }
        public double PredictedRtMin { get; set; }
        public double PredictedRtMax { get; set; }
        public double XlogpMin { get; set; }
        public double XlogpMax { get; set; }
        public List<RtStructureQuery> RtStructureQueries;

        public RtPredictionResult() { RtStructureQueries = new List<RtStructureQuery>(); }
    }


    public sealed class RtPrediction {

        public static RtPredictionResult GetRtPredictionResult(List<RtStructureQuery> queries) {
            if (queries == null || queries.Count == 0) return null;

            var expRtArray = new double[queries.Count];
            var xlogpArray = new double[queries.Count];

            for (int i = 0; i < queries.Count; i++) {
                expRtArray[i] = queries[i].ExperimentRt;
                xlogpArray[i] = queries[i].Structure.XlogP;
            }

            var regressionResult = RegressionMathematics.PolynomialRegression(xlogpArray, expRtArray, 1);
            if (regressionResult == null || regressionResult.Length != 2) return null;
            var coeff = regressionResult[0];
            var intercept = regressionResult[1];

            var predArray = new double[queries.Count];
            var minDiff = double.MaxValue;
            var maxDiff = double.MinValue;
            for (int i = 0; i < xlogpArray.Length; i++) {
                predArray[i] = coeff * xlogpArray[i] + intercept;
                queries[i].PredictedRt = predArray[i];
                var diff = Math.Abs(predArray[i] - expRtArray[i]);
                if (diff < minDiff) minDiff = diff;
                if (diff > maxDiff) maxDiff = diff;
            }

            var rSqure = Math.Pow(BasicMathematics.Coefficient(expRtArray, predArray), 2);
            var result = new RtPredictionResult() {
                Coefficient = coeff,
                Intercept = intercept,
                Rsqure = rSqure,
                RtStructureQueries = queries,
                XlogpMin = queries.Min(n => n.Structure.XlogP),
                XlogpMax = queries.Max(n => n.Structure.XlogP),
                ExperimentRtMin = queries.Min(n => n.ExperimentRt),
                ExperimentRtMax = queries.Max(n => n.ExperimentRt),
                PredictedRtMin = queries.Min(n => n.PredictedRt),
                PredictedRtMax = queries.Max(n => n.PredictedRt),
                MaxRtDiff = maxDiff, MinRtDiff = minDiff
            };
            return result;
        }


        //[0] Name [1] Rt(min) [2] smiles
        public static List<RtStructureQuery> ReadQueries(string input) {
            var queries = new List<RtStructureQuery>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == null || line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 3) continue;

                    var name = lineArray[0];

                    double rt;
                    if (!double.TryParse(lineArray[1], out rt)) continue;
                    var errorString = string.Empty;
                    var structure = MoleculeConverter.SmilesToStructure(lineArray[2], out errorString);
                    if (errorString != string.Empty || structure == null) continue;

                    var query = new RtStructureQuery() { Name = name, ExperimentRt = rt, Structure = structure };
                    queries.Add(query);
                }
            }
            return queries;
        }
    }
}
