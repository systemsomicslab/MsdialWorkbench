using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;

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
                var master_sim = masters_itr.Where(m => IsSimilarTo(m, target)).ToArray();
                if (master_sim.Length == 0) {
                    masters.Add(target);
                }
                foreach (var master in master_sim) {
                    foreach (var tdrift in target.DriftChromFeatures) {
                        if (!master.DriftChromFeatures.Any(mdrift => IsSimilarTo(mdrift, tdrift))) {
                            master.DriftChromFeatures.Add(tdrift);
                        }
                    }
                }
            }
            return masters;
        }

        public override void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
            var chromatogram = targets.Cast<ChromatogramPeakFeature>();
            var maxMatchs = new Dictionary<int, double>();

            foreach (var peak in chromatogram) {
                foreach (var drift in peak.DriftChromFeatures) {
                    int? matchId = null;
                    double matchFactor = double.MinValue;
                    foreach (var spot in spots) {
                        foreach (var sdrift in spot.AlignmentDriftSpotFeatures) {
                            var factor = GetSimilality(peak, drift, spot, sdrift);
                            if (factor > maxMatchs[sdrift.MasterAlignmentID] && factor > matchFactor) {
                                matchId = sdrift.MasterAlignmentID;
                                matchFactor = factor;
                            }
                        }
                    }
                    if (matchId.HasValue) {
                        var driftspot = spots.SelectMany(v => v.AlignmentDriftSpotFeatures).FirstOrDefault(v => v.MasterAlignmentID == matchId);
                        var dpeak = driftspot.AlignedPeakProperties.FirstOrDefault(p => p.FileID == fileId);
                        DataObjConverter.SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(dpeak, drift);

                        var spot = spots.FirstOrDefault(v => v.MasterAlignmentID == driftspot.ParentAlignmentID);
                        var apeak = spot.AlignedPeakProperties.FirstOrDefault(p => p.FileID == fileId);
                        DataObjConverter.SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(apeak, peak);
                    }
                }
            }
        }

        protected override bool IsSimilarTo(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol
                && Math.Abs(x.ChromXs.RT.Value - y.ChromXs.RT.Value) <= _rttol
                && Math.Abs(x.ChromXs.Drift.Value - y.ChromXs.Drift.Value) <= _dttol;
        }

        protected override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return _mzfactor * Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2))
                 + _rtfactor * Math.Exp(-.5 * Math.Pow((x.ChromXs.RT.Value - y.ChromXs.RT.Value) / _rttol, 2))
                 + _dtfactor * Math.Exp(-.5 * Math.Pow((x.ChromXs.Drift.Value - y.ChromXs.Drift.Value) / _dttol, 2));
        }

        private double GetSimilality(ChromatogramPeakFeature peak, ChromatogramPeakFeature drift, AlignmentSpotProperty spot, AlignmentSpotProperty sdrift) {
            return _mzfactor * Math.Exp(-.5 * Math.Pow((peak.PrecursorMz - spot.MassCenter) / _mztol, 2))
                 + _rtfactor * Math.Exp(-.5 * Math.Pow((peak.ChromXs.RT.Value - spot.TimesCenter.RT.Value) / _rttol, 2))
                 + _dtfactor * Math.Exp(-.5 * Math.Pow((drift.ChromXs.Drift.Value - sdrift.TimesCenter.Drift.Value) / _dttol, 2));
        }
    }
}
