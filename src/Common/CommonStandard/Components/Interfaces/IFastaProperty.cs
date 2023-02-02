using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Interfaces {
    public interface IFastaProperty {
        string Header { get; set; }
        string Sequence { get; set; }
    }
}
