using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;

namespace CompMs.Common.Interfaces
{
    public interface IMoleculeProperty
    {
        int ID { get; set; }
        double PrecursorMz { get; set; }
        Times Times { get; set; }
    }
}
