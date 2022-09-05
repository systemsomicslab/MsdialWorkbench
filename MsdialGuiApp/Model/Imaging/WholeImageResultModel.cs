using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class WholeImageResultModel : BindableBase
    {
        public AnalysisFileBean File { get; }
        public ChromatogramPeakFeatureCollection Peaks { get; }
    }
}
