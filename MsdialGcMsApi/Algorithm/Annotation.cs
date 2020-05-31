using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialGcMsApi.Algorithm {
    public class Annotation {

        /// <summary>
        /// ref must be ordered by retention time or retention index
        /// </summary>
        /// <param name="ms1DecResults"></param>
        /// <param name="mspDB"></param>
        /// <param name="param"></param>
        /// <param name="carbon2RtDict"></param>
        /// <param name="reportAction"></param>
        public static void MainProcess(List<MSDecResult> ms1DecResults, List<MoleculeMsReference> mspDB, 
            MsdialGcmsParameter param, Dictionary<int, float> carbon2RtDict, Action<int> reportAction) {

            SetRetentionIndexForMS1DecResults(ms1DecResults, param, carbon2RtDict);

            if (param.IsIdentificationOnlyPerformedForAlignmentFile)
                return;

            if (mspDB != null && mspDB.Count > 0) {
                MspBasedProccess(ms1DecResults, mspDB, param, reportAction);
            }
        }

        public static void MspBasedProccess(List<MSDecResult> results, List<MoleculeMsReference> mspDB, MsdialGcmsParameter param, Action<int> reportAction) {
            foreach (var result in results) {
                MspBasedProccess(result, mspDB, param, reportAction);
            }
        }

        public static void MspBasedProccess(MSDecResult msdecResult, List<MoleculeMsReference> mspDB, MsdialGcmsParameter param, Action<int> reportAction) {
            var rType = param.RetentionType;
            var rValue = rType == RetentionType.RT ? msdecResult.ChromXs.RT.Value : msdecResult.ChromXs.RI.Value;
            var rTolerance = rType == RetentionType.RT ? param.MspSearchParam.RtTolerance : param.MspSearchParam.RiTolerance;
            var factor = param.MspSearchParam.IsUseTimeForAnnotationFiltering ? 1.0F : 2.0F;

            rTolerance *= factor;

            RetrieveMspBounds(mspDB, rType, rValue, rTolerance, out int startID, out int endID);

            var matchedQueries = new List<MsScanMatchResult>();
            for (int i = startID; i <= endID; i++) {
                var refQuery = mspDB[i];
                var refRetention = rType == RetentionType.RT ? refQuery.ChromXs.RT.Value : refQuery.ChromXs.RI.Value;
                if (Math.Abs(rValue - refRetention) < rTolerance) {
                    var result = MsScanMatching.CompareEIMSScanProperties(msdecResult, refQuery, param.MspSearchParam);
                    if (result.IsSpectrumMatch) {
                        result.LibraryID = i;
                        matchedQueries.Add(result);
                    }
                }
            }


        }

        private static void RetrieveMspBounds(List<MoleculeMsReference> mspDB, RetentionType rType, double rValue, float rTolerance, out int startID, out int endID) {
            startID = 0;
            endID = mspDB.Count - 1;
            if (rType == RetentionType.RT) {
                startID = SearchCollection.LowerBound(mspDB,
                    new MoleculeMsReference() { ChromXs = new ChromXs(rValue - rTolerance, ChromXType.RT, ChromXUnit.Min) },
                    (a, b) => a.ChromXs.RT.Value.CompareTo(b.ChromXs.RT.Value));
                endID = SearchCollection.UpperBound(mspDB,
                    new MoleculeMsReference() { ChromXs = new ChromXs(rValue + rTolerance, ChromXType.RT, ChromXUnit.Min) },
                    (a, b) => a.ChromXs.RT.Value.CompareTo(b.ChromXs.RT.Value));
            }
            else {
                startID = SearchCollection.LowerBound(mspDB,
                    new MoleculeMsReference() { ChromXs = new ChromXs(rValue - rTolerance, ChromXType.RI, ChromXUnit.None) },
                    (a, b) => a.ChromXs.RI.Value.CompareTo(b.ChromXs.RI.Value));
                endID = SearchCollection.UpperBound(mspDB,
                    new MoleculeMsReference() { ChromXs = new ChromXs(rValue + rTolerance, ChromXType.RI, ChromXUnit.None) },
                    (a, b) => a.ChromXs.RI.Value.CompareTo(b.ChromXs.RI.Value));
            }
        }

        public static void SetRetentionIndexForMS1DecResults(List<MSDecResult> ms1DecResults,
            MsdialGcmsParameter param, Dictionary<int, float> carbon2RtDict) {
            if (!carbon2RtDict.IsEmptyOrNull()) {
                return;
            }

            if (param.RiCompoundType == RiCompoundType.Alkanes)
                foreach (var result in ms1DecResults) result.ChromXs.RI = new RetentionIndex(RetentionIndexHandler.GetRetentionIndexByAlkanes(carbon2RtDict, (float)result.ChromXs.Value));
            else {
                var fiehnRiDict = RetentionIndexHandler.GetFiehnFamesDictionary();
                Execute(fiehnRiDict, carbon2RtDict, ms1DecResults);
            }
        }

        public static void Execute(Dictionary<int, float> fiehnRiDict, Dictionary<int, float> famesRtDict, List<MSDecResult> ms1DecResults) {
            var fiehnRiCoeff = RetentionIndexHandler.GetFiehnRiCoefficient(fiehnRiDict, famesRtDict);

            foreach (var result in ms1DecResults) {
                var rt = (float)result.ChromXs.RT.Value;
                result.ChromXs.RI = new RetentionIndex((float)Math.Round(RetentionIndexHandler.CalculateFiehnRi(fiehnRiCoeff, rt), 1));
            }
        }


    }
}
