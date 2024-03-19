using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj;

public sealed class DisplayPeakOfChromatogram(PeakOfChromatogram peak) : BindableBase
{
    public PeakItem[] Points { get; } = peak.SlicePeakArea().Select(p => new PeakItem(p)).ToArray();

    public IChromatogramPeak PeakTop { get; } = peak.GetTop();
    public IChromatogramPeak PeakLeft { get; } = peak.GetLeft();
    public IChromatogramPeak PeakRight { get; } = peak.GetRight();

    public double Time { get; } = peak.GetTop().ChromXs.Value;

    public double Intensity { get; } = peak.GetTop().Intensity;

    public double Area { get; } = peak.CalculateArea();

    public string Label { get; } = $"{peak.GetTop().ChromXs.GetRepresentativeXAxis()}\nArea: {peak.CalculateArea():F0}\nAbundance: {peak.GetTop().Intensity:F0}";
}
