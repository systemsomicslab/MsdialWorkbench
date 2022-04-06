using CompMs.Common.Components;

namespace CompMs.Common.Interfaces
{
    public interface IChromatogramPeak : ISpectrumPeak
    {
        int ID { get; set; }
        ChromXs ChromXs { get; set; }
    }
}
