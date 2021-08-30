using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class IsotopesMatchCalculator : IMatchScoreCalculator<IIsotopesMatchQuery, MoleculeMsReference, IsotopesMatchResult>
    {
        public IsotopesMatchResult Calculate(IIsotopesMatchQuery query, MoleculeMsReference reference) {
            return new IsotopesMatchResult(MsScanMatching.GetIsotopeRatioSimilarity(query.Isotopes, reference.IsotopicPeaks, query.Mz, query.MzTolerance));
        }
    }

    public interface IIsotopesMatchQuery
    {
        IReadOnlyList<IsotopicPeak> Isotopes { get; }
        double Mz { get; }
        double MzTolerance { get; }
    }

    public sealed class IsotopesMatchQuery : IIsotopesMatchQuery
    {
        public IReadOnlyList<IsotopicPeak> Isotopes { get; }
        public double Mz { get; }
        public double MzTolerance { get; }

        public IsotopesMatchQuery(IReadOnlyList<IsotopicPeak> isotopes, double mz, double mzTolerance) {
            Isotopes = isotopes;
            Mz = mz;
            MzTolerance = mzTolerance;
        }
    }

    public interface IIsotopesMatchResult : IMatchResult
    {
        double IsotopeSimilarity { get; }
    }

    public sealed class IsotopesMatchResult : IIsotopesMatchResult
    {
        public double IsotopeSimilarity { get; }
        public IEnumerable<double> Scores => new[] { IsotopeSimilarity, };

        public IsotopesMatchResult(double isotopesSimilarity) {
            IsotopeSimilarity = isotopesSimilarity;
        }

        public void Assign(MsScanMatchResult result) {
            result.IsotopeSimilarity = (float)IsotopeSimilarity;
        }
    }
}
