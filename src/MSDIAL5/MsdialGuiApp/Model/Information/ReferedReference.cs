using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;

namespace CompMs.App.Msdial.Model.Information;

public sealed class ReferedReference(IMSProperty? peak, MsScanMatchResult matchResult, IMoleculeMsProperty? reference)
{
    public IMSProperty? Peak { get; } = peak;
    public MsScanMatchResult MatchResult { get; } = matchResult;
    public IMoleculeMsProperty? Reference { get; } = reference;

    public double MzDiff => (Peak, Reference) is (IMSProperty, IMoleculeMsProperty) ? (Peak.PrecursorMz - Reference.PrecursorMz) * 1000d : -1d;
}
