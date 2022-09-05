using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiModel : BindableBase {
        public AnalysisFileBean File { get; }
        // public (int, int)[] Pixels { get; }
    }
}
