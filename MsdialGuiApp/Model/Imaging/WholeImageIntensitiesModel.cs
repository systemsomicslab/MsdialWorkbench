using CompMs.Common.Components;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class WholeImageIntensitiesModel : BindableBase
    {
        public double[,] Intensities { get; }
        public MzValue Mz { get; }
        public DriftTime DriftTime { get; }
    }
}
