using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class MassMatchCalculator : IMatchScoreCalculator<IMassMatchQuery, IMSProperty, MassMatchResult>
    {
        public MassMatchResult Calculate(IMassMatchQuery query, IMSProperty reference) {
            var similarity = MsScanMatching.GetGaussianSimilarity(query.Mz, reference.PrecursorMz, query.MzTolerance);
            var isPrecursorMzMatch = Math.Abs(query.Mz - reference.PrecursorMz) <= query.MzTolerance;
            return new MassMatchResult(similarity, isPrecursorMzMatch, massCompared: query.Mz > 0d && reference.PrecursorMz > 0d);
        }
    }

    public interface IMassMatchQuery
    {
        double Mz { get; }
        double MzTolerance { get; }
    }

    public sealed class MassMatchQuery : IMassMatchQuery
    {
        public MassMatchQuery(double mz, double mzTolerance) {
            Mz = mz;
            MzTolerance = mzTolerance;
        }

        public double Mz { get; }
        public double MzTolerance { get; }
    }

    public interface IMassMatchResult : IMatchResult
    {
        double AcurateMassSimilarity { get; }
        bool IsPrecursorMzMatch { get; }
    }

    public sealed class MassMatchResult : IMassMatchResult
    {
        public MassMatchResult(double acurateMassSimilarity, bool isPrecursorMzMatch, bool massCompared) {
            AcurateMassSimilarity = acurateMassSimilarity;
            IsPrecursorMzMatch = isPrecursorMzMatch;
            MassCompared = massCompared;
        }

        public double AcurateMassSimilarity { get; }
        public bool IsPrecursorMzMatch { get; }

        /// <summary>
        /// True when both masses were present, so <see cref="AcurateMassSimilarity"/> holds a
        /// measurement.
        /// </summary>
        /// <remarks>
        /// Carried explicitly because the calculator uses the unguarded GetGaussianSimilarity
        /// overload, which returns exp(-0.5 * ((actual - reference) / tolerance)^2) whether or not
        /// either mass exists. The returned value therefore has no sentinel to test.
        /// </remarks>
        public bool MassCompared { get; }

        public IEnumerable<double> Scores => new[] { AcurateMassSimilarity, };

        public void Assign(MsScanMatchResult result) {
            result.AcurateMassSimilarity = (float)AcurateMassSimilarity;
            result.IsPrecursorMzMatch = IsPrecursorMzMatch;
            if (MassCompared) {
                result.MeasuredTerms |= MeasuredTerms.AccurateMass;
            }
        }
    }
}
