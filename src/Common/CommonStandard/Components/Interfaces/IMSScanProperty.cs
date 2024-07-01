using CompMs.Common.Components;
using System.Collections.Generic;

namespace CompMs.Common.Interfaces
{
    public interface IMSScanProperty : IMSProperty
    {
        int ScanID { get; set; }
        List<SpectrumPeak> Spectrum { get; set; }
        void AddPeak(double mass, double intensity, string comment = null);
    }
}
