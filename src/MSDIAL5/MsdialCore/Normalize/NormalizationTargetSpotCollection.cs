using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Mathematics.Basic;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.Normalize
{
    internal sealed class NormalizationTargetSpotCollection
    {
        private readonly IReadOnlyList<INormalizationTarget> _targets;
        private readonly List<INormalizationTarget> _orderedTargets;

        public NormalizationTargetSpotCollection(IReadOnlyList<INormalizationTarget> targets) {
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));
            _orderedTargets = _targets
                .SelectMany(t => t.Children is null ? IEnumerableExtension.Return(t) : t.Children.Prepend(t))
                .OrderBy(t => t.Id)
                .ToList();
            TargetSpots = _targets.Select(t => new NormalizationTargetSpot(t)).ToList().AsReadOnly();
        }

        public IReadOnlyList<INormalizationTarget> Spots => _targets;

        public ReadOnlyCollection<NormalizationTargetSpot> TargetSpots { get; }

        public void Initialize(bool initializeIntenralStandardId) {
            foreach (var spot in _targets) {
                foreach (var prop in spot.Values) {
                    prop.NormalizedPeakHeight = -1d;
                    prop.NormalizedPeakAreaAboveBaseline = -1d;
                    prop.NormalizedPeakAreaAboveZero = -1d;
                }
            }
            if (initializeIntenralStandardId) {
                foreach (var spot in _targets) {
                    spot.InternalStandardId = -1;
                }
            }
        }

        public void NormalizeInternalStandardSpot(StandardCompound compound, IonAbundanceUnit unit) {
            var index = _orderedTargets.LowerBound(compound.PeakID, (spot, id) => spot.Id.CompareTo(id));
            if (index >= _orderedTargets.Count || _orderedTargets[index].Id != compound.PeakID) {
                return;
            }
            var targetSpot = _orderedTargets[index];
            targetSpot.IonAbundanceUnit = unit;
            targetSpot.InternalStandardId = compound.PeakID;
            foreach (var prop in targetSpot.Values) {
                prop.NormalizedPeakHeight = compound.Concentration;
                prop.NormalizedPeakAreaAboveBaseline = compound.Concentration;
                prop.NormalizedPeakAreaAboveZero = compound.Concentration;
            }
        }

        public INormalizationTarget FindSpot(int spotId) {
            if (spotId < 0) {
                return null;
            }
            if (spotId >= 0 && spotId < _targets.Count && _targets[spotId].Id == spotId) {
                return _targets[spotId];
            }
            var index = _orderedTargets.BinarySearch(spotId, (spot, id) => spot.Id.CompareTo(id));
            if (index >= 0 && index < _orderedTargets.Count) {
                return _orderedTargets[index];
            }
            return null;
        }

        public double LowessSpanTune(IReadOnlyList<AnalysisFileBean> files) {

            var fileCollection = new AnalysisFileCollection(files);
            var minSpan = fileCollection.MinimumLowessSpan();
            var optSpanList = new List<double>();
            foreach (var eachBatch in files.GroupBy(item => item.AnalysisBatch)) {
                var analysisFileBeanCollectionPerBatch = eachBatch
                    .Where(bean => bean.AnalysisFileType == AnalysisFileType.QC && bean.AnalysisFileIncluded)
                    .OrderBy(bean => bean.AnalysisFileAnalyticalOrder)
                    .ToArray();
                var xQcArray = analysisFileBeanCollectionPerBatch.Select(bean => (double)bean.AnalysisFileAnalyticalOrder).ToArray();
                foreach (var spot in _targets) {
                    var yQcArray = analysisFileBeanCollectionPerBatch.Select(bean => spot.Values[bean.AnalysisFileId].PeakHeight).ToArray();

                    var recoSpan = SmootherMathematics.GetOptimalLowessSpanByCrossValidation(xQcArray, yQcArray, minSpan, 0.05, 3, 7);
                    optSpanList.Add(recoSpan);
                }
            }
            var optSpan = BasicMathematics.Mean(optSpanList);
            return Math.Round(optSpan, 2);
        }

        public void LowessNormalize(
            IReadOnlyList<AnalysisFileBean> files,
            double lowessSpan) {

            var batchDict = files.GroupBy(item => item.AnalysisBatch).ToDictionary(grp => grp.Key, grp => grp.ToList());
            var medQcHeights = new List<double>();
            var medQcAreaZeros = new List<double>();
            var medQcAreaBases = new List<double>();
            foreach (var spot in _targets) {
                var qcHeights = new List<double>();
                var qcAreaZeros = new List<double>();
                var qcAreaBases = new List<double>();
                var targetProps = spot.Values;
                foreach (var prop in targetProps) {
                    var fileProp = files[prop.FileID];
                    if (fileProp.AnalysisFileType == AnalysisFileType.QC && fileProp.AnalysisFileIncluded) {
                        qcHeights.Add(prop.PeakHeight);
                        qcAreaZeros.Add(prop.PeakAreaAboveZero);
                        qcAreaBases.Add(prop.PeakAreaAboveBaseline);
                    }
                }
                medQcHeights.Add(BasicMathematics.Median(qcHeights.ToArray()));
                medQcAreaZeros.Add(BasicMathematics.Median(qcAreaZeros.ToArray()));
                medQcAreaBases.Add(BasicMathematics.Median(qcAreaBases.ToArray()));
            }

            foreach (var eachBatch in batchDict) {
                var analysisFileBeanCollectionPerBatch = eachBatch.Value;
                var index = 0;
                foreach (var spot in TargetSpots) {
                    spot.Target.IonAbundanceUnit = IonAbundanceUnit.NormalizedByQcPeakHeight;
                    spot.LowessNormalize(analysisFileBeanCollectionPerBatch, medQcHeights[index], lowessSpan, "height");
                    spot.LowessNormalize(analysisFileBeanCollectionPerBatch, medQcAreaZeros[index], lowessSpan, "areazero");
                    spot.LowessNormalize(analysisFileBeanCollectionPerBatch, medQcAreaBases[index], lowessSpan, "areabase");
                    index++;
                }
            }
        }
    }
}
