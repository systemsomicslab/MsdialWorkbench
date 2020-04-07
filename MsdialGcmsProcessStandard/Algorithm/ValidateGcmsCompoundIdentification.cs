using Msdial.Gcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msdial.Gcms.Dataprocess.Validation
{
    public class ValidationResult
    {
        public bool IsCorrect { get; set; }
        public bool IsCorrectWithShortInChIKey { get; set; }
        public double Score { get; set; }
        public MspFormatCompoundInformationBean QueryRecord { get; set; }
        public MspFormatCompoundInformationBean DbRecord { get; set; }

        public ValidationResult()
        {
            IsCorrect = false; IsCorrectWithShortInChIKey = false; Score = -1; QueryRecord = new MspFormatCompoundInformationBean(); DbRecord = new MspFormatCompoundInformationBean();
        }
    }

    public class FalseIdentificationRate
    {
        public int SearchSpaceTotal { get; set; }
        public int QueryNumber { get; set; }
        
        public int TrueNumberOfWholeInChIKey { get; set; }
        public int FalseNumberOfWholeInChIKey { get; set; }
        public float FalsePositiveRateOfWholeInChIKey { get; set; }

        public int TrueNumberOfShortInChIKey { get; set; }
        public int FalseNumberOfShortInChIKey { get; set; }
        public float FalsePositiveRateOfShortInChIKey { get; set; }
    }

    public sealed class ValidateGcmsCompoundIdentification
    {
        private ValidateGcmsCompoundIdentification() { }

        public static void ValidateEffectOfRetentionIndexTolerance(string mspKit, string mspDatabase, string outputFolder, 
            float riTol, float mzTol, float step, float lastRiTol)
        {
            Console.WriteLine("Preparing spectra");
            var mspDB = MspFileParcer.MspFileReader(mspDatabase); mspDB = mspDB.OrderBy(n => n.RetentionIndex).ToList();
            var queries = MspFileParcer.MspFileReader(mspKit); queries = queries.OrderBy(n => n.RetentionIndex).ToList();
            var mainOutputPath = outputFolder + "\\False discovery rate-VS2.txt";
            var fpResults = new List<FalseIdentificationRate>();

            Console.WriteLine("Validation start");
            while (riTol <= lastRiTol) {
                Console.WriteLine("RI tolerance: {0}", riTol);

                var results = new List<ValidationResult>();
                var searchSpaceCounter = 0;
                var detailOutputpath = outputFolder + "\\Result of RI tolerance-" + Math.Round(riTol, 0).ToString() + ".txt";
                foreach(var query in queries){
                    
                    var startID = getMspStartIndex(query, mspDB, riTol);
                    var maxMspScore = double.MinValue;
                    var maxMspID = -1;

                    for (int j = startID; j < mspDB.Count; j++) {
                        if (mspDB[j].RetentionIndex < query.RetentionIndex - riTol) continue;
                        if (mspDB[j].RetentionIndex > query.RetentionIndex + riTol) break;

                        searchSpaceCounter++;

                        var riSimilarity = getRiSimilarity(query, mspDB[j], riTol);
                        var dotProduct = GcmsScoring.GetDotProduct(query.MzIntensityCommentBeanList, mspDB[j].MzIntensityCommentBeanList, mzTol, 85, 500);
                        var revDotProduct = GcmsScoring.GetReverseDotProduct(query.MzIntensityCommentBeanList, mspDB[j].MzIntensityCommentBeanList, mzTol, 85, 500);
                        var presencePercentage = GcmsScoring.GetPresencePercentage(query.MzIntensityCommentBeanList, mspDB[j].MzIntensityCommentBeanList, mzTol, 85, 500);
                        var eiSimilairty = GcmsScoring.GetEiSpectraSimilarity(dotProduct, revDotProduct, presencePercentage);

                        if (eiSimilairty < 0.7) continue;

                        var totalScore = GcmsScoring.GetTotalSimilarity(riSimilarity, eiSimilairty, true);

                        if (maxMspScore < totalScore) {
                            maxMspScore = totalScore;
                            maxMspID = j;
                        }
                    }

                    var result = new ValidationResult() { QueryRecord = query };
                    if (maxMspID != -1) {
                        result.DbRecord = mspDB[maxMspID];
                        result.Score = Math.Round(maxMspScore * 1000, 0);

                        if (result.QueryRecord.InchiKey == result.DbRecord.InchiKey) {
                            result.IsCorrect = true;
                        }

                        if (result.QueryRecord.InchiKey.Substring(0, 14) == result.DbRecord.InchiKey.Substring(0, 14)) {
                            result.IsCorrectWithShortInChIKey = true;
                        }

                        results.Add(result);
                        if (!Console.IsOutputRedirected) {
                            Console.Write("{0} %", Math.Round((double)results.Count / (double)queries.Count * 100, 1));
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }
                        else {
                            Console.WriteLine("{0} %", Math.Round((double)results.Count / (double)queries.Count * 100, 1));
                        }
                    }
                }
                writeValidationResults(detailOutputpath, results);

                var fpResult = new FalseIdentificationRate() {
                    QueryNumber = results.Count,
                    SearchSpaceTotal = searchSpaceCounter,
                    TrueNumberOfWholeInChIKey = results.Count(n => n.IsCorrect == true),
                    TrueNumberOfShortInChIKey = results.Count(n => n.IsCorrectWithShortInChIKey == true),
                    FalseNumberOfWholeInChIKey = results.Count(n => n.IsCorrect == false),
                    FalseNumberOfShortInChIKey = results.Count(n => n.IsCorrectWithShortInChIKey == false)
                };

                fpResult.FalsePositiveRateOfWholeInChIKey = (float)Math.Round((double)fpResult.FalseNumberOfWholeInChIKey / (double)results.Count * 100, 2);
                fpResult.FalsePositiveRateOfShortInChIKey = (float)Math.Round((double)fpResult.FalseNumberOfShortInChIKey / (double)results.Count * 100, 2);
                fpResults.Add(fpResult);
                riTol += step;
            }
            writeFalsePositiveResult(mainOutputPath, fpResults);
        }

        public static void ValidateEffectOfRetentionIndexToleranceWithoutRIfilter(string mspKit, string mspDatabase, string outputFolder)
        {
            Console.WriteLine("Preparing spectra");
            var mspDB = MspFileParcer.MspFileReader(mspDatabase); mspDB = mspDB.OrderBy(n => n.RetentionIndex).ToList();
            var queries = MspFileParcer.MspFileReader(mspKit); queries = queries.OrderBy(n => n.RetentionIndex).ToList();
            var mainOutputPath = outputFolder + "\\False discovery rate-withoutRI.txt";
            var fpResults = new List<FalseIdentificationRate>();
            var mzTol = 0.5F;

            Console.WriteLine("Validation start");

            var results = new List<ValidationResult>();
            var searchSpaceCounter = 0;
            var detailOutputpath = outputFolder + "\\Result of RI tolerance-without.txt";
            foreach (var query in queries) {

                var startID = 0;
                var maxMspScore = double.MinValue;
                var maxMspID = -1;

                for (int j = startID; j < mspDB.Count; j++) {

                    searchSpaceCounter++;

                    var dotProduct = GcmsScoring.GetDotProduct(query.MzIntensityCommentBeanList, mspDB[j].MzIntensityCommentBeanList, mzTol, 85, 500);
                    var revDotProduct = GcmsScoring.GetReverseDotProduct(query.MzIntensityCommentBeanList, mspDB[j].MzIntensityCommentBeanList, mzTol, 85, 500);
                    var presencePercentage = GcmsScoring.GetPresencePercentage(query.MzIntensityCommentBeanList, mspDB[j].MzIntensityCommentBeanList, mzTol, 85, 500);
                    var eiSimilairty = GcmsScoring.GetEiSpectraSimilarity(dotProduct, revDotProduct, presencePercentage);

                    if (maxMspScore < eiSimilairty) {
                        maxMspScore = eiSimilairty;
                        maxMspID = j;
                    }
                }

                var result = new ValidationResult() { QueryRecord = query };
                if (maxMspID != -1) {
                    result.DbRecord = mspDB[maxMspID];
                    result.Score = Math.Round(maxMspScore * 1000, 0);

                    if (result.QueryRecord.InchiKey == result.DbRecord.InchiKey) {
                        result.IsCorrect = true;
                    }

                    if (result.QueryRecord.InchiKey.Substring(0, 14) == result.DbRecord.InchiKey.Substring(0, 14)) {
                        result.IsCorrectWithShortInChIKey = true;
                    }

                    results.Add(result);
                    if (!Console.IsOutputRedirected) {
                        Console.Write("{0} %", Math.Round((double)results.Count / (double)queries.Count * 100, 1));
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("{0} %", Math.Round((double)results.Count / (double)queries.Count * 100, 1));
                    }
                }
            }
            writeValidationResults(detailOutputpath, results);

            var fpResult = new FalseIdentificationRate() {
                QueryNumber = results.Count,
                SearchSpaceTotal = searchSpaceCounter,
                TrueNumberOfWholeInChIKey = results.Count(n => n.IsCorrect == true),
                TrueNumberOfShortInChIKey = results.Count(n => n.IsCorrectWithShortInChIKey == true),
                FalseNumberOfWholeInChIKey = results.Count(n => n.IsCorrect == false),
                FalseNumberOfShortInChIKey = results.Count(n => n.IsCorrectWithShortInChIKey == false)
            };

            fpResult.FalsePositiveRateOfWholeInChIKey = (float)Math.Round((double)fpResult.FalseNumberOfWholeInChIKey / (double)results.Count * 100, 2);
            fpResult.FalsePositiveRateOfShortInChIKey = (float)Math.Round((double)fpResult.FalseNumberOfShortInChIKey / (double)results.Count * 100, 2);
            fpResults.Add(fpResult);
            writeFalsePositiveResult(mainOutputPath, fpResults);
        }


        private static void writeFalsePositiveResult(string mainOutputPath, List<FalseIdentificationRate> fpResults)
        {
            using (var sw = new StreamWriter(mainOutputPath, false, Encoding.ASCII)) {

                sw.WriteLine("Query number\tSearch candidates number\tTrue number of whole inchikey\tFalse number of whole inchikey\tFalse positive rate of whole inchikey\tTrue number of short inchikey\tFalse number of short inchikey\tFalse positive rate of short inchikey");

                foreach (var result in fpResults) {
                    sw.WriteLine(result.QueryNumber + "\t" + result.SearchSpaceTotal + "\t" + result.TrueNumberOfWholeInChIKey + "\t" + result.FalseNumberOfWholeInChIKey + "\t" + result.FalsePositiveRateOfWholeInChIKey
                        + "\t" + result.TrueNumberOfShortInChIKey + "\t" + result.FalseNumberOfShortInChIKey + "\t" + result.FalsePositiveRateOfShortInChIKey);
                }
            }
        }

        private static void writeValidationResults(string detailOutputpath, List<ValidationResult> results)
        {
            using (var sw = new StreamWriter(detailOutputpath, false, Encoding.ASCII)) {

                sw.WriteLine("Is InChIKey correct\tIs ShortInChIKey correct\tScore\tQuery Name\tQuery SMILES\tQuery InChIKey\tQuery Short InChIKey\tQuery RI\tDB Name\tDB SMILES\tDB InChIKey\tDB Short InChIKey\tDB RI");

                foreach (var result in results) {
                    sw.WriteLine(result.IsCorrect + "\t" + result.IsCorrectWithShortInChIKey + "\t" + result.Score
                        + "\t" + result.QueryRecord.Name + "\t" + result.QueryRecord.Smiles + "\t" + result.QueryRecord.InchiKey + "\t" + result.QueryRecord.InchiKey.Substring(0, 14) + "\t" + result.QueryRecord.RetentionIndex
                        + "\t" + result.DbRecord.Name + "\t" + result.DbRecord.Smiles + "\t" + result.DbRecord.InchiKey + "\t" + result.DbRecord.InchiKey.Substring(0, 14) + "\t" + result.DbRecord.RetentionIndex);
                }
            }
        }

        private static int getMspStartIndex(MspFormatCompoundInformationBean query, List<MspFormatCompoundInformationBean> mspDB, float riTol)
        {
            int startIndex = 0, endIndex = mspDB.Count - 1;
            var targetRI = query.RetentionIndex - riTol;

            int counter = 0;
            while (counter < 5) {
                if (mspDB[startIndex].RetentionIndex <= targetRI && targetRI < mspDB[(startIndex + endIndex) / 2].RetentionIndex) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (mspDB[(startIndex + endIndex) / 2].RetentionIndex <= targetRI && targetRI < mspDB[endIndex].RetentionIndex) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        private static double getRiSimilarity(MspFormatCompoundInformationBean query, MspFormatCompoundInformationBean msp, float riTol)
        {
            if (msp.RetentionIndex >= 0 && query.RetentionIndex >= 0)
                return GcmsScoring.GetGaussianSimilarity(query.RetentionIndex, msp.RetentionIndex, riTol);
            else
                return -1;
        }
    }
}
