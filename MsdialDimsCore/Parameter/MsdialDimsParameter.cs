using CompMs.MsdialCore.Parameter;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialDimsCore.Parameter {
    public class MsdialDimsParameter : ParameterBase {
        public MsdialDimsParameter() {
            this.MachineCategory = MachineCategory.IFMS;
        }
    }
}
