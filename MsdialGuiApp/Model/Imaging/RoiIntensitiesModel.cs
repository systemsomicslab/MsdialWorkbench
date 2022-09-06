using CompMs.Common.Components;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiIntensitiesModel : BindableBase
    {
        public RoiIntensitiesModel Roi { get; }
        public MzValue Mz { get; }
        public DriftTime Mobility { get; }
        public double[] Intensities { get; }
    }
}
