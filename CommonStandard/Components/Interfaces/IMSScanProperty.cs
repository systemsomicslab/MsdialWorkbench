using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;

namespace CompMs.Common.Interfaces
{
    public interface IMSScanProperty : IMoleculeProperty
    {
        List<SpectrumPeak> Spectrum { get; set; }
    }
}
