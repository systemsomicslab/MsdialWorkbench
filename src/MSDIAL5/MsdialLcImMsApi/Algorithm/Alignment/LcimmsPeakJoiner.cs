using CompMs.Common.DataStructure;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcImMsApi.Algorithm.Alignment
{
    public class LcimmsPeakJoiner : PeakJoiner
    {
        private readonly double _rttol, _mztol, _dttol, _rtfactor, _mzfactor, _dtfactor;

        public LcimmsPeakJoiner(double rttol, double rtfactor, double mztol, double mzfactor, double dttol, double dtfactor) {
            _rttol = rttol;
            _rtfactor = rtfactor;
            _mztol = mztol;
            _mzfactor = mzfactor;
            _dttol = dttol;
            _dtfactor = dtfactor;
        }

        public LcimmsPeakJoiner(double rttol, double mztol, double dttol) : this(rttol, 1, mztol, 1, dttol, 1) { }

        public override List<IMSScanProperty> MergeChromatogramPeaks(List<IMSScanProperty> masters, List<IMSScanProperty> targets) {
            var masters_itr = masters.Cast<ChromatogramPeakFeature>();
            var targets_itr = targets.Cast<ChromatogramPeakFeature>();
            foreach (var target in targets_itr) {
                var master_sim = masters_itr.DefaultIfEmpty().Argmax(m => (m is null ? double.NegativeInfinity : GetSimilalityWoDt(m, target)));
                if (master_sim != null && IsSimilarWoDtTo(master_sim, target)) {
                    foreach (var tdrift in target.DriftChromFeatures) {
                        if (master_sim.DriftChromFeatures.All(mdrift => !IsSimilarTo(mdrift, tdrift))) {
                            master_sim.DriftChromFeatures.Add(tdrift);
                        }
                    }
                }
                else {
                    masters.Add(target);
                }
            }
            return masters;
        }

        public override void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
            var tree = KdTree.Build(spots.SelectMany(spot => spot.AlignmentDriftSpotFeatures, (parent, drift) => (parent, drift)), p => p.drift.MassCenter, p => p.drift.TimesCenter.RT.Value, p => p.drift.TimesCenter.Drift.Value);
            var pairs = targets.Cast<ChromatogramPeakFeature>()
                .SelectMany(target => target.DriftChromFeatures, (parent, drift) => (parent, drift))
                .SelectMany(p => tree.RangeSearch(
                    new[] { p.drift.PrecursorMz - _mztol, p.drift.ChromXs.RT.Value - _rttol, p.drift.ChromXs.Drift.Value - _dttol },
                    new[] { p.drift.PrecursorMz + _mztol, p.drift.ChromXs.RT.Value + _rttol, p.drift.ChromXs.Drift.Value + _dttol }
                ), (peak, spot) => (peak, spot))
            .Where(pair => IsSimilarWoDtTo(pair.peak.parent, pair.spot.parent))
            .OrderByDescending(pq => GetSimilality(pq.peak.drift, pq.spot.drift));

            var usedDspots = new HashSet<AlignmentSpotProperty>();
            var usedDpeaks = new HashSet<ChromatogramPeakFeature>();
            foreach (var (peak, spot) in pairs) {
                if (!usedDspots.Contains(spot.drift) && !usedDpeaks.Contains(peak.drift)) {
                    if (spot.drift.AlignedPeakProperties.FirstOrDefault(p => p.FileID == fileId) is AlignmentChromPeakFeature dprop) {
                        DataObjConverter.SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(dprop, peak.drift);
                        if (spot.parent.AlignedPeakProperties.FirstOrDefault(p => p.FileID == fileId) is AlignmentChromPeakFeature prop) {
                            DataObjConverter.SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(prop, peak.parent);
                        }
                        usedDspots.Add(spot.drift);
                        usedDpeaks.Add(peak.drift);
                    }
                }
            }
        }

        private bool IsSimilarWoDtTo(IMSProperty x, IMSProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol
                && Math.Abs(x.ChromXs.RT.Value - y.ChromXs.RT.Value) <= _rttol;
        }

        private bool IsSimilarTo(IMSProperty x, IMSProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol
                && Math.Abs(x.ChromXs.RT.Value - y.ChromXs.RT.Value) <= _rttol
                && Math.Abs(x.ChromXs.Drift.Value - y.ChromXs.Drift.Value) <= _dttol;
        }

        protected override bool IsSimilarTo(IMSScanProperty x, IMSScanProperty y) {
            return IsSimilarTo(x, y);
        }

        private double GetSimilalityWoDt(IMSProperty x, IMSProperty y) {
            return _mzfactor * Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2))
                 + _rtfactor * Math.Exp(-.5 * Math.Pow((x.ChromXs.RT.Value - y.ChromXs.RT.Value) / _rttol, 2));
        }

        protected override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return GetSimilality(x, y);
        }

        private double GetSimilality(IMSProperty x, IMSProperty y) {
            return _mzfactor * Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2))
                 + _rtfactor * Math.Exp(-.5 * Math.Pow((x.ChromXs.RT.Value - y.ChromXs.RT.Value) / _rttol, 2))
                 + _dtfactor * Math.Exp(-.5 * Math.Pow((x.ChromXs.Drift.Value - y.ChromXs.Drift.Value) / _dttol, 2));
        }
    }
}
