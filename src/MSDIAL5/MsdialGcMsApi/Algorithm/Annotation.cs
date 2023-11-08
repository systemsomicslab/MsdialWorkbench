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
        private readonly string _annotatorID;
        private readonly MsdialGcmsParameter _parameter;
        private readonly RetentionType _rType;
        private readonly float _rTolerance;

        public Annotation(DataBaseItem<MoleculeDataBase> mspDB, MsdialGcmsParameter parameter) {
            _parameter = parameter;
            _rType = parameter.RetentionType;
            ChromXType type;
            float tolerance;
            switch (_rType) {
                case RetentionType.RI:
                    type = ChromXType.RI;
                    tolerance = parameter.MspSearchParam.RtTolerance;
                    break;
                case RetentionType.RT:
                    type = ChromXType.RT;
                    tolerance = parameter.MspSearchParam.RiTolerance;
                    break;
                default:
                    throw new Exception($"Unknown {nameof(RetentionType)}: {_rType}");
            }
            _mspDB = mspDB?.DataBase.Database.OrderBy(r => r.ChromXs.GetChromByType(type).Value).ToList();
            _annotatorID = mspDB?.Pairs.FirstOrDefault()?.AnnotatorID;
            var factor = _parameter.MspSearchParam.IsUseTimeForAnnotationFiltering ? 1.0F : 2.0F;
            _rTolerance = tolerance * factor;
        }

        /// <summary>
        /// ref must be ordered by retention time or retention index
        /// </summary>
        /// <param name="ms1DecResults"></param>
        /// <param name="reporter"></param>
        /// 
        public AnnotatedMSDecResult[] MainProcess(IReadOnlyList<MSDecResult> ms1DecResults, ReportProgress reporter) {
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
                if (_parameter.OnlyReportTopHitInMspSearch) {

                }
                else {

                }
                return features;
            }

            return ms1DecResults.Select(r => new AnnotatedMSDecResult(r, new MsScanMatchResultContainer())).ToArray();
        }

        private List<MsScanMatchResult> MspBasedProccess(MSDecResult msdecResult) {
            var rValue = _rType == RetentionType.RT ? msdecResult.ChromXs.RT.Value : msdecResult.ChromXs.RI.Value;
            var normMSScanProp = DataAccess.GetNormalizedMSScanProperty(msdecResult, _parameter.MspSearchParam);

            var (startID, endID) = RetrieveMspBounds(rValue);
            var matchedQueries = new List<MsScanMatchResult>();
            for (int i = startID; i < endID; i++) {
                var refQuery = _mspDB[i];
                var refRetention = _rType == RetentionType.RT ? refQuery.ChromXs.RT.Value : refQuery.ChromXs.RI.Value;
                System.Diagnostics.Debug.Assert(Math.Abs(rValue - refRetention) < _rTolerance);
                if (Math.Abs(rValue - refRetention) < _rTolerance) {
                    var result = MsScanMatching.CompareEIMSScanProperties(normMSScanProp, refQuery, _parameter.MspSearchParam);
                    if (result.IsSpectrumMatch) {
                        result.LibraryIDWhenOrdered = i;
                        result.AnnotatorID = _annotatorID;
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

        private (int, int) RetrieveMspBounds(double rValue) {
            switch (_rType) {
                case RetentionType.RT: {
                        var startID = SearchCollection.UpperBound(_mspDB, rValue - _rTolerance, (a, b) => a.ChromXs.RT.Value.CompareTo(b));
                        var endID = SearchCollection.LowerBound(_mspDB, rValue + _rTolerance, (a, b) => a.ChromXs.RT.Value.CompareTo(b));
                        return (startID, endID);
                    }
                case RetentionType.RI: {
                        var startID = SearchCollection.UpperBound(_mspDB, rValue - _rTolerance, (a, b) => a.ChromXs.RI.Value.CompareTo(b));
                        var endID = SearchCollection.LowerBound(_mspDB, rValue + _rTolerance, (a, b) => a.ChromXs.RI.Value.CompareTo(b));
                        return (startID, endID);
                    }
                default:
                    throw new Exception($"Unknown {nameof(RetentionType)}: {_rType}");
            }
        }
    }
}
