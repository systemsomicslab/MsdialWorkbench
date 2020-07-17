using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialDimsCore.Common {
    public sealed class AnnotationProcess {
        private AnnotationProcess() { }

        public static void Run(
            ChromatogramPeakFeature feature,
            List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB,
            MsRefSearchParameterBase param, TargetOmics omics,
            out List<MsScanMatchResult> mspResults, out List<MsScanMatchResult> textResults) {

            var mz = feature.PrecursorMz;
            var ms1Tol = param.Ms1Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (mz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mz, ppm);
            }
            #endregion

            mspResults = new List<MsScanMatchResult>(0);
            textResults = new List<MsScanMatchResult>(0);

            if (mspDB != null)
            {
                mspResults = GetMatchResults(feature, mspDB, ms1Tol, param, omics);
                if (mspResults.Count > 0)
                {
                    feature.MSRawID2MspIDs[feature.MS2RawSpectrumID] = mspResults.Select(result => result.LibraryIDWhenOrdered).ToList();

                    var best = mspResults.Select((result, index) => (result.TotalScore, -index, result)).Max().result;

                    feature.MSRawID2MspBasedMatchResult[feature.MS2RawSpectrumID] = best;
                    DataAccess.SetMoleculeMsProperty(feature, mspDB[best.LibraryIDWhenOrdered], best);
                }
            }

            if (textDB != null)
            {
                textResults = GetMatchResults(feature, textDB, ms1Tol, param, omics);
                feature.TextDbIDs = textResults.Select(result => result.LibraryIDWhenOrdered).ToList();
                if (textResults.Count > 0)
                {
                    var best = textResults.Select((result, index) => (result.TotalScore, -index, result)).Max().result;
                    feature.TextDbBasedMatchResult = best;
                    DataAccess.SetMoleculeMsProperty(feature, textDB[best.LibraryIDWhenOrdered], best, true);
                }
            }
        }

        private static List<MsScanMatchResult> GetMatchResults(
            ChromatogramPeakFeature chromPeak, List<MoleculeMsReference> db,
            double tolerance, MsRefSearchParameterBase param, TargetOmics omics)
        {
            var mz = chromPeak.Mass;
            var startID = SearchCollection.LowerBound(
                db, new MoleculeMsReference() { PrecursorMz = mz - tolerance },
                (a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));
            var candidates = new List<MsScanMatchResult>();
            for (int i = startID; i < db.Count; i++)
            {
                var refSpec = db[i];
                if (refSpec.PrecursorMz < mz - tolerance) continue;
                if (refSpec.PrecursorMz > mz + tolerance) break;

                MsScanMatchResult result = null;
                if (omics == TargetOmics.Lipidomics) {
                    result = MsScanMatching.CompareMS2LipidomicsScanProperties(chromPeak, refSpec, param);
                }
                else if (omics == TargetOmics.Metablomics) {
                    result = MsScanMatching.CompareMS2ScanProperties(chromPeak, refSpec, param);
                }
                if (result != null && (result.IsPrecursorMzMatch || result.IsSpectrumMatch)) {
                    result.LibraryIDWhenOrdered = i;
                    candidates.Add(result);
                }
            }

            return candidates;
        }

        public static List<(MoleculeMsReference reference, double ratio)> RunMultiAlignment(
            ChromatogramPeakFeature chromatogram, List<MoleculeMsReference> MspDB,
            MsRefSearchParameterBase param, double threshold = .01)
        {
            if (chromatogram.MSRawID2MspIDs.IsEmptyOrNull())
                return new List<(MoleculeMsReference reference, double ratio)>();

            var mspIDs = chromatogram.MSRawID2MspIDs[chromatogram.MS2RawSpectrumID];

            if (mspIDs.IsEmptyOrNull())
                return new List<(MoleculeMsReference reference, double ratio)>();

            var refs = mspIDs.Select(id => MspDB[id]);
            var results = CalcAbundanceRatio(chromatogram, refs, param.Ms2Tolerance);

            return results.Where(reference => reference.ratio > threshold).ToList();
        }

        private static List<(MoleculeMsReference reference, double ratio)> CalcAbundanceRatio(
            ChromatogramPeakFeature chromatogram, IEnumerable<MoleculeMsReference> references, double msTolerance)
        {
            (double[] measureBin, double[,] referenceBins) = GetSpectrumBin(chromatogram.Spectrum, references.Select(reference => reference.Spectrum), msTolerance);

            if (measureBin == null || referenceBins == null) return new List<(MoleculeMsReference, double)>();

            var calculator = new AbundanceRatioCalculator(measureBin, referenceBins);
            (double[] result, bool success) = calculator.Calculate();

            if (!success) return new List<(MoleculeMsReference, double)>();

            return references.Zip(result, (reference, ratio) => (reference, ratio)).ToList();
        }

        private static (double[], double[,]) GetSpectrumBin(
            List<SpectrumPeak> measure, IEnumerable<List<SpectrumPeak>> references, double msTolerance)
        {
            var n = references.Count();

            if (n == 0) return (null, null);

            var peaks = references.Select((reference, index) => reference.Select(peak => (Id: index + 1, peak)))
                                  .Concat(new List<IEnumerable<(int Id, SpectrumPeak peak)>> { measure.Select(peak => (Id: 0, peak)) })
                                  .SelectMany(e => e).Where(e => !double.IsNaN(e.peak.Intensity)).ToList();
            peaks.Sort((u, v) => u.peak.Mass.CompareTo(v.peak.Mass));

            List<double[]> bins = new List<double[]>();
            int k = 0;
            while (k < peaks.Count)
            {
                var bin = new double[n + 1];
                var mass = peaks[k].peak.Mass + msTolerance;

                while (k < peaks.Count && peaks[k].peak.Mass < mass)
                {
                    bin[peaks[k].Id] += peaks[k].peak.Intensity;
                    ++k;
                }

                bins.Add(bin);
            }

            var mmax = bins.Max(bin => bin[0]);
            if (mmax == 0d) mmax = 1d; 
            var rmax = bins.Max(bin => bin.Skip(1).Max());
            if (rmax == 0d) rmax = 1d; 

            var mresult = bins.Select(bin => bin[0] / mmax).ToArray();
            var rresults = new double[bins.Count, n];
            for (int i = 0; i < bins.Count; i++)
                for (int j = 0; j < n; j++)
                    rresults[i, j] = bins[i][j + 1] / rmax;

            return (mresult, rresults);
        }
    }
}
