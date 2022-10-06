using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm.Alignment
{
    public class DimsPeakJoiner : IPeakJoiner
    {
        private static readonly IComparer<IMSScanProperty> Comparer;

        private readonly double _mztol, _mzfactor;

        static DimsPeakJoiner() {
            Comparer = MassComparer.Comparer;
        }

        public DimsPeakJoiner(double mztol, double mzfactor) {
            _mztol = mztol;
            _mzfactor = mzfactor;
        }

        public DimsPeakJoiner(double mztol) : this(mztol, 1) { }

        public List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {
            var master = GetMasterList(analysisFiles, referenceId, accessor);
            var spots = JoinAll(master, analysisFiles, accessor);
            return spots;
        }

        private List<IMSScanProperty> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
            if (referenceFile == null) return new List<IMSScanProperty>();

            // prepare master list and add sentinel
            var master = new LinkedList<IMSScanProperty>(
                new IMSScanProperty[] {
                    new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = double.MaxValue }),
            });

            var reference = accessor.GetMSScanProperties(referenceFile);
            reference.Sort(Comparer);
            MergeChromatogramPeaks(master, reference);

            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId)
                    continue;
                var target = accessor.GetMSScanProperties(analysisFile);
                target.Sort(Comparer);
                MergeChromatogramPeaks(master, target);
            }

            // remove sentinel
            master.RemoveLast();
            return master.ToList();
        }

        private void MergeChromatogramPeaks(LinkedList<IMSScanProperty> masters, IEnumerable<IMSScanProperty> targets) {
            var itr = masters.First;
            foreach (var target in targets) {
                while (Comparer.Compare(itr.Value, target) < 0 && !IsSimilarTo(itr.Value, target)) {
                    itr = itr.Next;
                }
                if (!IsSimilarTo(itr.Value, target)) {
                    itr = masters.AddBefore(itr, target);
                }
            }
        }

        private bool IsSimilarTo(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol;
        }

        private List<AlignmentSpotProperty> JoinAll(List<IMSScanProperty> master, IReadOnlyList<AnalysisFileBean> analysisFiles, DataAccessor accessor) {
            var result = GetSpots(master, analysisFiles);
            
            foreach (var analysisFile in analysisFiles) {
                var props = accessor.GetMSScanProperties(analysisFile);
                props.Sort(Comparer);
                AlignPeaksToMaster(result, master, props, analysisFile.AnalysisFileId);
            }
            
            return result;
        }

        private static List<AlignmentSpotProperty> GetSpots(IReadOnlyCollection<IMSScanProperty> masters, IEnumerable<AnalysisFileBean> analysisFiles) {
            var masterId = 0;
            return InitSpots(masters, analysisFiles, ref masterId);
        }

        private static List<AlignmentSpotProperty> InitSpots(IEnumerable<IMSScanProperty> scanProps,
            IEnumerable<AnalysisFileBean> analysisFiles, ref int masterId, int parentId = -1) {

            if (scanProps == null) return new List<AlignmentSpotProperty>();

            var spots = new List<AlignmentSpotProperty>();
            foreach ((var scanProp, var localId) in scanProps.WithIndex()) {
                var spot = new AlignmentSpotProperty
                {
                    MasterAlignmentID = masterId++,
                    AlignmentID = localId,
                    ParentAlignmentID = parentId,
                    TimesCenter = scanProp.ChromXs,
                    MassCenter = scanProp.PrecursorMz,
                    IonMode = scanProp.IonMode,
                };
                spot.InternalStandardAlignmentID = -1;

                var peaks = new List<AlignmentChromPeakFeature>();
                foreach (var file in analysisFiles) {
                    peaks.Add(new AlignmentChromPeakFeature
                    {
                        MasterPeakID = -1,
                        PeakID = -1,
                        FileID = file.AnalysisFileId,
                        FileName = file.AnalysisFileName,
                        IonMode = scanProp.IonMode,
                    });
                }
                spot.AlignedPeakProperties = peaks;

                if (scanProp is ChromatogramPeakFeature chrom)
                    spot.AlignmentDriftSpotFeatures = InitSpots(chrom.DriftChromFeatures, analysisFiles, ref masterId, spot.AlignmentID);

                spots.Add(spot);
            }

            return spots;
        }

        private void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
            var results = new List<Tuple<double, int, int, IMSScanProperty>>();
            var dummy = 0;

            var lo = 0;
            foreach (var target in targets) {
                while (Comparer.Compare(masters[lo], target) < 0 && !IsSimilarTo(masters[lo], target)) {
                    lo++;
                }
                for (var itr = lo; itr < masters.Count; itr++) {
                    var master = masters[itr];
                    if (!IsSimilarTo(target, master)) {
                        break;
                    }
                    results.Add(Tuple.Create(GetSimilality(target, master), itr, dummy++, target));
                }
            }
            results.Sort();

            var used = new HashSet<IMSScanProperty>();
            var inserted = new bool[spots.Count];
            foreach (var result in results) {
                if (!inserted[result.Item2] && !used.Contains(result.Item4)) {
                    DataObjConverter.SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(spots[result.Item2].AlignedPeakProperties[fileId], result.Item4 as ChromatogramPeakFeature);
                    inserted[result.Item2] = true;
                    used.Add(result.Item4);
                }
            }
        }

        private double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return _mzfactor * Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2));
        }
    }
}
