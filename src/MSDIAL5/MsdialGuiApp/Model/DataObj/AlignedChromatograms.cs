using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class AlignedChromatograms
    {
        public AlignedChromatograms(AlignmentSpotPropertyModel spot, IObservable<List<PeakChromatogram>> chromatograms) {
            Spot = spot;
            Chromatograms = chromatograms;
        }

        public AlignmentSpotPropertyModel Spot { get; }
        public IObservable<List<PeakChromatogram>> Chromatograms { get; }
    }
}