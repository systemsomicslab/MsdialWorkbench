using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;

namespace CompMs.App.Msdial.Model.Information;

public sealed class ReferedReference(MsScanMatchResult matchResult, IMoleculeProperty? reference)
{
    public MsScanMatchResult MatchResult { get; } = matchResult;
    public IMoleculeProperty? Reference { get; } = reference;
}
