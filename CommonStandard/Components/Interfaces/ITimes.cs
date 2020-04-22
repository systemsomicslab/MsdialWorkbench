using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;

namespace CompMs.Common.Interfaces
{
    public interface ITimes
    {
        Time RT { get; set; }
        Time RI { get; set; }
        Time Drift { get; set; }
        TimeType MainType { get; set; }
    }
}
