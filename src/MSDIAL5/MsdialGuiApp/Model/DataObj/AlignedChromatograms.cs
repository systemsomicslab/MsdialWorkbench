using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.DataObj;

internal sealed class AlignedChromatograms
{
    public AlignedChromatograms(AlignmentSpotPropertyModel spot, IObservable<List<PeakChromatogram>> chromatograms)
        : this(spot, chromatograms, Observable.Return(AlignedSpotRtAxes.NotCorrected)) {
    }

    /// <param name="rtAxes">
    /// How each aligned peak's RTs move between its file's measured RT axis, the axis of the
    /// chromatograms, and the aligned RT axis of the peak's ChromXs and the spot's TimesCenter.
    /// </param>
    public AlignedChromatograms(AlignmentSpotPropertyModel spot, IObservable<List<PeakChromatogram>> chromatograms, IObservable<AlignedSpotRtAxes> rtAxes) {
        Spot = spot;
        Chromatograms = chromatograms;
        RtAxes = rtAxes ?? Observable.Return(AlignedSpotRtAxes.NotCorrected);
    }

    public AlignmentSpotPropertyModel Spot { get; }
    public IObservable<List<PeakChromatogram>> Chromatograms { get; }
    public IObservable<AlignedSpotRtAxes> RtAxes { get; }
}
