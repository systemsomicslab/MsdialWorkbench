using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parameter;
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

        public static List<MsScanMatchResult> Run(ChromatogramPeakFeature feature, List<MoleculeMsReference> mspDB, MsRefSearchParameterBase param, TargetOmics omics) {
            var mz = feature.PrecursorMz;
            var ms1Tol = param.Ms1Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (mz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(mz, ppm);
            }
            #endregion

            var results = new List<MsScanMatchResult>();

            var startIndex = DataAccess.GetDatabaseStartIndex(mz, ms1Tol, mspDB);
            for (int i = startIndex; i < mspDB.Count; i++) {
                var query = mspDB[i];
                if (query.PrecursorMz > mz + ms1Tol) break;
                if (query.PrecursorMz < mz - ms1Tol) continue;

                MsScanMatchResult result = null;
                if (omics == TargetOmics.Lipidomics) {
                    result = MsScanMatching.CompareMS2LipidomicsScanProperties(feature, query, param);
                }
                else {
                    result = MsScanMatching.CompareMS2ScanProperties(feature, query, param);
                }
                if (result.IsSpectrumMatch) {
                    feature.MspIDs.Add(i);
                }
                else {
                    continue;
                }

                //temp method
                var totalscore = result.SimpleDotProduct + result.WeightedDotProduct + result.MatchedPeaksPercentage + result.ReverseDotProduct;
                result.TotalScore = totalscore;
                results.Add(result);

                if (feature.MspBasedMatchResult.TotalScore < totalscore) {
                    feature.MspID = i;
                    feature.MspBasedMatchResult = result;
                }
            }

            if (feature.MspBasedMatchResult.LibraryID >= 0) {
                var mspID = feature.MspID;
                var refQuery = mspDB[mspID];
                feature.Name = refQuery.Name;
                feature.Formula = refQuery.Formula;
                feature.Ontology = refQuery.Ontology;
                feature.SMILES = refQuery.SMILES;
                feature.InChIKey = refQuery.InChIKey;
            }
            else {
                feature.Name = "Unknown";
            }

            return results;
        }

        public static List<(MoleculeMsReference reference, double ratio)> RunMultiAlignment(
            ChromatogramPeakFeature chromatogram, List<MoleculeMsReference> MspDB,
            MsRefSearchParameterBase param, double threshold = .01)
        {
            var refs = chromatogram.MspIDs.Select(id => MspDB[id]);

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
