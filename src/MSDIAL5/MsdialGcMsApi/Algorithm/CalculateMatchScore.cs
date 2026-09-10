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
        // Required rather than defaulted: the two retention-index scales differ by a factor of
        // about 390, so a match cap chosen for one is meaningless on the other, and a new call
        // site should have to say which it is.
        private readonly RiCompoundType _riCompoundType;

        public CalculateMatchScore(DataBaseItem<MoleculeDataBase> mspDB, MsRefSearchParameterBase searchParameter, RetentionType retentionType, RiCompoundType riCompoundType) {
            _searchParameter = searchParameter;
            RetentionType = retentionType;
            _riCompoundType = riCompoundType;
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

        private CalculateMatchScore(MoleculeMsReference[] mspDB, MsRefSearchParameterBase searchParameter, RetentionType retentionType, string annotatorID, RiCompoundType riCompoundType) {
            _searchParameter = searchParameter;
            RetentionType = retentionType;
            _mspDB = mspDB;
            _annotatorID = annotatorID;
            _riCompoundType = riCompoundType;
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

        /// <summary>
        /// The widest retention difference that may be called a match on the axis this run uses.
        /// Deliberately not the same number as <see cref="Tolerance"/>: that one is the search
        /// window, and it is doubled when retention filtering is off.
        /// </summary>
        private double RetentionMatchTolerance {
            get {
                switch (RetentionType) {
                    case RetentionType.RI:
                        return RetentionMatchPolicy.EffectiveRetentionIndexTolerance(_searchParameter.RiTolerance, _riCompoundType);
                    case RetentionType.RT:
                        return RetentionMatchPolicy.EffectiveRetentionTimeTolerance(_searchParameter.RtTolerance);
                    default:
                        throw new Exception($"Unknown {nameof(RetentionType)}: {RetentionType}");
                }
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
                    var result = MsScanMatching.CompareEIMSScanProperties(normMSScanProp, refQuery, _searchParameter, RetentionType == RetentionType.RI, RetentionMatchTolerance);
                    result.LibraryIDWhenOrdered = i;
                    result.AnnotatorID = _annotatorID;
                    // Recorded here and not inside CompareEIMSScanProperties: that function is also
                    // how GcmsPeakJoiner compares two SAMPLE spectra to each other during alignment,
                    // where there is no reference at all and "a reference spectrum was compared"
                    // would be false. This is the caller that knows -- an EI entry from an acquired
                    // MSP library -- and it is the single funnel for the whole GC-MS mode.
                    //
                    // This value does survive a GC-MS session. The primary store is MessagePack:
                    // the container reaches SpectrumFeatureCollection through AnnotatedMSDecResult,
                    // whose hand-written formatter serialises MsScanMatchResultContainer with the
                    // standard resolver, so every [Key] member is kept. That is the copy the
                    // exporter reads (AnnotatedMSDecResult.MatchResults.Representative, in
                    // GcmsAnalysisMetadataAccessor) and the copy alignment merges in
                    // DataObjConverter.
                    //
                    // The .dcl block is a second, lossy copy of the same annotation: it carries
                    // only numeric scores, IDs and booleans, so MSDecResult.MspBasedMatchResult and
                    // the legacy MSRawID2MspBasedMatchResult dictionary built from it come back
                    // with no evidence. Pinned by ADclRoundTripDiscardsTheEvidenceSource. Do not
                    // try to extend that layout: its record size comes from a hand-written member
                    // list with no version gate, so one more field makes every existing .dcl
                    // unreadable.
                    result.EvidenceSource = AnnotationEvidence.WhenSpectrumCompared(
                        result.MeasuredTerms, AnnotationEvidenceSource.ReferenceSpectrum);
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
            return new CalculateMatchScore(_mspDB, searchParameter, RetentionType, _annotatorID, _riCompoundType);
        }
    }
}
