using CompMs.Common.Components;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialLcmsApi.Parameter {
    public class MsdialLcmsParameter : ParameterBase {
        public MsdialLcmsParameter() { this.MachineCategory = Common.Enum.MachineCategory.LCMS; }

        public override List<string> ParametersAsText() {
            var pStrings = base.ParametersAsText();
            return pStrings;
        }
    }
}
