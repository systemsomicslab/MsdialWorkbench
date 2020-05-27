using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;

namespace CompMs.Common.Interfaces
{
    public interface IChromXs
    {
        ChromX RT { get; set; }
        ChromX RI { get; set; }
        ChromX Drift { get; set; }
        ChromX Mz { get; set; }
        ChromXType MainType { get; set; }
    }
}
