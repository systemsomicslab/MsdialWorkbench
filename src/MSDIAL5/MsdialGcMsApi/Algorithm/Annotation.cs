using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialGcMsApi.Algorithm
{
    public class Annotation {
        private readonly MsdialGcmsParameter _parameter;
        private readonly CalculateMatchScore _calculateMatchScore;
        private readonly int _numThreads;

        /// <param name="numThreads">
        /// The degree of parallelism of this file's annotation, at least 1; see
        /// FileProcess.AnnotationThreadsPerFile. Each peak's matches are written to its own index, so
        /// the results do not depend on it.
        /// </param>
        public Annotation(CalculateMatchScore calculateMatchScore, MsdialGcmsParameter parameter, int numThreads) {
            _calculateMatchScore = calculateMatchScore;
            _parameter = parameter;
            _numThreads = numThreads;
        }

        public Annotation(DataBaseItem<MoleculeDataBase> mspDB, MsdialGcmsParameter parameter, int numThreads)
            : this(new CalculateMatchScore(mspDB, parameter.MspSearchParam, parameter.RetentionType, parameter.RiCompoundType), parameter, numThreads) {

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

            if (_calculateMatchScore != null && !_calculateMatchScore.LibraryIsEmpty) {
                var containers = new MsScanMatchResultContainer[ms1DecResults.Count];
                var counter = 0;
                Parallel.For(0, ms1DecResults.Count, new ParallelOptions { MaxDegreeOfParallelism = _numThreads }, index => {
                    var results = containers[index] = new MsScanMatchResultContainer();
                    // Materialised, unlike every other site: CalculateMatches is a yield-return
                    // iterator over the retention window of the EI library, so the population size
                    // is not otherwise knowable here.
                    //
                    // The cost, stated plainly because it is not free: the original chain buffered
                    // only the IsSpectrumMatch survivors, since OrderByDescending sat downstream of
                    // the Where. This buffers every reference in the retention window instead, so
                    // peak memory grows with window size times the degree of parallelism of this
                    // Parallel.For -- and the window is doubled when IsUseTimeForAnnotationFiltering
                    // is off (see Tolerance in CalculateMatchScore), which is the configuration
                    // where the largest EI libraries are searched. Values are identical either way.
                    //
                    // The cap here is 5, not NUMBER_OF_ANNOTATION_RESULTS.
                    var scored = _calculateMatchScore.CalculateMatches(ms1DecResults[index]).ToList();
                    // This path uses no evaluator; IsSpectrumMatch is the filter that stands in for
                    // FilterByThreshold, so it is the line CandidatesAboveThreshold is drawn at.
                    var named = scored.Where(result => result.IsSpectrumMatch).ToList();
                    // No evaluator on this path, so the stored boolean is the run's own definition here --
                    // CompareEIMSScanProperties sets it from IsSpectrumMatch, TotalScore and the
                    // retention verdict, and nothing reinterprets it afterwards.
                    var population = CandidatePopulation.Of(scored, named, result => result.IsReferenceMatched);
                    results.AddResults(population.RecordOnAll(named.OrderByDescending(r => r.TotalScore).Take(5)));
                    System.Diagnostics.Debug.WriteLine("Done {0}/{1}", index, ms1DecResults.Count);
                    reporter.Report(Interlocked.Increment(ref counter), ms1DecResults.Count);
                });

                var features = new AnnotatedMSDecResult[ms1DecResults.Count];
                if (_parameter.OnlyReportTopHitInMspSearch) {
                    var used = new HashSet<MoleculeMsReference>(new MoleculeMsReferenceEqualityComparer());
                    foreach (var (container, i) in containers.WithIndex().OrderByDescending(p => p.Item1.Representative.TotalScore)) {
                        if (container.Representative is MsScanMatchResult topHit && !topHit.IsUnknown) {
                            ms1DecResults[i].MspIDs.AddRange(containers[i].MatchResults.OrderByDescending(r => r.TotalScore).Select(r => r.LibraryID));
                            var best = containers[i].Representative;
                            ms1DecResults[i].MspBasedMatchResult = best;
                            ms1DecResults[i].MspID = best.LibraryID;
                            ms1DecResults[i].MspIDWhenOrdered = best.LibraryIDWhenOrdered;

                            var reference = _calculateMatchScore.Reference(container.Representative);
                            if (used.Contains(reference)) {
                                features[i] = new AnnotatedMSDecResult(ms1DecResults[i], container, reference.AsPutative(), QuantMassOf(reference, ms1DecResults[i]));
                            }
                            else {
                                features[i] = new AnnotatedMSDecResult(ms1DecResults[i], container, reference, QuantMassOf(reference, ms1DecResults[i]));
                                used.Add(reference);
                            }
                        }
                        else {
                            features[i] = new AnnotatedMSDecResult(ms1DecResults[i], container);
                        }
                    }
                }
                else {
                    for (int i = 0; i < ms1DecResults.Count; i++) {
                        var results = containers[i];
                        if (results.Representative is MsScanMatchResult topHit && !topHit.IsUnknown) {
                            var reference = _calculateMatchScore.Reference(topHit);
                            features[i] = new AnnotatedMSDecResult(ms1DecResults[i], results, reference, QuantMassOf(reference, ms1DecResults[i]));

                            MsScanMatchResult[] msScanMatchResults = containers[i].MatchResults.OrderByDescending(r => r.TotalScore).ToArray();
                            ms1DecResults[i].MspIDs.AddRange(msScanMatchResults.Select(r => r.LibraryID));
                            var best = msScanMatchResults.First();
                            ms1DecResults[i].MspBasedMatchResult = best;
                            ms1DecResults[i].MspID = best.LibraryID;
                            ms1DecResults[i].MspIDWhenOrdered = best.LibraryIDWhenOrdered;
                        }
                        else {
                            features[i] = new AnnotatedMSDecResult(ms1DecResults[i], results);
                        }
                    }
                }
                return features;
            }

            return ms1DecResults.Select(r => new AnnotatedMSDecResult(r, new MsScanMatchResultContainer())).ToArray();
        }

        /// <summary>
        /// The m/z this file's annotated peak is quantified at: the library QUANTMASS only when
        /// "Replace quant mass by user defined value" is on and the reference defines a positive one,
        /// the deconvoluted model peak m/z otherwise. One rule for both the top-hit and the
        /// every-hit path. The MSP parser stores -1 for a QUANTMASS it cannot read (for example a
        /// line run into the next field), which is not an m/z.
        /// </summary>
        private double QuantMassOf(MoleculeMsReference reference, MSDecResult result) {
            return _parameter.IsReplaceQuantmassByUserDefinedValue && reference.QuantMass > 0 ? reference.QuantMass : result.ModelPeakMz;
        }
    }

    public class MoleculeMsReferenceEqualityComparer : IEqualityComparer<MoleculeMsReference> {
        public bool Equals(MoleculeMsReference x, MoleculeMsReference y) {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            if (!string.IsNullOrEmpty(x.InChIKey) && !string.IsNullOrEmpty(y.InChIKey)) {
                if (x.InChIKey == y.InChIKey) return true;
            }

            if (x.ScanID != 0 && y.ScanID != 0 && x.ScanID == y.ScanID) {
                return true;
            }

            if (!string.IsNullOrEmpty(x.Name) && !string.IsNullOrEmpty(y.Name)) {
                if (x.Name.ToLower() == y.Name.ToLower()) return true;
            }

            return false;
        }

        public int GetHashCode(MoleculeMsReference obj) {
            if (obj is null) return 0;

            if (!string.IsNullOrEmpty(obj.InChIKey)) {
                return obj.InChIKey.GetHashCode();
            }

            if (obj.ScanID != 0) {
                return obj.ScanID.GetHashCode();
            }

            if (!string.IsNullOrEmpty(obj.Name)) {
                return obj.Name.ToLower().GetHashCode();
            }

            return 0;
        }
    }
}
