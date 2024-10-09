using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

public class LcmsPeakJoiner : IPeakJoiner
{
    private static readonly IComparer<IMSScanProperty> Comparer;

    private readonly double _mztol;
    private readonly double _rttol;
    private readonly double _mzfactor;
    private readonly double _rtfactor;
    private readonly double _mzbucket;
    private readonly double _rtbucket;
    private readonly int _mzwidth;
    private readonly int _rtwidth;
    private IProgress<int>? _progress;

    static LcmsPeakJoiner() {
        Comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);
    }

    public LcmsPeakJoiner(double rttol, double rtfactor, double rtbucket, double mztol, double mzfactor, double mzbucket) {
        _rttol = rttol;
        _rtfactor = rtfactor;
        _rtbucket = rtbucket;
        _rtwidth = (int)Math.Ceiling(_rttol / _rtbucket);
        _mztol = mztol;
        _mzfactor = mzfactor;
        _mzbucket = mzbucket;
        _mzwidth = (int)Math.Ceiling(_mztol / _mzbucket);
    }

    public LcmsPeakJoiner(double rttol, double rtfactor, double mztol, double mzfactor, IProgress<int>? progress = null) : this(rttol, rtfactor, rttol * 2, mztol, mzfactor, mztol * 2) {
        _progress = progress;
    }

    public LcmsPeakJoiner(double rttol, double mztol) : this(rttol, 1, mztol, 1) { }

    private bool IsSimilarTo(IMSScanProperty x, IMSScanProperty y) {
        return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol && Math.Abs(x.ChromXs.RT.Value - y.ChromXs.RT.Value) <= _rttol;
    }

    private double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
        return _mzfactor * Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2))
             + _rtfactor * Math.Exp(-0.5 * Math.Pow((x.ChromXs.RT.Value - y.ChromXs.RT.Value) / _rttol, 2));
    }

    public List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

        // test process from 0 to 20
        var master = GetMasterList(analysisFiles, referenceId, accessor);

        // test process from 20 to 40
        var spots = JoinAll(master, analysisFiles, accessor);
        return spots;
    }

    public List<IMSScanProperty> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

        var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
        if (referenceFile == null) return new List<IMSScanProperty>();

        var master = accessor.GetMSScanProperties(referenceFile)
                             .GroupBy(prop => ((int)Math.Ceiling(prop.ChromXs.RT.Value / _rtbucket), (int)Math.Ceiling(prop.PrecursorMz / _mzbucket)))
                             .ToDictionary(group => group.Key, group => group.ToList());

        var counter = 0;
        ReportProgress reporter = ReportProgress.FromLength(_progress, 0.0, 20.0);
        foreach (var analysisFile in analysisFiles) {
            reporter.Report(++counter, analysisFiles.Count - 1);

            if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId) {
                continue;
            }
            var target = accessor.GetMSScanProperties(analysisFile);
            MergeChromatogramPeaks(master, target);
        }

        return master.Values.SelectMany(props => props).ToList();
    }

    public void MergeChromatogramPeaks(IDictionary<(int, int), List<IMSScanProperty>> master, IEnumerable<IMSScanProperty> targets) {
        foreach (var target in targets) {
            SetToMaster(master, target);
        }
    }

    private bool SetToMaster(IDictionary<(int, int), List<IMSScanProperty>> master, IMSScanProperty target) {
        var mzTarget = (int)Math.Ceiling(target.PrecursorMz / _mzbucket);
        var rtTarget = (int)Math.Ceiling(target.ChromXs.RT.Value / _rtbucket);
        for(int rtIdc = rtTarget - _rtwidth; rtIdc <= rtTarget + _rtwidth; rtIdc++) { // in many case, rtIdc is from rtTarget - 1 to rtTarget + 1
            for(int mzIdc = mzTarget - _mzwidth; mzIdc <= mzTarget + _mzwidth; mzIdc++) { // in many case, mzIdc is from mzTarget - 1 to mzTarget + 1
                if (master.ContainsKey((rtIdc, mzIdc))) {
                    foreach (var candidate in master[(rtIdc, mzIdc)]) {
                        if (IsSimilarTo(candidate, target)) {
                            return false;
                        }
                    }
                }
            }
        }
        if (!master.ContainsKey((rtTarget, mzTarget)))
            master[(rtTarget, mzTarget)] = new List<IMSScanProperty>();
        master[(rtTarget, mzTarget)].Add(target);
        return true;
    }

    public List<AlignmentSpotProperty> JoinAll(List<IMSScanProperty> master, IReadOnlyList<AnalysisFileBean> analysisFiles, DataAccessor accessor) {
        master = master.OrderBy(prop => (prop.PrecursorMz, prop.ChromXs.RT.Value)).ToList();
        var result = GetSpots(master, analysisFiles);
        var counter = 0;
        ReportProgress reporter = ReportProgress.FromLength(_progress, 20.0, 20.0);
        foreach (var analysisFile in analysisFiles) {
            reporter.Report(++counter, analysisFiles.Count - 1);
            var chromatogram = accessor.GetMSScanProperties(analysisFile);
            AlignPeaksToMaster(result, master, chromatogram, analysisFile.AnalysisFileId);
        }
        
        return result;
    }

    public void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
        var n = masters.Count;
        var maxMatchs = new double[n];
        var dummy = new ChromatogramPeakFeature(); // dummy instance for binary search.

        foreach (var target in targets) {
            int? matchIdx = null;
            double matchFactor = double.MinValue;
            dummy.ChromXs = new ChromXs(target.ChromXs.RT.Value - _rttol, ChromXType.RT);
            dummy.PrecursorMz = target.PrecursorMz - _mztol;
            var lo = SearchCollection.LowerBound(masters, dummy, Comparer);
            dummy.ChromXs.RT = new RetentionTime(target.ChromXs.RT.Value + _rttol, dummy.ChromXs.RT.Unit);
            dummy.PrecursorMz = target.PrecursorMz + _mztol;
            for (var i = lo; i < n; i++) {
                if (Comparer.Compare(masters[i], dummy) > 0)
                    break;
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
                TimesCenter = scanProp.ChromXs, // temporary, after alignment, set true center.
                MassCenter = scanProp.PrecursorMz, // temporary, after alignment, set true center.
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
}
