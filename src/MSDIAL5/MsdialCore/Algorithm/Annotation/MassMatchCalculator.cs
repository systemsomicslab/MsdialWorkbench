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
            return new MassMatchResult(similarity, isPrecursorMzMatch);
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
        public MassMatchResult(double acurateMassSimilarity, bool isPrecursorMzMatch) {
            AcurateMassSimilarity = acurateMassSimilarity;
            IsPrecursorMzMatch = isPrecursorMzMatch;
        }

        public double AcurateMassSimilarity { get; }
        public bool IsPrecursorMzMatch { get; }

        public IEnumerable<double> Scores => new[] { AcurateMassSimilarity, };

        public void Assign(MsScanMatchResult result) {
            result.AcurateMassSimilarity = (float)AcurateMassSimilarity;
            result.IsPrecursorMzMatch = IsPrecursorMzMatch;
        }
    }
}
