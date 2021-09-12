using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialDimsCore.Common {
    public sealed class AnnotationProcess {
        private AnnotationProcess() { }

        async public static Task<List<MsScanMatchResult>> RunMspAnnotationAsync(
            double precursorMz, MSDecResult msdecResult,
            List<MoleculeMsReference> mspDB, MsRefSearchParameterBase param,
            TargetOmics omics, IReadOnlyList<IsotopicPeak> isotopes,
            double ms1Tol
            ) {
            if (mspDB == null)
                return new List<MsScanMatchResult>();

            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (precursorMz > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(precursorMz, ppm);
            }
            #endregion

            Func<MoleculeMsReference, MsScanMatchResult> getMatchResult = refSpec => MsScanMatching.CompareMS2ScanProperties(msdecResult, refSpec, param, omics, -1.0, isotopes, refSpec.IsotopicPeaks);
            //if (omics == TargetOmics.Lipidomics)
            //    getMatchResult = refSpec => MsScanMatching.CompareMS2LipidomicsScanProperties(msdecResult, refSpec, param, isotopes, refSpec.IsotopicPeaks);
            //else if (omics == TargetOmics.Metabolomics)
            //    getMatchResult = refSpec => MsScanMatching.CompareMS2ScanProperties(msdecResult, refSpec, param, isotopes, refSpec.IsotopicPeaks);

            return await Task.Run(() => GetMatchResults(mspDB, precursorMz, ms1Tol, getMatchResult));
        }

        public static void Run(
            ChromatogramPeakFeature feature, MSDecResult msdecResult,
            List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB,
            MsRefSearchParameterBase mspParam, MsRefSearchParameterBase textParam,
            TargetOmics omics, IReadOnlyList<IsotopicPeak> isotopes
            ) {
            var mspAnnotator = new DimsMspAnnotator(new MoleculeDataBase(mspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB), mspParam, omics, "MspDB", -1);
            var textAnnotator = new MassAnnotator(new MoleculeDataBase(textDB, "TextDB", DataBaseSource.Text, SourceType.TextDB), textParam, omics, SourceType.TextDB, "TextDB", -1);
            Run(feature, msdecResult, mspAnnotator, textAnnotator, mspParam, textParam, isotopes);
        }

        public static void Run(
            ChromatogramPeakFeature feature, MSDecResult msdecResult,
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> mspAnnotator,
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> textAnnotator,
            MsRefSearchParameterBase mspParam,
            MsRefSearchParameterBase textParam,
            IReadOnlyList<IsotopicPeak> isotopes) {

            if (mspAnnotator != null)
            {
                var candidates = mspAnnotator.FindCandidates(new AnnotationQuery(feature, msdecResult, isotopes, mspParam));
                var results = mspAnnotator.FilterByThreshold(candidates, mspParam);
                feature.MSRawID2MspIDs[msdecResult.RawSpectrumID] = results.Select(result => result.LibraryIDWhenOrdered).ToList();
                var matches = mspAnnotator.SelectReferenceMatchResults(results, mspParam);
                if (matches.Count > 0) {
                    var best = matches.Argmax(result => result.TotalScore);
                    feature.MSRawID2MspBasedMatchResult[msdecResult.RawSpectrumID] = best;
                    feature.MatchResults.AddMspResult(msdecResult.RawSpectrumID, best);
                    DataAccess.SetMoleculeMsProperty(feature, mspAnnotator.Refer(best), best);
                }
                else if (results.Count > 0)
                {
                    var best = results.Argmax(result => result.TotalScore);
                    feature.MSRawID2MspBasedMatchResult[msdecResult.RawSpectrumID] = best;
                    feature.MatchResults.AddMspResult(msdecResult.RawSpectrumID, best);
                    DataAccess.SetMoleculeMsPropertyAsSuggested(feature, mspAnnotator.Refer(best), best);
                }
            }

            if (textAnnotator != null)
            {
                var candidates = textAnnotator.FindCandidates(new AnnotationQuery(feature, msdecResult, isotopes, textParam));
                var results = textAnnotator.FilterByThreshold(candidates, textParam);
                feature.TextDbIDs = results.Select(result => result.LibraryIDWhenOrdered).ToList();
                var matches = textAnnotator.SelectReferenceMatchResults(results, textParam);
                foreach (var result in results) {
                    feature.MatchResults.AddTextDbResult(result);
                }
                if (results.Count > 0)
                {
                    var best = results.Argmax(result => result.TotalScore);
                    feature.TextDbBasedMatchResult = best;
                    DataAccess.SetTextDBMoleculeMsProperty(feature, textAnnotator.Refer(best), best);
                }
            }
        }

        private static List<MsScanMatchResult> GetMatchResults(
            List<MoleculeMsReference> db, double mz, double tolerance, 
            Func<MoleculeMsReference, MsScanMatchResult> func)
        {
            if (func == null) return new List<MsScanMatchResult>(0);
            var startID = SearchCollection.LowerBound(
                db, new MoleculeMsReference() { PrecursorMz = mz - tolerance },
                (a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));
            var candidates = new List<MsScanMatchResult>();
            for (int i = startID; i < db.Count; i++)
            {
                var refSpec = db[i];
                if (refSpec.PrecursorMz < mz - tolerance) continue;
                if (refSpec.PrecursorMz > mz + tolerance) break;

                MsScanMatchResult result = func(refSpec);
                if (result != null && (result.IsPrecursorMzMatch || result.IsSpectrumMatch)) {
                    result.LibraryID = i;
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
