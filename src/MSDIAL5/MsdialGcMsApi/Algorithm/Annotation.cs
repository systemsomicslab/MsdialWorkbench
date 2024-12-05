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

        public Annotation(CalculateMatchScore calculateMatchScore, MsdialGcmsParameter parameter) {
            _calculateMatchScore = calculateMatchScore;
            _parameter = parameter;
        }

        public Annotation(DataBaseItem<MoleculeDataBase> mspDB, MsdialGcmsParameter parameter)
            : this(new CalculateMatchScore(mspDB, parameter.MspSearchParam, parameter.RetentionType), parameter) {

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
                Parallel.For(0, ms1DecResults.Count, index => {
                    var results = containers[index] = new MsScanMatchResultContainer();
                    results.AddResults(_calculateMatchScore.CalculateMatches(ms1DecResults[index]).Where(result => result.IsSpectrumMatch).OrderByDescending(r => r.TotalScore).Take(5));
                    System.Diagnostics.Debug.WriteLine("Done {0}/{1}", index, ms1DecResults.Count);
                    reporter.Report(Interlocked.Increment(ref counter), ms1DecResults.Count);
                });

                var features = new AnnotatedMSDecResult[ms1DecResults.Count];
                if (_parameter.OnlyReportTopHitInMspSearch) {
                    var used = new HashSet<MoleculeMsReference>();
                    foreach (var (container, i) in containers.WithIndex().OrderByDescending(p => p.Item1.Representative.TotalScore)) {
                        if (container.Representative is MsScanMatchResult topHit && !topHit.IsUnknown) {
                            ms1DecResults[i].MspIDs.AddRange(containers[i].MatchResults.OrderByDescending(r => r.TotalScore).Select(r => r.LibraryID));
                            var best = containers[i].Representative;
                            ms1DecResults[i].MspBasedMatchResult = best;
                            ms1DecResults[i].MspID = best.LibraryID;
                            ms1DecResults[i].MspIDWhenOrdered = best.LibraryIDWhenOrdered;

                            var reference = _calculateMatchScore.Reference(container.Representative);
                            if (used.Contains(reference)) {
                                features[i] = new AnnotatedMSDecResult(ms1DecResults[i], container, reference.AsPutative(), _parameter.IsReplaceQuantmassByUserDefinedValue && reference.QuantMass != 0 ? reference.QuantMass : ms1DecResults[i].ModelPeakMz);
                            }
                            else {
                                features[i] = new AnnotatedMSDecResult(ms1DecResults[i], container, reference, _parameter.IsReplaceQuantmassByUserDefinedValue && reference.QuantMass != 0 ? reference.QuantMass : ms1DecResults[i].ModelPeakMz);
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
                            features[i] = new AnnotatedMSDecResult(ms1DecResults[i], results, _calculateMatchScore.Reference(topHit));

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
    }
}
