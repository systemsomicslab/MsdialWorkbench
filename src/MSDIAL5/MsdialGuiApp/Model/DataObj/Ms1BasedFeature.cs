using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class Ms1BasedFeature : BindableBase
    {
        public MoleculeModel Molecule { get; }
        public ScanModel Scan { get; }
        public MsScanMatchResultContainer MatchResults { get; }
    }
}
