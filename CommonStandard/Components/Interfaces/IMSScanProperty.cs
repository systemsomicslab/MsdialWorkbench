using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;
using CompMs.Common.Enum;

namespace CompMs.Common.Interfaces
{
    public interface IMSScanProperty
    {
        int ID { get; set; }
        double PrecursorMz { get; set; }
        IonMode IonMode { get; set; }
        Times Times { get; set; }
        List<SpectrumPeak> Spectrum { get; set; }
        void AddPeak(double mass, double intensity, string comment = null);
    }
}
