using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm
{
    public class Annotation {
        private readonly IReadOnlyList<MoleculeMsReference> _mspDB;
        private readonly MsdialGcmsParameter _parameter;

        public Annotation(IReadOnlyList<MoleculeMsReference> mspDB, MsdialGcmsParameter parameter) {
            _mspDB = mspDB;
            _parameter = parameter;
        }

        /// <summary>
        /// ref must be ordered by retention time or retention index
        /// </summary>
        /// <param name="ms1DecResults"></param>
        /// <param name="carbon2RtDict"></param>
        /// <param name="reporter"></param>
        public AnnotatedMSDecResult[] MainProcess(IReadOnlyList<MSDecResult> ms1DecResults, Dictionary<int, float> carbon2RtDict, ReportProgress reporter) {
            Console.WriteLine("Annotation started");
            if (_parameter.IsIdentificationOnlyPerformedForAlignmentFile) {
                return ms1DecResults.Select(r => new AnnotatedMSDecResult(r, new MsScanMatchResultContainer())).ToArray();
            }

            if (_mspDB != null && _mspDB.Count > 0) {
                var features = new AnnotatedMSDecResult[ms1DecResults.Count];
                foreach (var (decResult, index) in ms1DecResults.WithIndex()) {
                    var results = new MsScanMatchResultContainer();
                    results.AddResults(MspBasedProccess(decResult));
                    if (results.Representative is MsScanMatchResult topHit && !topHit.IsUnknown) {
                        features[index] = new AnnotatedMSDecResult(decResult, results, _mspDB[topHit.LibraryID]);
                    }
                    else {
                        features[index] = new AnnotatedMSDecResult(decResult, results);
                    }
                    Console.WriteLine("Done {0}/{1}", index, ms1DecResults.Count);
                    reporter.Show(index, ms1DecResults.Count);
                }
                return features;
            }

            return ms1DecResults.Select(r => new AnnotatedMSDecResult(r, new MsScanMatchResultContainer())).ToArray();
        }

        public List<MsScanMatchResult> MspBasedProccess(MSDecResult msdecResult) {
            var rType = _parameter.RetentionType;
            var rValue = rType == RetentionType.RT ? msdecResult.ChromXs.RT.Value : msdecResult.ChromXs.RI.Value;
            var rTolerance = rType == RetentionType.RT ? _parameter.MspSearchParam.RtTolerance : _parameter.MspSearchParam.RiTolerance;
            var factor = _parameter.MspSearchParam.IsUseTimeForAnnotationFiltering ? 1.0F : 2.0F;
            var normMSScanProp = DataAccess.GetNormalizedMSScanProperty(msdecResult, _parameter.MspSearchParam);

            rTolerance *= factor;

            var (startID, endID) = RetrieveMspBounds(rType, rValue, rTolerance);
            //Console.WriteLine("ID {0}, Start {1}, End {2}", msdecResult.ScanID, startID, endID);
            var matchedQueries = new List<MsScanMatchResult>();
            for (int i = startID; i < endID; i++) {
                var refQuery = _mspDB[i];
                var refRetention = rType == RetentionType.RT ? refQuery.ChromXs.RT.Value : refQuery.ChromXs.RI.Value;
                if (Math.Abs(rValue - refRetention) < rTolerance) {
                    var result = MsScanMatching.CompareEIMSScanProperties(normMSScanProp, refQuery, _parameter.MspSearchParam);
                    if (result.IsSpectrumMatch) {
                        result.LibraryIDWhenOrdered = i;
                        matchedQueries.Add(result);
                    }
                }
            }
            
            foreach (var (result, index) in matchedQueries.OrEmptyIfNull().OrderByDescending(n => n.TotalScore).WithIndex()) {
                if (index == 0) {
                    msdecResult.MspBasedMatchResult = result;
                    msdecResult.MspID = result.LibraryID;
                    msdecResult.MspIDWhenOrdered = result.LibraryIDWhenOrdered;
                }
                msdecResult.MspIDs.Add(result.LibraryID);
            }
            return matchedQueries;
        }

        private (int, int) RetrieveMspBounds(RetentionType rType, double rValue, float rTolerance) {
            if (rType == RetentionType.RT) {
                var startID = SearchCollection.LowerBound(_mspDB,
                    new MoleculeMsReference() { ChromXs = new ChromXs(rValue - rTolerance, ChromXType.RT, ChromXUnit.Min) },
                    (a, b) => a.ChromXs.RT.Value.CompareTo(b.ChromXs.RT.Value));
                var endID = SearchCollection.UpperBound(_mspDB,
                    new MoleculeMsReference() { ChromXs = new ChromXs(rValue + rTolerance, ChromXType.RT, ChromXUnit.Min) },
                    (a, b) => a.ChromXs.RT.Value.CompareTo(b.ChromXs.RT.Value));
                return (startID, endID);
            }
            else {
                var startID = SearchCollection.LowerBound(_mspDB,
                    new MoleculeMsReference() { ChromXs = new ChromXs(rValue - rTolerance, ChromXType.RI, ChromXUnit.None) },
                    (a, b) => a.ChromXs.RI.Value.CompareTo(b.ChromXs.RI.Value));
                var endID = SearchCollection.UpperBound(_mspDB,
                    new MoleculeMsReference() { ChromXs = new ChromXs(rValue + rTolerance, ChromXType.RI, ChromXUnit.None) },
                    (a, b) => a.ChromXs.RI.Value.CompareTo(b.ChromXs.RI.Value));
                return (startID, endID);
            }
        }
    }
}
