using CompMs.Common.Components;
using CompMs.CommonMVVM;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj;

public sealed class DisplayPeakOfChromatogram(PeakOfChromatogram peak) : BindableBase
{
    public PeakItem[] Points { get; } = peak.SlicePeakArea().Select(p => new PeakItem(p)).ToArray();

    public double Time { get; } = peak.GetTop().ChromXs.Value;

    public double Area { get; } = peak.CalculateArea();
}
