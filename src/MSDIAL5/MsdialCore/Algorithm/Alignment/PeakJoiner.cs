using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public abstract class PeakJoiner : IPeakJoiner
    {
        protected abstract bool IsSimilarTo(IMSScanProperty x, IMSScanProperty y);
        protected abstract double GetSimilality(IMSScanProperty x, IMSScanProperty y);

        public List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

            var master = GetMasterList(analysisFiles, referenceId, accessor);
            var spots = JoinAll(master, analysisFiles, accessor);
            return spots;
        }

        public virtual List<IMSScanProperty> MergeChromatogramPeaks(List<IMSScanProperty> masters, List<IMSScanProperty> targets) {
            var merged = new List<IMSScanProperty>(masters);
            foreach (var target in targets) {
                if (!merged.Any(m => IsSimilarTo(m, target))) {
                    merged.Add(target);
                }
            }
            return merged;
        }
        public virtual void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
            var n = masters.Count;
            var maxMatchs = new double[n];

            foreach (var target in targets) {
                // TODO: check tolerance
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                for (var i = 0; i < n; i++) {
                    if (!IsSimilarTo(masters[i], target))
                        continue;
                    var factor = GetSimilality(masters[i], target);
                    if (factor > maxMatchs[i] && factor > matchFactor) {
                        matchIdx = i;
                        maxMatchs[i] = matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue)
                    DataObjConverter.SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(spots[matchIdx.Value].AlignedPeakProperties[fileId], target as ChromatogramPeakFeature);
            }
        }

        public virtual List<AlignmentSpotProperty> JoinAll(List<IMSScanProperty> master, IReadOnlyList<AnalysisFileBean> analysisFiles, DataAccessor accessor) {
            var result = GetSpots(master, analysisFiles);
            
            foreach (var analysisFile in analysisFiles) {
                var chromatogram = accessor.GetMSScanProperties(analysisFile);
                AlignPeaksToMaster(result, master, chromatogram, analysisFile.AnalysisFileId);
            }
            
            return result;
        }

        public virtual List<IMSScanProperty> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
            if (referenceFile == null) return new List<IMSScanProperty>();

            var master = new List<IMSScanProperty>();
            master = MergeChromatogramPeaks(master, accessor.GetMSScanProperties(referenceFile));
            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId)
                    continue;
                master = MergeChromatogramPeaks(master, accessor.GetMSScanProperties(analysisFile));
            }

            return master;
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
                    spot.AlignmentDriftSpotFeatures = InitSpots(chrom.DriftChromFeatures, analysisFiles, ref masterId, spot.MasterAlignmentID);

                spots.Add(spot);
            }

            return spots;
        }
    }
}
