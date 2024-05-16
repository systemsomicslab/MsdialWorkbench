using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm
{
    public sealed class CalculateMatchScore
    {
        private readonly MsRefSearchParameterBase _searchParameter;
        private readonly MoleculeMsReference[] _mspDB;
        private readonly string _annotatorID;

        public CalculateMatchScore(DataBaseItem<MoleculeDataBase> mspDB, MsRefSearchParameterBase searchParameter, RetentionType retentionType) {
            _searchParameter = searchParameter;
            RetentionType = retentionType;
            ChromXType type;
            switch (retentionType) {
                case RetentionType.RI:
                    type = ChromXType.RI;
                    break;
                case RetentionType.RT:
                    type = ChromXType.RT;
                    break;
                default:
                    throw new Exception($"Unknown {nameof(RetentionType)}: {retentionType}");
            }
            _mspDB = mspDB?.DataBase.Database.OrderBy(r => r.ChromXs.GetChromByType(type).Value).ToArray();
            _annotatorID = mspDB?.Pairs.FirstOrDefault()?.AnnotatorID;
        }

        private CalculateMatchScore(MoleculeMsReference[] mspDB, MsRefSearchParameterBase searchParameter, RetentionType retentionType, string annotatorID) {
            _searchParameter = searchParameter;
            RetentionType = retentionType;
            _mspDB = mspDB;
            _annotatorID = annotatorID;
        }

        public MsRefSearchParameterBase CopySearchParameter() => new MsRefSearchParameterBase(_searchParameter);

        public RetentionType RetentionType { get; }

        public bool LibraryIsEmpty => _mspDB is null || _mspDB.Length == 0;

        private float Tolerance {
            get {
                float tolerance;
                switch (RetentionType) {
                    case RetentionType.RI:
                        tolerance = _searchParameter.RiTolerance;
                        break;
                    case RetentionType.RT:
                        tolerance = _searchParameter.RtTolerance;
                        break;
                    default:
                        throw new Exception($"Unknown {nameof(RetentionType)}: {RetentionType}");
                }
                var factor = _searchParameter.IsUseTimeForAnnotationFiltering ? 1.0F : 2.0F;
                return tolerance * factor;
            }
        }

        public MoleculeMsReference Reference(MsScanMatchResult result) {
            return _mspDB[result.LibraryIDWhenOrdered];
        }

        public IEnumerable<MsScanMatchResult> CalculateMatches(IMSScanProperty msScan) {
            var rValue = RetentionType == RetentionType.RT ? msScan.ChromXs.RT.Value : msScan.ChromXs.RI.Value;
            var normMSScanProp = DataAccess.GetNormalizedMSScanProperty(msScan, _searchParameter);

            var tolerance = Tolerance;
            var (startID, endID) = RetrieveMspBounds(rValue, tolerance);
            for (int i = startID; i < endID; i++) {
                var refQuery = _mspDB[i];
                var refRetention = RetentionType == RetentionType.RT ? refQuery.ChromXs.RT.Value : refQuery.ChromXs.RI.Value;
                System.Diagnostics.Debug.Assert(Math.Abs(rValue - refRetention) < tolerance);
                if (!_searchParameter.IsUseTimeForAnnotationFiltering || Math.Abs(rValue - refRetention) < tolerance) {
                    var result = MsScanMatching.CompareEIMSScanProperties(normMSScanProp, refQuery, _searchParameter, RetentionType == RetentionType.RI);
                    result.LibraryIDWhenOrdered = i;
                    result.AnnotatorID = _annotatorID;
                    yield return result;
                }
            }
        }

        private (int, int) RetrieveMspBounds(double rValue, double tolerance) {
            switch (RetentionType) {
                case RetentionType.RT: {
                        var startID = SearchCollection.UpperBound(_mspDB, rValue - tolerance, (a, b) => a.ChromXs.RT.Value.CompareTo(b));
                        var endID = SearchCollection.LowerBound(_mspDB, rValue + tolerance, (a, b) => a.ChromXs.RT.Value.CompareTo(b));
                        return (startID, endID);
                    }
                case RetentionType.RI: {
                        var startID = SearchCollection.UpperBound(_mspDB, rValue - tolerance, (a, b) => a.ChromXs.RI.Value.CompareTo(b));
                        var endID = SearchCollection.LowerBound(_mspDB, rValue + tolerance, (a, b) => a.ChromXs.RI.Value.CompareTo(b));
                        return (startID, endID);
                    }
                default:
                    throw new Exception($"Unknown {nameof(RetentionType)}: {RetentionType}");
            }
        }

        public CalculateMatchScore With(MsRefSearchParameterBase searchParameter) {
            return new CalculateMatchScore(_mspDB, searchParameter, RetentionType, _annotatorID);
        }
    }
}
