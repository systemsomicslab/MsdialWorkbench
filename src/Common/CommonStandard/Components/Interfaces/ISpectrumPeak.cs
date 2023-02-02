using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Interfaces
{
    public interface ISpectrumPeak
    {
        double Mass { get; set; }
        double Intensity { get; set; }
    }
}
