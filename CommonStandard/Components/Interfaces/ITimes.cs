using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;

namespace CompMs.Common.Interfaces
{
    public interface ITimes
    {
        Time Absolute { get; set; }
        Time Relative { get; set; }
        bool IsAbsolute { get; set; }
    }
}
