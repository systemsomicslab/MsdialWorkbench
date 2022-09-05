using CompMs.Common.Components;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiResultModel : BindableBase
    {
        public RoiModel Roi { get; }
        public MzValue Mz { get; }
        public DriftTime Mobility { get; }
        public double Intensity { get; }
    }
}
