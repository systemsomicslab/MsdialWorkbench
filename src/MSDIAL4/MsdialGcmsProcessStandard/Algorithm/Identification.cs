using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Msdial.Gcms.Dataprocess.Algorithm
{
    public class IdentificationResultTemp {
        private int mspID;
        private double mspScore;
        private double eiSpecScore;
        private double rtSimScore;
        private double riSimScore;
        private double dotproduct;
        private double revDotproduct;
        private double presencePercentage;
       
        #region constructor
        public IdentificationResultTemp() {
            MspScore = double.MinValue;
            EiSpecScore = -1.0;
            RtSimScore = -1.0;
            RiSimScore = -1.0;
            Dotproduct = -1.0;
            RevDotproduct = -1.0;
            PresencePercentage = -1.0;
            MspID = -1;
        }
        #endregion

        #region propeties
        public int MspID {
            get {
                return mspID;
            }

            set {
                mspID = value;
            }
        }

        public double MspScore {
            get {
                return mspScore;
            }

            set {
                mspScore = value;
            }
        }

        public double EiSpecScore {
            get {
                return eiSpecScore;
            }

            set {
                eiSpecScore = value;
            }
        }

        public double RtSimScore {
            get {
                return rtSimScore;
            }

            set {
                rtSimScore = value;
            }
        }

        public double RiSimScore {
            get {
                return riSimScore;
            }

            set {
                riSimScore = value;
            }
        }

        public double Dotproduct {
            get {
                return dotproduct;
            }

            set {
                dotproduct = value;
            }
        }

        public double RevDotproduct {
            get {
                return revDotproduct;
            }

            set {
                revDotproduct = value;
            }
        }

        public double PresencePercentage {
            get {
                return presencePercentage;
            }

            set {
                presencePercentage = value;
            }
        }
        #endregion
    }


    public sealed class Identification
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetCurrentProcessorNumber();

        private Identification() { }

        private const double initialProgress = 60.0;
        private const double progressMax = 40.0;

        public static void MainProcess(List<MS1DecResult> ms1DecResults, List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param, AnalysisFileBean file, Action<int> reportAction)
        {
            SetRetentionIndexForMS1DecResults(ms1DecResults, param, file);
            if (param.IsIdentificationOnlyPerformedForAlignmentFile)
                return;

            if (mspDB != null && mspDB.Count > 0)
            {
                //if (param.RetentionType == RetentionType.RT)
                //    mspDB = mspDB.OrderBy(n => n.RetentionTime).ToList();
                //else
                //    mspDB = mspDB.OrderBy(n => n.RetentionIndex).ToList();
                MspBasedProccess(ms1DecResults, mspDB, param, reportAction);
            
                //mspDB = mspDB.OrderBy(n => n.Id).ToList();
            }
        }

        public static void SetRetentionIndexForMS1DecResults(List<MS1DecResult> ms1DecResults, 
            AnalysisParamOfMsdialGcms param, AnalysisFileBean file)
        {
            if (!param.FileIdRiInfoDictionary.ContainsKey(file.AnalysisFilePropertyBean.AnalysisFileId)) return;

            var carbonRtDict = param.FileIdRiInfoDictionary[file.AnalysisFilePropertyBean.AnalysisFileId].RiDictionary;
            if (carbonRtDict == null || carbonRtDict.Count == 0) return;

            if (param.RiCompoundType == RiCompoundType.Alkanes)
                foreach (var result in ms1DecResults) result.RetentionIndex = GcmsScoring.GetRetentionIndexByAlkanes(carbonRtDict, result.RetentionTime);
            else
            {
                var fiehnRiDict = MspFileParcer.GetFiehnFamesDictionary();
                FiehnRiCalculator.Execute(fiehnRiDict, carbonRtDict, ms1DecResults);
            }
        }

        public static void MspBasedProccess(List<MS1DecResult> ms1DecResults, List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param, Action<int> reportAction)
        {
            var syncObj = new object();
            var progress = 0;

            Parallel.ForEach(ms1DecResults, result =>
            {
                var maxMspScore = double.MinValue;
                var maxEiSpecScore = -1.0;
                var maxRtSimScore = -1.0;
                var maxRiSimScore = -1.0;
                var maxDotproduct = -1.0;
                var maxRevDotproduct = -1.0;
                var maxPresencePercentage = -1.0;
                var maxMspID = -1;

                MspBasedProcess(result, mspDB, param, out maxMspID, out maxMspScore,
                    out maxEiSpecScore, out maxRtSimScore, out maxRiSimScore,
                    out maxDotproduct, out maxRevDotproduct, out maxPresencePercentage);

                if (maxMspID >= 0)
                {
                    result.MetaboliteName = mspDB[maxMspID].Name;
                    result.MspDbID = mspDB[maxMspID].Id;
                    result.RetentionTimeSimilarity = (float)maxRtSimScore * 1000;
                    result.RetentionIndexSimilarity = (float)maxRiSimScore * 1000;
                    result.DotProduct = (float)maxDotproduct * 1000;
                    result.ReverseDotProduct = (float)maxRevDotproduct * 1000;
                    result.PresencePersentage = (float)maxPresencePercentage * 1000;
                    result.EiSpectrumSimilarity = (float)maxEiSpecScore * 1000;
                    result.TotalSimilarity = (float)maxMspScore * 1000;
                }

                progress++;
                progressReports(progress, ms1DecResults.Count, reportAction);
            });

            if (param.IsOnlyTopHitReport)
                ms1DecResults = removeDuplicateIdentifications(ms1DecResults);
        }

        public static void MspBasedProcess(MS1DecResult result, List<MspFormatCompoundInformationBean> mspDB, 
            AnalysisParamOfMsdialGcms param, out int maxMspID, out double maxMspScore, 
            out double maxEiSpecScore, out double maxRtSimScore, out double maxRiSimScore, 
            out double maxDotproduct, out double maxRevDotproduct, out double maxPresencePercentage) {

            //maxMspScore = -1.0;
            //maxRtSimScore = -1.0;
            //maxRiSimScore = -1.0;
            //maxEiSpecScore = -1.0;
            //maxDotproduct = -1.0;
            //maxRevDotproduct = -1.0;
            //maxPresencePercentage = -1.0;
            //maxMspID = -1;

            var maxMspScoreLocal = -1.0;
            var maxEiSpecScoreLocal = -1.0;
            var maxRtSimScoreLocal = -1.0;
            var maxRiSimScoreLocal = -1.0;
            var maxDotproductLocal = -1.0;
            var maxRevDotproductLocal = -1.0;
            var maxPresencePercentageLocal = -1.0;
            var maxMspIDLocal = -1;

            var startID = getMspStartIndex(result, mspDB, param);
            var endID = getMspEndIndex(result, mspDB, param);

            for (int i = startID; i <= endID; i++) {

                if (!isBetweenRetentionRange(result, mspDB[i], param)) continue;

                var rtSimilarity = getRtSimilarity(result, mspDB[i], param);
                var riSimilarity = getRiSimilarity(result, mspDB[i], param);
                var dotProduct = GcmsScoring.GetDotProduct(result.Spectrum, mspDB[i].MzIntensityCommentBeanList, 
                    param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
                var revDotProduct = GcmsScoring.GetReverseDotProduct(result.Spectrum, mspDB[i].MzIntensityCommentBeanList, 
                    param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
                var presencePercentage = GcmsScoring.GetPresencePercentage(result.Spectrum, mspDB[i].MzIntensityCommentBeanList, 
                    param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
                var eiSimilairty = GcmsScoring.GetEiSpectraSimilarity(dotProduct, revDotProduct, presencePercentage);

                if (eiSimilairty * 100 > param.EiSimilarityLibrarySearchCutOff) {
                    var totalScore = -1.0;
                    if (param.RetentionType == RetentionType.RI)
                        totalScore = GcmsScoring.GetTotalSimilarity(riSimilarity, eiSimilairty, 
                            param.IsUseRetentionInfoForIdentificationScoring);
                    else
                        totalScore = GcmsScoring.GetTotalSimilarity(rtSimilarity, eiSimilairty, 
                            param.IsUseRetentionInfoForIdentificationScoring);

                    if (totalScore * 100 > param.IdentificationScoreCutOff && maxMspScoreLocal < totalScore) {
                            maxMspScoreLocal = totalScore;
                            maxRtSimScoreLocal = rtSimilarity;
                            maxRiSimScoreLocal = riSimilarity;
                            maxEiSpecScoreLocal = eiSimilairty;
                            maxDotproductLocal = dotProduct;
                            maxRevDotproductLocal = revDotProduct;
                            maxPresencePercentageLocal = presencePercentage;
                            //maxMspIDLocal = mspDB[i].Id;
                            maxMspIDLocal = i;
                    }
                }
            }

            #region
            //var syncObj = new object();

            //Parallel.For(startID, endID, j => {

            //    var rtSimilarity = getRtSimilarity(result, mspDB[j], param);
            //    var riSimilarity = getRiSimilarity(result, mspDB[j], param);
            //    var dotProduct = GcmsScoring.GetDotProduct(result.Spectrum, mspDB[j].MzIntensityCommentBeanList, param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
            //    var revDotProduct = GcmsScoring.GetReverseDotProduct(result.Spectrum, mspDB[j].MzIntensityCommentBeanList, param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
            //    var presencePercentage = GcmsScoring.GetPresencePercentage(result.Spectrum, mspDB[j].MzIntensityCommentBeanList, param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
            //    var eiSimilairty = GcmsScoring.GetEiSpectraSimilarity(dotProduct, revDotProduct, presencePercentage);

            //    if (eiSimilairty * 100 > param.EiSimilarityLibrarySearchCutOff) {
            //        var totalScore = -1.0;
            //        if (param.RetentionType == RetentionType.RI)
            //            totalScore = GcmsScoring.GetTotalSimilarity(riSimilarity, eiSimilairty, param.IsUseRetentionInfoForIdentificationScoring);
            //        else
            //            totalScore = GcmsScoring.GetTotalSimilarity(rtSimilarity, eiSimilairty, param.IsUseRetentionInfoForIdentificationScoring);

            //        if (totalScore * 100 > param.IdentificationScoreCutOff && maxMspScoreLocal < totalScore) {
            //            lock (syncObj) {
            //                maxMspScoreLocal = totalScore;
            //                maxRtSimScoreLocal = rtSimilarity;
            //                maxRiSimScoreLocal = riSimilarity;
            //                maxEiSpecScoreLocal = eiSimilairty;
            //                maxDotproductLocal = dotProduct;
            //                maxRevDotproductLocal = revDotProduct;
            //                maxPresencePercentageLocal = presencePercentage;
            //                maxMspIDLocal = mspDB[j].Id;
            //            }
            //        }
            //    }
            //});
            #endregion

            maxMspScore = maxMspScoreLocal;
            maxRtSimScore = maxRtSimScoreLocal;
            maxRiSimScore = maxRiSimScoreLocal;
            maxEiSpecScore = maxEiSpecScoreLocal;
            maxDotproduct = maxDotproductLocal;
            maxRevDotproduct = maxRevDotproductLocal;
            maxPresencePercentage = maxPresencePercentageLocal;
            maxMspID = maxMspIDLocal;
        }

        private static bool isBetweenRetentionRange(MS1DecResult result, 
            MspFormatCompoundInformationBean query, AnalysisParamOfMsdialGcms param) {

            var rtTol = param.RetentionTimeLibrarySearchTolerance;
            var riTol = param.RetentionIndexLibrarySearchTolerance;
            var factor = param.IsUseRetentionInfoForIdentificationFiltering ? 1.0 : 2.0;

            if (param.RetentionType == RetentionType.RI) {
                if (Math.Abs(result.RetentionIndex - query.RetentionIndex) < riTol * factor)
                    return true;
                else
                    return false;
            }
            else {
                if (Math.Abs(result.RetentionTime - query.RetentionTime) < rtTol * factor)
                    return true;
                else
                    return false;
            }

        }

        public static void MspBasedNewProcess(MS1DecResult result, List<MspFormatCompoundInformationBean> mspDB,
           AnalysisParamOfMsdialGcms param, out int maxMspID, out double maxMspScore,
           out double maxEiSpecScore, out double maxRtSimScore, out double maxRiSimScore,
           out double maxDotproduct, out double maxRevDotproduct, out double maxPresencePercentage) {

            var startID = getMspStartIndex(result, mspDB, param);
            var endID = getMspEndIndex(result, mspDB, param);
            var results = new IdentificationResultTemp[mspDB.Count];
            Parallel.For(0, mspDB.Count, i => {
                results[i] = new IdentificationResultTemp();
            });

            Parallel.For(startID, endID, i => {
                var rtSimilarity = getRtSimilarity(result, mspDB[i], param);
                var riSimilarity = getRiSimilarity(result, mspDB[i], param);
                var dotProduct = GcmsScoring.GetDotProduct(result.Spectrum, mspDB[i].MzIntensityCommentBeanList,
                    param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
                var revDotProduct = GcmsScoring.GetReverseDotProduct(result.Spectrum, mspDB[i].MzIntensityCommentBeanList,
                    param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
                var presencePercentage = GcmsScoring.GetPresencePercentage(result.Spectrum, mspDB[i].MzIntensityCommentBeanList,
                    param.MzLibrarySearchTolerance, param.MassRangeBegin, param.MassRangeEnd);
                var eiSimilairty = GcmsScoring.GetEiSpectraSimilarity(dotProduct, revDotProduct, presencePercentage);

                if (eiSimilairty * 100 > param.EiSimilarityLibrarySearchCutOff) {
                    var totalScore = -1.0;
                    if (param.RetentionType == RetentionType.RI)
                        totalScore = GcmsScoring.GetTotalSimilarity(riSimilarity, eiSimilairty,
                            param.IsUseRetentionInfoForIdentificationScoring);
                    else
                        totalScore = GcmsScoring.GetTotalSimilarity(rtSimilarity, eiSimilairty,
                            param.IsUseRetentionInfoForIdentificationScoring);

                    if (totalScore * 100 > param.IdentificationScoreCutOff) {
                        //results[i].MspID = mspDB[i].Id;
                        results[i].MspID = i;
                        results[i].Dotproduct = dotProduct;
                        results[i].EiSpecScore = eiSimilairty;
                        results[i].MspScore = totalScore;
                        results[i].PresencePercentage = presencePercentage;
                        results[i].RevDotproduct = revDotProduct;
                        results[i].RiSimScore = riSimilarity;
                        results[i].RtSimScore = rtSimilarity;
                        Debug.WriteLine("msp ID {0}, thread ID {1}, core ID {2}", i, Thread.CurrentThread.ManagedThreadId, GetCurrentProcessorNumber());
                    }
                }
            });

            results = results.OrderByDescending(n => n.MspScore).ToArray();
            var maxResult = results[0];
            maxMspScore = maxResult.MspScore;
            maxRtSimScore = maxResult.RtSimScore;
            maxRiSimScore = maxResult.RiSimScore;
            maxEiSpecScore = maxResult.EiSpecScore;
            maxDotproduct = maxResult.Dotproduct;
            maxRevDotproduct = maxResult.RevDotproduct;
            maxPresencePercentage = maxResult.PresencePercentage;
            maxMspID = maxResult.MspID;
        }

        public static void MspBasedParallelProcess(MS1DecResult result, List<MspFormatCompoundInformationBean> mspDB,
            AnalysisParamOfMsdialGcms param, out int maxMspID, out double maxMspScore,
            out double maxEiSpecScore, out double maxRtSimScore, out double maxRiSimScore,
            out double maxDotproduct, out double maxRevDotproduct, out double maxPresencePercentage) {

            var syncObj = new object();
            var parallelCount = 10;
            if (mspDB.Count > 100000)
                parallelCount = 100;

            var maxMspScoreLocal = double.MinValue;
            var maxEiSpecScoreLocal = -1.0;
            var maxRtSimScoreLocal = -1.0;
            var maxRiSimScoreLocal = -1.0;
            var maxDotproductLocal = -1.0;
            var maxRevDotproductLocal = -1.0;
            var maxPresencePercentageLocal = -1.0;
            var maxMspIDLocal = -1;

            Parallel.For(0, parallelCount - 1, i => {
                var mspParallel = mspDB.Where(n => n.Id % parallelCount == i).ToList();

                var maxMspScoreParallel = double.MinValue;
                var maxEiSpecScoreParallel = -1.0;
                var maxRtSimScoreParallel = -1.0;
                var maxRiSimScoreParallel = -1.0;
                var maxDotproductParallel = -1.0;
                var maxRevDotproductParallel = -1.0;
                var maxPresencePercentageParallel = -1.0;
                var maxMspIDParallel = -1;

                MspBasedProcess(result, mspParallel, param, out maxMspIDParallel, out maxMspScoreParallel, out maxEiSpecScoreParallel,
                out maxRtSimScoreParallel, out maxRiSimScoreParallel, out maxDotproductParallel,
                out maxRevDotproductParallel, out maxPresencePercentageParallel);

                if (maxMspScoreParallel * 100 > param.IdentificationScoreCutOff) {
                    lock (syncObj) {
                        if (maxMspScoreParallel > maxMspScoreLocal) {

                            maxMspScoreLocal = maxMspScoreParallel;
                            maxRtSimScoreLocal = maxRtSimScoreParallel;
                            maxRiSimScoreLocal = maxRiSimScoreParallel;
                            maxEiSpecScoreLocal = maxEiSpecScoreParallel;
                            maxDotproductLocal = maxDotproductParallel;
                            maxRevDotproductLocal = maxRevDotproductParallel;
                            maxPresencePercentageLocal = maxPresencePercentageParallel;
                            maxMspIDLocal = maxMspIDParallel;
                        }
                    }
                }

            });

            maxMspScore = maxMspScoreLocal;
            maxRtSimScore = maxRtSimScoreLocal;
            maxRiSimScore = maxRiSimScoreLocal;
            maxEiSpecScore = maxEiSpecScoreLocal;
            maxDotproduct = maxDotproductLocal;
            maxRevDotproduct = maxRevDotproductLocal;
            maxPresencePercentage = maxPresencePercentageLocal;
            maxMspID = maxMspIDLocal;
        }


        private static List<MS1DecResult> removeDuplicateIdentifications(List<MS1DecResult> ms1DecResults)
        {
            if (ms1DecResults.Count <= 1) return ms1DecResults;

            ms1DecResults = ms1DecResults.OrderByDescending(n => n.MspDbID).ToList();

            var currentLibraryId = ms1DecResults[0].MspDbID;
            var currentPeakId = 0;

            for (int i = 1; i < ms1DecResults.Count; i++) {
                if (ms1DecResults[i].MspDbID < 0) break;
                if (ms1DecResults[i].MspDbID != currentLibraryId) {
                    currentLibraryId = ms1DecResults[i].MspDbID;
                    currentPeakId = i;
                    continue;
                }
                else {
                    if (ms1DecResults[currentPeakId].TotalSimilarity < ms1DecResults[i].TotalSimilarity) {
                        setDefaultCompoundInformation(ms1DecResults[currentPeakId]);
                        currentPeakId = i;
                    }
                    else {
                        setDefaultCompoundInformation(ms1DecResults[i]);
                    }
                }
            }

            return ms1DecResults.OrderBy(n => n.Ms1DecID).ToList();
        }

        private static void setDefaultCompoundInformation(MS1DecResult mS1DecResult)
        {
            mS1DecResult.MetaboliteName = string.Empty;
            mS1DecResult.MspDbID = -1;
            mS1DecResult.RetentionTimeSimilarity = -1;
            mS1DecResult.RetentionIndexSimilarity = -1;
            mS1DecResult.DotProduct = -1;
            mS1DecResult.ReverseDotProduct = -1;
            mS1DecResult.PresencePersentage = -1;
            mS1DecResult.EiSpectrumSimilarity = -1;
            mS1DecResult.TotalSimilarity = -1;
        }

        private static double getRtSimilarity(MS1DecResult result, MspFormatCompoundInformationBean msp, AnalysisParamOfMsdialGcms param)
        {
            if (msp.RetentionTime >= 0 && result.RetentionTime >= 0)
                return GcmsScoring.GetGaussianSimilarity(result.RetentionTime, msp.RetentionTime, param.RetentionTimeLibrarySearchTolerance);
            else
                return -1;
        }

        private static double getRiSimilarity(MS1DecResult result, MspFormatCompoundInformationBean msp, AnalysisParamOfMsdialGcms param)
        {
            if (msp.RetentionIndex >= 0 && result.RetentionIndex >= 0)
                return GcmsScoring.GetGaussianSimilarity(result.RetentionIndex, msp.RetentionIndex, param.RetentionIndexLibrarySearchTolerance);
            else
                return -1;
        }

        private static int getMspStartIndex(MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param)
        {
            int startIndex = 0, endIndex = mspDB.Count - 1;
            var targetRT = ms1DecResult.RetentionIndex - param.RetentionIndexLibrarySearchTolerance * 1.5;
            if (param.RetentionType == RetentionType.RT) targetRT = ms1DecResult.RetentionTime - param.RetentionTimeLibrarySearchTolerance * 1.5;

            int counter = 0;
            while (counter < 5)
            {
                if (param.RetentionType == RetentionType.RT)
                {
                    if (mspDB[startIndex].RetentionTime <= targetRT && targetRT < mspDB[(startIndex + endIndex) / 2].RetentionTime)
                    {
                        endIndex = (startIndex + endIndex) / 2;
                    }
                    else if (mspDB[(startIndex + endIndex) / 2].RetentionTime <= targetRT && targetRT < mspDB[endIndex].RetentionTime)
                    {
                        startIndex = (startIndex + endIndex) / 2;
                    }
                }
                else
                {
                    if (mspDB[startIndex].RetentionIndex <= targetRT && targetRT < mspDB[(startIndex + endIndex) / 2].RetentionIndex)
                    {
                        endIndex = (startIndex + endIndex) / 2;
                    }
                    else if (mspDB[(startIndex + endIndex) / 2].RetentionIndex <= targetRT && targetRT < mspDB[endIndex].RetentionIndex)
                    {
                        startIndex = (startIndex + endIndex) / 2;
                    }
                }
                counter++;
            }
            return startIndex;
        }

        private static int getMspEndIndex(MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param)
        {
            int startIndex = 0, endIndex = mspDB.Count - 1;
            var targetRT = ms1DecResult.RetentionIndex + param.RetentionIndexLibrarySearchTolerance * 1.5;
            if (param.RetentionType == RetentionType.RT) targetRT = ms1DecResult.RetentionTime + param.RetentionTimeLibrarySearchTolerance * 1.5;

            int counter = 0;
            while (counter < 10) {
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
            return endIndex;
        }


        private static void progressReports(float progress, float max, Action<int> reportAction)
        {
            var value = initialProgress + progress / max * progressMax;
            reportAction?.Invoke((int)value);
        }
    }
}
