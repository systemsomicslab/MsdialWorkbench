using CompMs.Common.Components;
using CompMs.Common.Enum;

namespace CompMs.Common.Interfaces
{
    public interface IMSProperty
    {
        ChromXs ChromXs { get; set; }
        IonMode IonMode { get; set; }
        double PrecursorMz { get; set; }
    }
}